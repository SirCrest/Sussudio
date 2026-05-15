using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelAutomationContractChecksAsync(List<CheckResult> results)
    {
        // --- AutomationContracts ---
        await AddCheckAsync(results,
            "AutomationCommandKind preserves numeric values through GetAutomationManifest",
            AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest);
        await AddCheckAsync(results,
            "AutomationWindowAction has expected values",
            AutomationWindowAction_HasExpectedValues);
    }
}
