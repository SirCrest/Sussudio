using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var mjpegTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
            .Replace("\r\n", "\n");
        var mjpegPacketHashProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(snapshotProjectionText, "MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,");
        AssertContains(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.LastDropReason,");
        AssertContains(snapshotProjectionText, "MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,");
        AssertContains(snapshotProjectionText, "MjpegPerDecoder = mjpeg.Timing.PerDecoder,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegDecodeSampleCount = mjpeg.DecodeSampleCount,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPacketHashPattern = mjpeg.PacketHashPattern,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPerDecoder = mjpeg.PerDecoder,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");

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
        AssertContains(mjpegTimingProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegTimingProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegTimingProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegTimingProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegTimingProjectionText, "private readonly record struct MjpegTimingProjection");

        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterProjection");

        AssertContains(mjpegPacketHashProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPacketHashProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegPacketHashProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegPacketHashProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegPacketHashProjectionText, "private readonly record struct MjpegPacketHashProjection");

        return Task.CompletedTask;
    }

}
