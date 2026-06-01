using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{

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
    public Task CaptureServicePreviewLifecycleLivesInCohesiveOwner()
        => global::Program.CaptureService_PreviewLifecycleLivesInCohesiveOwner();

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
    public Task SubmissionLivesWithRendererRoot()
        => global::Program.D3D11PreviewRenderer_SubmissionLivesWithRendererRoot();

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
    public Task RenderPassesOwnInputUpload()
        => global::Program.D3D11PreviewRenderer_RenderPassesOwnInputUpload();

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
    public Task ShaderResourcesLiveWithD3DResources()
        => global::Program.D3D11PreviewRenderer_ShaderResourcesLiveWithD3DResources();

    [Fact]
    public Task ShaderCompilationLivesInFocusedFiles()
        => global::Program.D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles();

    [Fact]
    public Task FrameLatencyLivesWithRenderThread()
        => global::Program.D3D11PreviewRenderer_FrameLatencyLivesWithRenderThread();

    [Fact]
    public Task RenderThreadLivesInRendererRoot()
        => global::Program.D3D11PreviewRenderer_RenderThreadLivesInRendererRoot();

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
    public Task FrameRateTimingPolicyLivesWithViewModelSelectionPolicies()
        => global::Program.FrameRateTimingPolicy_LivesWithViewModelSelectionPolicies();

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

    [Fact]
    public Task StatsOverlayLifecycleLivesInController()
        => global::Program.StatsOverlayLifecycle_LivesInController();

    [Fact]
    public Task StatsSectionChromeLivesInFocusedPartial()
        => global::Program.StatsSectionChrome_LivesInFocusedPartial();
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

}

static partial class Program
{
    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var dependenciesText = compositionText;

        AssertContains(rootText, "public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel");
        AssertContains(rootText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "private readonly DeviceService _deviceService;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Composition.cs")),
            "MainViewModel.Composition.cs folded into MainViewModel.cs");
        AssertContains(compositionText, "public MainViewModel()\n        : this(MainViewModelDependencies.CreateDefault())");
        AssertContains(compositionText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(compositionText, "private readonly DeviceService _deviceService;");
        AssertContains(compositionText, "_deviceService = dependencies.DeviceService;");
        AssertContains(compositionText, "_captureService = dependencies.CaptureService;");
        AssertContains(compositionText, "_sessionCoordinator = dependencies.SessionCoordinator;");
        AssertContains(compositionText, "_audioRampTraceRecorder = CreateAudioRampTraceRecorder();");
        AssertContains(compositionText, "_previewAudioVolumeTransitionController = CreatePreviewAudioVolumeTransitionController();");
        AssertContains(compositionText, "_deviceAudioControlService = dependencies.DeviceAudioControlService;");
        AssertContains(compositionText, "_dispatcherQueue = dependencies.DispatcherQueue;");
        AssertContains(compositionText, "_audioDeviceWatcher = dependencies.AudioDeviceWatcher;");
        AssertContains(compositionText, "var controllerGraph = MainViewModelControllerGraph.Create(this);");
        AssertContains(compositionText, "_uiDispatchController = controllerGraph.UiDispatchController;");
        AssertContains(compositionText, "_recordingTransitionController = controllerGraph.RecordingTransitionController;");
        AssertContains(compositionText, "_previewLifecycleController = controllerGraph.PreviewLifecycleController;");
        AssertContains(compositionText, "_deviceAudioRequestController = controllerGraph.DeviceAudioRequestController;");
        AssertContains(compositionText, "_recordingCapabilityController = controllerGraph.RecordingCapabilityController;");
        AssertContains(compositionText, "_captureSettingsAutomationController = controllerGraph.CaptureSettingsAutomationController;");
        AssertContains(compositionText, "_recordingSettingsAutomationController = controllerGraph.RecordingSettingsAutomationController;");
        AssertContains(compositionText, "_captureModeOptionRebuildController = controllerGraph.CaptureModeOptionRebuildController;");
        AssertDoesNotContain(rootText, "_resolutionOptionRebuildController");
        AssertDoesNotContain(compositionText, "_resolutionOptionRebuildController");
        AssertDoesNotContain(compositionText, "new MainViewModelResolutionOptionRebuildController");
        AssertContains(compositionText, "_deviceFormatProbeController = controllerGraph.DeviceFormatProbeController;");
        AssertContains(compositionText, "_sourceTelemetryController = controllerGraph.SourceTelemetryController;");
        AssertContains(compositionText, "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;");
        AssertContains(compositionText, "_disposalController = controllerGraph.DisposalController;");
        AssertContains(compositionText, "_runtimeLifecycleController.Start();");
        AssertContains(compositionText, "_runtimeLifecycleController.InitializePresentation();");
        AssertDoesNotContain(compositionText, "new MainViewModelUiDispatchController(");
        AssertDoesNotContain(compositionText, "new MainViewModelRecordingTransitionController(this)");
        AssertDoesNotContain(compositionText, "new MainViewModelRuntimeLifecycleController(this)");
        AssertDoesNotContain(compositionText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(compositionText, "_captureService = new CaptureService();");
        AssertDoesNotContain(compositionText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(compositionText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(compositionText, "_audioDeviceWatcher = new AudioDeviceWatcher();");
        AssertDoesNotContain(compositionText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(compositionText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertDoesNotContain(compositionText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertDoesNotContain(compositionText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(compositionText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");

        AssertContains(controllerGraphText, "private sealed class MainViewModelControllerGraph");
        AssertContains(controllerGraphText, "public static MainViewModelControllerGraph Create(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "var uiDispatchController = CreateUiDispatchController(viewModel);");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphText, "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "var sourceTelemetryController = CreateSourceTelemetryController(viewModel);");
        AssertContains(controllerGraphText, "public MainViewModelDisposalController DisposalController { get; }");
        AssertDoesNotContain(controllerGraphText, "RuntimeLifecycleController.Start();");
        AssertOccursBefore(
            compositionText,
            "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;",
            "_runtimeLifecycleController.Start();");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(");

        AssertContains(rootText, "public partial bool IsStatsVisible");
        AssertContains(rootText, "public partial bool IsSettingsVisible");
        AssertContains(rootText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(rootText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationUi.cs")),
            "MainViewModel.AutomationUi.cs folded into MainViewModel.cs");
        AssertContains(rootText, "public partial string StatusText");
        AssertContains(rootText, "private IntPtr _windowHandle;");
        AssertContains(rootText, "public void SetWindowHandle(IntPtr handle)");
        AssertContains(rootText, "_windowHandle = handle;");
        AssertContains(rootText, "private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)");
        AssertDoesNotContain(rootText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.State.cs")),
            "MainViewModel.State.cs folded into MainViewModel.cs");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertContains(rootText, "public partial bool IsPreviewing");
        AssertContains(rootText, "public event EventHandler? PreviewStartRequested");
        AssertContains(rootText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewState.cs")),
            "MainViewModel.PreviewState.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureState.cs")),
            "MainViewModel.CaptureState.cs folded into MainViewModel.cs");
        AssertContains(captureStateText, "public partial ObservableCollection<CaptureDevice> Devices");
        AssertContains(captureStateText, "public partial ObservableCollection<ResolutionOption> AvailableResolutions");
        AssertContains(captureStateText, "public partial ObservableCollection<FrameRateOption> AvailableFrameRates");
        AssertContains(captureStateText, "private const string HdrToggleBlockedWhileRecordingMessage");
        AssertContains(captureStateText, "public partial bool IsHdrEnabled");
        AssertContains(captureStateText, "public partial string HdrRuntimeState");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureHdrState.cs")),
            "MainViewModel.CaptureHdrState.cs folded into MainViewModel.cs");
        AssertContains(captureStateText, "private SourceSignalTelemetrySnapshot _latestSourceTelemetry");
        AssertContains(captureStateText, "public partial double? DetectedSourceFrameRate");
        AssertContains(captureStateText, "public partial string SourceTelemetryAvailability");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureSourceState.cs")),
            "MainViewModel.CaptureSourceState.cs folded into MainViewModel.cs");
        AssertContains(audioStateText, "public partial bool IsAudioPreviewActive");
        AssertContains(audioStateText, "private AudioRampTraceRecorder CreateAudioRampTraceRecorder()");
        AssertContains(audioStateText, "public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioRampTrace.cs")),
            "MainViewModel.AudioRampTrace.cs folded into MainViewModel.AudioState.cs");
        AssertContains(flashbackStateText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(flashbackStateText, "public void UpdateFlashbackBufferStatus()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");

        AssertContains(dependenciesText, "internal sealed class MainViewModelDependencies");
        AssertContains(dependenciesText, "public static MainViewModelDependencies CreateDefault()");
        AssertContains(dependenciesText, "var captureService = new CaptureService();");
        AssertContains(dependenciesText, "new CaptureSessionCoordinator(captureService)");
        AssertContains(dependenciesText, "DispatcherQueue.GetForCurrentThread()");
        AssertContains(dependenciesText, "new AudioDeviceWatcher()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModelDependencies.cs")),
            "MainViewModelDependencies.cs folded into MainViewModel.cs");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelUiDispatchController_UsesDependencyCompositionContext()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphText, "private sealed class MainViewModelControllerGraph");
        AssertContains(controllerGraphText, "private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "DispatcherQueue = viewModel._dispatcherQueue,");
        AssertContains(controllerGraphText, "IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,");
        AssertContains(controllerGraphText, "SetStatusText = value => viewModel.StatusText = value,");

        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchController");
        AssertContains(uiDispatchControllerText, "private readonly MainViewModelUiDispatchControllerContext _context;");
        AssertDoesNotContain(uiDispatchControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(uiDispatchControllerText, "_viewModel.");
        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(uiDispatchControllerText, "public required DispatcherQueue DispatcherQueue { get; init; }");
        AssertContains(uiDispatchControllerText, "public required Func<bool> IsDisposing { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelRecordingTransition_UsesDependencyCompositionContext()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphText, "private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(");
        AssertContains(controllerGraphText, "new MainViewModelRecordingTransitionController(\n                new MainViewModelRecordingTransitionControllerContext");
        AssertContains(controllerGraphText, "StartRecordingAsync = (settings, cancellationToken) =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),");
        AssertContains(controllerGraphText, "StopRecordingAsync = cancellationToken =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");

        AssertContains(recordingTransitionControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingTransitionControllerText, "internal sealed class MainViewModelRecordingTransitionController");
        AssertDoesNotContain(recordingTransitionControllerText, "partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerText, "internal sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertDoesNotContain(recordingTransitionControllerText, "await _viewModel.InitializeDeviceAsync(cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelRecordingTransitionController.cs")),
            "recording transition controller lives with the ViewModel capture lifecycle owner");

        return Task.CompletedTask;
    }

internal static Task MainViewModelPresentationControllers_UseDependencyCompositionContexts()
    {
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerText = previewLifecycleControllerText;

        AssertContains(controllerGraphText, "private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphText, "new MainViewModelPreviewLifecycleController(\n                new MainViewModelPreviewLifecycleControllerContext");
        AssertContains(controllerGraphText, "SessionCoordinator = viewModel._sessionCoordinator,");
        AssertContains(controllerGraphText, "BuildCaptureSettings = viewModel.BuildCaptureSettings,");
        AssertContains(controllerGraphText, "InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),");
        AssertContains(controllerGraphText, "RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,");
        AssertContains(controllerGraphText, "CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(");
        AssertContains(controllerGraphText, "new MainViewModelPreviewReinitializeControllerContext");
        AssertContains(controllerGraphText, "IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "PreviewReinitializeDebounceMs = PreviewReinitializeDebounceMs,");
        AssertContains(controllerGraphText, "ClearPendingFlashbackCycleIfSameAndCompleted = task =>");
        AssertContains(controllerGraphText, "FlashbackCycleBeforeReinitializeTimeoutMs = FlashbackCycleBeforeReinitializeTimeoutMs,");
        AssertContains(controllerGraphText, "AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,");
        AssertContains(controllerGraphText, "SelectedDevice = () => viewModel.SelectedDevice,");
        AssertContains(controllerGraphText, "SetSelectedDevice = device => viewModel.SelectedDevice = device,");
        AssertContains(controllerGraphText, "IsInitialized = () => viewModel.IsInitialized,");
        AssertContains(controllerGraphText, "SetIsInitialized = value => viewModel.IsInitialized = value,");
        AssertContains(controllerGraphText, "IsPreviewing = () => viewModel.IsPreviewing,");
        AssertContains(controllerGraphText, "SetIsPreviewing = value => viewModel.IsPreviewing = value,");
        AssertContains(controllerGraphText, "IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,");
        AssertContains(controllerGraphText, "IsRecording = () => viewModel.IsRecording,");
        AssertContains(controllerGraphText, "ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,");
        AssertContains(controllerGraphText, "IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,");
        AssertContains(controllerGraphText, "SetStatusText = value => viewModel.StatusText = value,");
        AssertContains(controllerGraphText, "RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphText, "RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphText, "ApplyLatestSourceTelemetryForPreviewStart = () =>");

        AssertContains(previewStateText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(previewStateText, "internal void CancelPendingPreviewRestart()");
        AssertContains(previewStateText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewStateText, "public Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public partial bool IsPreviewing");
        AssertContains(previewStateText, "public partial bool IsPreviewReinitializing");
        AssertContains(previewStateText, "public partial bool IsInitialized");
        AssertContains(previewStateText, "private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);");
        AssertContains(previewStateText, "private int _previewReinitializeGeneration;");
        AssertContains(previewStateText, "private bool _cancelPreviewRestartAfterReinitialize;");
        AssertContains(previewStateText, "public event EventHandler? PreviewStartRequested;");
        AssertContains(previewStateText, "public event EventHandler? PreviewStopRequested;");
        AssertContains(previewStateText, "public event Func<string, Task>? PreviewReinitRequested;");
        AssertContains(previewStateText, "public event Func<Task>? PreviewRendererStopRequested;");

        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleControllerContext");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewLifecycleControllerContext _context;");
        AssertDoesNotContain(previewLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewLifecycleControllerText, "_viewModel.");
        AssertContains(previewLifecycleControllerText, "public required CaptureSessionCoordinator SessionCoordinator { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<CaptureSettings> BuildCaptureSettings { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<CaptureDevice?> SelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<CaptureDevice> SetSelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<bool> SetIsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<bool> SetIsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsPreviewReinitializing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsRecording { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> ShouldStartAudioPreview { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsAudioPreviewActive { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<string> SetStatusText { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action RaisePreviewStartRequested { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action RaisePreviewStopRequested { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }");
        AssertContains(previewLifecycleControllerText, "public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewLifecycleControllerText, "_previewReinitializeController = _context.CreateReinitializeController(this);");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");

        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeControllerContext");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public required int PreviewReinitializeDebounceMs { get; init; }");
        AssertContains(previewReinitializeControllerText, "public required int FlashbackCycleBeforeReinitializeTimeoutMs { get; init; }");
        AssertContains(previewReinitializeControllerText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelPreviewReinitializeController.cs")),
            "preview reinitialize transaction controller lives with preview lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelPreviewLifecycleController.cs")),
            "preview lifecycle controller lives with recording transitions in the ViewModel capture lifecycle owner");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(previewStateText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);");
        AssertContains(previewStateText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "=> _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(mainViewModelText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(mainViewModelText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "return _context.InvokeOnUiThreadAsync(async () =>");
        AssertContains(previewLifecycleControllerText, "CancelPendingPreviewRestart();");
        AssertContains(previewLifecycleControllerText, "if (enabled == _context.IsPreviewing())");
        AssertContains(previewLifecycleControllerText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(previewLifecycleControllerText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureServiceText, "private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertContains(captureServiceText, "await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Probes.cs")),
            "CaptureService probe partial folded into snapshots");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel preview automation partial");

        return Task.CompletedTask;
    }

internal static Task MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = audioStateText;
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = captureSettingsAutomationControllerText;
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var frameRateTimingResolverText = captureModeOptionRebuildControllerText;
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = deviceFormatProbeControllerText;

        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs")),
            "MainViewModel device-audio state folded into MainViewModel.AudioState.cs");

        AssertContains(controllerGraphText, "var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);");
        AssertContains(controllerGraphText, "var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);");
        AssertContains(controllerGraphText, "var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");

        AssertContains(controllerGraphText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),");
        AssertContains(controllerGraphText, "viewModel._frameRateTimingResolver);");
        AssertContains(controllerGraphText, "private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureModeOptionRebuildController(\n                new MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(controllerGraphText, "TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,");
        AssertContains(controllerGraphText, "ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,");
        AssertContains(controllerGraphText, "SetSelectedFormat = value => viewModel.SelectedFormat = value,");

        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(");
        AssertContains(controllerGraphText, "new MainViewModelDeviceRefreshControllerContext");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");
        AssertContains(controllerGraphText, "BeginBackgroundFormatProbe = (device, scanGeneration) =>");

        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerText, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(controllerGraphText, "ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(controllerGraphText, "ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>");

        AssertContains(captureSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(controllerGraphText, "private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(controllerGraphText, "SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,");
        AssertContains(controllerGraphText, "ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,");

        AssertContains(recordingSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertContains(recordingSettingsAutomationControllerText, "_context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsAutomationControllerContext");

        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingCapabilityControllerContext");
        AssertContains(controllerGraphText, "ReplaceAvailableRecordingFormats = formats =>");
        AssertContains(controllerGraphText, "NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),");

        AssertContains(captureModeOptionRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, double, FrameRateTimingFamily> ResolvePreferredTimingFamily");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, IReadOnlyList<FrameRateOption>, double, (double? Rate, string? Arg, string Origin)> ResolveDetectedSourceFrameRate");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, IReadOnlyList<FrameRateTimingVariant>> BuildFrameRateTimingVariants");
        AssertContains(frameRateTimingResolverText, "namespace Sussudio.Controllers;");
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolver");
        AssertContains(frameRateTimingResolverText, "public FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(frameRateTimingResolverText, "public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(frameRateTimingResolverText, "public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelFrameRateTimingResolver.cs")),
            "frame-rate timing resolver lives with capture mode option rebuild owner");
        AssertContains(captureModeOptionRebuildControllerText, "public required string AutoResolutionValue { get; init; }");
        AssertContains(captureModeOptionRebuildControllerText, "public required double AutoFrameRateValue { get; init; }");
        AssertContains(controllerGraphText, "AutoResolutionValue = AutoResolutionValue,");
        AssertContains(controllerGraphText, "AutoFrameRateValue = AutoFrameRateValue,");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "_viewModel.");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionRebuildControllerText.Split('\n').Length >= 300,
            "capture mode option rebuild controller is a substantial ownership file");
        AssertContains(captureModeOptionRebuildControllerText, "_frameRateTimingResolver.ResolveDetectedSourceFrameRate(");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");

        AssertContains(deviceFormatProbeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(deviceFormatProbeRetargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(deviceFormatProbeRetargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "_viewModel.");
        AssertEqual(
            true,
            deviceFormatProbeRetargetApplierText.Split('\n').Length >= 100,
            "device format probe retarget applier is a substantial ownership file");
        AssertContains(deviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeRetargetApplierContext");

        return Task.CompletedTask;
    }

internal static Task MainViewModelRuntimeControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");

        AssertContains(sourceTelemetryControllerText, "namespace Sussudio.Controllers;");
        AssertContains(sourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "private readonly MainViewModelSourceTelemetryControllerContext _context;");
        AssertDoesNotContain(sourceTelemetryControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(sourceTelemetryControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Func<string?, bool> IsAutoResolutionValue { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Action RebuildResolutionOptions { get; init; }");
        AssertContains(controllerGraphText, "SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,");
        AssertContains(controllerGraphText, "BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,");
        AssertContains(controllerGraphText, "IsAutoResolutionValue = MainViewModel.IsAutoResolutionValue,");
        AssertContains(controllerGraphText, "RebuildResolutionOptions = viewModel.RebuildResolutionOptions,");
        AssertContains(controllerGraphText, "UpdateTargetSummary = viewModel.UpdateTargetSummary,");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(controllerGraphText, "private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeLifecycleController(\n                new MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(controllerGraphText, "CreateEventIngressController = () => CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "deviceFormatProbeController,");
        AssertContains(controllerGraphText, "sourceTelemetryController),");
        AssertContains(controllerGraphText, "ApplySourceTelemetrySnapshot = sourceTelemetryController.ApplySourceTelemetrySnapshot,");
        AssertContains(controllerGraphText, "RefreshSourceTelemetrySummaryAge = sourceTelemetryController.RefreshSourceTelemetrySummaryAge,");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(");

        AssertContains(runtimeLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeLifecycleControllerContext _context;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeLifecycleController.cs")),
            "runtime lifecycle controller folded into MainViewModelLifecycleController.cs");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeLifecycleControllerText, "_viewModel.");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController = _context.CreateEventIngressController();");
        AssertContains(runtimeLifecycleControllerText, "public void Start()");
        AssertContains(runtimeLifecycleControllerText, "=> _eventIngressController.Attach();");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController.Detach();");
        AssertContains(runtimeLifecycleControllerText, "public void InitializePresentation()");
        AssertContains(runtimeLifecycleControllerText, "var latestSourceTelemetry = _context.GetLatestSourceTelemetrySnapshot();");
        AssertContains(runtimeLifecycleControllerText, "_context.SetLatestSourceTelemetrySnapshot(latestSourceTelemetry);");
        AssertContains(runtimeLifecycleControllerText, "_context.ApplySourceTelemetrySnapshot(latestSourceTelemetry, false);");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateLiveCaptureInfo();");
        AssertContains(runtimeLifecycleControllerText, "SetupTimer();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateDiskSpace();");

        AssertContains(runtimeEventIngressControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressController");
        AssertDoesNotContain(runtimeEventIngressControllerText, "partial class MainViewModelRuntimeEventIngressController");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "private readonly MainViewModelRuntimeEventIngressControllerContext _context;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(controllerGraphText, "private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeEventIngressControllerContext");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeEventIngressController.cs")),
            "runtime event ingress controller folded into MainViewModelLifecycleController.cs");
        AssertContains(runtimeEventIngressControllerText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(runtimeEventIngressControllerText, "public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"system resume\")");
        AssertEqual(
            true,
            runtimeEventIngressControllerText.Split('\n').Length >= 100,
            "runtime event ingress controller is a substantial ownership file");
        AssertContains(runtimeEventIngressControllerText, "public void Attach()");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCaptureErrorOccurred(OnCaptureError);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachFrameCaptured(OnFrameCaptured);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioDevicesChanged(_context.OnAudioDevicesChanged);");
        AssertContains(runtimeEventIngressControllerText, "public void Detach()");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");

        AssertContains(controllerGraphText, "private static MainViewModelDisposalController CreateDisposalController(");
        AssertContains(controllerGraphText, "MainViewModelDeviceAudioRequestController deviceAudioRequestController,");
        AssertContains(controllerGraphText, "MainViewModelRuntimeLifecycleController runtimeLifecycleController)");
        AssertContains(controllerGraphText, "new MainViewModelDisposalController(\n                new MainViewModelDisposalControllerContext");
        AssertContains(controllerGraphText, "TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,");
        AssertContains(controllerGraphText, "CancelPendingAudioControlWork = deviceAudioRequestController.CancelPendingAudioControlWork,");
        AssertContains(controllerGraphText, "StopRuntimeForDispose = runtimeLifecycleController.StopForDispose,");
        AssertContains(controllerGraphText, "CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),");
        AssertContains(controllerGraphText, "AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,");
        AssertContains(controllerGraphText, "public MainViewModelDisposalController DisposalController { get; }");

        AssertContains(disposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalText, "=> _disposalController.Dispose();");
        AssertContains(disposalText, "=> await _disposalController.DisposeAsync().ConfigureAwait(false);");
        AssertContains(disposalControllerText, "namespace Sussudio.Controllers;");
        AssertContains(disposalControllerText, "internal sealed class MainViewModelDisposalController");
        AssertContains(disposalControllerText, "internal sealed class MainViewModelDisposalControllerContext");
        AssertContains(disposalControllerText, "private readonly MainViewModelDisposalControllerContext _context;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDisposalController.cs")),
            "disposal controller folded into MainViewModelLifecycleController.cs");
        AssertContains(disposalControllerText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
        AssertDoesNotContain(disposalControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(disposalControllerText, "_viewModel.");
        AssertEqual(
            true,
            disposalControllerText.Split('\n').Length >= 100,
            "view-model disposal controller is a substantial ownership file");
        AssertContains(disposalControllerText, "private const int DefaultDisposeTimeoutMs = 30000;");
        AssertContains(disposalControllerText, "private async Task DisposeCoreAsync()");
        AssertContains(disposalControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.CancelPendingAudioControlWork();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS");
        AssertDoesNotContain(disposalText, "_captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertDoesNotContain(disposalText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");

        return Task.CompletedTask;
    }

    internal static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeEventIngressControllerText, "_context.SetIsInitialized(_context.IsCaptureInitialized());");
        AssertContains(runtimeEventIngressControllerText, "_context.SetIsPreviewing(_context.IsVideoPreviewActive());");
        AssertContains(runtimeEventIngressControllerText, "_context.SetIsRecording(_context.IsCaptureRecording());");
        AssertContains(runtimeEventIngressControllerText, "_context.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeEventIngressControllerText, "_context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}
