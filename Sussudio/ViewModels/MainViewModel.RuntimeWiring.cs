using Microsoft.Win32;

namespace Sussudio.ViewModels;

/// <summary>
/// Runtime event wiring and initial presentation bootstrap for the compatibility facade.
/// </summary>
public partial class MainViewModel
{
    private void AttachRuntimeWiring()
    {
        _deviceService.FormatProbeCompleted += OnDeviceFormatProbeCompleted;

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.PreCleanupRequested += OnCapturePreCleanupRequested;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;
        _captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;

        // SystemEvents.PowerModeChanged is the managed desktop wake signal used
        // to recover capture after sleep or hibernate resume.
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

        _audioDeviceWatcher.DevicesChanged += OnAudioDevicesChanged;
    }

    private void DetachRuntimeWiring()
    {
        _deviceService.FormatProbeCompleted -= OnDeviceFormatProbeCompleted;

        SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;

        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.PreCleanupRequested -= OnCapturePreCleanupRequested;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.AudioLevelUpdated -= OnAudioLevelUpdated;
        _captureService.MicrophoneAudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated -= OnSourceTelemetryUpdated;

        _audioDeviceWatcher.DevicesChanged -= OnAudioDevicesChanged;
    }

    private void InitializeRuntimePresentation()
    {
        _latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);
        UpdateHdrRuntimeStatusFromCapture();
        UpdateLiveCaptureInfo();

        SetupTimer();
        UpdateDiskSpace();
    }
}
