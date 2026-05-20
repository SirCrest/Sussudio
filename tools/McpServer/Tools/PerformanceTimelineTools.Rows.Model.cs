namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public string Timestamp { get; set; } = string.Empty;
        public double CaptureFps { get; set; }
        public double PreviewFps { get; set; }
        public int VidQueue { get; set; }
        public long VidDrops { get; set; }
        public double CaptureAvgMs { get; set; }
        public double CaptureP95Ms { get; set; }
        public double CaptureP99Ms { get; set; }
        public double CaptureMaxMs { get; set; }
        public double CaptureOnePercentLowFps { get; set; }
        public double CaptureFivePercentLowFps { get; set; }
        public double PreviewAvgMs { get; set; }
        public double PreviewP95Ms { get; set; }
        public double PreviewP99Ms { get; set; }
        public double PreviewMaxMs { get; set; }
        public double PreviewOnePercentLowFps { get; set; }
        public double PreviewFivePercentLowFps { get; set; }
        public double PreviewSlowPct { get; set; }
    }
}
