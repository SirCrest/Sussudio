using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            QueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            BackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            BackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs
        };

    private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(
        RecordingIntegrityBackpressureProjection backpressure)
        => new()
        {
            QueueMaxDepth = backpressure.QueueMaxDepth,
            QueueOldestFrameAgeMs = backpressure.QueueOldestFrameAgeMs,
            BackpressureWaitMs = backpressure.BackpressureWaitMs,
            BackpressureEvents = backpressure.BackpressureEvents,
            BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs
        };

    private readonly record struct RecordingIntegrityBackpressureProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private readonly record struct RecordingIntegrityBackpressureFlattenedProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }
}
