using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns the debounced preview reinitialization transaction for the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelPreviewReinitializeController
    {
        private readonly MainViewModel _viewModel;
        private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

        public MainViewModelPreviewReinitializeController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
        }

        public void CancelPendingPreviewRestart()
        {
            if (_viewModel.IsPreviewReinitializing)
            {
                _viewModel._cancelPreviewRestartAfterReinitialize = true;
            }
        }

        public void ResetPendingPreviewRestartCancellation()
        {
            _viewModel._cancelPreviewRestartAfterReinitialize = false;
        }

        public async Task ReinitializeDeviceAsync(string reason)
        {
            if (_viewModel.SelectedDevice == null || _viewModel.SelectedFormat == null)
            {
                return;
            }

            if (_viewModel.IsRecording)
            {
                Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' — stop recording before changing capture settings.");
                _viewModel.StatusText = "Stop recording before changing capture settings.";
                return;
            }

            var reinitializeGeneration = Interlocked.Increment(ref _viewModel._previewReinitializeGeneration);
            await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);
            if (Volatile.Read(ref _viewModel._previewReinitializeGeneration) != reinitializeGeneration)
            {
                Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
                return;
            }

            var pendingCycle = _viewModel._pendingFlashbackCycleTask;
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
                    _viewModel.StatusText = $"Failed to apply format: {ex.Message}";
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
                }

                if (ReferenceEquals(_viewModel._pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)
                {
                    _viewModel._pendingFlashbackCycleTask = null;
                }
            }

            await _viewModel._previewReinitializeGate.WaitAsync();
            var shouldRestartPreview = _viewModel.IsPreviewing;
            try
            {
                _viewModel.StatusText = "Applying new settings...";
                Logger.Log($"=== Reinitializing device ({reason}) ===");

                if (shouldRestartPreview)
                {
                    _viewModel.IsPreviewReinitializing = true;
                    ResetPendingPreviewRestartCancellation();
                    await _viewModel.NotifyPreviewReinitRequestedAsync(reason);
                    await _viewModel.NotifyRendererStopAsync();
                }

                if (_viewModel.IsPreviewing)
                {
                    await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);
                }

                _viewModel.IsInitialized = false;
                Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
                await _previewLifecycleController.InitializeDeviceAsync();
                Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

                if (_viewModel.IsInitialized && shouldRestartPreview && !_viewModel._cancelPreviewRestartAfterReinitialize)
                {
                    Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                    await _previewLifecycleController.StartPreviewAsync(userInitiated: false);
                    Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");

                    _viewModel.StatusText = $"Preview: {_viewModel.SelectedFormat.Width}x{_viewModel.SelectedFormat.Height}@{_viewModel.SelectedFormat.FrameRate}fps";
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _viewModel.StatusText = $"Failed to apply format: {ex.Message}";
            }
            finally
            {
                ResetPendingPreviewRestartCancellation();
                if (shouldRestartPreview)
                {
                    _viewModel.IsPreviewReinitializing = false;
                }

                _viewModel._previewReinitializeGate.Release();
            }
        }
    }
}
