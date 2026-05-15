using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Capture lifecycle: device initialization, preview start/stop, and selected-device apply.
/// </summary>
public partial class MainViewModel
{
    private async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            StatusText = "Initializing device...";
            var settings = BuildCaptureSettings();
            Logger.Log(
                $"CAPTURE_INIT device='{SelectedDevice.Name}' id='{SelectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

            await _sessionCoordinator.InitializeAsync(SelectedDevice, settings, cancellationToken);

            IsInitialized = true;
            StatusText = "Device ready";
            Logger.Log("CAPTURE_INIT_READY");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Device initialization canceled";
            IsInitialized = false;
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to initialize: {ex.Message}";
            IsInitialized = false;
        }
    }

    public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated)
        {
            _cancelPreviewRestartAfterReinitialize = false;
        }

        PreviewStartRequested?.Invoke(this, EventArgs.Empty);
        Logger.Log($"PREVIEW_START requested initialized={IsInitialized} audio={IsAudioPreviewEnabled && IsAudioEnabled}");

        if (!IsInitialized)
        {
            await InitializeDeviceAsync(cancellationToken);
        }

        if (IsInitialized)
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

            IsPreviewing = true;
            StatusText = "Preview starting...";

            if (IsAudioPreviewEnabled && IsAudioEnabled)
            {
                await _sessionCoordinator.StartAudioPreviewAsync(cancellationToken);
            }

            ApplySourceTelemetrySnapshot(_captureService.GetLatestSourceTelemetrySnapshot(), allowAutoRetarget: true);
            Logger.Log($"PREVIEW_START_READY audio={IsAudioPreviewEnabled && IsAudioEnabled}");
        }
        else
        {
            Logger.Log("Cannot start preview - device not initialized");
            StatusText = "Cannot start preview - device not initialized";
        }
    }

    public Task StopPreviewAsync()
        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated)
        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline)
        => StopPreviewAsync(userInitiated, teardownPipeline, CancellationToken.None);

    public async Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsRecording)
        {
            StatusText = "Stop recording before switching capture devices.";
            return;
        }

        if (SelectedDevice != null &&
            string.Equals(SelectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={IsPreviewing} initialized={IsInitialized}");
        SelectedDevice = device;

        if (IsPreviewing)
        {
            await ReinitializeDeviceAsync("device selection apply").ConfigureAwait(true);
            return;
        }

        IsInitialized = false;
        StatusText = $"Selected device: {device.Name}";
    }

    public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated && IsPreviewReinitializing)
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }

        if (userInitiated && !IsPreviewReinitializing && _captureService.IsAudioPreviewActive)
        {
            await RampPreviewVolumeDownForStopAsync(cancellationToken);
        }

        PreviewStopRequested?.Invoke(this, EventArgs.Empty);
        var commitStoppedState = false;
        try
        {
            if (teardownPipeline)
            {
                await _sessionCoordinator.StopVideoPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _sessionCoordinator.StopVideoPreviewAsync(cancellationToken);
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
                IsPreviewing = false;
            }
        }

        // Stop audio preview
        if (_captureService.IsAudioPreviewActive)
        {
            if (teardownPipeline)
            {
                await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _sessionCoordinator.StopAudioPreviewAsync(cancellationToken);
            }
        }

        if (!IsPreviewReinitializing)
        {
            StatusText = "Preview stopped";
        }
    }

}
