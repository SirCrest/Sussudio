using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Public non-Flashback command facade. These methods name the lifecycle/audio
// mutations while EnqueueAsync remains the single serialized execution path.
public sealed partial class CaptureSessionCoordinator
{
    public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Initialize, ct => _captureService.InitializeAsync(device, settings, ct), cancellationToken);

    public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartVideoPreview, ct => _captureService.StartVideoPreviewAsync(settings, ct), cancellationToken);

    public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopVideoPreview, ct => _captureService.StopVideoPreviewAsync(ct), cancellationToken);

    public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopVideoPreview, ct => _captureService.StopVideoPreviewWithTeardownAsync(ct), cancellationToken);

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartRecording, ct => _captureService.StartRecordingAsync(settings, ct), cancellationToken);

    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopRecording, ct => _captureService.StopRecordingAsync(ct), cancellationToken);

    // Used exclusively by MainViewModel.StopRecordingForEmergencyAsync -> routes through the
    // same coordinator queue but signals CaptureService to use EmergencyStopTimeoutMs (5s)
    // instead of StopTimeoutMs (30s) so the emergency stop fits inside App.xaml.cs's 8s wrapper.
    public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopRecording, ct => _captureService.StopRecordingAsync(emergency: true, ct), cancellationToken);

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StartAudioPreview, ct => _captureService.StartAudioPreviewAsync(ct), cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopAudioPreview, ct => _captureService.StopAudioPreviewAsync(ct), cancellationToken);

    public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.StopAudioPreview, ct => _captureService.StopAudioPreviewWithTeardownAsync(ct), cancellationToken);

    public Task UpdateAudioMonitoringAsync(bool enabled, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateAudioMonitoring,
            async ct =>
            {
                if (enabled)
                {
                    await _captureService.StartAudioPreviewAsync(ct).ConfigureAwait(false);
                    _captureService.SetMonitoringMuted(false);
                }
                else
                {
                    _captureService.SetMonitoringMuted(true);
                    await _captureService.StopAudioPreviewAsync(ct).ConfigureAwait(false);
                }
            },
            cancellationToken);

    internal void SetPreviewVolume(double volume)
    {
        ThrowIfDisposed();
        _captureService.SetPreviewVolume((float)volume);
    }

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateAudioInput,
            ct => _captureService.UpdateAudioInputAsync(audioDeviceId, audioDeviceName, ct),
            cancellationToken);

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? micDeviceId, string? micDeviceName, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateMicrophoneMonitor,
            ct => _captureService.UpdateMicrophoneMonitorAsync(enabled, micDeviceId, micDeviceName, ct),
            cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(CaptureCommandKind.Cleanup, ct => _captureService.CleanupAsync(ct), cancellationToken);
}
