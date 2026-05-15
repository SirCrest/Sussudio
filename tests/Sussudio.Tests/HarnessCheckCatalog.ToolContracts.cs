using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddToolContractChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Recording pipeline options preserve defaults and capacity bounds",
            RecordingPipelineOptions_DefaultsAndCapacityBounds);
        await AddCheckAsync(results,
            "RecordingPipelineOptions resolves video queue capacity from frame rate",
            RecordingPipelineOptions_ResolvesVideoQueueCapacity);

        // --- NvmlSnapshot computed properties ---
        await AddCheckAsync(results,
            "NvmlSnapshot computed properties convert units correctly",
            NvmlSnapshot_ComputedProperties_ConvertUnits);

        // --- CaptureSessionSnapshot defaults ---
        await AddCheckAsync(results,
            "CaptureSessionSnapshot has correct default state",
            CaptureSessionSnapshot_DefaultState);

        // --- ProcessSpec and ProcessRunResult contracts ---
        await AddCheckAsync(results,
            "ProcessSpec default timeout is 30 seconds",
            ProcessSpec_DefaultTimeout_Is30Seconds);

        // --- Tool CommandMap & Formatter Alignment ---
        await AddCheckAsync(results,
            "Automation pipe protocol resolves commands timeouts auth and envelopes",
            AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes);
        await AddCheckAsync(results,
            "Automation command catalog covers command metadata and policy",
            AutomationCommandCatalog_CoversCommandsAndPolicyMetadata);
        await AddCheckAsync(results,
            "Automation pipe connect failures are classified for CLI and MCP",
            AutomationPipeConnectFailures_AreClassifiedForCliAndMcp);
        await AddCheckAsync(results,
            "Reliability gates run tools and offline regression harness",
            ReliabilityGates_RunToolsAndOfflineHarness);
        await AddCheckAsync(results,
            "Architecture agent map references existing files",
            ArchitectureAgentMap_FileReferencesResolve);
        await AddCheckAsync(results,
            "Architecture agent map test-owner paths use resolving code spans",
            ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve);
        await AddCheckAsync(results,
            "Architecture agent map covers automation consumer checklist",
            ArchitectureAgentMap_CoversAutomationConsumerChecklist);
        await AddCheckAsync(results,
            "Architecture agent map covers UI presentation ownership files",
            ArchitectureAgentMap_CoversUiPresentationOwnershipFiles);
        await AddCheckAsync(results,
            "Architecture agent map covers tool automation partial families with exact paths",
            ArchitectureAgentMap_CoversToolAutomationPartialFamiliesWithExactPaths);
        await AddCheckAsync(results,
            "Automation manifest covers catalog metadata",
            AutomationManifest_CoversCatalogMetadata);
        await AddCheckAsync(results,
            "Automation path-bearing commands have validation coverage",
            AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage);
        await AddCheckAsync(results,
            "Automation manifest serialization is stable",
            AutomationManifest_SerializationIsStable);
        await AddCheckAsync(results,
            "Automation response state parses status and retry contracts",
            AutomationResponseState_ParsesStatusAndRetryContracts);
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
            "Shared automation formatter renders MJPEG timing section when fields exist",
            SharedFormatter_RendersMjpegTimingSection_WhenFieldsExist);
        await AddCheckAsync(results,
            "Shared AutomationPipeProtocol CommandMap covers every AutomationCommandKind enum value",
            SharedProtocol_CommandMap_CoversEveryAutomationCommandKind);
        await AddCheckAsync(results,
            "MCP PipeClient delegates to shared protocol for command resolution",
            PipeClient_UsesSharedProtocol_ForCommandResolution);
        await AddCheckAsync(results,
            "ResponseFormatter.IsSuccess correctly parses success and failure JSON",
            ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson);
        await AddCheckAsync(results,
            "ResponseFormatter.Get handles all JSON value kinds correctly",
            ResponseFormatter_Get_HandlesAllJsonValueKinds);
        await AddCheckAsync(results,
            "ssctl Formatters snapshot fields align with MCP ResponseFormatter",
            SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter);
        await AddCheckAsync(results,
            "AutomationClient delegates to shared protocol for command resolution",
            AutomationClient_UsesSharedProtocol_ForCommandResolution);
        await AddCheckAsync(results,
            "send-automation-command tracks automation contract inputs",
            SendAutomationCommand_HelperTracksAutomationContractsInputs);
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
            "PresentMon parser selects dominant non-artifact swap chain",
            PresentMonParser_SelectsDominantNonArtifactSwapChain);
        await AddCheckAsync(results,
            "PresentMon probe source ownership is split",
            PresentMonProbe_SourceOwnership_IsSplit);
        await AddCheckAsync(results,
            "ssctl Formatters emit core snapshot sections",
            SsctlFormatters_EmitCoreSnapshotSections);
        await AddCheckAsync(results,
            "ssctl Formatters timeline output preserves table and summary",
            SsctlFormatters_TimelineOutputPreservesTableAndSummary);
        await AddCheckAsync(results,
            "ssctl PipeTransport exposes advanced automation command ids",
            SsctlPipeTransport_ExposesAdvancedAutomationCommandIds);
        await AddCheckAsync(results,
            "RTK I2C probe guards unsafe native paths",
            RtkI2cProbe_GuardsUnsafeNativePaths);
        await AddCheckAsync(results,
            "KS audio node probe source ownership is split",
            KsAudioNodeProbe_SourceOwnership_IsSplit);
    }
}
