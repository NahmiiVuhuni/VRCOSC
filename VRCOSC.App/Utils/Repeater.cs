﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.Utils;

using System;
using System.Threading;
using System.Threading.Tasks;

public class Repeater
{
    private readonly Action action;
    private CancellationTokenSource? cancellationTokenSource;

    public Repeater(Action action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Start(TimeSpan interval)
    {
        if (cancellationTokenSource != null)
            throw new InvalidOperationException("Repeater is already started.");

        cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationTokenSource.Token);

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
            }
        }, cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (cancellationTokenSource == null)
            return;

        cancellationTokenSource.Cancel();

        var manualResetEvent = new ManualResetEvent(false);
        cancellationTokenSource.Token.Register(() => manualResetEvent.Set());

        await Task.Run(() => manualResetEvent.WaitOne());

        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }
}
