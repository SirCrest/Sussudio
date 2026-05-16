using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddMcpToolSurfaceChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MCP raw app state keeps capture options separate",
            McpToolSurface_KeepsCaptureOptionsSeparateFromRawState);
        await AddCheckAsync(results,
            "MCP fixed automation routes use command kinds",
            McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds);
        await AddCheckAsync(results,
            "MCP host tool schema uses PipeClient as a service",
            McpHostToolSchema_UsesPipeClientAsService);
        await AddCheckAsync(results,
            "MCP PipeClient honors Sussudio pipe environment",
            McpPipeClient_HonorsSussudioAutomationPipeEnvironment);
        await AddCheckAsync(results,
            "MCP host tool invocation returns pipe failures",
            McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport);
        await AddCheckAsync(results,
            "MCP capture settings tool routes provided settings",
            McpCaptureSettingsTools_RouteProvidedSettings);
        await AddCheckAsync(results,
            "MCP recording tool routes recording toggle",
            McpRecordingTools_RouteRecordingToggle);
        await AddCheckAsync(results,
            "MCP flashback tool routes enable toggle",
            McpFlashbackTools_RouteEnableToggle);
        await AddCheckAsync(results,
            "MCP tool command formatter batches pending commands",
            McpToolCommandFormatter_BatchesPendingCommands);
        await AddCheckAsync(results,
            "MCP device tool routes refresh selections and custom audio",
            McpDeviceTools_RouteRefreshSelectionsAndCustomAudio);
        await AddCheckAsync(results,
            "MCP pipeline settings tool routes pipeline and audio commands",
            McpPipelineSettingsTools_RoutePipelineAndAudioCommands);
        await AddCheckAsync(results,
            "MCP UI settings tools route UI commands",
            McpUiSettingsTools_RouteUiCommands);
        await AddCheckAsync(results,
            "MCP verification tools format verification responses",
            McpVerificationTools_FormatVerificationResponses);
        await AddCheckAsync(results,
            "MCP diagnostic session tool records snapshot artifacts",
            McpDiagnosticSessionTool_RecordsSnapshotArtifacts);
        await AddCheckAsync(results,
            "MCP diagnostic session tool surfaces diagnostic failures",
            McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError);
    }

    private static async Task AddMcpPerformanceAndProbeToolChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MCP PresentMon tool routes snapshot correlation",
            McpPresentMonTools_RouteSnapshotCorrelation);
        await AddCheckAsync(results,
            "MCP performance timeline exposes D3D P99 stage timing",
            McpPerformanceTimelineTool_ExposesD3DP99StageTiming);
        await AddCheckAsync(results,
            "MCP performance timeline renders flashback command counters",
            McpPerformanceTimelineTool_RendersFlashbackCommandCounters);
        await AddCheckAsync(results,
            "MCP frame pacing verdict flags half-rate preview and playback",
            McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback);
        await AddCheckAsync(results,
            "MCP frame pacing verdict flags insufficient sample duration",
            McpFramePacingVerdictTool_FlagsInsufficientSampleDuration);
        await AddCheckAsync(results,
            "MCP frame pacing verdict ownership is split",
            McpFramePacingVerdictTool_SourceOwnershipIsSplit);
        await AddCheckAsync(results,
            "MCP wait tool uses catalog response timeout",
            McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits);
        await AddCheckAsync(results,
            "MCP wait tool routes condition waits",
            McpWaitTools_RouteConditionWaits);
        await AddCheckAsync(results,
            "MCP window screenshot tool formats screenshot responses",
            McpWindowScreenshotTool_FormatsScreenshotResponses);
        await AddCheckAsync(results,
            "MCP preview frame capture tool formats frame reports",
            McpPreviewFrameCaptureTool_FormatsCaptureResponses);
        await AddCheckAsync(results,
            "MCP window tool routes window actions",
            McpWindowTools_RouteWindowActions);
        await AddCheckAsync(results,
            "MCP preview color probe tool formats probe responses",
            McpPreviewColorProbeTool_FormatsProbeResponses);
        await AddCheckAsync(results,
            "MCP preview tool routes preview toggle",
            McpPreviewTools_RoutePreviewToggle);
        await AddCheckAsync(results,
            "MCP video source probe tool formats probe responses",
            McpVideoSourceProbeTool_FormatsProbeResponses);
    }
}
