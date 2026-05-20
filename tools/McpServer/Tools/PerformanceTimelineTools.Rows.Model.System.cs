namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public long LatencyMs { get; set; }
        public double WorkingMb { get; set; }
        public double ManagedMb { get; set; }
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
        public double GcPause { get; set; }
        public int Workers { get; set; }
        public int IoThreads { get; set; }
    }
}
