using System;
using System.Threading;
using System.Threading.Tasks;
using LightSync; // Assuming NanoleafController and related types are in this namespace.
                 // If not, replace LightSync with the correct namespace or remove if they are global.

public class SyncManager : IDisposable
{
    private readonly NanoleafController _nanoleafController;
    private readonly CyncController _cyncController;
    private CancellationTokenSource _cts;
    private bool _isSyncing = false;

    public SyncManager(NanoleafController nanoleafController, CyncController cyncController)
    {
        _nanoleafController = nanoleafController ?? throw new ArgumentNullException(nameof(nanoleafController));
        _cyncController = cyncController ?? throw new ArgumentNullException(nameof(cyncController));
    }

    public async Task StartSyncAsync(CancellationTokenSource cts)
    {
        if (_isSyncing)
        {
            Console.WriteLine("SyncManager: Sync is already active.");
            return;
        }

        _cts = cts;
        _nanoleafController.StateChanged += NanoleafController_StateChanged;
        // It's important that NanoleafController's StartListeningAsync is called by Program.cs 
        // or another appropriate place to actually start receiving events.
        // Here we just ensure we are subscribed.

        Console.WriteLine("SyncManager: Subscribed to Nanoleaf state changes. Waiting for events...");
        _isSyncing = true;

        // Keep the task alive until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("SyncManager: Sync task was canceled.");
        }
        finally
        {
            StopSyncInternal();
        }
    }

    private async void NanoleafController_StateChanged(object sender, NanoleafStateEventArgs e)
    {
        if (e.AverageColor != null)
        {
            Console.WriteLine($"SyncManager: Nanoleaf average color changed: R={e.AverageColor.R}, G={e.AverageColor.G}, B={e.AverageColor.B}");
            await _cyncController.SetColorAsync(e.AverageColor.R, e.AverageColor.G, e.AverageColor.B);
        }
    }

    public void StopSync()
    {
        if (!_isSyncing)
        {
            return;
        }
        Console.WriteLine("SyncManager: StopSync called.");
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        // StopSyncInternal will be called by the catch/finally block in StartSyncAsync
    }

    private void StopSyncInternal()
    {
        _nanoleafController.StateChanged -= NanoleafController_StateChanged;
        _isSyncing = false;
        Console.WriteLine("SyncManager: Unsubscribed from Nanoleaf state changes. Sync stopped.");
    }

    public void Dispose()
    {
        StopSync();
        // No other managed resources to dispose directly in SyncManager in this setup,
        // NanoleafController and CyncController are managed by Program.cs
    }
}