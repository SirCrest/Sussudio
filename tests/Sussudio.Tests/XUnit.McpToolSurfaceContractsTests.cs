using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpToolSurfaceContractsTests
{
    [Fact]
    public Task RawAppStateKeepsCaptureOptionsSeparate()
        => global::Program.McpToolSurface_KeepsCaptureOptionsSeparateFromRawState();

    [Fact]
    public Task FixedAutomationRoutesUseAutomationCommandKinds()
        => global::Program.McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds();

    [Fact]
    public Task HostToolSchemaUsesPipeClientAsService()
        => global::Program.McpHostToolSchema_UsesPipeClientAsService();

    [Fact]
    public Task PipeClientHonorsSussudioAutomationPipeEnvironment()
        => global::Program.McpPipeClient_HonorsSussudioAutomationPipeEnvironment();

    [Fact]
    public Task HostToolInvocationReturnsPipeFailures()
        => global::Program.McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport();

    [Fact]
    public Task CaptureSettingsToolsRouteProvidedSettings()
        => global::Program.McpCaptureSettingsTools_RouteProvidedSettings();

    [Fact]
    public Task RecordingToolsRouteRecordingToggle()
        => global::Program.McpRecordingTools_RouteRecordingToggle();

    [Fact]
    public Task FlashbackToolsRouteEnableToggle()
        => global::Program.McpFlashbackTools_RouteEnableToggle();

    [Fact]
    public Task ToolCommandFormatterBatchesPendingCommands()
        => global::Program.McpToolCommandFormatter_BatchesPendingCommands();

    [Fact]
    public Task DeviceToolsRouteRefreshSelectionsAndCustomAudio()
        => global::Program.McpDeviceTools_RouteRefreshSelectionsAndCustomAudio();

    [Fact]
    public Task PipelineSettingsToolsRoutePipelineAndAudioCommands()
        => global::Program.McpPipelineSettingsTools_RoutePipelineAndAudioCommands();

    [Fact]
    public Task UiSettingsToolsRouteUiCommands()
        => global::Program.McpUiSettingsTools_RouteUiCommands();

    [Fact]
    public Task VerificationToolsFormatVerificationResponses()
        => global::Program.McpVerificationTools_FormatVerificationResponses();

    [Fact]
    public Task DiagnosticSessionToolRecordsSnapshotArtifacts()
        => global::Program.McpDiagnosticSessionTool_RecordsSnapshotArtifacts();

    [Fact]
    public Task DiagnosticSessionToolSurfacesDiagnosticFailures()
        => global::Program.McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError();
}
