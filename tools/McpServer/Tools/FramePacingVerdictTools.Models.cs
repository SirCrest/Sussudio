namespace McpServer.Tools;

public static partial class FramePacingVerdictTools
{
    private sealed record FramePacingChannel(
        double ObservedFps,
        double FivePercentLowFps,
        double OnePercentLowFps,
        int SampleCount,
        double SampleDurationMs,
        double[] IntervalsMs);

    private sealed record TimelineRow(
        long DxgiRecentMissed,
        long MjpegJitterDropped,
        long PlaybackDroppedFrames);
}
