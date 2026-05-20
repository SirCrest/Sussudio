using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewRuntimePolicyContractsTests
{
    public PresentationPreviewRuntimePolicyContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewRuntimeSnapshotControllerPreservesNullD3dProjectionPolicy()
        => global::Program.PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy();

    [Fact]
    public Task PreviewRuntimeSnapshotHealthPolicyPreservesSuspicionRules()
        => global::Program.PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules();

    [Fact]
    public Task PreviewRuntimeSnapshotHealthInputFactoryProjectsControllerInputs()
        => global::Program.PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs();

    [Fact]
    public Task PreviewRuntimeSnapshotSurfaceProjectionPolicyPreservesVisibilityAndHealthFields()
        => global::Program.PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields();

    [Fact]
    public Task PreviewRuntimeSnapshotStartupProjectionPolicyPreservesSampledStartupFields()
        => global::Program.PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields();

    [Fact]
    public Task PreviewRuntimeSnapshotGpuPlaybackProjectionPolicyPreservesRendererAndEventFields()
        => global::Program.PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields();

    [Fact]
    public Task PreviewRuntimeD3DFrameCounterPolicyPreservesCpuFallbackCounters()
        => global::Program.PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters();

    [Fact]
    public Task PreviewRuntimeD3DProjectionBuilderAppliesPolicyGroups()
        => global::Program.PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups();

    [Fact]
    public Task PreviewRuntimeD3DRendererStatePolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DDisplayCadencePolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DRenderCpuTimingPolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DPipelineLatencyPolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DFrameStatisticsPolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DFrameLatencyWaitPolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults();

    [Fact]
    public Task PreviewRuntimeD3DFrameOwnershipPolicyPreservesNullRendererDefaults()
        => global::Program.PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults();
}
