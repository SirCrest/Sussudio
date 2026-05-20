using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
