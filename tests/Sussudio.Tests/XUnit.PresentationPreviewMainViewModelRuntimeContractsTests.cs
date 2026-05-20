using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelRuntimeContractsTests
{
    public PresentationPreviewMainViewModelRuntimeContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationRoutesPreviewVolumePersistenceThroughSaveHook()
        => global::Program.MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook();

    [Fact]
    public Task AutomationPreviewEnablementLivesInPreviewLifecycleController()
        => global::Program.MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController();

    [Fact]
    public Task AutomationHdrEnablementLivesInCaptureModeTransactions()
        => global::Program.MainViewModelAutomation_HdrEnablementLivesInCaptureModeTransactions();

    [Fact]
    public Task CaptureRoutesAudioMonitoringThroughCoordinator()
        => global::Program.MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator();

    [Fact]
    public Task CaptureSettingsProjectionLivesInFocusedPartial()
        => global::Program.MainViewModelCaptureSettings_OwnsSettingsProjection();

    [Fact]
    public Task CaptureSettingsFrameRateProjectionPreservesPrecedence()
        => global::Program.MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence();

    [Fact]
    public Task PreviewLifecycleLivesInController()
        => global::Program.MainViewModelPreviewLifecycle_LivesInController();

    [Fact]
    public Task AudioRampTraceExposesControlAndRenderSideEnvelopeTelemetry()
        => global::Program.AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry();
}
