using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackStorageAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-temp-cache-pressure",
            snapshot.FlashbackActive &&
            (snapshot.FlashbackStartupCacheOverBudget ||
             (snapshot.FlashbackTempDriveFreeBytes >= 0 && snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes)),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback temp storage is under pressure: freeBytes={snapshot.FlashbackTempDriveFreeBytes} " +
            $"cacheBytes={snapshot.FlashbackStartupCacheBytes} budgetBytes={snapshot.FlashbackStartupCacheBudgetBytes} " +
            $"sessions={snapshot.FlashbackStartupCacheSessionCount} deleted={snapshot.FlashbackStartupCacheDeletedSessionCount} " +
            $"freedBytes={snapshot.FlashbackStartupCacheFreedBytes} overBudget={snapshot.FlashbackStartupCacheOverBudget}.",
            "Flashback temp storage returned to healthy range.",
            throttleMs: 10000);
    }
}
