using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddToolContractChecksAsync(List<CheckResult> results)
    {
        // --- NvmlSnapshot computed properties ---
        await AddCheckAsync(results,
            "NvmlSnapshot computed properties convert units correctly",
            NvmlSnapshot_ComputedProperties_ConvertUnits);

        // --- CaptureSessionSnapshot defaults ---
        await AddCheckAsync(results,
            "CaptureSessionSnapshot has correct default state",
            CaptureSessionSnapshot_DefaultState);

        // --- Tool CommandMap & Formatter Alignment ---
        await AddCheckAsync(results,
            "ssctl Formatters emit core snapshot sections",
            SsctlFormatters_EmitCoreSnapshotSections);
        await AddCheckAsync(results,
            "ssctl Formatters snapshot source ownership is split",
            SsctlFormatters_SnapshotSourceOwnership_IsSplit);
        await AddCheckAsync(results,
            "ssctl Formatters timeline output preserves table and summary",
            SsctlFormatters_TimelineOutputPreservesTableAndSummary);
        await AddCheckAsync(results,
            "RTK I2C probe guards unsafe native paths",
            RtkI2cProbe_GuardsUnsafeNativePaths);
    }
}
