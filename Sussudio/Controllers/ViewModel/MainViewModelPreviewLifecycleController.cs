using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Controllers;

/// <summary>
/// Owns UI-facing preview lifecycle operations behind the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelPreviewLifecycleController
{
    private readonly MainViewModelPreviewLifecycleControllerContext _context;
    private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;

    public MainViewModelPreviewLifecycleController(MainViewModelPreviewLifecycleControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewReinitializeController = _context.CreateReinitializeController(this);
    }

    public void CancelPendingPreviewRestart()
        => _previewReinitializeController.CancelPendingPreviewRestart();

    public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
    {
        var selectedDevice = _context.SelectedDevice();
        if (selectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _context.SetStatusText("Initializing device...");
            var settings = _context.BuildCaptureSettings();
            Logger.Log(
                $"CAPTURE_INIT device='{selectedDevice.Name}' id='{selectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

            await _context.SessionCoordinator.InitializeAsync(selectedDevice, settings, cancellationToken);

            _context.SetIsInitialized(true);
            _context.SetStatusText("Device ready");
            Logger.Log("CAPTURE_INIT_READY");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _context.SetStatusText("Device initialization canceled");
            _context.SetIsInitialized(false);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetStatusText($"Failed to initialize: {ex.Message}");
            _context.SetIsInitialized(false);
        }
    }

    public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated)
        {
            _previewReinitializeController.ResetPendingPreviewRestartCancellation();
        }

        _context.RaisePreviewStartRequested();
        Logger.Log($"PREVIEW_START requested initialized={_context.IsInitialized()} audio={_context.ShouldStartAudioPreview()}");

        if (!_context.IsInitialized())
        {
            await InitializeDeviceAsync(cancellationToken);
        }

        if (_context.IsInitialized())
        {
            var settings = _context.BuildCaptureSettings();
            await _context.SessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

            _context.SetIsPreviewing(true);
            _context.SetStatusText("Preview starting...");

            if (_context.ShouldStartAudioPreview())
            {
                await _context.SessionCoordinator.StartAudioPreviewAsync(cancellationToken);
            }

            _context.ApplyLatestSourceTelemetryForPreviewStart();
            Logger.Log($"PREVIEW_START_READY audio={_context.ShouldStartAudioPreview()}");
        }
        else
        {
            Logger.Log("Cannot start preview - device not initialized");
            _context.SetStatusText("Cannot start preview - device not initialized");
        }
    }

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(async () =>
        {
            if (!enabled && _context.IsPreviewReinitializing())
            {
                CancelPendingPreviewRestart();
                if (!_context.IsPreviewing())
                {
                    return;
                }
            }

            if (enabled == _context.IsPreviewing())
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
        if (_context.IsRecording())
        {
            _context.SetStatusText("Stop recording before switching capture devices.");
            return;
        }

        var selectedDevice = _context.SelectedDevice();
        if (selectedDevice != null &&
            string.Equals(selectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={_context.IsPreviewing()} initialized={_context.IsInitialized()}");
        _context.SetSelectedDevice(device);

        if (_context.IsPreviewing())
        {
            await ReinitializeDeviceAsync("device selection apply").ConfigureAwait(true);
            return;
        }

        _context.SetIsInitialized(false);
        _context.SetStatusText($"Selected device: {device.Name}");
    }

    public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated && _context.IsPreviewReinitializing())
        {
            CancelPendingPreviewRestart();
        }

        if (userInitiated && !_context.IsPreviewReinitializing() && _context.IsAudioPreviewActive())
        {
            await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);
        }

        _context.RaisePreviewStopRequested();
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
                _context.SetIsPreviewing(false);
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

        if (!_context.IsPreviewReinitializing())
        {
            _context.SetStatusText("Preview stopped");
        }
    }

    public Task ReinitializeDeviceAsync(string reason)
        => _previewReinitializeController.ReinitializeDeviceAsync(reason);
}
