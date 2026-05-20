using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
