﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VRCOSC.App.Actions.Packages;
using VRCOSC.App.Packages;
using VRCOSC.App.UI;
using VRCOSC.App.Utils;

namespace VRCOSC.App.Pages.Packages;

public partial class PackagePage : INotifyPropertyChanged
{
    public PackagePage()
    {
        InitializeComponent();

        DataContext = this;
        SizeChanged += OnSizeChanged;

        SearchTextBox.TextChanged += (_, _) => filterDataGrid(SearchTextBox.Text);
        filterDataGrid(string.Empty);
    }

    private double packageScrollViewerHeight = double.NaN;

    public double PackageScrollViewerHeight
    {
        get => packageScrollViewerHeight;
        set
        {
            packageScrollViewerHeight = value;
            OnPropertyChanged();
        }
    }

    public Visibility UpdateAllButtonVisibility => PackageManager.GetInstance().Sources.Any(packageSource => packageSource.IsUpdateAvailable()) ? Visibility.Visible : Visibility.Collapsed;

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        _ = MainWindow.GetInstance().ShowLoadingOverlay("Refreshing All Packages", PackageManager.GetInstance().RefreshAllSources(true));
    }

    private void UpdateAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        var updateAllPackagesAction = new UpdateAllPackagesAction();

        foreach (var packageSource in PackageManager.GetInstance().Sources.Where(packageSource => packageSource.IsUpdateAvailable()))
        {
            updateAllPackagesAction.AddAction(PackageManager.GetInstance().InstallPackage(packageSource));
        }

        _ = MainWindow.GetInstance().ShowLoadingOverlay("Updating All Packages", updateAllPackagesAction);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => evaluateContentHeight();

    private int itemsCount;

    private void evaluateContentHeight()
    {
        if (itemsCount == 0)
        {
            PackageScrollViewerHeight = 0;
            return;
        }

        var contentHeight = PackageList.ActualHeight;
        var targetHeight = GridContainer.ActualHeight - 45;
        PackageScrollViewerHeight = contentHeight >= targetHeight ? targetHeight : double.NaN;
    }

    private void filterDataGrid(string filterText)
    {
        var packageManager = PackageManager.GetInstance();

        if (string.IsNullOrEmpty(filterText))
        {
            showDefaultItemsSource();
            return;
        }

        var filteredItems = packageManager.Sources.Where(item => item.DisplayName.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)).ToList();

        PackageList.ItemsSource = filteredItems;
        itemsCount = filteredItems.Count;
        evaluateContentHeight();
    }

    private void showDefaultItemsSource()
    {
        var packageManager = PackageManager.GetInstance();

        PackageList.ItemsSource = packageManager.Sources;
        itemsCount = packageManager.Sources.Count;
        evaluateContentHeight();
    }

    public void Refresh() => Dispatcher.Invoke(() =>
    {
        OnPropertyChanged(nameof(UpdateAllButtonVisibility));
        PackageList.ItemsSource = null;
        filterDataGrid(SearchTextBox.Text);
    });

    private void InfoButton_ButtonClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var packageSource = (PackageSource)button.Tag;

        InfoImageContainer.Visibility = Visibility.Collapsed;

        ImageLoader.RetrieveFromURL(packageSource.CoverURL, (bitmapImage, cached) =>
        {
            InfoImage.ImageSource = bitmapImage;
            InfoImageContainer.FadeInFromZero(cached ? 0 : 150);
        });

        InfoOverlay.DataContext = packageSource;
        InfoOverlay.FadeInFromZero(150);
    }

    private void PackageGithub_ButtonClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var packageSource = (PackageSource)button.Tag;

        Process.Start(new ProcessStartInfo(packageSource.URL) { UseShellExecute = true });
    }

    private void InfoOverlayExit_ButtonClick(object sender, RoutedEventArgs e)
    {
        InfoOverlay.FadeOutFromOne(150);
    }

    private void InstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        var package = (PackageSource)element.Tag;

        var action = PackageManager.GetInstance().InstallPackage(package);
        action.OnComplete += () => MainWindow.GetInstance().PackagePage.Refresh();
        _ = MainWindow.GetInstance().ShowLoadingOverlay($"Installing {package.DisplayName}", action);
    }

    private void UninstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        var package = (PackageSource)element.Tag;

        var result = MessageBox.Show($"Are you sure you want to uninstall {package.DisplayName}?\nUninstalling will remove all saved module data, and all ChatBox data containing modules provided by this package.", "Uninstall Warning", MessageBoxButton.YesNo);

        if (result != MessageBoxResult.Yes) return;

        var action = PackageManager.GetInstance().UninstallPackage(package);
        action.OnComplete += () => MainWindow.GetInstance().PackagePage.Refresh();
        _ = MainWindow.GetInstance().ShowLoadingOverlay($"Uninstalling {package.DisplayName}", action);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
