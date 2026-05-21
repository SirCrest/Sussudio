using Microsoft.Win32;

namespace Sussudio.Controllers;

internal sealed partial class MainViewModelRuntimeEventIngressController
{
    public void Attach()
    {
        _context.AttachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);

        _context.AttachCaptureStatusChanged(OnCaptureStatusChanged);
        _context.AttachCaptureErrorOccurred(OnCaptureError);
        _context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);
        _context.AttachFrameCaptured(OnFrameCaptured);
        _context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);
        _context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);
        _context.AttachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);

        // SystemEvents.PowerModeChanged is the managed desktop wake signal used
        // to recover capture after sleep or hibernate resume.
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

        _context.AttachAudioDevicesChanged(_context.OnAudioDevicesChanged);
    }

    public void Detach()
    {
        _context.DetachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);

        SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;

        _context.DetachCaptureStatusChanged(OnCaptureStatusChanged);
        _context.DetachCaptureErrorOccurred(OnCaptureError);
        _context.DetachCapturePreCleanupRequested(OnCapturePreCleanupRequested);
        _context.DetachFrameCaptured(OnFrameCaptured);
        _context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);
        _context.DetachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);
        _context.DetachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);

        _context.DetachAudioDevicesChanged(_context.OnAudioDevicesChanged);
    }
}
