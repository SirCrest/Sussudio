using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DGeometryContractsTests
{
    public PresentationPreviewD3DGeometryContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task LetterboxRectCalculatesCorrectly()
        => global::Program.D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly();

    [Fact]
    public Task BlackEdgeCountingWorksCorrectly()
        => global::Program.D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly();

    [Fact]
    public Task PngCrcTableGenerates256Entries()
        => global::Program.D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries();

    [Fact]
    public Task PreviewPngCaptureWrites16BitRgbPng()
        => global::Program.D3D11PreviewRenderer_PreviewPngCapture_Writes16BitRgbPng();
}
