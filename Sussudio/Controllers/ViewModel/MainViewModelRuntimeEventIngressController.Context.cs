using System;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Controllers;

internal sealed class MainViewModelRuntimeEventIngressControllerContext
{
    public required Action<EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs>> AttachFormatProbeCompleted { get; init; }
    public required Action<EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs>> DetachFormatProbeCompleted { get; init; }
    public required EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs> OnDeviceFormatProbeCompleted { get; init; }
    public required Action<EventHandler<string>> AttachCaptureStatusChanged { get; init; }
    public required Action<EventHandler<string>> DetachCaptureStatusChanged { get; init; }
    public required Action<EventHandler<Exception>> AttachCaptureErrorOccurred { get; init; }
    public required Action<EventHandler<Exception>> DetachCaptureErrorOccurred { get; init; }
    public required Action<Action> AttachCapturePreCleanupRequested { get; init; }
    public required Action<Action> DetachCapturePreCleanupRequested { get; init; }
    public required Action<EventHandler<ulong>> AttachFrameCaptured { get; init; }
    public required Action<EventHandler<ulong>> DetachFrameCaptured { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> AttachAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> DetachAudioLevelUpdated { get; init; }
    public required EventHandler<AudioLevelEventArgs> OnAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> AttachMicrophoneAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> DetachMicrophoneAudioLevelUpdated { get; init; }
    public required EventHandler<AudioLevelEventArgs> OnMicrophoneAudioLevelUpdated { get; init; }
    public required Action<EventHandler<SourceSignalTelemetrySnapshot>> AttachSourceTelemetryUpdated { get; init; }
    public required Action<EventHandler<SourceSignalTelemetrySnapshot>> DetachSourceTelemetryUpdated { get; init; }
    public required EventHandler<SourceSignalTelemetrySnapshot> OnSourceTelemetryUpdated { get; init; }
    public required Action<Action> AttachAudioDevicesChanged { get; init; }
    public required Action<Action> DetachAudioDevicesChanged { get; init; }
    public required Action OnAudioDevicesChanged { get; init; }
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateLiveCaptureInfo { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateHdrRuntimeStatusFromCapture { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsCaptureInitialized { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsPreviewing { get; init; }
    public required Func<bool> IsVideoPreviewActive { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Action<bool> SetIsRecording { get; init; }
    public required Func<bool> IsCaptureRecording { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Action ResetAudioMeter { get; init; }
    public required Func<Func<Task>[]> GetPreviewRendererStopHandlers { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
    public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
}
