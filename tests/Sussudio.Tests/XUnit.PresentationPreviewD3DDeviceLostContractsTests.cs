using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DDeviceLostContractsTests
{
    public PresentationPreviewD3DDeviceLostContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task DeviceLostExceptionsClassifyCorrectly()
        => global::Program.D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly();

    [Fact]
    public Task DeviceLostRecoveryLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial();
}
