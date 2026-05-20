namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(
        RecordingIntegrityProjection recordingIntegrity)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),
            Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),
            Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),
            Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),
            AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)
        };

    private readonly record struct RecordingIntegrityFlattenedProjection
    {
        public RecordingIntegritySummaryFlattenedProjection Summary { get; init; }
        public RecordingIntegrityVideoFlattenedProjection Video { get; init; }
        public RecordingIntegrityBackpressureFlattenedProjection Backpressure { get; init; }
        public RecordingIntegrityAudioFlattenedProjection Audio { get; init; }
        public RecordingIntegrityAvSyncFlattenedProjection AvSync { get; init; }
    }
}
