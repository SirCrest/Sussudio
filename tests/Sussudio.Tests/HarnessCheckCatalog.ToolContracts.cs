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
            "Automation snapshot formatter formats core sections and typed accessors",
            AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors);
        await AddCheckAsync(results,
            "Automation snapshot formatter renders Flashback sections when included",
            AutomationSnapshotFormatter_RendersFlashbackSections_WhenIncluded);
        await AddCheckAsync(results,
            "Automation snapshot formatter renders Preview D3D sections",
            AutomationSnapshotFormatter_RendersPreviewD3DSections);
        await AddCheckAsync(results,
            "Automation snapshot formatter source ownership is split",
            AutomationSnapshotFormatter_SourceOwnership_IsSplit);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route device commands",
            SsctlCommandHandlers_RouteDeviceCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route capture control commands",
            SsctlCommandHandlers_RouteCaptureControlCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route recordings commands",
            SsctlCommandHandlers_RouteRecordingsCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route Flashback commands",
            SsctlCommandHandlers_RouteFlashbackCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route window commands",
            SsctlCommandHandlers_RouteWindowCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route manifest command",
            SsctlCommandHandlers_RouteManifestCommand);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route observability commands",
            SsctlCommandHandlers_RouteObservabilityCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route automation flow commands",
            SsctlCommandHandlers_RouteAutomationFlowCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route UI visibility commands",
            SsctlCommandHandlers_RouteUiVisibilityCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers route verification commands",
            SsctlCommandHandlers_RouteVerificationCommands);
        await AddCheckAsync(results,
            "ssctl CommandHandlers source ownership is split",
            SsctlCommandHandlers_SourceOwnership_IsSplit);
        await AddCheckAsync(results,
            "ssctl help uses catalog CLI help for automation commands",
            SsctlHelp_UsesCatalogCliHelpForAutomationCommands);
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
