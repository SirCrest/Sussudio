using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var mjpegTimingFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegTiming.cs")
            .Replace("\r\n", "\n");
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var mjpegTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterQueueProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Queue.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Timing.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterAdaptiveProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Adaptive.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterEventsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Events.cs")
            .Replace("\r\n", "\n");
        var mjpegPacketHashProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(snapshotFlatteningText, "var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);");
        AssertContains(snapshotFlatteningText, "MjpegTotalDecoded = mjpegFlattening.TotalDecoded,");
        AssertContains(snapshotFlatteningText, "var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);");
        AssertContains(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpegTimingFlattening.DecodeSampleCount,");
        AssertContains(snapshotFlatteningText, "var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);");
        AssertContains(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpegPreviewJitterFlattening.Events.LastDropReason,");
        AssertContains(snapshotFlatteningText, "var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);");
        AssertContains(snapshotFlatteningText, "MjpegPacketHashPattern = mjpegPacketHashFlattening.Pattern,");
        AssertContains(snapshotFlatteningText, "MjpegPerDecoder = mjpegTimingFlattening.PerDecoder,");
        AssertContains(mjpegTimingFlatteningText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(mjpegTimingFlatteningText, "DecodeSampleCount = timing.DecodeSampleCount,");
        AssertContains(mjpegTimingFlatteningText, "PipelineMaxMs = timing.PipelineMaxMs,");
        AssertContains(mjpegTimingFlatteningText, "PerDecoder = timing.PerDecoder");
        AssertContains(mjpegTimingFlatteningText, "private readonly record struct MjpegTimingFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegTotalDecoded = mjpeg.TotalDecoded,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegCompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.LastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.Timing.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");

        AssertContains(mjpegProjectionText, "private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "var timing = BuildMjpegTimingProjection(health);");
        AssertContains(mjpegProjectionText, "Timing = timing,");
        AssertContains(mjpegProjectionText, "var previewJitter = BuildMjpegPreviewJitterProjection(health);");
        AssertContains(mjpegProjectionText, "var packetHash = BuildMjpegPacketHashProjection(health);");
        AssertContains(mjpegProjectionText, "PreviewJitter = previewJitter,");
        AssertDoesNotContain(mjpegProjectionText, "PreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegProjectionText, "PacketHash = packetHash,");
        AssertDoesNotContain(mjpegProjectionText, "PacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(mjpegProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(mjpegProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegProjection");
        AssertContains(mjpegProjectionText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(mjpegProjectionText, "TotalDecoded = mjpeg.TotalDecoded,");
        AssertContains(mjpegProjectionText, "CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegFlattenedProjection");
        AssertContains(mjpegTimingProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegTimingProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegTimingProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegTimingProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegTimingProjectionText, "private readonly record struct MjpegTimingProjection");

        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventProjection(health)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterProjection");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterFlattenedProjection");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "Enabled = queue.Enabled,");
        AssertContains(mjpegPreviewJitterQueueProjectionText, "ResumeReprimeCount = queue.ResumeReprimeCount");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "InputSampleCount = health.MjpegPreviewJitterInputSampleCount,");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "InputSampleCount = timing.InputSampleCount,");
        AssertContains(mjpegPreviewJitterTimingProjectionText, "LatencyMaxMs = timing.LatencyMaxMs");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "DeadlineDropCount = adaptive.DeadlineDropCount,");
        AssertContains(mjpegPreviewJitterAdaptiveProjectionText, "TargetDecreaseCount = adaptive.TargetDecreaseCount");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "LastDropReason = events.LastDropReason,");
        AssertContains(mjpegPreviewJitterEventsProjectionText, "ScheduleLateCount = events.ScheduleLateCount");

        AssertContains(mjpegPacketHashProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPacketHashProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegPacketHashProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegPacketHashProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegPacketHashProjectionText, "private readonly record struct MjpegPacketHashProjection");
        AssertContains(mjpegPacketHashProjectionText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(mjpegPacketHashProjectionText, "SampleCount = packetHash.SampleCount,");
        AssertContains(mjpegPacketHashProjectionText, "Pattern = packetHash.Pattern,");
        AssertContains(mjpegPacketHashProjectionText, "RecentDuplicateFlags = packetHash.RecentDuplicateFlags");
        AssertContains(mjpegPacketHashProjectionText, "private readonly record struct MjpegPacketHashFlattenedProjection");

        return Task.CompletedTask;
    }

}
