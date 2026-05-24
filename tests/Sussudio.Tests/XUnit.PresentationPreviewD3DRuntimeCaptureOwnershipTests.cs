using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DRuntimeCaptureOwnershipTests
{
    public PresentationPreviewD3DRuntimeCaptureOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SubmissionLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_SubmissionLivesInFocusedPartial();

    [Fact]
    public Task PublicLifecycleLivesInRendererRoot()
        => global::Program.D3D11PreviewRenderer_PublicLifecycleLivesInRendererRoot();
}
