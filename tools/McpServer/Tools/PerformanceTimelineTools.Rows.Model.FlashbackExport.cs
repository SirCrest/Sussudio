namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public bool FlashbackExportActive { get; set; }
        public string FlashbackExportStatus { get; set; } = string.Empty;
        public string FlashbackExportFailureKind { get; set; } = string.Empty;
        public long FlashbackExportElapsedMs { get; set; }
        public long FlashbackExportLastProgressAgeMs { get; set; }
        public long FlashbackExportOutputBytes { get; set; }
        public double FlashbackExportThroughputBytesPerSec { get; set; }
        public int FlashbackExportSegmentsProcessed { get; set; }
        public int FlashbackExportTotalSegments { get; set; }
        public double FlashbackExportPercent { get; set; }
        public long FlashbackExportInPointMs { get; set; }
        public long FlashbackExportOutPointMs { get; set; }
        public string FlashbackExportMessage { get; set; } = string.Empty;
    }
}
