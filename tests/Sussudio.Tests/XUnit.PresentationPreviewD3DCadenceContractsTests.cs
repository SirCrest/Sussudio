using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DCadenceContractsTests
{
    public PresentationPreviewD3DCadenceContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PresentCadenceMetricsExposeExpectedProperties()
        => global::Program.D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties();

    [Fact]
    public Task PresentCadenceSuppressionSkipsSamplesAndResetsBaseline()
        => global::Program.D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline();
}
