using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewResponsiveLayoutContractsTests
{
    public PresentationPreviewResponsiveLayoutContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ResponsiveShellLayoutLivesInController()
        => global::Program.ResponsiveShellLayout_LivesInController();

    [Fact]
    public Task ResponsiveShellLayoutPolicyPreservesBreakpointsAndPlacements()
        => global::Program.ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements();
}
