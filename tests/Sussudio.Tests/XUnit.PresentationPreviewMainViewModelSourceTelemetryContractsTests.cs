using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelSourceTelemetryContractsTests
{
    public PresentationPreviewMainViewModelSourceTelemetryContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SourceTelemetryPresentationPreservesSummaryAndTargetText()
        => global::Program.SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText();

    [Fact]
    public Task SourceTelemetryPresentationLivesInFocusedHelper()
        => global::Program.SourceTelemetryPresentationBuilder_LivesInFocusedHelper();

    [Fact]
    public Task LiveSignalTextProjectionPreservesPixelFormatFallbackOrder()
        => global::Program.LiveSignalTextProjection_PreservesPixelFormatFallbackOrder();
}
