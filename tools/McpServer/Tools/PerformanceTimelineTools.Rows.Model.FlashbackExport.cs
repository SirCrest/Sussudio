namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public bool FlashbackExportActive { get; init; }
        public string FlashbackExportStatus { get; init; } = string.Empty;
        public string FlashbackExportFailureKind { get; init; } = string.Empty;
        public long FlashbackExportElapsedMs { get; init; }
        public long FlashbackExportLastProgressAgeMs { get; init; }
        public long FlashbackExportOutputBytes { get; init; }
        public double FlashbackExportThroughputBytesPerSec { get; init; }
        public int FlashbackExportSegmentsProcessed { get; init; }
        public int FlashbackExportTotalSegments { get; init; }
        public double FlashbackExportPercent { get; init; }
        public long FlashbackExportInPointMs { get; init; }
        public long FlashbackExportOutPointMs { get; init; }
        public string FlashbackExportMessage { get; init; } = string.Empty;
    }
}
