using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
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
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegProjection");
        AssertContains(mjpegProjectionText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(mjpegProjectionText, "TotalDecoded = mjpeg.TotalDecoded,");
        AssertContains(mjpegProjectionText, "CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegFlattenedProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = timing.DecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = timing.PipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = timing.PerDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingFlattenedProjection");

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
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = queue.Enabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = queue.ResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = health.MjpegPreviewJitterInputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = timing.InputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = timing.LatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = adaptive.DeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = adaptive.TargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = events.LastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = events.ScheduleLateCount");

        AssertContains(mjpegProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashProjection");
        AssertContains(mjpegProjectionText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(mjpegProjectionText, "SampleCount = packetHash.SampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = packetHash.Pattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = packetHash.RecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashFlattenedProjection");

        return Task.CompletedTask;
    }

}
