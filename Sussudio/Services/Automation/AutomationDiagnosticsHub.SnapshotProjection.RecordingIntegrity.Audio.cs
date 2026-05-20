using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            AudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            AudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            AudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            AudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            AudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            AudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            AudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps
        };

    private readonly record struct RecordingIntegrityAudioProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }
}
