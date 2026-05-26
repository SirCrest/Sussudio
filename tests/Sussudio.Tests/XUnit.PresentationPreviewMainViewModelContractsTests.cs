using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelAudioControlsContractsTests
{
    public PresentationPreviewMainViewModelAudioControlsContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AudioControlsMapAnalogGainCurveAndClampEndpoints()
        => global::Program.MainViewModelAudioControls_MapsAnalogGainCurveAndClamps();

    [Fact]
    public Task AudioMonitoringPreservesVolumePersistenceAndRampedRouting()
        => global::Program.MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting();

    [Fact]
    public Task AudioControlsPreserveMicrophoneAndDeviceGuards()
        => global::Program.MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards();

    [Fact]
    public Task DeviceAudioRequestControllerOwnsRequestLifetime()
        => global::Program.MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime();

    [Fact]
    public Task AudioDeviceSelectionPolicyLivesInFocusedHelper()
        => global::Program.AudioDeviceSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task AudioDeviceSelectionPolicyStartupFiltersCaptureAudioAndUsesSavedFallbacks()
        => global::Program.AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks();

    [Fact]
    public Task AudioDeviceSelectionPolicyStartupPreservesPreviousSelections()
        => global::Program.AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections();

    [Fact]
    public Task AudioDeviceSelectionPolicyRefreshPreservesSelections()
        => global::Program.AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback();

    [Fact]
    public Task AudioDeviceSelectionPolicyHandlesEmptyLists()
        => global::Program.AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections();

    [Fact]
    public Task NativeXuAudioControlServiceLivesInCohesiveServiceFile()
        => global::Program.NativeXuAudioControlService_LivesInCohesiveServiceFile();

    [Fact]
    public Task AudioMetersOwnCallbackMeterState()
        => global::Program.MainViewModelAudioMeters_OwnCallbackMeterState();
}

public sealed class PresentationPreviewMainViewModelDependencyCompositionContractsTests
{
    public PresentationPreviewMainViewModelDependencyCompositionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task MainViewModelUsesDependencyCompositionSeam()
        => global::Program.MainViewModel_UsesDependencyCompositionSeam();

    [Fact]
    public Task UiDispatchControllerUsesDependencyCompositionContext()
        => global::Program.MainViewModelUiDispatchController_UsesDependencyCompositionContext();

    [Fact]
    public Task PresentationControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelPresentationControllers_UseDependencyCompositionContexts();

    [Fact]
    public Task RecordingTransitionUsesDependencyCompositionContext()
        => global::Program.MainViewModelRecordingTransition_UsesDependencyCompositionContext();

    [Fact]
    public Task CaptureAndDeviceControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts();

    [Fact]
    public Task RuntimeControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelRuntimeControllers_UseDependencyCompositionContexts();
}

public sealed class PresentationPreviewMainViewModelInitialContractsTests
{
    public PresentationPreviewMainViewModelInitialContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingStartAndStopFailuresPropagateToCallers()
        => global::Program.MainViewModelCapture_RecordingFailuresPropagateToCallers();
}

public sealed class PresentationPreviewMainViewModelOutputPathContractsTests
{
    public PresentationPreviewMainViewModelOutputPathContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task OutputPathSelectionLivesInFocusedOwner()
        => global::Program.MainViewModelOutputPathSelection_LivesInFocusedPartial();

    [Fact]
    public Task OutputDriveFreeSpacePresentationHandlesInvalidPaths()
        => global::Program.OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty();

    [Fact]
    public Task OutputDriveFreeSpacePresentationLivesInFocusedHelper()
        => global::Program.OutputDriveSpacePresentationBuilder_LivesInFocusedHelper();
}

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
    public Task AutomationHdrEnablementLivesInCaptureSelection()
        => global::Program.MainViewModelAutomation_HdrEnablementLivesInCaptureSelection();

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

public sealed class PresentationPreviewMainViewModelSourceTelemetryContractsTests
{
    public PresentationPreviewMainViewModelSourceTelemetryContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SourceTelemetryPresentationPreservesSummaryAndTargetText()
        => global::Program.SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText();

    [Fact]
    public Task SourceTelemetryPresentationLivesInFocusedHelper()
        => global::Program.SourceTelemetryPresentationBuilder_LivesInFocusedHelper();

    [Fact]
    public Task LiveSignalTextProjectionPreservesPixelFormatFallbackOrder()
        => global::Program.LiveSignalTextProjection_PreservesPixelFormatFallbackOrder();
}

public sealed class PresentationPreviewFrameRateSelectionContractsTests
{
    public PresentationPreviewFrameRateSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SourceFilteredFrameRatesAreAlwaysUnlocked()
        => global::Program.SourceFilteredFrameRatesAreAlwaysUnlocked();

    [Fact]
    public Task FrameRateSourceFilterPolicyLivesInFocusedHelper()
        => global::Program.FrameRateSourceFilterPolicy_LivesInFocusedHelper();

    [Fact]
    public Task FrameRateAutoSelectionPolicyLivesInFocusedHelper()
        => global::Program.FrameRateAutoSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task FrameRateAutoSelectionPolicyPreservesSelectionBehavior()
        => global::Program.FrameRateAutoSelectionPolicy_PreservesSelectionBehavior();

    [Fact]
    public Task FrameRateTimingPolicyLivesInFocusedPartial()
        => global::Program.FrameRateTimingPolicy_LivesInFocusedPartial();

    [Fact]
    public Task FrameRateTimingPolicyPreservesPureTimingBehavior()
        => global::Program.FrameRateTimingPolicy_PreservesPureTimingBehavior();
}

public sealed class PresentationPreviewDeviceFormatProbeRetargetContractsTests
{
    public PresentationPreviewDeviceFormatProbeRetargetContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task DeviceFormatProbeRetargetPolicyLivesInFocusedHelper()
        => global::Program.DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper();

    [Fact]
    public Task DeviceFormatProbeRetargetPolicyPreservesRetargetDecisionBehavior()
        => global::Program.DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior();

    [Fact]
    public Task DeviceFormatProbeRetargetApplicationLivesInFocusedPartial()
        => global::Program.DeviceFormatProbeRetargetApplication_LivesInFocusedPartial();
}

public sealed class PresentationPreviewCaptureSelectionPolicyContractsTests
{
    public PresentationPreviewCaptureSelectionPolicyContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ModeSelectionStateLivesInFocusedPartial()
        => global::Program.ModeSelectionState_LivesInFocusedPartial();

    [Fact]
    public Task CaptureFormatSelectionPolicyLivesInFocusedHelper()
        => global::Program.CaptureFormatSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task CaptureFormatSelectionPolicyPreservesSelectionBehavior()
        => global::Program.CaptureFormatSelectionPolicy_PreservesSelectionBehavior();

    [Fact]
    public Task RecordingSettingsSelectionPolicyLivesInFocusedHelper()
        => global::Program.RecordingSettingsSelectionPolicy_LivesInFocusedHelper();
}
