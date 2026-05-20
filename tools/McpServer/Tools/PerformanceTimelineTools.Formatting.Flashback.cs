namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static string FormatCleanupCell(bool fatalCleanup, bool flashbackCleanup, bool forceRotateRequested, bool forceRotateDraining)
        => fatalCleanup ? "F" : flashbackCleanup ? "B" : forceRotateDraining ? "D" : forceRotateRequested ? "R" : "-";

    private static string FormatFlashbackStageCell(TimelineRow row)
        => $"{row.FlashbackPlaybackSegmentSwitches}/{row.FlashbackPlaybackFmp4Reopens}/{row.FlashbackPlaybackWriteHeadWaits}/{row.FlashbackPlaybackNearLiveSnaps}/{row.FlashbackPlaybackLastWriteHeadWaitGapMs}";

    private static string FormatExportFailureKind(string failureKind)
        => CompactCell(string.IsNullOrWhiteSpace(failureKind) ? "-" : failureKind, 6);

    private static string FormatExportOutPoint(long outPointMs)
        => outPointMs < 0 ? "live" : $"{outPointMs}ms";

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";
        if (bytes >= 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##}GB";
        if (bytes >= 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.##}MB";
        if (bytes >= 1024L) return $"{bytes / 1024.0:0.##}KB";
        return $"{bytes}B";
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
        => double.IsFinite(bytesPerSecond) && bytesPerSecond > 0
            ? $"{FormatBytes((long)bytesPerSecond)}/s"
            : "N/A";
}
