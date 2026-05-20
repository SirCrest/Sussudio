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

}
