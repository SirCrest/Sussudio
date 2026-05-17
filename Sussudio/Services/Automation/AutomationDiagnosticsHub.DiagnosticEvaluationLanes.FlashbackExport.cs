using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildFlashbackExportLane(CaptureHealthSnapshot health)
    {
        var exportFailureKind = string.IsNullOrWhiteSpace(health.FlashbackExportFailureKind)
            ? "None"
            : health.FlashbackExportFailureKind;

        return $"export active={health.FlashbackExportActive} status={health.FlashbackExportStatus} kind={exportFailureKind} id={health.FlashbackExportId} lastResultId={health.LastExportId} progress={health.FlashbackExportPercent:0.##}% segments={health.FlashbackExportSegmentsProcessed}/{health.FlashbackExportTotalSegments} elapsedMs={health.FlashbackExportElapsedMs} progressAgeMs={health.FlashbackExportLastProgressAgeMs} bytes={health.FlashbackExportOutputBytes} throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##} lastProgressUtc={health.FlashbackExportLastProgressUtcUnixMs} completedUtc={health.FlashbackExportCompletedUtcUnixMs}";
    }

    private static string BuildFlashbackTempCacheLane(CaptureHealthSnapshot health)
    {
        return $"flashback temp freeBytes={health.FlashbackTempDriveFreeBytes} cacheBytes={health.FlashbackStartupCacheBytes} budgetBytes={health.FlashbackStartupCacheBudgetBytes} sessions={health.FlashbackStartupCacheSessionCount} deleted={health.FlashbackStartupCacheDeletedSessionCount} freedBytes={health.FlashbackStartupCacheFreedBytes} overBudget={health.FlashbackStartupCacheOverBudget}";
    }
}
