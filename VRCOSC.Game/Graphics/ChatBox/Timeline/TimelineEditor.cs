﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;
using VRCOSC.Game.ChatBox;
using VRCOSC.Game.ChatBox.Clips;
using VRCOSC.Game.Graphics.ChatBox.Timeline.Menu.Clip;
using VRCOSC.Game.Graphics.ChatBox.Timeline.Menu.Layer;
using VRCOSC.Game.Graphics.Themes;
using VRCOSC.Game.Modules;

namespace VRCOSC.Game.Graphics.ChatBox.Timeline;

[Cached]
public partial class TimelineEditor : Container
{
    [Resolved]
    private ChatBoxManager chatBoxManager { get; set; } = null!;

    [Resolved]
    private GameManager gameManager { get; set; } = null!;

    [Resolved]
    private TimelineLayerMenu layerMenu { get; set; } = null!;

    [Resolved]
    private TimelineClipMenu clipMenu { get; set; } = null!;

    private const int grid_line_width = 3;

    private Dictionary<int, TimelineLayer> layers = new();
    private Container gridGenerator = null!;
    private Container positionIndicator = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Masking = true;
        CornerRadius = 10;

        GridContainer layerContainer;

        Children = new Drawable[]
        {
            new Box
            {
                Colour = ThemeManager.Current[ThemeAttribute.Mid],
                RelativeSizeAxes = Axes.Both
            },
            gridGenerator = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(-(grid_line_width / 2f))
            },
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = layerContainer = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(),
                        new Dimension(),
                        new Dimension(),
                        new Dimension(),
                        new Dimension(),
                    },
                    Content = new[]
                    {
                        new Drawable[] { new TimelineLayer(5) },
                        new Drawable[] { new TimelineLayer(4) },
                        new Drawable[] { new TimelineLayer(3) },
                        new Drawable[] { new TimelineLayer(2) },
                        new Drawable[] { new TimelineLayer(1) },
                        new Drawable[] { new TimelineLayer(0) }
                    }
                }
            },
            positionIndicator = new Container
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Y,
                RelativePositionAxes = Axes.X,
                Width = 5,
                CornerRadius = 2,
                EdgeEffect = VRCOSCEdgeEffects.UniformShadow,
                Masking = true,
                Child = new Box
                {
                    Colour = ThemeManager.Current[ThemeAttribute.Accent],
                    RelativeSizeAxes = Axes.Both
                }
            }
        };

        foreach (var array in layerContainer.Content)
        {
            var timelineLayer = (TimelineLayer)array[0];
            layers.Add(timelineLayer.Priority, timelineLayer);
        }

        chatBoxManager.Clips.BindCollectionChanged((_, e) =>
        {
            if (e.OldItems is not null)
            {
                foreach (Clip oldClip in e.OldItems)
                {
                    layers[oldClip.Priority.Value].Remove(oldClip);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (Clip newClip in e.NewItems)
                {
                    layers[newClip.Priority.Value].Add(newClip);

                    newClip.Priority.BindValueChanged(priorityValue =>
                    {
                        layers[priorityValue.OldValue].Remove(newClip);
                        layers[priorityValue.NewValue].Add(newClip);
                    });
                }
            }
        }, true);

        generateGrid();
    }

    protected override void Update()
    {
        if (gameManager.State.Value == GameManagerState.Started)
        {
            positionIndicator.Alpha = 1;
            positionIndicator.X = chatBoxManager.CurrentPercentage;
        }
        else
        {
            positionIndicator.Alpha = 0;
        }
    }

    private void generateGrid()
    {
        for (var i = 0; i < 60; i++)
        {
            gridGenerator.Add(new Box
            {
                Colour = ThemeManager.Current[ThemeAttribute.Dark].Opacity(0.5f),
                RelativeSizeAxes = Axes.Y,
                RelativePositionAxes = Axes.X,
                Width = grid_line_width,
                X = (chatBoxManager.TimelineResolution * i)
            });
        }

        for (var i = 0; i < 6; i++)
        {
            gridGenerator.Add(new Box
            {
                Colour = ThemeManager.Current[ThemeAttribute.Dark].Opacity(0.5f),
                RelativeSizeAxes = Axes.X,
                RelativePositionAxes = Axes.Y,
                Height = grid_line_width,
                Y = (DrawHeight / 6 * i)
            });
        }
    }

    public void ShowLayerMenu(MouseDownEvent e, int xPos, TimelineLayer layer)
    {
        clipMenu.Hide();
        layerMenu.Hide();
        layerMenu.SetPosition(e);
        layerMenu.XPos = xPos;
        layerMenu.Layer = layer;
        layerMenu.Show();
    }

    public void HideClipMenu()
    {
        clipMenu.Hide();
    }

    public void ShowClipMenu(Clip clip, MouseDownEvent e)
    {
        layerMenu.Hide();
        clipMenu.Hide();
        clipMenu.SetClip(clip);
        clipMenu.SetPosition(e);
        clipMenu.Show();
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button == MouseButton.Left)
        {
            chatBoxManager.SelectedClip.Value = null;
            clipMenu.Hide();
            layerMenu.Hide();
        }

        return true;
    }
}