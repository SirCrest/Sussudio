using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupOwnershipContractsTests
{
    public PresentationPreviewStartupOwnershipContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSessionAndReinitOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSessionReinitOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupWatchdogOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupWatchdogOwnership_LivesInFocusedController();

    [Fact]
    public Task PreviewStartupSignalOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSignalsOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupLifecycleEventOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupLifecycleEventOwnership_LivesInFocusedController();
}

public sealed class PresentationPreviewStartupBehaviorContractsTests
{
    public PresentationPreviewStartupBehaviorContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupWatchdogControllerPreservesTimeoutContracts()
        => global::Program.PreviewStartupWatchdogController_PreservesTimeoutContracts();

    [Fact]
    public Task PreviewStartupWatchdogControllerGatesFailureStopScheduling()
        => global::Program.PreviewStartupWatchdogController_GatesFailureStopScheduling();

    [Fact]
    public Task PreviewStartupSessionControllerPreservesAttemptStateContracts()
        => global::Program.PreviewStartupSessionController_PreservesAttemptStateContracts();

    [Fact]
    public Task PreviewReinitTransitionControllerPreservesTransitionStateContracts()
        => global::Program.PreviewReinitTransitionController_PreservesTransitionStateContracts();

    [Fact]
    public Task PreviewReinitializationWaitsForPendingFlashbackCycle()
        => global::Program.PreviewReinitialization_WaitsForPendingFlashbackCycle();
}

public sealed class PresentationPreviewStartupSignalContractsTests
{
    public PresentationPreviewStartupSignalContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSignalFormatterPreservesStringContracts()
        => global::Program.PreviewStartupSignalFormatter_PreservesSignalStrings();

    [Fact]
    public Task PreviewStartupReadinessSignalControllerPreservesStateContracts()
        => global::Program.PreviewStartupReadinessSignalController_PreservesSignalStateContracts();

    [Fact]
    public Task PreviewStartupFailureTextFormatterPreservesStringContracts()
        => global::Program.PreviewStartupFailureTextFormatter_PreservesFailureStrings();
}

public sealed class PresentationPreviewStartupOrderingContractsTests
{
    public PresentationPreviewStartupOrderingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupBeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
        => global::Program.PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish();

    [Fact]
    public Task PreviewStartupPrimesUiAndAudioBeforePreviewReveal()
        => global::Program.PreviewStartup_PrimesUiAndAudioBeforePreviewReveal();

    [Fact]
    public Task PreviewStopRampsAudioDownBeforePreviewTeardown()
        => global::Program.PreviewStop_RampsAudioDownBeforePreviewTeardown();
}

public sealed class PresentationPreviewCapturePreviewLifecycleContractsTests
{
    public PresentationPreviewCapturePreviewLifecycleContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupToleratesMissingAudioCaptureDevices()
        => global::Program.PreviewStartup_ToleratesMissingAudioCaptureDevices();

    [Fact]
    public Task CaptureServicePreviewLifecycleLivesInFocusedPartials()
        => global::Program.CaptureService_PreviewLifecycleLivesInFocusedPartials();

    [Fact]
    public Task AudioPreviewRemainsInactiveWhenNoAudioCaptureDeviceExists()
        => global::Program.AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists();

    [Fact]
    public Task AudioMonitoringVisualsFollowRuntimePreviewActivity()
        => global::Program.AudioMonitoringVisuals_FollowRuntimePreviewActivity();

    [Fact]
    public Task PreviewBackendLogReflectsVideoOnlyFallback()
        => global::Program.PreviewBackendLog_ReflectsVideoOnlyFallback();
}

public sealed class PresentationPreviewCaptureFlashbackBufferContractsTests
{
    public PresentationPreviewCaptureFlashbackBufferContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackBufferManagerCleansStaleSessionDirectories()
        => global::Program.FlashbackBufferManager_CleansStaleSessionDirectories();

    [Fact]
    public Task FlashbackBufferManagerPreservesMarkedRecoverySessions()
        => global::Program.FlashbackBufferManager_PreservesMarkedRecoverySessions();
}

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
    public Task DxgiFrameStatisticsLiveWithMetrics()
        => global::Program.D3D11PreviewRenderer_DxgiFrameStatisticsLiveWithMetrics();

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
        => global::Program.D3D11PreviewRenderer_PanelBindingLivesWithRendererFacade();

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

public sealed class PresentationPreviewAudioControlContractsTests
{
    public PresentationPreviewAudioControlContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewAudioFadeStateLivesInController()
        => global::Program.PreviewAudioFadeState_LivesInController();

    [Fact]
    public Task AudioControlPresentationLivesInController()
        => global::Program.AudioControlPresentation_LivesInController();

    [Fact]
    public Task PreviewButtonPresentationLivesInController()
        => global::Program.PreviewButtonPresentation_LivesInController();

    [Fact]
    public Task MicrophoneControlsLiveInController()
        => global::Program.MicrophoneControls_LiveInController();
}

public sealed class PresentationPreviewCaptureRuntimeGuardContractsTests
{
    public PresentationPreviewCaptureRuntimeGuardContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingStopPropagatesUnifiedVideoStopFailure()
        => global::Program.RecordingStop_PropagatesUnifiedVideoStopFailure();

    [Fact]
    public Task PreviewStopCompatibilityOverloadsArePreserved()
        => global::Program.PreviewStopCompatibilityOverloads_ArePreserved();

    [Fact]
    public Task PreviewStopApiSurfaceHasNoDefaultLiteralAmbiguity()
        => global::Program.PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity();

    [Fact]
    public Task EmergencyRecordingStopDoesNotDispatchToBlockedUiThread()
        => global::Program.EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread();
}

public sealed class PresentationPreviewCaptureSelectionContractsTests
{
    public PresentationPreviewCaptureSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task CaptureSelectionBindingSyncLivesInController()
        => global::Program.CaptureSelectionBindingSync_LivesInController();

    [Fact]
    public Task CaptureSelectionBindingPropertyRouterLivesInController()
        => global::Program.CaptureSelectionBindingPropertyRouter_LivesInController();

    [Fact]
    public Task CaptureSelectionBindingCollectionSyncLivesInControllerPartial()
        => global::Program.CaptureSelectionBindingCollectionSync_LivesInControllerPartial();

    [Fact]
    public Task CaptureSelectionBindingSelectionOwnersLiveInFocusedPartials()
        => global::Program.CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials();

    [Fact]
    public Task CaptureSelectionBindingDeviceAudioProjectionLivesInFocusedPartial()
        => global::Program.CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial();

    [Fact]
    public Task CaptureComboBoxSelectionNormalizerPreservesSelectionFallbacks()
        => global::Program.CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks();
}

public sealed class PresentationPreviewLaunchStartupContractsTests
{
    public PresentationPreviewLaunchStartupContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SplashLoadingPhrasesLiveInController()
        => global::Program.SplashLoadingPhrases_LiveInController();

    [Fact]
    public Task SplashLoadingPhrasePacingPolicyPreservesIntervalBands()
        => global::Program.SplashLoadingPhrasePacingPolicy_PreservesIntervalBands();

    [Fact]
    public Task LaunchEntranceAnimationLivesInController()
        => global::Program.LaunchEntranceAnimation_LivesInController();

    [Fact]
    public Task MainWindowStartupHostingLivesInStartupPartial()
        => global::Program.MainWindowStartupHosting_LivesInStartupPartial();
}

public sealed class PresentationPreviewRecordingContractsTests
{
    public PresentationPreviewRecordingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingButtonChromeLivesInController()
        => global::Program.RecordingButtonChrome_LivesInController();

    [Fact]
    public Task RecordingStatePresentationLivesInController()
        => global::Program.RecordingStatePresentation_LivesInController();

    [Fact]
    public Task RecordingStatePresentationPolicyPreservesLockoutRules()
        => global::Program.RecordingStatePresentationPolicy_PreservesLockoutRules();

    [Fact]
    public Task RecordingButtonActionLivesInController()
        => global::Program.RecordingButtonAction_LivesInController();
}

public sealed class PresentationPreviewResolutionSelectionContractsTests
{
    public PresentationPreviewResolutionSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ResolutionSelectionPolicyLivesInFocusedPartial()
        => global::Program.ResolutionSelectionPolicy_LivesInFocusedPartial();

    [Fact]
    public Task CaptureResolutionSelectionPolicyPreservesHdrSourceRetargetBehavior()
        => global::Program.CaptureResolutionSelectionPolicy_PreservesHdrSourceRetargetBehavior();

    [Fact]
    public Task CaptureResolutionSelectionPolicyPreservesSdrAutoBucketPreference()
        => global::Program.CaptureResolutionSelectionPolicy_PreservesSdrAutoBucketPreference();

    [Fact]
    public Task AutoCaptureSelectionPolicyPreservesSourceBoundedSelection()
        => global::Program.AutoCaptureSelectionPolicy_PreservesSourceBoundedSelection();
}

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

public sealed class PresentationPreviewShellChromeContractsTests
{
    public PresentationPreviewShellChromeContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SettingsShelfLifecycleLivesInController()
        => global::Program.SettingsShelfLifecycle_LivesInController();

    [Fact]
    public Task MainWindowTitlePresentationLivesInController()
        => global::Program.MainWindowTitlePresentation_LivesInController();

    [Fact]
    public Task WindowTitleControllerFormatsBuildStampAndRecordingSuffix()
        => global::Program.WindowTitleController_FormatsBuildStampAndRecordingSuffix();

    [Fact]
    public Task LiveSignalInfoPresentationLivesInController()
        => global::Program.LiveSignalInfoPresentation_LivesInController();

    [Fact]
    public Task StatusStripPresentationLivesInController()
        => global::Program.StatusStripPresentation_LivesInController();
}

public sealed class PresentationPreviewVisualShellContractsTests
{
    public PresentationPreviewVisualShellContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ControlBarHoverAnimationsLiveInController()
        => global::Program.ControlBarHoverAnimations_LiveInController();

    [Fact]
    public Task ShellElevationSetupLivesInController()
        => global::Program.ShellElevationSetup_LivesInController();

    [Fact]
    public Task PreviewTransitionAnimationsLiveInController()
        => global::Program.PreviewTransitionAnimations_LiveInController();

    [Fact]
    public Task PreviewStartupOverlayLivesInController()
        => global::Program.PreviewStartupOverlay_LivesInController();

    [Fact]
    public Task PreviewFadeInRevealLivesInController()
        => global::Program.PreviewFadeInReveal_LivesInController();
}

public sealed class PresentationPreviewWindowLifecycleContractsTests
{
    public PresentationPreviewWindowLifecycleContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task MainWindowNativeBootstrapLivesInFocusedController()
        => global::Program.MainWindowNativeBootstrap_LivesInFocusedController();

    [Fact]
    public Task MainWindowCloseLifecycleAndShutdownCleanupAreSplit()
        => global::Program.MainWindowCloseLifecycleAndShutdownCleanup_AreSplit();

    [Fact]
    public Task MainWindowCloseLifecycleControllersOwnCloseRequestAndAppClosing()
        => global::Program.MainWindowCloseLifecycleControllers_OwnCloseRequestAndAppClosing();

    [Fact]
    public Task MainWindowCloseRecordingFinalizationOwnsRecordingStopPolicy()
        => global::Program.MainWindowCloseRecordingFinalization_OwnsRecordingStopPolicy();

    [Fact]
    public Task MainWindowShutdownCleanupOwnsPostCloseCleanupOrder()
        => global::Program.MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder();
}

public sealed class PresentationPreviewMainWindowInitialContractsTests
{
    public PresentationPreviewMainWindowInitialContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task WindowCloseCancelsUntilRecordingStopCompletes()
        => global::Program.MainWindowClose_CancelsCloseUntilRecordingStopCompletes();

    [Fact]
    public Task WindowScreenshotCaptureCompletesOnDispatcherFailureAndCancellation()
        => global::Program.MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation();

    [Fact]
    public Task WindowScreenshotNativeCaptureLivesWithController()
        => global::Program.WindowScreenshotNativeCapture_LivesWithWindowScreenshotController();

    [Fact]
    public Task WindowScreenshotImageEncodingLivesInFocusedHelper()
        => global::Program.WindowScreenshotImageEncoding_LivesInFocusedHelper();

    [Fact]
    public Task PropertyChangedRoutingDelegatesToFocusedControllers()
        => global::Program.MainWindowPropertyChangedRouting_DelegatesToFocusedControllers();
}

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

public sealed class PresentationPreviewCaptureOptionContractsTests
{
    public PresentationPreviewCaptureOptionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task CaptureDeviceButtonActionsLiveInController()
        => global::Program.CaptureDeviceButtonActions_LiveInController();

    [Fact]
    public Task CaptureOptionPresentationLivesInController()
        => global::Program.CaptureOptionPresentation_LivesInController();

    [Fact]
    public Task CaptureOptionPresentationPolicyPreservesAffordanceRules()
        => global::Program.CaptureOptionPresentationPolicy_PreservesAffordanceRules();

    [Fact]
    public Task CaptureOptionBindingsLiveInController()
        => global::Program.CaptureOptionBindings_LiveInController();

    [Fact]
    public Task CaptureOptionTooltipFormatterPreservesTooltipTextPolicy()
        => global::Program.CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy();
}

public sealed class PresentationPreviewOutputPathContractsTests
{
    public PresentationPreviewOutputPathContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task OutputPathDisplayLivesInController()
        => global::Program.OutputPathDisplay_LivesInController();

    [Fact]
    public Task OutputPathDisplayTextFormatterPreservesTruncationPolicy()
        => global::Program.OutputPathDisplayTextFormatter_PreservesTruncationPolicy();

    [Fact]
    public Task OutputPathButtonActionsLiveInController()
        => global::Program.OutputPathButtonActions_LiveInController();
}
