using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns UI-facing preview lifecycle operations behind the MainViewModel compatibility facade.
    /// </summary>
    private sealed class MainViewModelPreviewLifecycleController
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelPreviewLifecycleController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void CancelPendingPreviewRestart()
        {
            if (_viewModel.IsPreviewReinitializing)
            {
                _viewModel._cancelPreviewRestartAfterReinitialize = true;
            }
        }

        public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
        {
            if (_viewModel.SelectedDevice == null)
            {
                Logger.Log("ERROR: SelectedDevice is NULL");
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _viewModel.StatusText = "Initializing device...";
                var settings = _viewModel.BuildCaptureSettings();
                Logger.Log(
                    $"CAPTURE_INIT device='{_viewModel.SelectedDevice.Name}' id='{_viewModel.SelectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

                await _viewModel._sessionCoordinator.InitializeAsync(_viewModel.SelectedDevice, settings, cancellationToken);

                _viewModel.IsInitialized = true;
                _viewModel.StatusText = "Device ready";
                Logger.Log("CAPTURE_INIT_READY");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _viewModel.StatusText = "Device initialization canceled";
                _viewModel.IsInitialized = false;
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _viewModel.StatusText = $"Failed to initialize: {ex.Message}";
                _viewModel.IsInitialized = false;
            }
        }

        public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (userInitiated)
            {
                _viewModel._cancelPreviewRestartAfterReinitialize = false;
            }

            _viewModel.PreviewStartRequested?.Invoke(_viewModel, EventArgs.Empty);
            Logger.Log($"PREVIEW_START requested initialized={_viewModel.IsInitialized} audio={_viewModel.IsAudioPreviewEnabled && _viewModel.IsAudioEnabled}");

            if (!_viewModel.IsInitialized)
            {
                await InitializeDeviceAsync(cancellationToken);
            }

            if (_viewModel.IsInitialized)
            {
                var settings = _viewModel.BuildCaptureSettings();
                await _viewModel._sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

                _viewModel.IsPreviewing = true;
                _viewModel.StatusText = "Preview starting...";

                if (_viewModel.IsAudioPreviewEnabled && _viewModel.IsAudioEnabled)
                {
                    await _viewModel._sessionCoordinator.StartAudioPreviewAsync(cancellationToken);
                }

                _viewModel.ApplySourceTelemetrySnapshot(_viewModel._captureService.GetLatestSourceTelemetrySnapshot(), allowAutoRetarget: true);
                Logger.Log($"PREVIEW_START_READY audio={_viewModel.IsAudioPreviewEnabled && _viewModel.IsAudioEnabled}");
            }
            else
            {
                Logger.Log("Cannot start preview - device not initialized");
                _viewModel.StatusText = "Cannot start preview - device not initialized";
            }
        }

        public async Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_viewModel.IsRecording)
            {
                _viewModel.StatusText = "Stop recording before switching capture devices.";
                return;
            }

            if (_viewModel.SelectedDevice != null &&
                string.Equals(_viewModel.SelectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={_viewModel.IsPreviewing} initialized={_viewModel.IsInitialized}");
            _viewModel.SelectedDevice = device;

            if (_viewModel.IsPreviewing)
            {
                await ReinitializeDeviceAsync("device selection apply").ConfigureAwait(true);
                return;
            }

            _viewModel.IsInitialized = false;
            _viewModel.StatusText = $"Selected device: {device.Name}";
        }

        public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (userInitiated && _viewModel.IsPreviewReinitializing)
            {
                _viewModel._cancelPreviewRestartAfterReinitialize = true;
            }

            if (userInitiated && !_viewModel.IsPreviewReinitializing && _viewModel._captureService.IsAudioPreviewActive)
            {
                await _viewModel.RampPreviewVolumeDownForStopAsync(cancellationToken);
            }

            _viewModel.PreviewStopRequested?.Invoke(_viewModel, EventArgs.Empty);
            var commitStoppedState = false;
            try
            {
                if (teardownPipeline)
                {
                    await _viewModel._sessionCoordinator.StopVideoPreviewWithTeardownAsync(cancellationToken);
                }
                else
                {
                    await _viewModel._sessionCoordinator.StopVideoPreviewAsync(cancellationToken);
                }

                commitStoppedState = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                commitStoppedState = true;
                throw;
            }
            finally
            {
                if (commitStoppedState)
                {
                    _viewModel.IsPreviewing = false;
                }
            }

            if (_viewModel._captureService.IsAudioPreviewActive)
            {
                if (teardownPipeline)
                {
                    await _viewModel._sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
                }
                else
                {
                    await _viewModel._sessionCoordinator.StopAudioPreviewAsync(cancellationToken);
                }
            }

            if (!_viewModel.IsPreviewReinitializing)
            {
                _viewModel.StatusText = "Preview stopped";
            }
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
                    _viewModel._cancelPreviewRestartAfterReinitialize = false;
                    await _viewModel.NotifyPreviewReinitRequestedAsync(reason);
                    await _viewModel.NotifyRendererStopAsync();
                }

                if (_viewModel.IsPreviewing)
                {
                    await StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);
                }

                _viewModel.IsInitialized = false;
                Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
                await InitializeDeviceAsync();
                Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

                if (_viewModel.IsInitialized && shouldRestartPreview && !_viewModel._cancelPreviewRestartAfterReinitialize)
                {
                    Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                    await StartPreviewAsync(userInitiated: false);
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
                _viewModel._cancelPreviewRestartAfterReinitialize = false;
                if (shouldRestartPreview)
                {
                    _viewModel.IsPreviewReinitializing = false;
                }

                _viewModel._previewReinitializeGate.Release();
            }
        }
    }
}
