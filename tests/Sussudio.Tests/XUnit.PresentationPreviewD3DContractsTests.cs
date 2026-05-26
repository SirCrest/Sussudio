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

public sealed class PresentationPreviewD3DDiagnosticsContractsTests
{
    public PresentationPreviewD3DDiagnosticsContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SwapChainAndRenderTimingContractIsExposed()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming();

    [Fact]
    public Task SnapshotModelsExposeExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_SnapshotModelsExposeExpectedProperties();

    [Fact]
    public Task PerformanceTimelineExposesExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties();
}

public sealed class PresentationPreviewD3DContractsAndMetricsOwnershipTests
{
    public PresentationPreviewD3DContractsAndMetricsOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ConfigurationLivesWithRendererFacade()
        => global::Program.D3D11PreviewRenderer_ConfigurationLivesWithRendererFacade();

    [Fact]
    public Task NativeInteropLivesWithBehaviorOwners()
        => global::Program.D3D11PreviewRenderer_NativeInteropLivesWithBehaviorOwners();

    [Fact]
    public Task FrameTypesLiveWithPendingFrameQueue()
        => global::Program.D3D11PreviewRenderer_FrameTypesLiveWithPendingFrameQueue();

    [Fact]
    public Task FrameOwnershipLivesWithMetrics()
        => global::Program.D3D11PreviewRenderer_FrameOwnershipLivesWithMetrics();

    [Fact]
    public Task DxgiFrameStatisticsLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial();

    [Fact]
    public Task SlowFrameDiagnosticsLiveWithMetrics()
        => global::Program.D3D11PreviewRenderer_SlowFrameDiagnosticsLiveWithMetrics();

    [Fact]
    public Task MetricTrackingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial();
}

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

public sealed class PresentationPreviewD3DRenderSetupOwnershipTests
{
    public PresentationPreviewD3DRenderSetupOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PanelBindingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial();

    [Fact]
    public Task SharedDeviceLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial();

    [Fact]
    public Task FrameUploadLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial();

    [Fact]
    public Task InputResourcesLiveWithD3DResources()
        => global::Program.D3D11PreviewRenderer_InputResourcesLiveWithD3DResources();

    [Fact]
    public Task DeviceInitializationOwnsSwapChainSetup()
        => global::Program.D3D11PreviewRenderer_DeviceInitializationOwnsSwapChainSetup();
}

public sealed class PresentationPreviewD3DRenderPipelineOwnershipTests
{
    public PresentationPreviewD3DRenderPipelineOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RenderPassesLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial();

    [Fact]
    public Task ShaderRenderingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial();

    [Fact]
    public Task ShaderCompilationLivesInFocusedFiles()
        => global::Program.D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles();

    [Fact]
    public Task FrameLatencyLivesWithRenderThread()
        => global::Program.D3D11PreviewRenderer_FrameLatencyLivesWithRenderThread();

    [Fact]
    public Task RenderThreadLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_RenderThreadLivesInFocusedPartial();

    [Fact]
    public Task PresentAccountingLivesWithRenderPasses()
        => global::Program.D3D11PreviewRenderer_PresentAccountingLivesWithRenderPasses();

    [Fact]
    public Task ViewportHelpersLiveWithRenderPasses()
        => global::Program.D3D11PreviewRenderer_ViewportHelpersLiveWithRenderPasses();

    [Fact]
    public Task ScreenshotEncodingLivesWithScreenshotCapture()
        => global::Program.D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture();
}
