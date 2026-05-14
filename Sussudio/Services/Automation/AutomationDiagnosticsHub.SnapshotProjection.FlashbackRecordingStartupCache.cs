using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,
            BudgetBytes = health.FlashbackStartupCacheBudgetBytes,
            Bytes = health.FlashbackStartupCacheBytes,
            SessionCount = health.FlashbackStartupCacheSessionCount,
            DeletedSessionCount = health.FlashbackStartupCacheDeletedSessionCount,
            FreedBytes = health.FlashbackStartupCacheFreedBytes,
            OverBudget = health.FlashbackStartupCacheOverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheProjection
    {
        public long TempDriveFreeBytes { get; init; }
        public long BudgetBytes { get; init; }
        public long Bytes { get; init; }
        public int SessionCount { get; init; }
        public int DeletedSessionCount { get; init; }
        public long FreedBytes { get; init; }
        public bool OverBudget { get; init; }
    }
}
