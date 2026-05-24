using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DContractsAndMetricsOwnershipTests
{
    public PresentationPreviewD3DContractsAndMetricsOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ConfigurationLivesWithRendererFacade()
        => global::Program.D3D11PreviewRenderer_ConfigurationLivesWithRendererFacade();

    [Fact]
    public Task NativeInteropLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_NativeInteropLivesInFocusedPartial();

    [Fact]
    public Task FrameTypesLiveWithPendingFrameQueue()
        => global::Program.D3D11PreviewRenderer_FrameTypesLiveWithPendingFrameQueue();

    [Fact]
    public Task FrameOwnershipLivesWithMetrics()
        => global::Program.D3D11PreviewRenderer_FrameOwnershipLivesWithMetrics();

    [Fact]
    public Task DxgiFrameStatisticsLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial();

    [Fact]
    public Task SlowFrameDiagnosticsLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial();

    [Fact]
    public Task MetricTrackingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial();
}
