namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(
        FlashbackRecordingStartupCacheProjection startupCache)
        => new()
        {
            TempDriveFreeBytes = startupCache.TempDriveFreeBytes,
            BudgetBytes = startupCache.BudgetBytes,
            Bytes = startupCache.Bytes,
            SessionCount = startupCache.SessionCount,
            DeletedSessionCount = startupCache.DeletedSessionCount,
            FreedBytes = startupCache.FreedBytes,
            OverBudget = startupCache.OverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection
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
