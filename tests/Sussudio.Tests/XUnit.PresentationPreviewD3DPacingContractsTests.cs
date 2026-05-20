using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DPacingContractsTests
{
    public PresentationPreviewD3DPacingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task TransitionDrainDropsPendingFrames()
        => global::Program.D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration();

    [Fact]
    public Task FrameCaptureCancellationClearsPendingRequest()
        => global::Program.D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest();

    [Fact]
    public Task SharedDeviceReferencesDuplicateUnderLifecycleLock()
        => global::Program.SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock();
}
