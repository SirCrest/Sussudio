using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewRuntimeShellContractsTests
{
    public PresentationPreviewRuntimeShellContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewResizeTelemetryLivesInController()
        => global::Program.PreviewResizeTelemetry_LivesInController();

    [Fact]
    public Task PreviewRendererHostControllerOwnsRuntimeState()
        => global::Program.PreviewRendererHostController_OwnsRuntimeState();

    [Fact]
    public Task PreviewRuntimeSnapshotControllerOwnsSnapshotMapping()
        => global::Program.PreviewRuntimeSnapshotController_OwnsSnapshotMapping();

    [Fact]
    public Task PreviewRuntimeD3DProjectionOwnsPolicyGroups()
        => global::Program.PreviewRuntimeD3DProjection_OwnsPolicyGroups();

    [Fact]
    public Task PreviewSurfacePresentationAndShadowLiveInControllers()
        => global::Program.PreviewSurfacePresentationAndShadow_LiveInControllers();

    [Fact]
    public Task PreviewRendererStartupPlanBuilderPreservesFallbackPolicy()
        => global::Program.PreviewRendererStartupPlanBuilder_PreservesFallbackPolicy();
}
