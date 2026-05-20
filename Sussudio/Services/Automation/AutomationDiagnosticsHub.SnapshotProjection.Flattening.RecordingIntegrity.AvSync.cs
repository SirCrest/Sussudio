namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(
        RecordingIntegrityAvSyncProjection avSync)
        => new()
        {
            AvSyncDriftMs = avSync.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = avSync.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncFlattenedProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }
}
