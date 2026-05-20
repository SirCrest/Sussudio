using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DDiagnosticsContractsTests
{
    public PresentationPreviewD3DDiagnosticsContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SwapChainAndRenderTimingContractIsExposed()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming();

    [Fact]
    public Task SnapshotModelsExposeExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_SnapshotModelsExposeExpectedProperties();

    [Fact]
    public Task PerformanceTimelineExposesExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties();
}
