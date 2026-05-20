using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),
            Video = BuildRecordingIntegrityVideoProjection(captureRuntime),
            Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),
            Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),
            AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)
        };

    private readonly record struct RecordingIntegrityProjection
    {
        public RecordingIntegritySummaryProjection Summary { get; init; }
        public RecordingIntegrityVideoProjection Video { get; init; }
        public RecordingIntegrityBackpressureProjection Backpressure { get; init; }
        public RecordingIntegrityAudioProjection Audio { get; init; }
        public RecordingIntegrityAvSyncProjection AvSync { get; init; }
    }
}
