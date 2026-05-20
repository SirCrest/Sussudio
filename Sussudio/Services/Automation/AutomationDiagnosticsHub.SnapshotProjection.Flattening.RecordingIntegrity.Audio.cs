namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(
        RecordingIntegrityAudioProjection audio)
        => new()
        {
            AudioStatus = audio.AudioStatus,
            AudioEnabled = audio.AudioEnabled,
            AudioCaptureActive = audio.AudioCaptureActive,
            AudioFramesArrived = audio.AudioFramesArrived,
            AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audio.AudioSamplesEncoded,
            AudioDropEvents = audio.AudioDropEvents,
            AudioDiscontinuities = audio.AudioDiscontinuities,
            AudioTimestampErrors = audio.AudioTimestampErrors,
            AudioCallbackGaps = audio.AudioCallbackGaps
        };

    private readonly record struct RecordingIntegrityAudioFlattenedProjection
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
