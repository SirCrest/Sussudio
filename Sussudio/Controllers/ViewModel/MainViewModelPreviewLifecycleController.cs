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
        private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;

        public MainViewModelPreviewLifecycleController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _previewReinitializeController = new MainViewModelPreviewReinitializeController(_viewModel, this);
        }

        public void CancelPendingPreviewRestart()
            => _previewReinitializeController.CancelPendingPreviewRestart();

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
                _previewReinitializeController.ResetPendingPreviewRestartCancellation();
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

        public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            return _viewModel.InvokeOnUiThreadAsync(async () =>
            {
                if (!enabled && _viewModel.IsPreviewReinitializing)
                {
                    CancelPendingPreviewRestart();
                    if (!_viewModel.IsPreviewing)
                    {
                        return;
                    }
                }

                if (enabled == _viewModel.IsPreviewing)
                {
                    return;
                }

                if (enabled)
                {
                    await StartPreviewAsync(userInitiated: true, cancellationToken);
                }
                else
                {
                    await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);
                }
            }, cancellationToken);
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
                CancelPendingPreviewRestart();
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

        public Task ReinitializeDeviceAsync(string reason)
            => _previewReinitializeController.ReinitializeDeviceAsync(reason);
    }
}
