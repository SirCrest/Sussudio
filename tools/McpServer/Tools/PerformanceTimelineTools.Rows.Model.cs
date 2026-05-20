namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public double CaptureFps { get; init; }
        public double PreviewFps { get; init; }
        public int VidQueue { get; init; }
        public long VidDrops { get; init; }
        public double CaptureAvgMs { get; init; }
        public double CaptureP95Ms { get; init; }
        public double CaptureP99Ms { get; init; }
        public double CaptureMaxMs { get; init; }
        public double CaptureOnePercentLowFps { get; init; }
        public double CaptureFivePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewP99Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewFivePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
    }
}
