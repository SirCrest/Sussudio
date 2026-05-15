using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Preview reinitialization gate: debounced settings retarget, renderer handoff,
/// full capture teardown, and preview restart after capture-mode changes.
/// </summary>
public partial class MainViewModel
{
    private async Task ReinitializeDeviceAsync(string reason)
    {
        if (SelectedDevice == null || SelectedFormat == null)
            return;

        // Guard: a reinit tears down the capture pipeline (StopPreviewAsync with
        // teardownPipeline:true) and rebuilds it via InitializeDeviceAsync. Doing
        // this mid-recording silently truncates the in-flight file (the encoder's
        // moov is whatever state the teardown caught) and purges the flashback
        // buffer. Refuse the change and require the operator to stop recording
        // first. See ApplySelectedDeviceAsync for the equivalent device-switch guard.
        if (IsRecording)
        {
            Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' — stop recording before changing capture settings.");
            StatusText = "Stop recording before changing capture settings.";
            return;
        }

        var reinitializeGeneration = Interlocked.Increment(ref _previewReinitializeGeneration);
        await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);
        if (Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration)
        {
            Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
            return;
        }

        // If a flashback encoder cycle (codec/quality/bitrate change) is still
        // in progress, wait for it to release the session transition lock before
        // we attempt the reinit. Without this, the reinit can read stale encoder
        // settings or partially fail because the transition lock is contended.
        var pendingCycle = _pendingFlashbackCycleTask;
        if (pendingCycle != null)
        {
            try
            {
                await AwaitWithTimeoutAsync(
                    pendingCycle,
                    FlashbackCycleBeforeReinitializeTimeoutMs,
                    "Flashback encoder settings cycle before reinitialize").ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}");
                StatusText = $"Failed to apply format: {ex.Message}";
                return;
            }
            catch (Exception ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
                // Cycle errors don't block reinit; the reinitialize path should still
                // converge the live preview to the current UI settings.
            }

            if (ReferenceEquals(_pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)
            {
                _pendingFlashbackCycleTask = null;
            }
        }

        await _previewReinitializeGate.WaitAsync();
        var shouldRestartPreview = IsPreviewing;
        try
        {
            StatusText = "Applying new settings...";
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = true;
                _cancelPreviewRestartAfterReinitialize = false;
                await NotifyPreviewReinitRequestedAsync(reason);

                // Stop the D3D11 renderer BEFORE tearing down the capture pipeline.
                // The renderer shares the D3D11 device with the MF source reader via
                // SharedD3DDeviceManager. If the renderer is still calling
                // VideoProcessorBlt/Present when UnifiedVideoCapture.DisposeAsync
                // releases the source reader and DXGI device manager, the concurrent
                // native calls race and trigger an uncatchable AccessViolationException.
                // The flashback encoder drain (DisposeFlashbackPreviewBackendAsync)
                // widens this window to hundreds of milliseconds.
                await NotifyRendererStopAsync();
            }

            if (IsPreviewing)
            {
                // Reinit applies new device/format/codec settings — the existing flashback
                // backend is keyed to the OLD settings, so force a full teardown.
                await StopPreviewAsync(userInitiated: false, teardownPipeline: true);
            }

            // Reinitialize the device with new settings
            IsInitialized = false;
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
            await InitializeDeviceAsync();
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

            // Restart preview
            if (IsInitialized && shouldRestartPreview && !_cancelPreviewRestartAfterReinitialize)
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                await StartPreviewAsync(userInitiated: false);
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");

                StatusText = $"Preview: {SelectedFormat.Width}x{SelectedFormat.Height}@{SelectedFormat.FrameRate}fps";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to apply format: {ex.Message}";
        }
        finally
        {
            _cancelPreviewRestartAfterReinitialize = false;
            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = false;
            }

            _previewReinitializeGate.Release();
        }
    }
}
