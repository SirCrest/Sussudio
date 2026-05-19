using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns UI-facing preview lifecycle operations behind the MainViewModel compatibility facade.
    /// </summary>
    private sealed class MainViewModelPreviewLifecycleController
    {
        private readonly MainViewModel _viewModel;
        private readonly MainViewModelPreviewLifecycleControllerContext _context;
        private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;

        public MainViewModelPreviewLifecycleController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleControllerContext context)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
                var settings = _context.BuildCaptureSettings();
                Logger.Log(
                    $"CAPTURE_INIT device='{_viewModel.SelectedDevice.Name}' id='{_viewModel.SelectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

                await _context.SessionCoordinator.InitializeAsync(_viewModel.SelectedDevice, settings, cancellationToken);

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
                var settings = _context.BuildCaptureSettings();
                await _context.SessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

                _viewModel.IsPreviewing = true;
                _viewModel.StatusText = "Preview starting...";

                if (_viewModel.IsAudioPreviewEnabled && _viewModel.IsAudioEnabled)
                {
                    await _context.SessionCoordinator.StartAudioPreviewAsync(cancellationToken);
                }

                _context.ApplyLatestSourceTelemetryForPreviewStart();
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
            return _context.InvokeOnUiThreadAsync(async () =>
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

            if (userInitiated && !_viewModel.IsPreviewReinitializing && _context.IsAudioPreviewActive())
            {
                await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);
            }

            _viewModel.PreviewStopRequested?.Invoke(_viewModel, EventArgs.Empty);
            var commitStoppedState = false;
            try
            {
                if (teardownPipeline)
                {
                    await _context.SessionCoordinator.StopVideoPreviewWithTeardownAsync(cancellationToken);
                }
                else
                {
                    await _context.SessionCoordinator.StopVideoPreviewAsync(cancellationToken);
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

            if (_context.IsAudioPreviewActive())
            {
                if (teardownPipeline)
                {
                    await _context.SessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
                }
                else
                {
                    await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);
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

    private sealed class MainViewModelPreviewLifecycleControllerContext
    {
        public required CaptureSessionCoordinator SessionCoordinator { get; init; }
        public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
        public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
        public required Func<CancellationToken, Task> RampPreviewVolumeDownForStopAsync { get; init; }
        public required Func<bool> IsAudioPreviewActive { get; init; }
        public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }
    }
}
