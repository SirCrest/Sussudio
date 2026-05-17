using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// View-model teardown and bounded disposal policy.
/// </summary>
public partial class MainViewModel
{
    private const int DefaultDisposeTimeoutMs = 30000;

    private async Task DisposeCoreAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 1)
        {
            return;
        }
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
        _gainFlashDebounceCts?.Cancel();
        _gainXuDebounceCts?.Cancel();
        _deviceAudioModeCts?.Cancel();
        _deviceAudioRefreshCts?.Cancel();
        _timer?.Stop();
        DetachRuntimeWiring();
        _audioDeviceWatcher.Dispose();
        var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

        try
        {
            await AwaitWithTimeoutAsync(_sessionCoordinator.CleanupAsync(), stepTimeoutMs, "Coordinator cleanup")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel cleanup during dispose failed: {ex.Message}");
        }

        try
        {
            await AwaitWithTimeoutAsync(_sessionCoordinator.DisposeAsync().AsTask(), stepTimeoutMs, "Coordinator dispose")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Coordinator dispose failed: {ex.Message}");
        }

        try
        {
            await AwaitWithTimeoutAsync(_captureService.DisposeAsync().AsTask(), stepTimeoutMs, "Capture service dispose")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Capture service async dispose failed: {ex.Message}");
            _captureService.Dispose();
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback — MainWindow.Closed calls
    // await ViewModel.DisposeAsync(). This sync path exists for GC finalizer safety
    // and uses Task.Run to avoid deadlocking if called from a UI context.
    public void Dispose()
    {
        var disposeTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);
        var disposeTask = Task.Run(DisposeCoreAsync);
        var completed = Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).GetAwaiter().GetResult();
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            disposeTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);
        var disposeTask = DisposeCoreAsync();
        var completed = await Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).ConfigureAwait(false);
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel async dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            await disposeTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel async dispose failed: {ex.Message}");
        }
    }
}
