using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewScreenshotContractsTests
{
    public PresentationPreviewScreenshotContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewScreenshotButtonWorkflowLivesInController()
        => global::Program.PreviewScreenshotButtonWorkflow_LivesInController();

    [Fact]
    public Task PreviewScreenshotPlanPolicyPreservesPathAndTextContracts()
        => global::Program.PreviewScreenshotPlanPolicy_PreservesPathAndTextContracts();
}
