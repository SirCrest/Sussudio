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

public sealed class McpPerformanceToolContractsTests
{
    [Fact]
    public Task PresentMonToolsRouteSnapshotCorrelation()
        => global::Program.McpPresentMonTools_RouteSnapshotCorrelation();

    [Fact]
    public Task PerformanceTimelineToolExposesD3DP99StageTiming()
        => global::Program.McpPerformanceTimelineTool_ExposesD3DP99StageTiming();

    [Fact]
    public Task PerformanceTimelineToolRendersFlashbackCommandCounters()
        => global::Program.McpPerformanceTimelineTool_RendersFlashbackCommandCounters();

    [Fact]
    public Task FramePacingVerdictToolFlagsHalfRatePreviewAndPlayback()
        => global::Program.McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback();

    [Fact]
    public Task FramePacingVerdictToolFlagsInsufficientSampleDuration()
        => global::Program.McpFramePacingVerdictTool_FlagsInsufficientSampleDuration();

    [Fact]
    public Task FramePacingVerdictToolSourceOwnershipIsSplit()
        => global::Program.McpFramePacingVerdictTool_SourceOwnershipIsSplit();
}

public sealed class McpWindowPreviewToolContractsTests
{
    [Fact]
    public Task WaitToolsUseCatalogResponseTimeoutForConditionWaits()
        => global::Program.McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits();

    [Fact]
    public Task WaitToolsRouteConditionWaits()
        => global::Program.McpWaitTools_RouteConditionWaits();

    [Fact]
    public Task WindowScreenshotToolFormatsScreenshotResponses()
        => global::Program.McpWindowScreenshotTool_FormatsScreenshotResponses();

    [Fact]
    public Task PreviewFrameCaptureToolFormatsFrameReports()
        => global::Program.McpPreviewFrameCaptureTool_FormatsCaptureResponses();

    [Fact]
    public Task WindowToolsRouteWindowActions()
        => global::Program.McpWindowTools_RouteWindowActions();

    [Fact]
    public Task PreviewToolsRoutePreviewToggle()
        => global::Program.McpPreviewTools_RoutePreviewToggle();

    [Fact]
    public Task PreviewColorProbeToolFormatsProbeResponses()
        => global::Program.McpPreviewColorProbeTool_FormatsProbeResponses();

    [Fact]
    public Task VideoSourceProbeToolFormatsProbeResponses()
        => global::Program.McpVideoSourceProbeTool_FormatsProbeResponses();
}
