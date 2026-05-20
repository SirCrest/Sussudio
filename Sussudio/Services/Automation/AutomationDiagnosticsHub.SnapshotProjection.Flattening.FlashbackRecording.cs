namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            EncodingFailed = flashbackRecording.EncodingFailed,
            EncodingFailureType = flashbackRecording.EncodingFailureType,
            EncodingFailureMessage = flashbackRecording.EncodingFailureMessage,
            FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress,
            CleanupInProgress = flashbackRecording.CleanupInProgress,
            ForceRotateActive = flashbackRecording.ForceRotateActive,
            ForceRotateRequested = flashbackRecording.ForceRotateRequested,
            ForceRotateDraining = flashbackRecording.ForceRotateDraining,
            StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),
            Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),
            Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),
            Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),
            Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)
        };

    private readonly record struct FlashbackRecordingFlattenedProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }
        public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }
        public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }
        public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }
    }
}
