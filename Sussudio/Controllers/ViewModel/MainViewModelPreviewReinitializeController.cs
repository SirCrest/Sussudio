using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the preview reinitialize transaction controller.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeControllerContext
{
    public required Func<CaptureDevice?> SelectedDevice { get; init; }
    public required Func<MediaFormat?> SelectedFormat { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsPreviewReinitializing { get; init; }
    public required Action<bool> SetIsPreviewReinitializing { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<bool> CancelPreviewRestartAfterReinitialize { get; init; }
    public required Action<bool> SetCancelPreviewRestartAfterReinitialize { get; init; }
    public required Func<int> IncrementReinitializeGeneration { get; init; }
    public required Func<int> ReadReinitializeGeneration { get; init; }
    public required int PreviewReinitializeDebounceMs { get; init; }
    public required Func<Task?> PendingFlashbackCycleTask { get; init; }
    public required int FlashbackCycleBeforeReinitializeTimeoutMs { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
    public required Action<Task> ClearPendingFlashbackCycleIfSameAndCompleted { get; init; }
    public required Func<Task> WaitReinitializeGateAsync { get; init; }
    public required Action ReleaseReinitializeGate { get; init; }
    public required Func<string, Task> NotifyPreviewReinitRequestedAsync { get; init; }
    public required Func<Task> NotifyRendererStopAsync { get; init; }
}

/// <summary>
/// Owns the debounced preview reinitialization transaction for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeController
{
    private readonly MainViewModelPreviewReinitializeControllerContext _context;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

    public MainViewModelPreviewReinitializeController(
        MainViewModelPreviewReinitializeControllerContext context,
        MainViewModelPreviewLifecycleController previewLifecycleController)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
    }

    public void CancelPendingPreviewRestart()
    {
        if (_context.IsPreviewReinitializing())
        {
            _context.SetCancelPreviewRestartAfterReinitialize(true);
        }
    }

    public void ResetPendingPreviewRestartCancellation()
    {
        _context.SetCancelPreviewRestartAfterReinitialize(false);
    }

    public async Task ReinitializeDeviceAsync(string reason)
    {
        if (_context.SelectedDevice() == null || _context.SelectedFormat() == null)
        {
            return;
        }

        if (_context.IsRecording())
        {
            Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' — stop recording before changing capture settings.");
            _context.SetStatusText("Stop recording before changing capture settings.");
            return;
        }

        var reinitializeGeneration = _context.IncrementReinitializeGeneration();
        await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);
        if (_context.ReadReinitializeGeneration() != reinitializeGeneration)
        {
            Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
            return;
        }

        var pendingCycle = _context.PendingFlashbackCycleTask();
        if (pendingCycle != null)
        {
            try
            {
                await _context.AwaitWithTimeoutAsync(
                    pendingCycle,
                    _context.FlashbackCycleBeforeReinitializeTimeoutMs,
                    "Flashback encoder settings cycle before reinitialize").ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
                _context.SetStatusText($"Failed to apply format: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
            }

            _context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);
        }

        await _context.WaitReinitializeGateAsync();
        var shouldRestartPreview = _context.IsPreviewing();
        try
        {
            _context.SetStatusText("Applying new settings...");
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (shouldRestartPreview)
            {
                _context.SetIsPreviewReinitializing(true);
                ResetPendingPreviewRestartCancellation();
                await _context.NotifyPreviewReinitRequestedAsync(reason);
                await _context.NotifyRendererStopAsync();
            }

            if (_context.IsPreviewing())
            {
                await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);
            }

            _context.SetIsInitialized(false);
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
            await _previewLifecycleController.InitializeDeviceAsync();
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

            if (_context.IsInitialized() && shouldRestartPreview && !_context.CancelPreviewRestartAfterReinitialize())
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                await _previewLifecycleController.StartPreviewAsync(userInitiated: false);
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");

                var selectedFormat = _context.SelectedFormat()!;
                _context.SetStatusText($"Preview: {selectedFormat.Width}x{selectedFormat.Height}@{selectedFormat.FrameRate}fps");
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetStatusText($"Failed to apply format: {ex.Message}");
        }
        finally
        {
            ResetPendingPreviewRestartCancellation();
            if (shouldRestartPreview)
            {
                _context.SetIsPreviewReinitializing(false);
            }

            _context.ReleaseReinitializeGate();
        }
    }
}
