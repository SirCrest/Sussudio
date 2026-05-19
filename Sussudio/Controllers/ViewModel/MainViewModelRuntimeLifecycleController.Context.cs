using System;
using Microsoft.UI.Dispatching;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelRuntimeLifecycleControllerContext
    {
        public required Func<MainViewModelRuntimeEventIngressController> CreateEventIngressController { get; init; }
        public required Func<DispatcherQueueTimer> CreateTimer { get; init; }
        public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
        public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetrySnapshot { get; init; }
        public required Action<SourceSignalTelemetrySnapshot> SetLatestSourceTelemetrySnapshot { get; init; }
        public required Action<SourceSignalTelemetrySnapshot, bool> ApplySourceTelemetrySnapshot { get; init; }
        public required Action UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot { get; init; }
        public required Action<CaptureRuntimeSnapshot> UpdateHdrRuntimeStatusFromCaptureWithSnapshot { get; init; }
        public required Action UpdateLiveCaptureInfoWithoutSnapshot { get; init; }
        public required Action<CaptureRuntimeSnapshot> UpdateLiveCaptureInfoWithSnapshot { get; init; }
        public required Action ResetLiveCaptureInfo { get; init; }
        public required Action UpdateDiskSpace { get; init; }
        public required Action RefreshSourceTelemetrySummaryAge { get; init; }
        public required Func<bool> IsRecording { get; init; }
        public required Func<bool> IsPreviewing { get; init; }
        public required Func<bool> IsFlashbackActive { get; init; }
        public required Func<TimeSpan> GetRecordingElapsed { get; init; }
        public required Action<string> SetRecordingTime { get; init; }
        public required Action UpdateRecordingStats { get; init; }
        public required Action UpdateFlashbackBitrate { get; init; }
        public required Action DisposeAudioDeviceWatcher { get; init; }

        public void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot snapshot)
            => UpdateLiveCaptureInfoWithSnapshot(snapshot);

        public void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot snapshot)
            => UpdateHdrRuntimeStatusFromCaptureWithSnapshot(snapshot);

        public void UpdateLiveCaptureInfo()
            => UpdateLiveCaptureInfoWithoutSnapshot();

        public void UpdateHdrRuntimeStatusFromCapture()
            => UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot();
    }
}
