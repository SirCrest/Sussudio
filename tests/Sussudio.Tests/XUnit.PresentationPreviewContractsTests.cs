using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;

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

    [Fact]
    public Task PreviewPngCaptureRefusesExistingFile()
        => global::Program.D3D11PreviewRenderer_PreviewPngCapture_RefusesExistingFile();

    [Fact]
    public Task PreviewBmpCaptureRefusesExistingFile()
        => global::Program.D3D11PreviewRenderer_PreviewBmpCapture_RefusesExistingFile();
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
    public Task PreviewRuntimeSnapshotEpochDoesNotAdvanceForUnchangedSignatures()
        => global::Program.PreviewRuntimeSnapshotEpoch_DoesNotAdvanceForUnchangedSignatures();

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
    internal static Task D3D11PreviewRenderer_ConfigurationLivesWithRendererFacade()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Configuration.cs")),
            "D3D11 preview renderer configuration partial");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_NativeInteropLivesWithBehaviorOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = rootText;
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "mixed native interop bucket retired into behavior owners");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Metrics.cs")),
            "renderer metrics partial folded into the renderer root");
        AssertContains(panelBindingText, "private interface ISwapChainPanelNative");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(resourcesText, "private interface ID3DBlob");
        AssertContains(resourcesText, "private static extern int D3DCompileNative(");
        AssertContains(resourcesText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(resourcesText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertContains(metricsText, "private static extern int DwmFlush()");
        AssertContains(metricsText, "_ = DwmFlush();");
        AssertContains(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameTypesLiveWithPendingFrameQueue()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrame.cs")), "pending-frame lifetime model stays folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrames.cs")), "pending-frame queue folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Submission.cs")), "pending-frame submission folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricTypes.cs")), "renderer metric model types folded into the renderer root");
        AssertContains(rootText, "private sealed class PendingFrame : IDisposable");
        AssertContains(rootText, "ArrayPool<byte>.Shared.Return(RawData);");
        AssertContains(rootText, "FrameLease?.Dispose();");
        AssertContains(metricsText, "public readonly record struct PresentCadenceMetrics(");
        AssertContains(metricsText, "public readonly record struct CpuStageTimingMetrics(");
        AssertContains(metricsText, "public readonly record struct RenderCpuTimingMetrics(");
        AssertContains(metricsText, "public readonly record struct PipelineLatencyMetrics(");
        AssertContains(metricsText, "public readonly record struct FrameLatencyWaitMetrics(");
        AssertContains(metricsText, "public readonly record struct FrameOwnershipMetrics(");
        AssertContains(metricsText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertContains(metricsText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricsText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricsText, "private static double TicksToMs(long ticks)");
        AssertContains(metricsText, "private static bool IsValidRenderCpuStageMs(double value)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameOwnershipLivesWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameOwnership.cs")),
            "frame ownership metrics folded into renderer metrics owner");
        AssertContains(metricsText, "private long _framesSubmitted;");
        AssertContains(metricsText, "private long _framesRendered;");
        AssertContains(metricsText, "private long _framesDropped;");
        AssertContains(metricsText, "private long _submissionGeneration;");
        AssertContains(metricsText, "private long _lastSubmittedPreviewPresentId;");
        AssertContains(metricsText, "private long _lastRenderedSchedulerToPresentTicks;");
        AssertContains(metricsText, "private long _lastDroppedUtcUnixMs;");
        AssertContains(metricsText, "private string _submissionGenerationDropReason = \"transition\";");
        AssertContains(metricsText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertContains(metricsText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertContains(metricsText, "private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)");
        AssertContains(metricsText, "private void TrackFrameDropped(PendingFrame frame, string reason)");
        AssertContains(metricsText, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(metricsText, "Volatile.Write(ref _lastDropReason, reason);");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DxgiFrameStatisticsLiveWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DxgiFrameStatistics.cs")),
            "DXGI frame-statistics partial folded into renderer metrics owner");
        AssertContains(metricsText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsSampleCount;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsMissedRefreshCount;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsPresentCount = -1;");
        AssertContains(metricsText, "private bool _dxgiFrameStatisticsHasBaseline;");
        AssertContains(metricsText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertContains(metricsText, "private void TrackDxgiFrameStatistics()");
        AssertContains(metricsText, "_ = DwmFlush();");
        AssertContains(metricsText, "_swapChain.GetFrameStatistics(out var stats)");
        AssertContains(metricsText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(metricsText, "private long GetEstimatedDisplayFrameIntervalTicks()");
        AssertContains(metricsText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertContains(metricsText, "new PreviewDisplayClockSnapshot(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DisplayClock.cs")),
            "D3D11 preview display-clock projection lives with renderer metrics");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SlowFrameDiagnosticsLiveWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Diagnostics.cs")),
            "slow-frame diagnostics folded into renderer metrics owner");
        AssertContains(metricsText, "private readonly object _slowFrameDiagnosticsLock = new();");
        AssertContains(metricsText, "private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];");
        AssertContains(metricsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(int maxEntries = 16)");
        AssertContains(metricsText, "private void RecordSlowFrameDiagnostic(");
        AssertContains(metricsText, "var dxgiSlip = CaptureSlowFrameDxgiSlipSnapshot();");
        AssertContains(metricsText, "DxgiMissedRefreshCount = dxgiSlip.MissedRefreshCount");
        AssertContains(metricsText, "private readonly record struct SlowFrameDxgiSlipSnapshot(");
        AssertContains(metricsText, "private SlowFrameDxgiSlipSnapshot CaptureSlowFrameDxgiSlipSnapshot()");
        AssertContains(metricsText, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(metricsText, "private static string BuildSlowFrameDiagnosticReason(");
        AssertContains(metricsText, "private static void AppendSlowFrameReason(");
        AssertContains(metricsText, "\"dxgi_refresh_slip\"");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = rootText;

        AssertContains(metricsText, "private readonly object _presentCadenceLock = new();");
        AssertContains(metricsText, "private double[] _presentIntervalWindowMs = new double[1200];");
        AssertContains(metricsText, "public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)");
        AssertContains(metricsText, "public double[] GetRecentPresentIntervalsMs(int maxSamples)");
        AssertContains(metricsText, "private readonly object _pipelineLatencyLock = new();");
        AssertContains(metricsText, "private double[] _pipelineLatencyWindowMs = new double[1200];");
        AssertContains(metricsText, "private readonly object _renderCpuTimingLock = new();");
        AssertContains(metricsText, "private readonly object _frameLatencyWaitTimingLock = new();");
        AssertContains(metricsText, "private long _frameLatencyWaitCallCount;");
        AssertContains(metricsText, "public RenderCpuTimingMetrics GetRenderCpuTimingMetrics()");
        AssertContains(metricsText, "public FrameLatencyWaitMetrics GetFrameLatencyWaitMetrics()");
        AssertContains(metricsText, "private long _lastPresentTick;");
        AssertContains(metricsText, "private int _presentCadenceBaselinePending;");
        AssertContains(metricsText, "private double TrackPresentCadence(bool countSample)");
        AssertContains(metricsText, "private void TrackPipelineLatency(long arrivalTick, long estimatedVisibleTick)");
        AssertContains(metricsText, "private void TrackRenderCpuTiming(");
        AssertContains(metricsText, "private void TrackFrameLatencyWait(uint result, long waitTicks)");
        AssertContains(metricsText, "public void SetExpectedFrameRate(double fps)");
        AssertContains(metricsText, "private void ResetPresentCadence()");
        AssertContains(metricsText, "var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));");
        AssertContains(metricsText, "Array.Clear(_slowFrameDiagnostics, 0, _slowFrameDiagnostics.Length);");
        AssertContains(metricsText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricsText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricsText, "private static double TicksToMs(long ticks)");
        AssertContains(metricsText, "private static bool IsValidRenderCpuStageMs(double value)");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PresentCadenceMetrics.cs")),
            "Present cadence metrics folded into the renderer root");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricTypes.cs")),
            "Renderer metric model types folded into the renderer root");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricsTracking.cs")),
            "Metric tracking folded into the renderer root");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricWindows.cs")),
            "Metric window lifecycle folded into the renderer root");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PanelBindingLivesWithRendererFacade()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D11 preview panel binding folded into renderer facade");
        AssertContains(panelBindingText, "private int _swapChainBound;");
        AssertContains(panelBindingText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "private void UnbindSwapChainFromPanel()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(panelBindingText, "private int _compositionTransformDirty;");
        AssertContains(panelBindingText, "private int _panelPixelWidth = 1;");
        AssertContains(panelBindingText, "private double _panelLogicalWidth = 1.0;");
        AssertContains(panelBindingText, "private double _rasterizationScale = 1.0;");
        AssertContains(panelBindingText, "public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)");
        AssertContains(panelBindingText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "swapChain2.MatrixTransform");
        AssertContains(rootText, "private int _swapChainBound;");
        AssertContains(rootText, "private int _compositionTransformDirty;");
        AssertDoesNotContain(resourcesText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(resourcesText, "private void UnbindSwapChainFromPanel()");
        AssertDoesNotContain(resourcesText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceInitializationOwnsSwapChainSetup()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = resourcesText;
        var videoProcessorPipelineText = resourcesText;

        AssertContains(resourcesText, "private ID3D11Device? _device;");
        AssertContains(resourcesText, "private IDXGISwapChain1? _swapChain;");
        AssertContains(resourcesText, "private ID3D11VideoProcessor? _videoProcessor;");
        AssertContains(deviceInitializationText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private void ConfigureMediaPresentDuration()");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(deviceInitializationText, "var (swapChain, pixelWidth, pixelHeight) = InitializeCompositionSwapChain(device);");
        AssertContains(deviceInitializationText, "private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)");
        AssertContains(deviceInitializationText, "private (IDXGISwapChain1 SwapChain, int PixelWidth, int PixelHeight) InitializeCompositionSwapChain(ID3D11Device device)");
        AssertContains(deviceInitializationText, "DXGI.CreateDXGIFactory2(false, out _factory)");
        AssertContains(deviceInitializationText, "_factory.CreateSwapChainForComposition(device, swapChainDescription, null);");
        AssertContains(deviceInitializationText, "private void EnsureHdrCapableSwapChainOrFallbackToSdr(");
        AssertContains(deviceInitializationText, "_swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020)");
        AssertContains(deviceInitializationText, "private void RecreateSdrCompositionSwapChain(");
        AssertContains(deviceInitializationText, "Format.B8G8R8A8_UNorm");
        AssertContains(deviceInitializationText, "_configuredOutputWidth = pixelWidth;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.SwapChainInitialization.cs")),
            "D3D11 preview swap-chain setup folded into D3D resource ownership");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DeviceInitialization.cs")),
            "D3D11 preview device initialization folded into D3D resource ownership");
        AssertContains(videoProcessorPipelineText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertContains(videoProcessorPipelineText, "private void EnsureSwapChainRTV()");
        AssertContains(videoProcessorPipelineText, "private void RecreateOutputView()");
        AssertContains(videoProcessorPipelineText, "private void ApplyColorSpaces(bool isHdr)");
        AssertContains(videoProcessorPipelineText, "private void DisposeProcessorResources()");
        AssertContains(videoProcessorPipelineText, "DisposeProcessorInputResources();");
        AssertContains(videoProcessorPipelineText, "DisposeNv12ShaderResourceViews();");
        AssertContains(videoProcessorPipelineText, "new VideoProcessorContentDescription");
        AssertContains(videoProcessorPipelineText, "RecreateOutputView();");
        AssertContains(videoProcessorPipelineText, "ApplyColorSpaces(isHdr);");
        AssertContains(videoProcessorPipelineText, "_videoDevice.CreateVideoProcessorOutputView(");
        AssertContains(videoProcessorPipelineText, "_videoContext1.VideoProcessorSetStreamColorSpace1(");
        AssertContains(videoProcessorPipelineText, "D3D11 preview color space input=");
        AssertContains(resourcesText, "private void CleanupD3DResources()");
        AssertContains(resourcesText, "DisposeInputTextureResources();");
        AssertContains(resourcesText, "DisposeShaderPipelineResources();");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.VideoProcessorPipeline.cs")),
            "VideoProcessor setup and output-view resources live with D3D resource ownership");
        AssertContains(resourcesText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertDoesNotContain(rootText, "private ID3D11Device? _device;");
        AssertDoesNotContain(rootText, "private IDXGISwapChain1? _swapChain;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderLifecycleText = rootText;
        var sharedDeviceText = deviceInitializationText;

        AssertContains(sharedDeviceText, "private ID3D11Device? _sharedDevice;");
        AssertContains(sharedDeviceText, "private int _sharedDeviceResetPending;");
        AssertContains(sharedDeviceText, "private int _sharedDeviceActive;");
        AssertContains(sharedDeviceText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertContains(sharedDeviceText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertContains(sharedDeviceText, "private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)");
        AssertContains(sharedDeviceText, "Marshal.AddRef(sharedDevice.NativePointer);");
        AssertContains(sharedDeviceText, "Interlocked.Exchange(ref _sharedDeviceResetPending, 1);");
        AssertContains(sharedDeviceText, "SignalFrameReady(\"shared_device_reset\");");
        AssertContains(sharedDeviceText, "AccessViolationException");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(renderLifecycleText, "Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1)");
        AssertContains(renderLifecycleText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(renderLifecycleText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderLifecycleText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderLifecycleText, "CleanupD3DResources();");
        AssertContains(renderLifecycleText, "InitializeD3D();");
        AssertDoesNotContain(rootText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertDoesNotContain(rootText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.SharedDevice.cs")),
            "shared D3D device lifecycle folded into D3D resource ownership");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_InputResourcesLiveWithD3DResources()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var videoProcessorPipelineText = resourcesText;
        var inputResourcesText = resourcesText;
        var hdrInputResourcesText = resourcesText;

        AssertContains(inputResourcesText, "private ID3D11Texture2D? _inputTexture;");
        AssertContains(inputResourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertContains(inputResourcesText, "private void DisposeProcessorInputResources()");
        AssertContains(inputResourcesText, "private void DisposeInputTextureResources()");
        AssertContains(inputResourcesText, "_inputTexture = _device.CreateTexture2D(inputDescription);");
        AssertContains(videoProcessorPipelineText, "DisposeHdrInputResources();");
        AssertContains(hdrInputResourcesText, "private ID3D11Texture2D? _hdrInputTexture;");
        AssertContains(hdrInputResourcesText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertContains(hdrInputResourcesText, "private bool _hdrPlaneViewsUnavailable;");
        AssertContains(hdrInputResourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertContains(hdrInputResourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)");
        AssertContains(hdrInputResourcesText, "private void DisposeHdrInputResources()");
        AssertContains(hdrInputResourcesText, "_hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);");
        AssertDoesNotContain(rootText, "private ID3D11Texture2D? _inputTexture;");
        AssertDoesNotContain(rootText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertDoesNotContain(rootText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertDoesNotContain(rootText, "private void EnsureHdrInputResources(int width, int height)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderPassesOwnInputUpload()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertContains(renderPassesText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(renderPassesText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(renderPassesText, "inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);");
        AssertContains(renderPassesText, "UploadRawFrameToTexture(frame.RawData, frame.RawDataLength");
        AssertContains(renderPassesText, "private bool _loggedDirectUploadFallback;");
        AssertContains(renderPassesText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(renderPassesText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(renderPassesText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(renderPassesText, "_deviceContext.UpdateSubresource(");
        AssertContains(renderPassesText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RawFrameUpload.cs")),
            "Raw frame upload helpers folded into render-pass owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameUpload.cs")),
            "Frame upload helpers folded into render-pass owner");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameLatencyLivesWithRenderThread()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameLatency.cs")),
            "D3D11 waitable frame-latency pacing lives with render-thread execution");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 render-thread execution and frame-latency pacing are folded into the renderer root");
        AssertContains(rootText, "private IntPtr _frameLatencyWaitHandle;");
        AssertContains(rootText, "private void ConfigureFrameLatencyWaitableObject()");
        AssertContains(rootText, "private void WaitForFrameLatencySignal()");
        AssertContains(rootText, "TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);");
        AssertContains(rootText, "private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);");
        AssertDoesNotContain(resourcesText, "private void WaitForFrameLatencySignal()");
        AssertDoesNotContain(renderPassesText, "private static extern uint WaitForSingleObject");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ViewportHelpersLiveWithRenderPasses()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Viewport.cs")),
            "D3D11 preview viewport helpers live with render-pass execution");
        AssertContains(renderPassesText, "private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)");
        AssertContains(renderPassesText, "private void UpdateViewportConstantBuffer(Viewport viewport)");
        AssertContains(renderPassesText, "private static Vortice.RawRect ComputeLetterboxRect(");
        AssertContains(renderPassesText, "MapMode.WriteDiscard");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("ComputeLetterboxRect",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeLetterboxRect not found.");

        var result1 = method.Invoke(null, new object[] { 1920, 1080, 1920, 1080 })!;
        var resultType = result1.GetType();
        var left1 = (int)resultType.GetField("Left")!.GetValue(result1)!;
        var top1 = (int)resultType.GetField("Top")!.GetValue(result1)!;
        var right1 = (int)resultType.GetField("Right")!.GetValue(result1)!;
        var bottom1 = (int)resultType.GetField("Bottom")!.GetValue(result1)!;
        AssertEqual(0, left1, "Same aspect: left=0");
        AssertEqual(0, top1, "Same aspect: top=0");
        AssertEqual(1920, right1, "Same aspect: right=1920");
        AssertEqual(1080, bottom1, "Same aspect: bottom=1080");

        var result2 = method.Invoke(null, new object[] { 1920, 1080, 1024, 768 })!;
        var top2 = (int)resultType.GetField("Top")!.GetValue(result2)!;
        var left2 = (int)resultType.GetField("Left")!.GetValue(result2)!;
        AssertEqual(true, top2 > 0, "16:9 into 4:3 should letterbox (top > 0)");
        AssertEqual(0, left2, "16:9 into 4:3 should not pillarbox");

        var result3 = method.Invoke(null, new object[] { 1024, 768, 1920, 1080 })!;
        var left3 = (int)resultType.GetField("Left")!.GetValue(result3)!;
        var top3 = (int)resultType.GetField("Top")!.GetValue(result3)!;
        AssertEqual(true, left3 > 0, "4:3 into 16:9 should pillarbox (left > 0)");
        AssertEqual(0, top3, "4:3 into 16:9 should not letterbox");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderLifecycleText = rootText;
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Nv12ShaderPass.cs")),
            "NV12 shader pass folded into render-pass owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.HdrShaderPass.cs")),
            "HDR shader pass folded into render-pass owner");
        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertContains(renderPassesText, "private void RenderFrame(PendingFrame frame)");
        AssertContains(renderPassesText, "private void ApplySwapChainColorSpaceIfDirty()");
        AssertContains(renderPassesText, "private void RenderFrameWithVideoProcessor(PendingFrame frame)");
        AssertContains(renderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertContains(renderPassesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertContains(renderPassesText, "RenderNv12WithShader(frame);");
        AssertContains(renderPassesText, "RenderHdrFrameWithShader(frame, _hdrPassthroughPS!);");
        AssertContains(renderPassesText, "RenderHdrFrameWithShader(frame, _hdrTonemapPS);");
        AssertContains(renderPassesText, "RenderFrameWithVideoProcessor(frame);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeNv12);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, RendererModeHdrPassthrough);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeHdr);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, RendererModeVideoProcessor);");
        AssertContains(renderPassesText, "if (!TryEnterNativeRenderCall())");
        AssertContains(renderPassesText, "ExitNativeRenderCall();");
        AssertContains(renderPassesText, "PresentAndTrackFrame(");
        AssertContains(renderPassesText, "TryEnsureNv12ShaderResources(frame)");
        AssertContains(renderPassesText, "EnsureHdrInputResources(frame.Width, frame.Height)");
        AssertContains(renderPassesText, "TryResolveInputView(frame, out var inputView, out var disposeInputView)");
        AssertContains(renderPassesText, "D3D11_PREVIEW_HDR_SHADER_FALLBACK");
        AssertContains(renderLifecycleText, "private bool TryEnterNativeRenderCall()");
        AssertContains(renderLifecycleText, "private void ExitNativeRenderCall()");
        AssertContains(renderLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 1);");
        AssertContains(renderLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 0);");
        AssertContains(renderLifecycleText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderLifecycleText, "RenderFrame(frame);");
        AssertDoesNotContain(rootText, "private void RenderFrame(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderResourcesLiveWithD3DResources()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderRendering.cs")),
            "shader rendering resources folded into D3D resource owner");
        AssertContains(resourcesText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertContains(resourcesText, "private ID3D11PixelShader? _nv12PS;");
        AssertContains(resourcesText, "private ID3D11PixelShader? _hdrPassthroughPS;");
        AssertContains(resourcesText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertContains(resourcesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertContains(resourcesText, "private void DisposeNv12ShaderResourceViews()");
        AssertContains(resourcesText, "private void DisposeShaderPipelineResources()");
        AssertContains(resourcesText, "private static readonly ID3D11ClassInstance[] EmptyClassInstances");
        AssertContains(renderPassesText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(renderPassesText, "RendererModeHdrPassthrough");
        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertDoesNotContain(rootText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertDoesNotContain(rootText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertDoesNotContain(renderPassesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private bool _loggedHdrShaderFallback;");
        AssertDoesNotContain(resourcesText, "private int _lastNv12IsHdr = -1;");
        AssertContains(resourcesText, "_linearSampler?.Dispose();");
        AssertContains(resourcesText, "_nv12PS?.Dispose();");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var previewShaderSourcesText = resourcesText;

        AssertContains(previewShaderSourcesText, "internal static class PreviewShaderSources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewShaderSources.cs")),
            "preview shader sources live with D3D resource ownership");
        AssertContains(previewShaderSourcesText, "internal const string FullscreenVertex");
        AssertContains(previewShaderSourcesText, "internal const string HdrTonemapPixel");
        AssertContains(previewShaderSourcesText, "internal const string HdrPassthroughPixel");
        AssertContains(previewShaderSourcesText, "internal const string Nv12Pixel");
        AssertContains(previewShaderSourcesText, "static const float PQ_m1");
        AssertContains(previewShaderSourcesText, "Texture2D<float> yPlane : register(t0);");
        AssertContains(previewShaderSourcesText, "BT2020_to_BT709");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderCompilation.cs")),
            "shader compilation folded into D3D resource owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderRendering.cs")),
            "shader rendering owner folded into D3D resource owner");
        AssertContains(resourcesText, "private unsafe void CompileTonemapShaders()");
        AssertContains(resourcesText, "PreviewShaderSources.FullscreenVertex");
        AssertContains(resourcesText, "PreviewShaderSources.HdrTonemapPixel");
        AssertContains(resourcesText, "PreviewShaderSources.HdrPassthroughPixel");
        AssertContains(resourcesText, "PreviewShaderSources.Nv12Pixel");
        AssertContains(resourcesText, "private interface ID3DBlob");
        AssertContains(resourcesText, "private static extern int D3DCompileNative(");
        AssertContains(resourcesText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(resourcesText, "private static byte[] ReadBlobBytes(IntPtr blobPtr)");
        AssertContains(resourcesText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "shader compiler interop folded into D3D resource owner");

        AssertDoesNotContain(rootText, "internal const string FullscreenVertex");
        AssertDoesNotContain(rootText, "static const float PQ_m1");
        AssertDoesNotContain(renderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderPassesText, "BT2020_to_BT709");

        return Task.CompletedTask;
    }
    internal static Task D3D11PreviewRenderer_SubmissionLivesWithRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var submissionText = rootText;
        var nv12SubmissionText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Submission.cs")),
            "D3D11 preview submission folded into the renderer root lifecycle owner");
        AssertContains(nv12SubmissionText, "private bool _loggedNv12ShaderMissing;");
        AssertContains(nv12SubmissionText, "private int _lastNv12IsHdr = -1;");
        AssertContains(submissionText, "private readonly ManualResetEventSlim _frameReadyEvent = new(false);");
        AssertContains(submissionText, "private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();");
        AssertContains(submissionText, "private sealed class PendingFrame : IDisposable");
        AssertContains(submissionText, "FrameLease?.Dispose();");
        AssertContains(submissionText, "private int _pendingFrameCount;");
        AssertContains(submissionText, "public void SubmitRawFrame(");
        AssertContains(submissionText, "public void SubmitRawFrameLease(");
        AssertContains(submissionText, "public void SubmitTexture(");
        AssertContains(submissionText, "public void SubmitNv12PlaneTextures(");
        AssertContains(submissionText, "private void EnqueueNv12Frame(");
        AssertContains(submissionText, "EnqueuePendingFrame(frame);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Nv12Submission.cs")),
            "NV12 texture submission folded into the D3D11 preview renderer root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrames.cs")),
            "pending-frame queue folded into the D3D11 preview renderer root");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PublicLifecycleLivesInRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Lifecycle.cs")),
            "D3D11 preview public lifecycle is consolidated into the renderer root facade");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 preview render thread lifecycle is consolidated into the renderer root facade");
        AssertContains(rootText, "private readonly object _lifecycleLock = new();");
        AssertContains(rootText, "private Thread? _renderThread;");
        AssertContains(rootText, "private int _disposed;");
        AssertContains(rootText, "private double _startupFps = 60.0;");
        AssertContains(rootText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "private int _stopRequested;");
        AssertContains(rootText, "private int _inNativeCall;");
        AssertContains(rootText, "public void StopRenderThread()");
        AssertContains(rootText, "public void Stop()");
        AssertContains(rootText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertContains(rootText, "WaitForNativeCallToDrainOrThrow(\"stop\");");
        AssertContains(rootText, "FailPendingFrameCapture(\"Preview renderer stopped before frame capture completed.\");");
        AssertContains(rootText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var previewScreenshotCaptureText = ReadRepoFile("Sussudio/Services/Preview/PreviewScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var previewPngEncoderText = previewScreenshotCaptureText;

        AssertContains(captureText, "private void TryCaptureFrameBeforePresent(string rendererMode)");
        AssertContains(captureText, "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)");
        AssertContains(captureText, "private const int FrameCaptureTimeoutMs = 5000;");
        AssertContains(captureText, "private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;");
        AssertContains(captureText, "private void FailPendingFrameCapture(string message)");
        AssertContains(captureText, "if (IsPngFrameCaptureCompletionInProgress())");
        AssertContains(captureText, "EnsureFrameCaptureStagingTexture(backBufferDescription, width, height)");
        AssertContains(captureText, "BeginPngFrameCaptureCompletion(");
        AssertContains(captureText, "TryBeginPngFrameCaptureCompletion()");
        AssertContains(captureText, "EndPngFrameCaptureCompletion();");
        AssertContains(captureText, "LogFrameCaptureResult(captureResult);");
        AssertContains(captureText, "LogFrameCaptureFailure(ex, rendererMode);");
        AssertContains(captureText, "PreviewScreenshotCapture.CopyMappedFrameToBuffer(");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureMappedFrameToBmp(");
        AssertContains(captureText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.bmp");
        AssertContains(captureText, "private void BeginPngFrameCaptureCompletion(");
        AssertContains(captureText, "private int _frameCaptureEncodeInProgress;");
        AssertContains(captureText, "private bool IsPngFrameCaptureCompletionInProgress()");
        AssertContains(captureText, "private bool TryBeginPngFrameCaptureCompletion()");
        AssertContains(captureText, "private void EndPngFrameCaptureCompletion()");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(captureText, "Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);");
        AssertContains(captureText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertContains(captureText, "private static void LogFrameCaptureResult(PreviewFrameCaptureResult captureResult)");
        AssertContains(captureText, "private static void LogFrameCaptureFailure(Exception ex, string rendererMode)");
        AssertContains(captureText, "LuminanceHistogram = new int[16]");
        AssertContains(resourcesText, "DisposeFrameCaptureStagingResources();");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(");
        AssertContains(previewScreenshotCaptureText, "internal static byte[] CopyMappedFrameToBuffer(");
        AssertContains(previewScreenshotCaptureText, "private sealed class PreviewScreenshotPixelAnalysis");
        AssertContains(previewScreenshotCaptureText, "analysis.AnalyzePixel(");
        AssertContains(previewScreenshotCaptureText, "private static void WriteBitmapHeaders(");
        AssertContains(previewScreenshotCaptureText, "new FileStream(outputPath, FileMode.CreateNew");
        AssertEqual(
            2,
            Regex.Matches(previewScreenshotCaptureText, "new FileStream\\(outputPath, FileMode\\.CreateNew").Count,
            "preview screenshot BMP and PNG writers refuse existing output files");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotEncoding.cs")),
            "renderer screenshot encoding partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotCapture.cs")),
            "renderer screenshot capture folded into the render-pass present transaction owner");
        AssertDoesNotContain(captureText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertDoesNotContain(captureText, "private static void WriteBitmapHeaders(");
        AssertDoesNotContain(resourcesText, "_captureStagingTexture?.Dispose();");
        AssertContains(previewScreenshotCaptureText, "PreviewPng16Encoder.WriteCompressedRgb16Png(");
        AssertContains(previewScreenshotCaptureText, "internal static class PreviewScreenshotCapture");
        AssertContains(previewPngEncoderText, "internal static class PreviewPng16Encoder");
        AssertContains(previewPngEncoderText, "internal static void WriteCompressedRgb16Png(");
        AssertContains(previewPngEncoderText, "internal static uint[] InitPngCrc32Table()");
        AssertContains(previewPngEncoderText, "private static void WritePngChunk(");
        AssertContains(previewPngEncoderText, "private static uint UpdatePngCrc32(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewPng16Encoder.cs")),
            "16-bit PNG encoder folded into PreviewScreenshotCapture.cs");
        AssertContains(captureText, "private ID3D11Texture2D? _captureStagingTexture;");
        AssertContains(captureText, "private ID3D11Texture2D EnsureFrameCaptureStagingTexture(");
        AssertContains(captureText, "_captureStagingTexture = _device!.CreateTexture2D(");
        AssertContains(captureText, "private void DisposeFrameCaptureStagingResources()");
        AssertContains(captureText, "_captureStagingTexture?.Dispose();");
        AssertContains(resourcesText, "DisposeFrameCaptureStagingResources();");
        AssertDoesNotContain(resourcesText, "_captureStagingTexture?.Dispose();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewScreenshotCapture.Png.cs")),
            "preview PNG capture is consolidated into PreviewScreenshotCapture.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewScreenshotCapture.Bmp.cs")),
            "preview BMP capture is consolidated into PreviewScreenshotCapture.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotStaging.cs")),
            "renderer screenshot staging is consolidated into D3D11PreviewRenderer.RenderPasses.cs");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");

        var leadingMethod = captureType.GetMethod("CountLeadingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountLeadingBlackEdges not found.");
        var trailingMethod = captureType.GetMethod("CountTrailingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountTrailingBlackEdges not found.");

        var values1 = new[] { true, true, false, true, false };
        AssertEqual(2, (int)leadingMethod.Invoke(null, new object[] { values1 })!, "Leading: 2 black edges");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { values1 })!, "Trailing: 0 black edges");

        var values2 = new[] { false, false, true, true, true };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { values2 })!, "Leading: 0");
        AssertEqual(3, (int)trailingMethod.Invoke(null, new object[] { values2 })!, "Trailing: 3");

        var allTrue = new[] { true, true, true, true, true };
        AssertEqual(5, (int)leadingMethod.Invoke(null, new object[] { allTrue })!, "All true leading");
        AssertEqual(5, (int)trailingMethod.Invoke(null, new object[] { allTrue })!, "All true trailing");

        var allFalse = new[] { false, false, false };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { allFalse })!, "All false leading");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { allFalse })!, "All false trailing");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries()
    {
        var encoderType = RequireType("Sussudio.Services.Preview.PreviewPng16Encoder");
        var method = encoderType.GetMethod("InitPngCrc32Table",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("InitPngCrc32Table not found.");

        var table = (uint[])method.Invoke(null, null)!;
        AssertEqual(256, table.Length, "CRC32 table has 256 entries");
        AssertEqual(0u, table[0], "CRC32 table[0] = 0");

        var unique = new HashSet<uint>(table);
        AssertEqual(256, unique.Count, "All 256 entries are unique");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PreviewPngCapture_Writes16BitRgbPng()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");
        var method = captureType.GetMethod(
            "CaptureFrameBufferTo16BitPng",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng not found.");

        var outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-preview-png-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputRoot, "preview", "frame.png");
        try
        {
            var format = ParseEnum("Vortice.DXGI.Format", "B8G8R8A8_UNorm");
            var result = method.Invoke(
                null,
                new object[]
                {
                    new byte[] { 0x30, 0x20, 0x10, 0xFF },
                    4,
                    1,
                    1,
                    outputPath,
                    "UnitTest",
                    format
                })
                ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng returned null.");

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "PNG capture succeeded");
            AssertEqual(1, GetIntProperty(result, "CapturedWidth"), "PNG captured width");
            AssertEqual(1, GetIntProperty(result, "CapturedHeight"), "PNG captured height");
            AssertEqual(outputPath, GetStringProperty(result, "FilePath"), "PNG output path");

            var bytes = File.ReadAllBytes(outputPath);
            AssertEqual(137, (int)bytes[0], "PNG signature byte 0");
            AssertEqual(80, (int)bytes[1], "PNG signature byte 1");
            AssertEqual(78, (int)bytes[2], "PNG signature byte 2");
            AssertEqual(71, (int)bytes[3], "PNG signature byte 3");
            AssertEqual((byte)'I', bytes[12], "PNG IHDR I");
            AssertEqual((byte)'H', bytes[13], "PNG IHDR H");
            AssertEqual((byte)'D', bytes[14], "PNG IHDR D");
            AssertEqual((byte)'R', bytes[15], "PNG IHDR R");
            AssertEqual(1, (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19], "PNG IHDR width");
            AssertEqual(1, (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23], "PNG IHDR height");
            AssertEqual(16, (int)bytes[24], "PNG bit depth");
            AssertEqual(2, (int)bytes[25], "PNG color type");
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PreviewPngCapture_RefusesExistingFile()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");
        var method = captureType.GetMethod(
            "CaptureFrameBufferTo16BitPng",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng not found.");

        var outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-preview-png-existing-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputRoot, "preview", "frame.png");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, "existing screenshot");
            var originalBytes = File.ReadAllBytes(outputPath);
            var format = ParseEnum("Vortice.DXGI.Format", "B8G8R8A8_UNorm");
            var refusedExistingFile = false;
            try
            {
                method.Invoke(
                    null,
                    new object[]
                    {
                        new byte[] { 0x30, 0x20, 0x10, 0xFF },
                        4,
                        1,
                        1,
                        outputPath,
                        "UnitTest",
                        format
                    });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is IOException)
            {
                refusedExistingFile = true;
            }

            AssertEqual(true, refusedExistingFile, "PNG capture refuses existing output path");
            AssertSequenceEqual(originalBytes, File.ReadAllBytes(outputPath), "existing PNG output remains unchanged");
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PreviewBmpCapture_RefusesExistingFile()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");
        var method = captureType.GetMethod(
            "CaptureMappedFrameToBmp",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureMappedFrameToBmp not found.");
        var mappedType = RequireType("Vortice.Direct3D11.MappedSubresource");

        var outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-preview-bmp-existing-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputRoot, "preview", "frame.bmp");
        var pixelPointer = IntPtr.Zero;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, "existing screenshot");
            var originalBytes = File.ReadAllBytes(outputPath);
            var pixelBytes = new byte[] { 0x30, 0x20, 0x10, 0xFF };
            pixelPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(pixelBytes.Length);
            System.Runtime.InteropServices.Marshal.Copy(pixelBytes, 0, pixelPointer, pixelBytes.Length);
            var mapped = Activator.CreateInstance(mappedType, pixelPointer, 4u, 4u)
                ?? throw new InvalidOperationException("MappedSubresource constructor returned null.");
            var format = ParseEnum("Vortice.DXGI.Format", "B8G8R8A8_UNorm");
            var refusedExistingFile = false;
            try
            {
                method.Invoke(
                    null,
                    new object[]
                    {
                        mapped,
                        1,
                        1,
                        outputPath,
                        "UnitTest",
                        format
                    });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is IOException)
            {
                refusedExistingFile = true;
            }

            AssertEqual(true, refusedExistingFile, "BMP capture refuses existing output path");
            AssertSequenceEqual(originalBytes, File.ReadAllBytes(outputPath), "existing BMP output remains unchanged");
        }
        finally
        {
            if (pixelPointer != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelPointer);
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderThreadLivesInRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = rootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 preview render-thread orchestration is folded into the renderer root");
        AssertContains(rootText, "private void RenderThreadMain()");
        AssertContains(rootText, "MmcssThreadRegistration.TryRegister");
        AssertContains(rootText, "_frameReadyEvent.Wait");
        AssertContains(rootText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(rootText, "TryApplyPendingCompositionTransformOnRenderThread(out var skipFrameDispatch)");
        AssertContains(rootText, "if (skipFrameDispatch)");
        AssertContains(rootText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(rootText, "CleanupRenderThreadExit();");
        AssertContains(rootText, "NotifyRenderThreadFailed(ex);");
        AssertContains(rootText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(rootText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(rootText, "UnbindSwapChainFromPanel();");
        AssertContains(rootText, "InitializeD3D();");
        AssertContains(rootText, "private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)");
        AssertContains(rootText, "skipFrameDispatch = true;");
        AssertContains(rootText, "if (Volatile.Read(ref _stopRequested) != 0)");
        AssertContains(rootText, "ApplyCompositionScaleTransform(swapChain);");
        AssertContains(rootText, "HandleDeviceLost(ex);");
        AssertContains(rootText, "private bool ProcessRenderThreadFrameOrIdle()");
        AssertContains(rootText, "WaitForFrameLatencySignal();");
        AssertContains(rootText, "RenderFrame(frame);");
        AssertContains(rootText, "TrackFrameDropped(frame, \"render-failed\");");
        AssertContains(rootText, "SignalFrameReady(\"render_loop_drain\");");
        AssertContains(rootText, "private void CleanupRenderThreadExit()");
        AssertContains(rootText, "TrackFrameDropped(stale, \"renderer-exit\");");
        AssertContains(rootText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        AssertDoesNotContain(agentMapText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertDoesNotContain(agentMapText, "D3D11PreviewRenderer.Metrics.cs");
        AssertContains(agentMapText, "D3D11PreviewRenderer.cs");
        AssertContains(agentMapText, "shared-device reset consumption/rebind");
        AssertContains(agentMapText, "queued-frame render dispatch");
        AssertDoesNotContain(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertDoesNotContain(cleanupPlanText, "D3D11PreviewRenderer.Metrics.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.cs");
        AssertContains(cleanupPlanText, "shared-device reset/rebind consumption");
        AssertContains(cleanupPlanText, "pending-frame render dispatch");
        AssertContains(diagnosticsText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertContains(diagnosticsText, "private long _renderThreadFailureCount;");
        AssertContains(diagnosticsText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(diagnosticsText, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(diagnosticsText, "private int _firstFrameRaised;");
        AssertContains(diagnosticsText, "private void ResetFirstFrameNotification()");
        AssertContains(diagnosticsText, "private void NotifyFirstFrameRendered(string message)");
        AssertContains(diagnosticsText, "FirstFrameRendered?.Invoke()");
        AssertContains(rootText, "ResetFirstFrameNotification();");
        AssertContains(renderPassesText, "NotifyFirstFrameRendered(firstFrameMessage);");
        var waitIndex = rootText.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var renderIndex = rootText.IndexOf("RenderFrame(frame);", StringComparison.Ordinal);
        if (waitIndex < 0 || renderIndex < 0 || waitIndex > renderIndex)
        {
            throw new InvalidOperationException("Render thread must wait for frame-latency signal before rendering the frame.");
        }

        AssertDoesNotContain(renderPassesText, "private void RenderThreadMain()");
        AssertDoesNotContain(renderPassesText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertDoesNotContain(renderPassesText, "FirstFrameRendered?.Invoke()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod(
            "IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = resourcesText;

        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertContains(deviceInitializationText, "TrackFrameDropped(stalePending, \"device-lost\");");
        AssertContains(deviceInitializationText, "ResultCode.DeviceRemoved");
        AssertContains(deviceInitializationText, "unchecked((int)0x887A0005)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DeviceLost.cs")),
            "D3D11 preview device-lost recovery folded into D3D resource ownership");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentAccountingLivesWithRenderPasses()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Present.cs")),
            "D3D11 preview present/accounting lives with render-pass execution");
        AssertContains(renderPassesText, "private void PresentAndTrackFrame(");
        AssertContains(renderPassesText, "TryCaptureFrameBeforePresent(rendererMode);");
        AssertContains(renderPassesText, "var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);");
        AssertContains(renderPassesText, "TrackPresentCadence(frame.CountForPresentCadence);");
        AssertContains(renderPassesText, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(renderPassesText, "RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);");
        var captureIndex = renderPassesText.IndexOf("TryCaptureFrameBeforePresent(rendererMode);", StringComparison.Ordinal);
        var presentIndex = renderPassesText.IndexOf("var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);", StringComparison.Ordinal);
        if (captureIndex < 0 || presentIndex < 0 || captureIndex > presentIndex)
        {
            throw new InvalidOperationException("Present transaction must capture screenshots before swap-chain Present.");
        }

        return Task.CompletedTask;
    }


    internal static Task D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = rendererType.GetNestedType("PendingFrame", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PendingFrame nested type not found.");
        var queueType = typeof(System.Collections.Concurrent.ConcurrentQueue<>).MakeGenericType(pendingFrameType);
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_lifecycleLock", new object());
        SetPrivateField(renderer, "_pendingFrames", Activator.CreateInstance(queueType));
        SetPrivateField(renderer, "_frameReadyEvent", new System.Threading.ManualResetEventSlim(false));
        SetPrivateField(renderer, "_renderThread", System.Threading.Thread.CurrentThread);
        SetPrivateField(renderer, "_maxPendingFrames", 4);

        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 101L, 1001L) });
        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 102L, 1002L) });

        AssertEqual(2, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count before drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesSubmitted")), "frames submitted before drain");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped before drain");

        var dropMethod = rendererType.GetMethod("DropPendingFrames", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DropPendingFrames method not found.");
        var dropped = Convert.ToInt32(dropMethod.Invoke(renderer, new object[] { "flashback-go-live" }));

        AssertEqual(2, dropped, "pending frames drained");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count after drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped after drain");
        AssertEqual(1L, GetLongPrivateField(renderer, "_submissionGeneration"), "submission generation after drain");
        AssertEqual("flashback-go-live", GetStringPrivateField(renderer, "_submissionGenerationDropReason"), "submission generation reason");

        var ownership = rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(renderer, Array.Empty<object>())
            ?? throw new InvalidOperationException("GetFrameOwnershipMetrics returned null.");
        AssertEqual("flashback-go-live", GetPropertyValue(ownership, "LastDropReason") as string, "last D3D drop reason");
        AssertEqual(1002L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedPreviewPresentId")), "last dropped preview present id");
        AssertEqual(102L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedSourceSequenceNumber")), "last dropped source sequence");

        var staleFrame = CreateRawPendingD3DFrame(pendingFrameType, 103L, 1003L);
        pendingFrameType.GetProperty("SubmissionGeneration", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(staleFrame, 0L);
        var staleGeneration = Convert.ToInt64(pendingFrameType.GetProperty("SubmissionGeneration")!.GetValue(staleFrame));
        AssertEqual(true, staleGeneration != GetLongPrivateField(renderer, "_submissionGeneration"), "stale frame generation is rejected by render loop contract");
        ((IDisposable)staleFrame).Dispose();

        return Task.CompletedTask;

        static object CreateRawPendingD3DFrame(Type pendingFrameType, long sourceSequenceNumber, long previewPresentId)
        {
            var constructor = pendingFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single(ctor => ctor.GetParameters().Any(parameter => parameter.Name == "rawData"));
            var args = constructor.GetParameters()
                .Select(parameter =>
                {
                    if (string.Equals(parameter.Name, "rawData", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    if (string.Equals(parameter.Name, "rawDataLength", StringComparison.Ordinal))
                    {
                        return 0;
                    }

                    if (string.Equals(parameter.Name, "width", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "height", StringComparison.Ordinal))
                    {
                        return 16;
                    }

                    if (string.Equals(parameter.Name, "isHdr", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (string.Equals(parameter.Name, "arrivalTick", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "schedulerSubmitTick", StringComparison.Ordinal))
                    {
                        return Stopwatch.GetTimestamp();
                    }

                    if (string.Equals(parameter.Name, "sourceSequenceNumber", StringComparison.Ordinal))
                    {
                        return sourceSequenceNumber;
                    }

                    if (string.Equals(parameter.Name, "previewPresentId", StringComparison.Ordinal))
                    {
                        return previewPresentId;
                    }

                    return parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                })
                .ToArray();
            return constructor.Invoke(args)
                   ?? throw new InvalidOperationException("PendingFrame constructor returned null.");
        }
    }

    internal static Task D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest()
    {
        var rendererText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var captureMethod = ExtractTextBetween(
            rendererText,
            "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)",
            "    private void TryCaptureFrameBeforePresent");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureMethod, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(captureMethod, "Preview frame capture canceled.");
        AssertContains(captureMethod, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(captureMethod, "cancellationToken.Register(");
        AssertContains(captureMethod, "Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request)");
        AssertContains(captureMethod, "Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);");
        AssertContains(captureMethod, "PREVIEW_FRAME_CAPTURE_CANCELED");
        AssertContains(captureMethod, "_ = request.Task.ContinueWith(");
        AssertContains(captureServiceText, "return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertDoesNotContain(captureServiceText, "cancellationToken.ThrowIfCancellationRequested();\n        return d3dSink.CaptureNextFrameAsync(outputPath);");

        return Task.CompletedTask;
    }

    internal static Task SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock()
    {
        var managerType = RequireType("Sussudio.Services.Preview.SharedD3DDeviceManager");
        AssertNotNull(
            managerType.GetMethod("TryCreateDeviceReference", BindingFlags.Public | BindingFlags.Instance),
            "SharedD3DDeviceManager.TryCreateDeviceReference");

        var managerText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n");
        var duplicateMethod = ExtractTextBetween(
            managerText,
            "public bool TryCreateDeviceReference",
            "\n    public void Dispose()");
        var disposeMethod = ExtractTextBetween(
            managerText,
            "public void Dispose()",
            "\n    private void Initialize()");
        var applyMethod = ExtractTextBetween(
            captureServiceText,
            "private void TryApplySharedPreviewDevice",
            "\n    private async Task DisposeTransientRecordingBackendAsync");

        AssertContains(managerText, "private readonly object _sync = new();");
        AssertContains(duplicateMethod, "lock (_sync)");
        AssertContains(duplicateMethod, "if (Volatile.Read(ref _disposed) != 0)");
        AssertContains(duplicateMethod, "var nativePointer = currentDevice.NativePointer;");
        AssertContains(duplicateMethod, "Marshal.AddRef(nativePointer);");
        AssertContains(duplicateMethod, "device = new ID3D11Device(nativePointer);");
        AssertContains(disposeMethod, "lock (_sync)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "SharedD3DDeviceManager.cs")),
            "shared D3D device manager lives with D3D resource ownership");
        AssertContains(applyMethod, "d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason)");
        AssertContains(applyMethod, "UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
        AssertContains(applyMethod, "sharedDevice.Dispose();");
        AssertDoesNotContain(applyMethod, "capture.D3DManager?.Device");

        return Task.CompletedTask;
    }

private readonly record struct D3D11PreviewRendererDiagnosticsContractSources(
        string Source,
        string RenderSource,
        string CaptureSource);

    internal static Task D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var sources = ReadD3D11PreviewRendererDiagnosticsContractSources();
        var source = sources.Source;
        var renderSource = sources.RenderSource;
        var captureSource = sources.CaptureSource;
        var allRendererSource = source
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs");
        AssertContains(source, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(source, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(source, "private long _dxgiFrameStatisticsFrameCounter;");
        AssertContains(source, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(source, "public PipelineLatencyMetrics GetPipelineLatencyMetrics()");
        AssertContains(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        lock (_pipelineLatencyLock)");
        AssertDoesNotContain(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        return GetPipelineLatencyMetrics().AverageMs;\n    }");
        AssertContains(source, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(renderSource, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(renderSource, "TrackPipelineLatency(frame.ArrivalTick, estimatedVisibleTick);");
        AssertContains(source, "var sorted = (double[])samples.Clone();");
        AssertContains(source, "Array.Sort(sorted);");
        AssertContains(source, "var frameCounter = Interlocked.Increment(ref _dxgiFrameStatisticsFrameCounter);");
        AssertContains(source, "frameCounter % _dxgiFrameStatisticsSampleIntervalFrames != 0");
        AssertContains(source, "_dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;");
        AssertContains(source, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(source, "private int _pendingFrameCount;");
        AssertContains(source, "public int PendingFrameCount => Math.Max(0, Volatile.Read(ref _pendingFrameCount));");
        AssertContains(source, "public event Action<string>? RenderThreadFailed;");
        AssertContains(source, "public long RenderThreadFailureCount => Interlocked.Read(ref _renderThreadFailureCount);");
        AssertContains(source, "public string LastRenderThreadFailureMessage => Volatile.Read(ref _lastRenderThreadFailureMessage);");
        AssertContains(renderSource, "NotifyRenderThreadFailed(ex);");
        AssertContains(renderSource, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(source, "IPreviewFrameQueueControl");
        AssertContains(source, "public int DropPendingFrames(string reason)");
        AssertContains(source, "Interlocked.Increment(ref _submissionGeneration);");
        AssertContains(source, "frame.SubmissionGeneration = Interlocked.Read(ref _submissionGeneration);");
        AssertContains(source, "var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);\n            _pendingFrames.Enqueue(frame);");
        AssertContains(source, "private void SignalFrameReady(string operation)");
        AssertContains(source, "private void ResetFrameReady(string operation)");
        AssertContains(source, "D3D11_PREVIEW_FRAME_SIGNAL_SKIPPED");
        AssertContains(source, "D3D11_PREVIEW_FRAME_RESET_SKIPPED");
        AssertContains(source, "SignalFrameReady(\"pending_frame\");");
        AssertContains(renderSource, "SignalFrameReady(\"render_loop_drain\");");
        AssertEqual(1, allRendererSource.Split("_frameReadyEvent.Set();", StringSplitOptions.None).Length - 1, "All D3D frame-ready signals go through SignalFrameReady");
        AssertEqual(1, allRendererSource.Split("_frameReadyEvent.Reset();", StringSplitOptions.None).Length - 1, "All D3D frame-ready resets go through ResetFrameReady");
        AssertContains(source, "private bool TryDequeuePendingFrame(out PendingFrame frame)");
        AssertContains(source, "DecrementPendingFrameCount();");
        AssertDoesNotContain(source, "_pendingFrames.Count");
        AssertDoesNotContain(source, "_pendingFrames.Enqueue(frame);\n            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);");
        AssertContains(source, "private void TrackFrameDropped(PendingFrame frame, string reason)\n    {\n        Interlocked.Increment(ref _framesDropped);");
        AssertContains(source, "Interlocked.Exchange(ref _lastSubmittedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(source, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(source, "Interlocked.Exchange(ref _lastDroppedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertDoesNotContain(source, "TrackFrameDropped(frame, \"renderer-stopped\");\n                frame.Dispose();\n                Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(source, "TrackFrameDropped(oldest, \"renderer-backlog\");\n                    oldest.Dispose();\n                    Interlocked.Increment(ref _framesDropped);");
        AssertContains(source, "_pendingFrames.TryDequeue(out var dequeued)");
        AssertDoesNotContain(renderSource, "_pendingFrames.TryDequeue(out var frame)");
        AssertContains(renderSource, "var framesRenderedBefore = Interlocked.Read(ref _framesRendered);");
        AssertContains(renderSource, "frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration)");
        AssertContains(renderSource, "if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)\n            {\n                TrackFrameDropped(frame, \"render-skipped\");\n            }");
        AssertContains(captureSource, "DropPendingPreviewFrames(\"live-preview-suppressed\")");
        AssertContains(captureSource, "DropPendingPreviewFrames(\"live-preview-resumed\")");
        AssertContains(captureSource, "queueControl.DropPendingFrames(reason)");
        AssertContains(captureSource, "private long _livePreviewPresentId;");
        AssertContains(captureSource, "var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);");
        AssertContains(captureSource, "SourceSequenceNumber = sourceSequence");
        AssertContains(captureSource, "PreviewPresentId = previewPresentId");
        AssertContains(captureSource, "SchedulerSubmitTick = submitTick");
        AssertNotNull(rendererType.GetProperty("SwapChainAddress", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.SwapChainAddress");
        AssertNotNull(rendererType.GetMethod("DropPendingFrames", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.DropPendingFrames");
        AssertNotNull(rendererType.GetMethod("GetRenderCpuTimingMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetRenderCpuTimingMetrics");
        AssertNotNull(rendererType.GetMethod("GetPipelineLatencyMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetPipelineLatencyMetrics");
        AssertNotNull(rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetFrameOwnershipMetrics");
        AssertNotNull(rendererType.GetMethod("GetDxgiFrameStatisticsMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetDxgiFrameStatisticsMetrics");
        AssertNotNull(rendererType.GetMethod("TryGetDisplayClock", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.TryGetDisplayClock");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+PresentCadenceMetrics");

        var expectedProps = new[]
        {
            "SampleCount", "ObservedFps", "ExpectedIntervalMs", "AverageIntervalMs",
            "P95IntervalMs", "P99IntervalMs", "MaxIntervalMs", "OnePercentLowFps", "JitterStdDevMs", "SlowFrameCount", "SlowFramePercent"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"PresentCadenceMetrics.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_presentCadenceLock", new object());
        SetPrivateField(renderer, "_presentIntervalWindowMs", new double[8]);

        var getMetrics = rendererType.GetMethod("GetPresentCadenceMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics not found.");

        var fakeStepTicks = System.Diagnostics.Stopwatch.Frequency / 60;

        SetPrivateField(renderer, "_lastPresentTick", 0L);
        InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true });

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var firstInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, firstInterval > 0, "first measured cadence interval is recorded");

        var metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "sample count after first measured interval");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var suppressedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { false }));
        AssertEqual(0.0, suppressedInterval, "suppressed present does not report interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after suppressed present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "suppressed present does not add a sample");
        AssertEqual(1L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "suppressed present marks baseline pending");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var baselineInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(0.0, baselineInterval, "first measured present after suppression resets baseline");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after baseline present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "baseline reset does not add transition gap sample");
        AssertEqual(0L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "baseline pending flag clears after measured present");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var resumedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, resumedInterval > 0, "second measured present after suppression records interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after resumed present.");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "measured cadence resumes after suppression baseline");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties()
    {
        var rootModelText = ReadRepoFile("Sussudio/Models/Automation/AutomationModels.cs");

        AssertContains(rootModelText, "public sealed class PerformanceTimelineEntry");
        AssertContains(rootModelText, "public double PreviewCadenceSlowFramePercent { get; init; }");
        AssertContains(rootModelText, "public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;");
        AssertContains(rootModelText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(rootModelText, "public double FlashbackExportThroughputBytesPerSec { get; init; }");
        AssertContains(rootModelText, "public double ProcessCpuPercent { get; init; }");
        AssertDoesNotContain(rootModelText, "partial class PerformanceTimelineEntry");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation", "PerformanceTimelineEntry.cs")),
            "performance timeline DTO folded into AutomationModels.cs");

        var performanceTimelineEntryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");
        foreach (var prop in new[]
                 {
                     "PreviewCadenceSlowFramePercent",
                     "PreviewCadenceOnePercentLowFps",
                     "MjpegPreviewJitterEnabled",
                     "MjpegPreviewJitterTargetDepth",
                     "MjpegPreviewJitterMaxDepth",
                     "MjpegPreviewJitterQueueDepth",
                     "MjpegPreviewJitterTotalDropped",
                     "MjpegPreviewJitterDeadlineDropCount",
                     "MjpegPreviewJitterClearedDropCount",
                     "MjpegPreviewJitterUnderflowCount",
                     "MjpegPreviewJitterResumeReprimeCount",
                     "MjpegPreviewJitterLatencyP95Ms",
                     "MjpegPreviewJitterLatencyMaxMs",
                     "MjpegPreviewJitterLastDropReason",
                     "PreviewD3DPendingFrameCount",
                     "PreviewD3DPresentCallP95Ms",
                     "PreviewD3DTotalFrameCpuP95Ms",
                     "PreviewD3DInputUploadCpuP99Ms",
                     "PreviewD3DRenderSubmitCpuP99Ms",
                     "PreviewD3DPresentCallP99Ms",
                     "PreviewD3DTotalFrameCpuP99Ms",
                     "PreviewD3DPipelineLatencyP95Ms",
                     "PreviewD3DPipelineLatencyP99Ms",
                     "PreviewD3DPipelineLatencyMaxMs",
                     "PreviewD3DFrameLatencyWaitTimeoutCount",
                     "PreviewD3DFrameLatencyWaitP95Ms",
                     "PreviewD3DFrameLatencyWaitMaxMs",
                     "PreviewD3DFrameStatsRecentMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentFailureCount",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastRenderedPipelineLatencyMs",
                     "PreviewD3DLastDropReason",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "FlashbackPlaybackState",
                     "FlashbackPlaybackP99FrameMs",
                     "FlashbackPlaybackDecodeP99Ms",
                     "FlashbackPlaybackMaxDecodePhase",
                     "FlashbackPlaybackMaxDecodeReceiveMs",
                     "FlashbackPlaybackMaxDecodeFeedMs",
                     "FlashbackPlaybackMaxDecodeReadMs",
                     "FlashbackPlaybackMaxDecodeSendMs",
                     "FlashbackPlaybackMaxDecodeAudioMs",
                     "FlashbackPlaybackMaxDecodeConvertMs",
                     "FlashbackPlaybackPendingCommands",
                     "FlashbackPlaybackSeekCommandsCoalesced",
                     "FlashbackPlaybackSubmitFailures",
                     "FlashbackPlaybackLastDropUtcUnixMs",
                     "FlashbackPlaybackLastDropReason",
                     "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
                     "FlashbackPlaybackLastSubmitFailure",
                     "FlashbackPlaybackAudioMasterDelayDoubles",
                     "FlashbackPlaybackAudioMasterDelayShrinks",
                     "FlashbackPlaybackAudioMasterFallbacks",
                     "FlashbackPlaybackSegmentSwitches",
                     "FlashbackPlaybackFmp4Reopens",
                     "FlashbackPlaybackWriteHeadWaits",
                     "FlashbackPlaybackNearLiveSnaps",
                     "FlashbackPlaybackDecodeErrorSnaps",
                     "FlashbackPlaybackLastWriteHeadWaitGapMs",
                     "FlashbackPlaybackLastCommandFailureUtcUnixMs",
                     "FlashbackPlaybackLastCommandFailure",
                     "FlashbackVideoQueueRejectedFrames",
                     "FlashbackVideoQueueLastRejectReason",
                     "FlashbackGpuQueueRejectedFrames",
                     "FlashbackGpuQueueLastRejectReason",
                     "FlashbackBackendSettingsStale",
                     "FlashbackBackendSettingsStaleReason",
                     "FlashbackBackendActiveFormat",
                     "FlashbackBackendRequestedFormat",
                     "FlashbackBackendActivePreset",
                     "FlashbackBackendRequestedPreset",
                     "FatalCleanupInProgress",
                     "FlashbackCleanupInProgress",
                     "FlashbackExportActive",
                     "FlashbackExportStatus",
                     "FlashbackExportFailureKind",
                     "FlashbackExportPercent",
                     "FlashbackExportInPointMs",
                     "FlashbackExportOutPointMs",
                     "FlashbackExportMessage",
                     "FlashbackExportForceRotateFallbacks",
                     "FlashbackExportLastForceRotateFallbackUtcUnixMs",
                     "FlashbackExportLastForceRotateFallbackSegments",
                     "FlashbackExportLastForceRotateFallbackInPointMs",
                     "FlashbackExportLastForceRotateFallbackOutPointMs",
                     "FlashbackExportThroughputBytesPerSec",
                     "FlashbackExportLastProgressAgeMs",
                     "ProcessCpuPercent"
                 })
        {
            AssertNotNull(performanceTimelineEntryType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DiagnosticsContract_SnapshotModelsExposeExpectedProperties()
    {
        var displayClockSnapshotType = RequireType("Sussudio.Services.Preview.PreviewDisplayClockSnapshot");
        foreach (var prop in new[] { "LastPresentTick", "FrameIntervalTicks", "ExpectedFrameIntervalMs", "SampleCount" })
        {
            AssertNotNull(displayClockSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewDisplayClockSnapshot.{prop}");
        }

        var stageTimingType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+CpuStageTimingMetrics");
        foreach (var prop in new[] { "SampleCount", "AverageMs", "P95Ms", "P99Ms", "MaxMs" })
        {
            AssertNotNull(stageTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"CpuStageTimingMetrics.{prop}");
        }

        var renderTimingType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+RenderCpuTimingMetrics");
        foreach (var prop in new[] { "InputUpload", "RenderSubmit", "PresentCall", "TotalFrame" })
        {
            AssertNotNull(renderTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"RenderCpuTimingMetrics.{prop}");
        }

        var pipelineLatencyType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+PipelineLatencyMetrics");
        foreach (var prop in new[] { "SampleCount", "AverageMs", "P95Ms", "P99Ms", "MaxMs" })
        {
            AssertNotNull(pipelineLatencyType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PipelineLatencyMetrics.{prop}");
        }

        var ownershipMetricsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+FrameOwnershipMetrics");
        var previewSinkType = RequireType("Sussudio.Services.Contracts.IPreviewFrameSink");
        var trackingType = RequireType("Sussudio.Services.Contracts.PreviewFrameTracking");
        foreach (var prop in new[] { "SourceSequenceNumber", "PreviewPresentId", "SourcePtsTicks", "ArrivalTick", "SchedulerSubmitTick", "CountForPresentCadence" })
        {
            AssertNotNull(trackingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewFrameTracking.{prop}");
        }

        var submitTexture = previewSinkType.GetMethod("SubmitTexture", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitTexture was not found.");
        AssertEqual(true, submitTexture.GetParameters().Any(parameter => parameter.ParameterType == trackingType), "SubmitTexture tracking parameter");
        var submitNv12PlaneTextures = previewSinkType.GetMethod("SubmitNv12PlaneTextures", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitNv12PlaneTextures was not found.");
        AssertEqual(true, submitNv12PlaneTextures.GetParameters().Any(parameter => parameter.ParameterType == trackingType), "SubmitNv12PlaneTextures tracking parameter");
        foreach (var prop in new[]
                 {
                     "LastSubmittedPreviewPresentId",
                     "LastSubmittedSourceSequenceNumber",
                     "LastSubmittedSourcePtsTicks",
                     "LastSubmittedUtcUnixMs",
                     "LastRenderedPreviewPresentId",
                     "LastRenderedSourceSequenceNumber",
                     "LastRenderedSourcePtsTicks",
                     "LastRenderedUtcUnixMs",
                     "LastRenderedSchedulerToPresentMs",
                     "LastRenderedPipelineLatencyMs",
                     "LastDroppedPreviewPresentId",
                     "LastDroppedSourceSequenceNumber",
                     "LastDroppedSourcePtsTicks",
                     "LastDroppedUtcUnixMs",
                     "LastDropReason"
                 })
        {
            AssertNotNull(ownershipMetricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"FrameOwnershipMetrics.{prop}");
        }

        var dxgiFrameStatsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+DxgiFrameStatisticsMetrics");
        foreach (var prop in new[]
                 {
                     "SampleCount",
                     "SuccessCount",
                     "FailureCount",
                     "LastError",
                     "PresentCount",
                     "PresentRefreshCount",
                     "SyncRefreshCount",
                     "SyncQpcTime",
                     "LastPresentDelta",
                     "LastPresentRefreshDelta",
                     "LastSyncRefreshDelta",
                     "MissedRefreshCount"
                 })
        {
            AssertNotNull(dxgiFrameStatsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"DxgiFrameStatisticsMetrics.{prop}");
        }

        var previewSnapshotType = RequireType("Sussudio.Models.PreviewRuntimeSnapshot");
        foreach (var prop in new[]
                 {
                     "D3DSwapChainAddress",
                     "D3DPresentSyncInterval",
                     "D3DMaxFrameLatency",
                     "D3DSwapChainBufferCount",
                     "D3DPendingFrameCount",
                     "DisplayCadenceP99IntervalMs",
                     "DisplayCadenceOnePercentLowFps",
                     "D3DCpuTimingSampleCount",
                     "D3DInputUploadCpuP95Ms",
                     "D3DInputUploadCpuP99Ms",
                     "D3DRenderSubmitCpuP95Ms",
                     "D3DRenderSubmitCpuP99Ms",
                     "D3DPresentCallP95Ms",
                     "D3DPresentCallP99Ms",
                     "D3DTotalFrameCpuP95Ms",
                     "D3DTotalFrameCpuP99Ms",
                     "D3DPipelineLatencySampleCount",
                     "D3DPipelineLatencyAvgMs",
                     "D3DPipelineLatencyP95Ms",
                     "D3DPipelineLatencyP99Ms",
                     "D3DPipelineLatencyMaxMs",
                     "D3DFrameLatencyWaitEnabled",
                     "D3DFrameLatencyWaitHandleActive",
                     "D3DFrameLatencyWaitCallCount",
                     "D3DFrameLatencyWaitSignaledCount",
                     "D3DFrameLatencyWaitTimeoutCount",
                     "D3DFrameLatencyWaitUnexpectedResultCount",
                     "D3DFrameLatencyWaitLastResult",
                     "D3DFrameLatencyWaitLastMs",
                     "D3DFrameLatencyWaitSampleCount",
                     "D3DFrameLatencyWaitAvgMs",
                     "D3DFrameLatencyWaitP95Ms",
                     "D3DFrameLatencyWaitP99Ms",
                     "D3DFrameLatencyWaitMaxMs",
                     "D3DFrameStatsSampleCount",
                     "D3DFrameStatsSuccessCount",
                     "D3DFrameStatsFailureCount",
                     "D3DFrameStatsLastError",
                     "D3DFrameStatsPresentCount",
                     "D3DFrameStatsPresentRefreshCount",
                     "D3DFrameStatsSyncRefreshCount",
                     "D3DFrameStatsSyncQpcTime",
                     "D3DFrameStatsLastPresentDelta",
                     "D3DFrameStatsLastPresentRefreshDelta",
                     "D3DFrameStatsLastSyncRefreshDelta",
                     "D3DFrameStatsMissedRefreshCount",
                     "D3DRenderThreadFailureCount",
                     "D3DLastRenderThreadFailureType",
                     "D3DLastRenderThreadFailureMessage",
                     "D3DLastRenderThreadFailureHResult",
                     "D3DLastSubmittedPreviewPresentId",
                     "D3DLastSubmittedSourceSequenceNumber",
                     "D3DLastSubmittedSourcePtsTicks",
                     "D3DLastSubmittedUtcUnixMs",
                     "D3DLastRenderedPreviewPresentId",
                     "D3DLastRenderedSourceSequenceNumber",
                     "D3DLastRenderedSourcePtsTicks",
                     "D3DLastRenderedUtcUnixMs",
                     "D3DLastRenderedSchedulerToPresentMs",
                     "D3DLastRenderedPipelineLatencyMs",
                     "D3DLastDroppedPreviewPresentId",
                     "D3DLastDroppedSourceSequenceNumber",
                     "D3DLastDroppedSourcePtsTicks",
                     "D3DLastDroppedUtcUnixMs",
                     "D3DLastDropReason",
                     "D3DRecentSlowFrames"
                 })
        {
            AssertNotNull(previewSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewRuntimeSnapshot.{prop}");
        }

        var slowFrameDiagnosticType = RequireType("Sussudio.Models.PreviewSlowFrameDiagnostic");
        foreach (var prop in new[]
                 {
                     "PreviewPresentId",
                     "SourceSequenceNumber",
                     "QpcTimestamp",
                     "UtcUnixMs",
                     "PresentIntervalMs",
                     "InputUploadCpuMs",
                     "RenderSubmitCpuMs",
                     "PresentCallMs",
                     "TotalFrameCpuMs",
                     "SchedulerToPresentMs",
                     "PipelineLatencyMs",
                     "ExpectedIntervalMs",
                     "DiagnosticThresholdMs",
                     "WorstOverBudgetMs",
                     "SlowReason",
                     "PendingFrameCount",
                     "DxgiPresentDelta",
                     "DxgiPresentRefreshDelta",
                     "DxgiSyncRefreshDelta",
                     "DxgiMissedRefreshCount"
                 })
        {
            AssertNotNull(slowFrameDiagnosticType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewSlowFrameDiagnostic.{prop}");
        }

        var automationSnapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        foreach (var prop in new[]
                 {
                     "PreviewD3DSwapChainAddress",
                     "PreviewD3DPresentSyncInterval",
                     "PreviewD3DMaxFrameLatency",
                     "PreviewD3DSwapChainBufferCount",
                     "PreviewD3DPendingFrameCount",
                     "PreviewCadenceP99IntervalMs",
                     "PreviewCadenceOnePercentLowFps",
                     "PreviewD3DCpuTimingSampleCount",
                     "PreviewD3DInputUploadCpuP95Ms",
                     "PreviewD3DInputUploadCpuP99Ms",
                     "PreviewD3DRenderSubmitCpuP95Ms",
                     "PreviewD3DRenderSubmitCpuP99Ms",
                     "PreviewD3DPresentCallP95Ms",
                     "PreviewD3DPresentCallP99Ms",
                     "PreviewD3DTotalFrameCpuP95Ms",
                     "PreviewD3DTotalFrameCpuP99Ms",
                     "PreviewD3DPipelineLatencySampleCount",
                     "PreviewD3DPipelineLatencyAvgMs",
                     "PreviewD3DPipelineLatencyP95Ms",
                     "PreviewD3DPipelineLatencyP99Ms",
                     "PreviewD3DPipelineLatencyMaxMs",
                     "PreviewD3DFrameLatencyWaitEnabled",
                     "PreviewD3DFrameLatencyWaitHandleActive",
                     "PreviewD3DFrameLatencyWaitCallCount",
                     "PreviewD3DFrameLatencyWaitSignaledCount",
                     "PreviewD3DFrameLatencyWaitTimeoutCount",
                     "PreviewD3DFrameLatencyWaitUnexpectedResultCount",
                     "PreviewD3DFrameLatencyWaitLastResult",
                     "PreviewD3DFrameLatencyWaitLastMs",
                     "PreviewD3DFrameLatencyWaitSampleCount",
                     "PreviewD3DFrameLatencyWaitAvgMs",
                     "PreviewD3DFrameLatencyWaitP95Ms",
                     "PreviewD3DFrameLatencyWaitP99Ms",
                     "PreviewD3DFrameLatencyWaitMaxMs",
                     "PreviewD3DFrameStatsSampleCount",
                     "PreviewD3DFrameStatsSuccessCount",
                     "PreviewD3DFrameStatsFailureCount",
                     "PreviewD3DFrameStatsLastError",
                     "PreviewD3DFrameStatsPresentCount",
                     "PreviewD3DFrameStatsPresentRefreshCount",
                     "PreviewD3DFrameStatsSyncRefreshCount",
                     "PreviewD3DFrameStatsSyncQpcTime",
                     "PreviewD3DFrameStatsLastPresentDelta",
                     "PreviewD3DFrameStatsLastPresentRefreshDelta",
                     "PreviewD3DFrameStatsLastSyncRefreshDelta",
                     "PreviewD3DFrameStatsMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentFailureCount",
                     "PreviewD3DLastSubmittedPreviewPresentId",
                     "PreviewD3DLastSubmittedSourceSequenceNumber",
                     "PreviewD3DLastSubmittedSourcePtsTicks",
                     "PreviewD3DLastSubmittedUtcUnixMs",
                     "PreviewD3DLastRenderedPreviewPresentId",
                     "PreviewD3DLastRenderedSourceSequenceNumber",
                     "PreviewD3DLastRenderedSourcePtsTicks",
                     "PreviewD3DLastRenderedUtcUnixMs",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastRenderedPipelineLatencyMs",
                     "PreviewD3DLastDroppedPreviewPresentId",
                     "PreviewD3DLastDroppedSourceSequenceNumber",
                     "PreviewD3DLastDroppedSourcePtsTicks",
                     "PreviewD3DLastDroppedUtcUnixMs",
                     "PreviewD3DLastDropReason",
                     "PreviewD3DRecentSlowFrames",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "ProcessCpuPercent",
                     "ProcessCpuTotalProcessorTimeMs"
                 })
        {
            AssertNotNull(automationSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"AutomationSnapshot.{prop}");
        }

        return Task.CompletedTask;
    }

    private static D3D11PreviewRendererDiagnosticsContractSources ReadD3D11PreviewRendererDiagnosticsContractSources()
    {
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();

        return new D3D11PreviewRendererDiagnosticsContractSources(
            source,
            renderSource,
            captureSource);
    }

    internal static Task PreviewStartupSignalsOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSignalsText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSignalCoordinatorText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewStartupReadinessSignalControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupSignalCoordinator.cs")),
            "preview startup signal coordinator folded into PreviewStartupControllers.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupReadinessSignalController.cs")),
            "preview startup readiness controller folded into PreviewStartupControllers.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupSignalsController.cs")),
            "preview startup signals controller folded into PreviewStartupControllers.cs");

        AssertContains(mainWindowText, "InitializePreviewStartupSignalCoordinator();");
        AssertContains(previewStartupSignalsText, "private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartup.Signals.cs")),
            "old marker-only preview startup signal partial removed");
        AssertContains(previewStartupSignalsText, "private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;");
        AssertContains(previewStartupSignalsText, "private void InitializePreviewStartupSignalCoordinator()");
        AssertContains(previewStartupSignalsText, "IsSignalWindowActive = IsPreviewStartupSignalWindowActive,");
        AssertContains(previewStartupSignalsText, "ConfirmFirstVisual = ConfirmPreviewFirstVisual,");
        AssertContains(previewStartupSignalsText, "GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState");
        AssertContains(previewStartupSignalsText, "private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;");
        AssertContains(previewStartupSignalsText, "private bool IsPreviewStartupSignalWindowActive()");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);");
        AssertContains(previewStartupSignalsText, "private void ResetPreviewSignalState()");
        AssertContains(previewStartupSignalsText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalsText, "private void LogPreviewStartupPlaybackSnapshot(string reason)");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.BuildMissingSignals();");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);");
        AssertContains(previewStartupSignalsText, "new PreviewStartupPlaybackSnapshotState(");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed class PreviewStartupSignalCoordinatorContext");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed record PreviewStartupPlaybackSnapshotState(");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed class PreviewStartupSignalCoordinator");
        AssertContains(previewStartupSignalCoordinatorText, "private readonly PreviewStartupReadinessSignalController _readinessSignals = new();");
        AssertContains(previewStartupSignalCoordinatorText, "private bool _expectGpuDualSignals;");
        AssertContains(previewStartupSignalCoordinatorText, "private long _positionEventCount;");
        AssertContains(previewStartupSignalCoordinatorText, "public PreviewStartupReadinessSignalSnapshot Snapshot => _readinessSignals.Snapshot;");
        AssertContains(previewStartupSignalCoordinatorText, "public long PositionEventCount => Interlocked.Read(ref _positionEventCount);");
        AssertContains(previewStartupSignalCoordinatorText, "public void Configure(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalCoordinatorText, "public void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)");
        AssertContains(previewStartupSignalCoordinatorText, "public void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)");
        AssertContains(previewStartupSignalCoordinatorText, "private void HandleGpuStartupSignalResult(PreviewStartupReadinessSignalResult? result, string signalName)");
        AssertContains(previewStartupSignalCoordinatorText, "private void TryConfirmFirstVisualFromGpuSignals(PreviewStartupReadinessSignalResult result)");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_STRATEGY");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_SIGNAL");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_WAITING");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_IGNORED");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_BASELINE");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_CHECK");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_PLAYBACK_SNAPSHOT");
        AssertContains(previewStartupReadinessSignalControllerText, "internal sealed class PreviewStartupReadinessSignalController");
        AssertContains(previewStartupReadinessSignalControllerText, "public static readonly TimeSpan PlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupReadinessSignalSnapshot Snapshot => new(");
        AssertContains(previewStartupReadinessSignalControllerText, "public string Configure(");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupReadinessSignalResult MarkSignal(");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupPlaybackPositionResult TrackPlaybackPosition(");
        AssertContains(previewStartupReadinessSignalControllerText, "PreviewStartupSignalFormatter.FormatMissingSignals(");
        AssertContains(previewStartupSignalCoordinatorText, "PreviewStartupSignalFormatter.FormatSignalList(");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter family remains a substantial adapter surface");
        AssertDoesNotContain(previewStartupSignalsText, "private readonly PreviewStartupReadinessSignalController");
        AssertDoesNotContain(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertDoesNotContain(previewStartupSignalsText, "_readinessSignals.TrackPlaybackPosition(");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_SIGNAL");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_WAITING");
        AssertDoesNotContain(previewStartupSignalsText, "private static string BuildPreviewStartupSignalList");
        AssertDoesNotContain(previewStartupSignalsText, "CurrentPreviewStartupState is PreviewStartupState.StartingSession");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupReadinessSignalController_PreservesSignalStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupReadinessSignalController");
        var signalType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        var strategyType = RequireType("Sussudio.Models.PreviewStartupStrategy");
        var statusType = RequireType("Sussudio.Controllers.PreviewStartupReadinessSignalStatus");
        var playbackStatusType = RequireType("Sussudio.Controllers.PreviewStartupPlaybackPositionStatus");

        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var configure = controllerType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.Configure was not found.");
        var markSignal = controllerType.GetMethod("MarkSignal", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.MarkSignal was not found.");
        var trackPlaybackPosition = controllerType.GetMethod("TrackPlaybackPosition", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.TrackPlaybackPosition was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.MarkFirstVisualConfirmed was not found.");
        var snapshotProperty = controllerType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.Snapshot was not found.");

        object Signals(int value) => Enum.ToObject(signalType, value);
        object Strategy(string name) => Enum.Parse(strategyType, name);
        object Status(string name) => Enum.Parse(statusType, name);
        object PlaybackStatus(string name) => Enum.Parse(playbackStatusType, name);

        var requiredSignals = Signals(1 | 2 | 4);
        var initialMissing = configure.Invoke(controller, new object[] { Strategy("D3D11VideoProcessor"), requiredSignals, true, false })?.ToString();
        AssertEqual("MediaOpened+FirstCaptureFrame+PlaybackAdvancing", initialMissing, "initial missing readiness signals");

        var mediaOpened = markSignal.Invoke(controller, new object[] { Signals(1), true, false })!;
        AssertEqual(Status("Accepted"), GetPropertyValue(mediaOpened, "Status"), "media-opened accepted");
        AssertEqual("FirstCaptureFrame+PlaybackAdvancing", GetStringProperty(mediaOpened, "MissingSignals"), "media-opened missing signals");
        AssertEqual(false, GetBoolProperty(mediaOpened, "AllRequiredSignalsReceived"), "media-opened not ready");

        var mediaSnapshot = GetPropertyValue(mediaOpened, "Snapshot")!;
        AssertEqual(true, GetBoolProperty(mediaSnapshot, "GpuSignalMediaOpened"), "media-opened snapshot flag");
        AssertEqual(Signals(1), GetPropertyValue(mediaSnapshot, "ReceivedSignals"), "media-opened received flags");

        var duplicate = markSignal.Invoke(controller, new object[] { Signals(1), true, false })!;
        AssertEqual(Status("Duplicate"), GetPropertyValue(duplicate, "Status"), "duplicate media-opened status");

        var playback = trackPlaybackPosition.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(40), true, false })!;
        AssertEqual(PlaybackStatus("BaselineCaptured"), GetPropertyValue(playback, "Status"), "playback baseline status");
        var playbackSignal = GetPropertyValue(playback, "SignalResult")!;
        AssertEqual(Status("Accepted"), GetPropertyValue(playbackSignal, "Status"), "playback advancing accepted");
        AssertEqual("FirstCaptureFrame", GetStringProperty(playbackSignal, "MissingSignals"), "playback advancing missing signals");

        var firstFrame = markSignal.Invoke(controller, new object[] { Signals(2), true, false })!;
        AssertEqual(Status("Accepted"), GetPropertyValue(firstFrame, "Status"), "first frame accepted");
        AssertEqual(true, GetBoolProperty(firstFrame, "AllRequiredSignalsReceived"), "all required readiness signals received");
        AssertEqual(string.Empty, GetStringProperty(firstFrame, "MissingSignals"), "no missing readiness signals");

        markFirstVisualConfirmed.Invoke(controller, Array.Empty<object>());
        var finalSnapshot = snapshotProperty.GetValue(controller)!;
        AssertEqual(Signals(1 | 2 | 4 | 8), GetPropertyValue(finalSnapshot, "ReceivedSignals"), "first visual signal preserved in received flags");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupSignalFormatter_PreservesSignalStrings()
    {
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
        var signalType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        var formatSignalList = formatterType.GetMethod("FormatSignalList", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatSignalList was not found.");
        var formatMissingSignals = formatterType.GetMethod("FormatMissingSignals", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatMissingSignals was not found.");

        object Signals(int value) => Enum.ToObject(signalType, value);

        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(0) })?.ToString(), "no startup signals");
        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(16) })?.ToString(), "unknown startup signals");
        AssertEqual(
            "MediaOpened+FirstCaptureFrame+PlaybackAdvancing+FirstVisual",
            formatSignalList.Invoke(null, new[] { Signals(1 | 2 | 4 | 8) })?.ToString(),
            "startup signal order");
        AssertEqual(
            "FirstCaptureFrame+FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2 | 4 | 8), Signals(1 | 4), false })?.ToString(),
            "missing startup signals");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2), Signals(1 | 2), false })?.ToString(),
            "no missing required startup signals");
        AssertEqual(
            "FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), false })?.ToString(),
            "first visual required when no explicit startup signals exist");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), true })?.ToString(),
            "first visual confirmed with no explicit startup signals");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupFailureTextFormatter_PreservesFailureStrings()
    {
        var watchdogType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var formatTimeoutReason = watchdogType.GetMethod("FormatTimeoutReason", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatTimeoutReason was not found.");
        var formatTimeoutStatusText = watchdogType.GetMethod("FormatTimeoutStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatTimeoutStatusText was not found.");
        var formatFailureStopStatusText = watchdogType.GetMethod("FormatFailureStopStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatFailureStopStatusText was not found.");

        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, null })?.ToString(),
            "timeout reason without missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, string.Empty })?.ToString(),
            "timeout reason with empty missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "   " })?.ToString(),
            "timeout reason with whitespace missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout reason with missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { null })?.ToString(),
            "timeout status without missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "   " })?.ToString(),
            "timeout status with whitespace missing signals");
        AssertEqual(
            "Preview failed to start (missing readiness signal: FirstCaptureFrame+FirstVisual).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout status with missing signals");
        AssertEqual(
            "Preview startup failed: no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatFailureStopStatusText.Invoke(null, new object?[] { "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual" })?.ToString(),
            "failure stop status");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupWatchdogOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupWatchdogText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupWatchdogControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalFormatterText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupWatchdogController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartup.Watchdog.cs")),
            "preview startup watchdog adapter folded into the preview startup session adapter");
        AssertContains(previewStartupWatchdogText, "private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;");
        AssertContains(previewStartupWatchdogText, "private void InitializePreviewStartupWatchdogController()");
        AssertContains(previewStartupWatchdogText, "IsWaitingForFirstVisual = () => _previewStartupSessionController.IsWaitingForFirstVisual,");
        AssertContains(previewStartupWatchdogText, "private void StartPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Start();");
        AssertContains(previewStartupWatchdogText, "private void StopPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Stop();");
        AssertContains(previewStartupWatchdogText, "private void SchedulePreviewStartupFailureStop(string reason)");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ScheduleFailureStop(reason);");
        AssertContains(previewStartupWatchdogText, "private void ResetPreviewStartupFailureStopSchedule()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ResetFailureStopSchedule();");
        AssertContains(previewStartupWatchdogText, "GetTimeoutDiagnosticSnapshot = GetPreviewStartupTimeoutDiagnosticSnapshot,");
        AssertContains(previewStartupWatchdogText, "private PreviewStartupTimeoutDiagnosticSnapshot GetPreviewStartupTimeoutDiagnosticSnapshot()");
        AssertContains(previewStartupWatchdogControllerText, "internal sealed class PreviewStartupWatchdogControllerContext");
        AssertContains(previewStartupWatchdogControllerText, "internal sealed class PreviewStartupWatchdogController");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupMinVisualTimeoutMs = 1000;");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupMaxVisualTimeoutMs = 15000;");
        AssertContains(previewStartupWatchdogControllerText, "private readonly Lazy<int> _visualTimeoutMs = new(static () =>");
        AssertContains(previewStartupWatchdogControllerText, "private DispatcherQueueTimer? _watchdogTimer;");
        AssertContains(previewStartupWatchdogControllerText, "private DispatcherQueueTimer? _telemetryTimer;");
        AssertContains(previewStartupWatchdogControllerText, "private int _failureStopScheduled;");
        AssertContains(previewStartupWatchdogControllerText, "public int VisualTimeoutMs => _visualTimeoutMs.Value;");
        AssertContains(previewStartupWatchdogControllerText, "public void Start()");
        AssertContains(previewStartupWatchdogControllerText, "public void Stop()");
        AssertContains(previewStartupWatchdogControllerText, "public void ScheduleFailureStop(string reason)");
        AssertContains(previewStartupWatchdogControllerText, "public void ResetFailureStopSchedule()");
        AssertContains(previewStartupWatchdogControllerText, "private void TelemetryTimer_Tick(object? sender, object e)");
        AssertContains(previewStartupWatchdogControllerText, "private async void WatchdogTimer_Tick(object? sender, object e)");
        AssertContains(previewStartupWatchdogControllerText, "private Task HandleTimeoutAsync()");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatTimeoutReason(int timeoutMs, string? missingSignals)");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatTimeoutStatusText(string? missingSignals)");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatFailureStopStatusText(string reason)");
        AssertContains(previewStartupWatchdogControllerText, "var timeoutReason = FormatTimeoutReason(");
        AssertContains(previewStartupWatchdogControllerText, "PreviewStartupSignalFormatter.FormatTimeoutDiagnosticPayload(");
        AssertContains(previewStartupWatchdogControllerText, "_context.GetTimeoutDiagnosticSnapshot()");
        AssertContains(previewStartupWatchdogControllerText, "FormatTimeoutStatusText(_context.GetMissingSignals())");
        AssertContains(previewStartupWatchdogControllerText, "FormatFailureStopStatusText(reason)");
        AssertContains(previewStartupSignalFormatterText, "internal readonly record struct PreviewStartupTimeoutDiagnosticSnapshot");
        AssertContains(previewStartupSignalFormatterText, "public static string FormatTimeoutDiagnosticPayload(PreviewStartupTimeoutDiagnosticSnapshot snapshot)");
        AssertContains(previewStartupSignalFormatterText, "required={FormatSignalList(snapshot.RequiredSignals)}");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_WATCHDOG_STARTED");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT_IGNORED reason=user-or-shutdown-stop-requested");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT attempt={_context.GetAttemptLabel()}");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_FAILURE_STOP begin");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Preview",
                "Startup",
                "PreviewStartupFailureTextFormatter.cs")),
            "preview startup failure text formatter helper");
        AssertDoesNotContain(mainWindowText, "_previewStartupVisualTimeoutMs");
        AssertDoesNotContain(mainWindowText, "_previewStartupWatchdogTimer");
        AssertDoesNotContain(previewStartupWatchdogText, "DispatcherQueueTimer");
        AssertDoesNotContain(previewStartupWatchdogText, "Interlocked");
        AssertDoesNotContain(previewStartupWatchdogText, "EnvironmentHelpers.GetIntFromEnv");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(");
        AssertDoesNotContain(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupWatchdogTimer;");
        AssertDoesNotContain(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupTelemetryTimer;");
        AssertDoesNotContain(previewStartupWatchdogText, "private int _previewStartupFailureStopScheduled;");
        AssertDoesNotContain(previewStartupWatchdogText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupWatchdogText, "CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual");
        AssertDoesNotContain(previewStartupWatchdogText, "placeholder={NoDevicePlaceholder.Visibility}");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)");
        AssertDoesNotContain(previewStartupText, "_previewStartupFailureStopScheduled");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter family remains a substantial adapter surface");
        AssertDoesNotContain(previewStartupText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertDoesNotContain(previewStartupText, "no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms");
        AssertDoesNotContain(previewStartupText, "Preview failed to attach to UI (session started but no visual confirmation).");
        AssertDoesNotContain(previewStartupText, "Preview failed to start (missing readiness signal:");

        return Task.CompletedTask;
    }

    internal static async Task PreviewStartupWatchdogController_PreservesTimeoutContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
        var formatTimeoutDiagnosticPayload = formatterType.GetMethod("FormatTimeoutDiagnosticPayload", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatTimeoutDiagnosticPayload was not found.");
        var timeoutDiagnosticSnapshot = CreatePreviewStartupTimeoutDiagnosticSnapshot();
        AssertEqual(
            "placeholder=False gpuVisible=True cpuVisible=False strategy=D3D11VideoProcessor required=FirstCaptureFrame+FirstVisual received=None missing=FirstCaptureFrame+FirstVisual",
            formatTimeoutDiagnosticPayload.Invoke(null, new[] { timeoutDiagnosticSnapshot }),
            "timeout diagnostic payload formatting");

        var context = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => false,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1234.0,
            buildMissingSignals: () => "FirstCaptureFrame+FirstVisual",
            out var recorder);
        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;

        var timeoutTask = InvokeNonPublicInstanceMethod(controller, "HandleTimeoutAsync", null) as Task
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.HandleTimeoutAsync did not return a Task.");
        await timeoutTask.ConfigureAwait(false);

        AssertEqual("FirstCaptureFrame+FirstVisual", recorder.MissingSignals, "timeout caches missing signals");
        AssertEqual("no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual", recorder.FailureReason, "timeout failure reason");
        AssertEqual(true, recorder.OverlayStopped, "timeout stops startup overlay");
        AssertEqual("timeout", recorder.PlaybackSnapshotReasons.Single(), "timeout logs playback snapshot");
        AssertEqual("no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual", recorder.StopPreviewReasons.Single(), "timeout forces teardown");
        AssertEqual(
            "Preview failed to start (missing readiness signal: FirstCaptureFrame+FirstVisual).",
            recorder.StatusTexts[0],
            "timeout status text");
        AssertEqual(
            "Preview startup failed: no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            recorder.StatusTexts[1],
            "failure stop status text");

        var ignoredContext = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => true,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1.0,
            buildMissingSignals: () => "FirstVisual",
            out var ignoredRecorder);
        var ignoredController = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { ignoredContext }, culture: null)!;
        var ignoredTask = InvokeNonPublicInstanceMethod(ignoredController, "HandleTimeoutAsync", null) as Task
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.HandleTimeoutAsync did not return a Task.");
        await ignoredTask.ConfigureAwait(false);

        AssertEqual(0, ignoredRecorder.StatusTexts.Count, "ignored timeout does not publish status");
        AssertEqual(0, ignoredRecorder.StopPreviewReasons.Count, "ignored timeout does not stop preview");
        AssertEqual(null, ignoredRecorder.FailureReason, "ignored timeout does not mark failed");
    }

    internal static Task PreviewStartupWatchdogController_GatesFailureStopScheduling()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var scheduledOperations = new List<(Func<Task> Operation, string Name)>();
        var context = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => false,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1.0,
            buildMissingSignals: () => "FirstVisual",
            out _,
            runUiEventHandlerAsync: (operation, name) =>
            {
                scheduledOperations.Add((operation, name));
                return Task.CompletedTask;
            });
        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
        var scheduleFailureStop = controllerType.GetMethod("ScheduleFailureStop", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.ScheduleFailureStop was not found.");
        var resetFailureStopSchedule = controllerType.GetMethod("ResetFailureStopSchedule", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.ResetFailureStopSchedule was not found.");

        scheduleFailureStop.Invoke(controller, new object[] { "first" });
        scheduleFailureStop.Invoke(controller, new object[] { "second" });
        AssertEqual(1, scheduledOperations.Count, "failure stop schedules once while pending");
        AssertEqual("PreviewStartupFailureStop", scheduledOperations[0].Name, "failure stop operation name");

        resetFailureStopSchedule.Invoke(controller, null);
        scheduleFailureStop.Invoke(controller, new object[] { "third" });
        AssertEqual(2, scheduledOperations.Count, "failure stop can schedule after reset");

        return Task.CompletedTask;
    }

    private static object CreatePreviewStartupWatchdogContext(
        Func<bool> isWaitingForFirstVisual,
        Func<bool> isWindowClosing,
        Func<bool> isPreviewStopRequestedByUser,
        Func<bool> isPreviewing,
        Func<double> getElapsedMilliseconds,
        Func<string> buildMissingSignals,
        out PreviewStartupWatchdogTestRecorder recorder,
        Func<Func<Task>, string, Task>? runUiEventHandlerAsync = null)
    {
        var contextType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogControllerContext");
        var context = Activator.CreateInstance(contextType, nonPublic: true)!;
        recorder = new PreviewStartupWatchdogTestRecorder();
        var localRecorder = recorder;

        SetPropertyOrBackingField(context, "DispatcherQueue", null);
        SetPropertyOrBackingField(context, "IsWaitingForFirstVisual", isWaitingForFirstVisual);
        SetPropertyOrBackingField(context, "IsSignalWindowActive", new Func<bool>(() => true));
        SetPropertyOrBackingField(context, "IsWindowClosing", isWindowClosing);
        SetPropertyOrBackingField(context, "IsPreviewStopRequestedByUser", isPreviewStopRequestedByUser);
        SetPropertyOrBackingField(context, "IsPreviewing", isPreviewing);
        SetPropertyOrBackingField(context, "GetElapsedMilliseconds", getElapsedMilliseconds);
        SetPropertyOrBackingField(context, "GetAttemptLabel", new Func<string>(() => "attempt-test"));
        SetPropertyOrBackingField(context, "BuildMissingSignals", buildMissingSignals);
        SetPropertyOrBackingField(context, "GetMissingSignals", new Func<string?>(() => localRecorder.MissingSignals));
        SetPropertyOrBackingField(context, "SetMissingSignals", new Action<string?>(value => localRecorder.MissingSignals = value));
        SetPropertyOrBackingField(context, "MarkStartupFailed", new Action<string>(reason => localRecorder.FailureReason = reason));
        SetPropertyOrBackingField(context, "GetTimeoutDiagnosticSnapshot", CreatePreviewStartupTimeoutDiagnosticSnapshotFactory());
        SetPropertyOrBackingField(context, "LogPlaybackSnapshot", new Action<string>(reason => localRecorder.PlaybackSnapshotReasons.Add(reason)));
        SetPropertyOrBackingField(context, "StopStartupOverlay", new Action(() => localRecorder.OverlayStopped = true));
        SetPropertyOrBackingField(context, "SetStatusText", new Action<string>(value => localRecorder.StatusTexts.Add(value)));
        SetPropertyOrBackingField(context, "StopPreviewForFailureAsync", new Func<string, Task>(reason =>
        {
            localRecorder.StopPreviewReasons.Add(reason);
            return Task.CompletedTask;
        }));
        SetPropertyOrBackingField(
            context,
            "RunUiEventHandlerAsync",
            runUiEventHandlerAsync ?? new Func<Func<Task>, string, Task>((operation, _) => operation()));
        return context;
    }

    private static object CreatePreviewStartupTimeoutDiagnosticSnapshot()
    {
        var snapshotType = RequireType("Sussudio.Controllers.PreviewStartupTimeoutDiagnosticSnapshot");
        var strategyType = RequireType("Sussudio.Models.PreviewStartupStrategy");
        var signalsType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        return Activator.CreateInstance(
            snapshotType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[]
            {
                "False",
                "True",
                "False",
                Enum.Parse(strategyType, "D3D11VideoProcessor"),
                Enum.Parse(signalsType, "FirstCaptureFrame, FirstVisual"),
                Enum.Parse(signalsType, "None"),
                "FirstCaptureFrame+FirstVisual",
            },
            culture: null)!;
    }

    private static Delegate CreatePreviewStartupTimeoutDiagnosticSnapshotFactory()
    {
        var snapshot = CreatePreviewStartupTimeoutDiagnosticSnapshot();
        var snapshotType = snapshot.GetType();
        var delegateType = typeof(Func<>).MakeGenericType(snapshotType);
        return Expression.Lambda(delegateType, Expression.Constant(snapshot, snapshotType)).Compile();
    }

    private sealed class PreviewStartupWatchdogTestRecorder
    {
        public string? MissingSignals { get; set; }
        public string? FailureReason { get; set; }
        public bool OverlayStopped { get; set; }
        public List<string> PlaybackSnapshotReasons { get; } = [];
        public List<string> StatusTexts { get; } = [];
        public List<string> StopPreviewReasons { get; } = [];
    }

    internal static Task PreviewStartupLifecycleEventOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();

        AssertContains(mainWindowText, "InitializePreviewLifecycleEventController();");
        AssertContains(previewFadeInText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(previewFadeInText, "private void InitializePreviewFadeInController()");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewFadeInControllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInControllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(previewFadeInControllerText, "public void Schedule()");
        AssertContains(previewFadeInControllerText, "public void Stop()");
        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewPropertyChangedText, "private PreviewLifecycleEventController _previewLifecycleEventController = null!;");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewLifecycleControllerText, "_context.HandlePreviewReinitializingChanged();");
        AssertContains(previewLifecycleControllerText, "if (_context.ShouldBeginPreviewStartupAttempt())");
        AssertContains(previewLifecycleControllerText, "_stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;");
        AssertContains(previewLifecycleControllerText, "_context.StartPreviewStartupWatchdog();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewReinitRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "HandlePreviewReinitializingChanged(");
        AssertDoesNotContain(previewReinitText, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioTransitionControllers.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");

        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewButtonClick = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(previewButtonClick, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(previewButtonClick, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertOccursBefore(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);", "await viewModel.StopPreviewAsync(userInitiated: true);");

        var uiFadeOut = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeOutAsync");
        AssertContains(uiFadeOut, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(uiFadeOut, "To = 0,");
        AssertContains(uiFadeOut, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(uiFadeOut, "PREVIEW_AUDIO_FADE_OUT_STARTED");

        var vmStopRamp = ExtractMemberCode(previewVolumeTransitionText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "_previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioVolumeTransitionText, "RampDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(0);");

        var stopPreview = ExtractTextBetween(
            previewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "_context.RaisePreviewStopRequested();");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        var previewReinitStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "=> _previewRendererHostController.StopRendererForReinitTeardownAsync();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
    {
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");

        var initialize = ExtractMemberCode(settingsText, "InitializeAsync");
        AssertContains(initialize, "LoadSettings();");
        AssertContains(initialize, "StartRecordingCapabilityRefresh();");
        AssertContains(initialize, "return Task.CompletedTask;");
        AssertDoesNotContain(initialize, "await Task.WhenAll");
        AssertOccursBefore(initialize, "LoadSettings();", "StartRecordingCapabilityRefresh();");

        var startupRefresh = ExtractMemberCode(recordingCapabilityControllerText, "Start");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), \"recording formats\");");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), \"split encode modes\");");
        AssertDoesNotContain(recordingCapabilityControllerText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "=> _recordingCapabilityController.Start();");

        var recordingFormatRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshRecordingFormatCapabilitiesAsync");
        AssertContains(recordingFormatRefresh, "support.HasH264Nvenc");
        AssertContains(recordingFormatRefresh, "support.HasHevcNvenc");
        AssertContains(recordingFormatRefresh, "support.HasAv1Nvenc");
        AssertDoesNotContain(recordingFormatRefresh, "support.HasAv1)");

        var splitEncodeRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshSplitEncodeCapabilitiesAsync");
        AssertContains(splitEncodeRefresh, "if (!support.Supports2Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"2-way\");");
        AssertContains(splitEncodeRefresh, "if (!support.Supports3Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"3-way\");");
        AssertContains(splitEncodeRefresh, "_context.SetSelectedSplitEncodeMode(\"Auto\");");

        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");

        var refreshDevices = ExtractMemberCode(deviceRefreshControllerText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var discovery = await _context.EnumerateCaptureDeviceDiscoveryAsync()");
        AssertContains(refreshDevices, "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "ApplyStartupAudioDeviceScan(", "_context.ReplaceDevices(devices.ToList());");
        AssertOccursBefore(refreshDevices, "_context.ReplaceDevices(devices.ToList());", "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertOccursBefore(refreshDevices, "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);", "ApplySuccessfulDeviceScanAsync(");
        var successfulScan = ExtractTextBetween(
            deviceRefreshControllerText,
            "private async Task ApplySuccessfulDeviceScanAsync",
            "\n    }\n}");
        AssertOccursBefore(successfulScan, "var savedDeviceId = _context.GetPendingSavedDeviceId();", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(successfulScan, "_context.SetSelectedDevice(nextSelectedDevice);", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplySuccessfulDeviceScanAsync(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewTransitionText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");

        var previewStartRequested = ExtractMemberCode(previewLifecycleControllerText, "HandlePreviewStartRequested");
        AssertContains(previewStartRequested, "_context.BeginPreviewStartupAttempt();");
        AssertContains(previewStartRequested, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(previewStartRequested, "_context.PreparePreviewStartupPresentation();");
        AssertOccursBefore(previewStartRequested, "_context.PrimePreviewAudioFadeIn();", "_context.PreparePreviewStartupPresentation();");

        var playEntranceAnimation = ExtractMemberCode(launchEntranceShellText, "PlayEntranceAnimation");
        AssertContains(playEntranceAnimation, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(playEntranceAnimation, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(playEntranceAnimation, "Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);");

        var animatePreviewInAdapter = ExtractMemberCode(previewTransitionText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewInAdapter, "_previewTransitionAnimationController.AnimatePreviewInAsync();");

        var animatePreviewIn = ExtractMemberCode(previewTransitionControllerText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);");
        AssertContains(animatePreviewIn, "AnimatePreviewShellInAsync(350)");
        AssertContains(animatePreviewIn, "AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut)");
        AssertOccursBefore(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);", "AnimatePreviewShellInAsync(350)");

        var preparePresentation = ExtractMemberCode(previewTransitionControllerText, "PrepareStartupPresentation");
        AssertContains(preparePresentation, "FadeOutElement(_context.NoDevicePlaceholder);");
        AssertContains(preparePresentation, "_context.StartPreviewStartupOverlay();");
        AssertContains(preparePresentation, "_context.PreviewContentGrid.Opacity = 0.0;");

        var revealUnavailable = ExtractMemberCode(previewTransitionControllerText, "RevealUnavailablePlaceholder");
        AssertContains(revealUnavailable, "AnimatePreviewShellInAsync(300)");
        AssertContains(revealUnavailable, "FadeInElement(_context.NoDevicePlaceholder);");

        var primeAudioAdapter = ExtractMemberCode(previewAudioFadeText, "PrimePreviewAudioFadeIn");
        AssertContains(primeAudioAdapter, "_previewAudioFadeController.PrimeFadeIn();");

        var primeAudio = ExtractMemberCode(previewAudioFadeControllerText, "PrimeFadeIn");
        AssertContains(primeAudio, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(primeAudio, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(primeAudio, "_context.PreviewVolumeSlider.Value = 0;");

        var startAudioFadeAdapter = ExtractMemberCode(previewAudioFadeText, "StartPreviewAudioFadeIn");
        AssertContains(startAudioFadeAdapter, "_previewAudioFadeController.StartFadeIn(durationMs);");

        var startAudioFade = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeIn");
        AssertContains(startAudioFade, "Storyboard.SetTarget(volumeAnimation, _context.PreviewVolumeSlider);");
        AssertContains(startAudioFade, "CompleteFadeIn(applyTarget: true)");

        AssertContains(previewFadeInText, "=> _previewFadeInController.Schedule();");
        var schedulePreviewFadeIn = ExtractMemberCode(previewFadeInControllerText, "Schedule");
        AssertContains(schedulePreviewFadeIn, "StartPreviewAudioFadeIn();");
        AssertOccursBefore(schedulePreviewFadeIn, "_ = _context.AnimatePreviewInAsync();", "_context.StartPreviewAudioFadeIn();");

        var setupBindings = ExtractMemberCode(bindingsText, "SetupBindings");
        AssertContains(setupBindings, "ApplyInitialAudioControlBindings();");

        var initialAudioBindingsAdapter = ExtractMemberCode(audioBindingsText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindingsAdapter, "_audioControlBindingController.ApplyInitialAudioControlBindings();");

        var initialAudioBindings = ExtractMemberCode(audioControlBindingControllerText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(initialAudioBindings, "_context.CancelPreviewAudioFadeInForUser();");
        AssertOccursBefore(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();", "_context.PreviewVolumeSlider.ValueChanged +=");

        var previewButtonClick = ExtractMemberCode(previewActionsText, "PreviewButton_Click");
        AssertContains(previewButtonClick, "RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click))");
        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var togglePreviewAsync = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(togglePreviewAsync, "if (!viewModel.IsPreviewing)\n        {\n            _context.RevealPreviewUnavailablePlaceholder();\n        }");

        var mainWindowLoaded = ExtractMemberCode(startupText, "MainWindow_Loaded");
        AssertContains(mainWindowLoaded, "=> _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));");
        var launchLoaded = ExtractMemberCode(launchStartupText, "HandleLoaded");
        AssertOccursBefore(launchLoaded, "_context.PrimePreviewAudioFadeIn();", "await _context.RefreshDevicesAsync();");
        AssertContains(launchLoaded, "_context.RevealPreviewUnavailablePlaceholder();");

        AssertDoesNotContain(xamlText, "No preview available");

        return Task.CompletedTask;
    }
    internal static Task PreviewStartupSessionReinitOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = previewRendererText;
        var previewRuntimeSnapshotSamplingControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "preview reinit adapter folded into MainWindow.xaml.cs");
        AssertContains(previewStartupText, "private PreviewStartupSessionController _previewStartupSessionController = null!;");
        AssertContains(previewStartupText, "private void InitializePreviewStartupSessionController()");
        AssertContains(previewStartupText, "private PreviewStartupState CurrentPreviewStartupState");
        AssertContains(previewStartupText, "private string PreviewStartupAttemptLabel");
        AssertContains(previewStartupText, "private bool ShouldBeginPreviewStartupAttempt");
        AssertContains(previewStartupText, "new PreviewStartupSessionControllerContext");
        AssertContains(previewStartupText, "ResetSignalState = ResetPreviewSignalState,");
        AssertContains(previewStartupText, "StopWatchdog = StopPreviewStartupWatchdog,");
        AssertContains(previewStartupText, "ScheduleFadeIn = SchedulePreviewFadeIn,");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.SetStartupState(state, reason);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.BeginStartupAttempt();");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ConfirmFirstVisual(source);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);");
        AssertContains(previewStartupSessionControllerText, "internal enum PreviewStartupState");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionControllerContext");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionController");
        AssertContains(previewStartupSessionControllerText, "public PreviewStartupState State { get; private set; } = PreviewStartupState.Idle;");
        AssertContains(previewStartupSessionControllerText, "public string? AttemptId { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RequestedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RendererAttachedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? FirstVisualUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? LastFailureReason { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? MissingSignals { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public int RecoveryAttemptCount { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool FirstVisualConfirmed { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldRefreshMissingSignalsForSnapshot => IsWaitingForFirstVisual || IsFailed;");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;");
        AssertContains(previewStartupSessionControllerText, "public bool IsSignalWindowActive(bool isPreviewing)");
        AssertContains(previewStartupSessionControllerText, "public string AttemptLabel => AttemptId ?? \"none\";");
        AssertContains(previewStartupSessionControllerText, "public void BeginStartupAttempt()");
        AssertContains(previewStartupSessionControllerText, "public void SetStartupState(PreviewStartupState state, string? reason = null)");
        AssertContains(previewStartupSessionControllerText, "public void ConfirmFirstVisual(string source)");
        AssertContains(previewStartupSessionControllerText, "public void ResetStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_STATE state={state} attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_REQUESTED attempt={AttemptId}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_IGNORED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "public void MarkRendererAttached(DateTimeOffset attachedUtc)");
        AssertContains(previewStartupSessionControllerText, "public bool MarkFirstVisualConfirmed(DateTimeOffset firstVisualUtc)");
        AssertContains(previewStartupSessionControllerText, "public void SetMissingSignals(string? missingSignals)");
        AssertContains(previewRuntimeSnapshotText, "StartupSessionController = _previewStartupSessionController,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "startupSession.State.ToString(),");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupState = signature.StartupState,");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.IsAnimating;");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.HandleReinitializingChanged(");
        AssertContains(previewReinitText, "new PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitText, "IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,");
        AssertContains(previewReinitText, "IsPreviewing = ViewModel.IsPreviewing,");
        AssertContains(previewReinitText, "IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,");
        AssertContains(previewReinitText, "AttemptLabel = PreviewStartupAttemptLabel,");
        AssertContains(previewReinitText, "CallerName = nameof(HandleViewModelPropertyChangedAsync),");
        AssertContains(previewReinitText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,");
        AssertContains(previewReinitText, "RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,");
        AssertContains(previewReinitText, "StopPreviewStartupOverlay = StopPreviewStartupOverlay,");
        AssertContains(previewReinitText, "ResetPreviewContentTransform = ResetPreviewContentTransform,");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewReinitTransitionController.cs")),
            "preview reinit transition state lives with preview transition animation ownership");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitTransitionControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(previewReinitTransitionControllerText, "public void BeginAnimateOut(string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public PreviewReinitCompletionPresentation GetCompletionPresentation(");
        AssertContains(previewReinitTransitionControllerText, "public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)");
        AssertContains(previewReinitTransitionControllerText, "public void CompleteFirstVisualTransition(string attemptLabel, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.BeginAttempt(");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.Reset(keepRecoveryCount)");
        AssertDoesNotContain(previewStartupText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt=");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertDoesNotContain(previewStartupText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewStartupText, "private bool _previewStopRequestedByUser;");
        AssertDoesNotContain(previewReinitText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(mainWindowText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupAttemptId;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewStartupRequestedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewRendererAttachedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewFirstVisualUtc;");
        AssertDoesNotContain(previewStartupText, "private string? _previewLastFailureReason;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupMissingSignals;");
        AssertDoesNotContain(previewStartupText, "private int _previewRecoveryAttemptCount;");
        AssertDoesNotContain(previewStartupText, "private bool _previewFirstVisualConfirmed;");
        AssertDoesNotContain(previewReinitText, "case PreviewReinitCompletionPresentation.");
        AssertDoesNotContain(previewReinitText, "GetCompletionPresentation(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupSessionController");
        var contextType = RequireType("Sussudio.Controllers.PreviewStartupSessionControllerContext");
        var stateType = RequireType("Sussudio.Controllers.PreviewStartupState");
        var events = new List<string>();
        var isPreviewing = true;
        var isStopRequested = false;
        var now = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        var context = Activator.CreateInstance(contextType, nonPublic: true)!;

        void SetContext(string propertyName, object value)
        {
            var property = contextType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"PreviewStartupSessionControllerContext.{propertyName} was not found.");
            property.SetValue(context, value);
        }

        SetContext("IsPreviewing", new Func<bool>(() => isPreviewing));
        SetContext("IsPreviewStopRequestedByUser", new Func<bool>(() => isStopRequested));
        SetContext("GetSelectedDeviceName", new Func<string?>(() => "Cam Link 4K"));
        SetContext("ResetSignalState", new Action(() => events.Add("reset-signals")));
        SetContext("ResetFailureStopSchedule", new Action(() => events.Add("reset-failure-stop")));
        SetContext("MarkFirstVisualSignalConfirmed", new Action(() => events.Add("mark-signal-visual")));
        SetContext("StopWatchdog", new Action(() => events.Add("stop-watchdog")));
        SetContext("StopOverlay", new Action(() => events.Add("stop-overlay")));
        SetContext("StopFadeInTimer", new Action(() => events.Add("stop-fade-timer")));
        SetContext("ScheduleFadeIn", new Action(() => events.Add("schedule-fade")));
        SetContext("CompleteFirstVisualTransition", new Action<string, string>((attempt, caller) => events.Add($"complete-reinit:{attempt}:{caller}")));
        SetContext("ClearReinitTransitionForStartupReset", new Action<bool, string>((preserve, caller) => events.Add($"clear-reinit:{preserve}:{caller}")));
        SetContext("Log", new Action<string>(message => events.Add($"log:{message}")));
        SetContext("CreateAttemptId", new Func<string>(() => "attempt-1"));
        SetContext("GetUtcNow", new Func<DateTimeOffset>(() => now));

        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
        var beginStartupAttempt = controllerType.GetMethod("BeginStartupAttempt")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.BeginStartupAttempt was not found.");
        var setStartupState = controllerType.GetMethod("SetStartupState")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetStartupState was not found.");
        var markRendererAttached = controllerType.GetMethod("MarkRendererAttached")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkRendererAttached was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkFirstVisualConfirmed was not found.");
        var confirmFirstVisual = controllerType.GetMethod("ConfirmFirstVisual")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ConfirmFirstVisual was not found.");
        var setMissingSignals = controllerType.GetMethod("SetMissingSignals")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetMissingSignals was not found.");
        var resetStartupTracking = controllerType.GetMethod("ResetStartupTracking")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ResetStartupTracking was not found.");
        var getElapsedMilliseconds = controllerType.GetMethod("GetElapsedMilliseconds")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.GetElapsedMilliseconds was not found.");
        var isSignalWindowActive = controllerType.GetMethod("IsSignalWindowActive")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.IsSignalWindowActive was not found.");

        object State(string value) => Enum.Parse(stateType, value);
        bool SignalWindowActive(bool previewing) => (bool)isSignalWindowActive.Invoke(controller, new object[] { previewing })!;

        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");
        AssertEqual(false, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "idle does not refresh missing signals");
        AssertEqual(false, SignalWindowActive(previewing: true), "idle signal window inactive");

        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin attempt");
        AssertEqual(true, SignalWindowActive(previewing: true), "starting session signal window active");
        AssertEqual(false, SignalWindowActive(previewing: false), "stopped preview signal window inactive");
        AssertEqual("attempt-1", GetStringProperty(controller, "AttemptId"), "attempt id after begin");
        AssertEqual(now, GetPropertyValue(controller, "RequestedUtc"), "requested UTC after begin");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual reset on begin");
        AssertEqual(false, GetBoolProperty(controller, "ShouldBeginAttempt"), "active attempt gate");
        AssertEqual(1250.0, getElapsedMilliseconds.Invoke(controller, new object[] { now.AddMilliseconds(1250) }), "elapsed milliseconds");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K",
            string.Join("|", events),
            "begin startup orchestration order");

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("StartingSession"), null });
        AssertEqual(string.Empty, string.Join("|", events), "duplicate state without reason suppresses log");
        setStartupState.Invoke(controller, new object?[] { State("Failed"), "renderer-attach-failed:test" });
        AssertEqual(State("Failed"), GetPropertyValue(controller, "State"), "failed state");
        AssertEqual(false, SignalWindowActive(previewing: true), "failed state signal window inactive");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "failed state refreshes missing signals");
        AssertEqual("renderer-attach-failed:test", GetStringProperty(controller, "LastFailureReason"), "failure reason retained");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "failed attempt gate");
        resetStartupTracking.Invoke(controller, new object[] { false, false });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "terminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "AttemptId"), "terminal reset clears attempt id");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        markRendererAttached.Invoke(controller, new object[] { now.AddMilliseconds(100) });
        AssertEqual(true, GetBoolProperty(controller, "IsWaitingForFirstVisual"), "waiting state predicate");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "waiting state refreshes missing signals");
        AssertEqual(true, SignalWindowActive(previewing: true), "waiting state signal window active");
        AssertEqual(now.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached UTC");
        AssertEqual(true, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(300) }), "first visual confirmation");
        AssertEqual(false, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(400) }), "duplicate first visual suppressed");
        AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual confirmed flag");
        AssertEqual(false, SignalWindowActive(previewing: true), "confirmed first visual signal window inactive");
        AssertEqual(now.AddMilliseconds(300), GetPropertyValue(controller, "FirstVisualUtc"), "first visual UTC");
        AssertEqual("FirstVisual", GetStringProperty(controller, "MissingSignals"), "missing signals cached until adapter clears them");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        now = now.AddMilliseconds(250);
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(State("Rendering"), GetPropertyValue(controller, "State"), "first visual moves to rendering");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "first visual clears missing signals");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K|log:PREVIEW_START_STATE state=WaitingForFirstVisual attempt=attempt-1 recovery=0 reason=-|mark-signal-visual|log:PREVIEW_START_STATE state=Rendering attempt=attempt-1 recovery=0 reason=-|stop-watchdog|stop-overlay|schedule-fade|complete-reinit:attempt-1:ConfirmPreviewFirstVisual|log:PREVIEW_FIRST_VISUAL_CONFIRMED attempt=attempt-1 source=D3D11FirstFrame elapsedMs=250 recovery=0",
            string.Join("|", events),
            "first visual orchestration order");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        isStopRequested = true;
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "stop request suppresses first visual");
        AssertContains(string.Join("|", events), "log:PREVIEW_FIRST_VISUAL_IGNORED attempt=attempt-1 source=D3D11FirstFrame reason=stop-requested");
        isStopRequested = false;

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        resetStartupTracking.Invoke(controller, new object[] { false, true });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "nonterminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "nonterminal reset clears missing signals");
        AssertEqual(
            "stop-watchdog|stop-overlay|stop-fade-timer|clear-reinit:True:ResetPreviewStartupTracking|reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=Idle attempt=none recovery=0 reason=-",
            string.Join("|", events),
            "reset orchestration order");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitialization_WaitsForPendingFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelSharedStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawPreviewReinitializeControllerText = rawPreviewLifecycleControllerText;

        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(rawPreviewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewReinitializeControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitTransitionController_PreservesTransitionStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewReinitTransitionController");
        var presentationType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentation");
        var contextType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentationContext");
        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var beginAnimateOut = controllerType.GetMethod("BeginAnimateOut")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.BeginAnimateOut was not found.");
        var getCompletionPresentation = controllerType.GetMethod("GetCompletionPresentation")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.GetCompletionPresentation was not found.");
        var handleReinitializingChanged = controllerType.GetMethod("HandleReinitializingChanged")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.HandleReinitializingChanged was not found.");
        var completeFirstVisualTransition = controllerType.GetMethod("CompleteFirstVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.CompleteFirstVisualTransition was not found.");
        var resetConfirmedVisualTransition = controllerType.GetMethod("ResetConfirmedVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ResetConfirmedVisualTransition was not found.");
        var clearForStartupReset = controllerType.GetMethod("ClearForStartupReset")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ClearForStartupReset was not found.");
        var clear = controllerType.GetMethod("Clear")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.Clear was not found.");

        object Presentation(string value) => Enum.Parse(presentationType, value);

        object GetPresentation(bool isPreviewReinitializing, bool isPreviewing, bool isFirstVisualConfirmed)
            => getCompletionPresentation.Invoke(
                controller,
                new object[] { isPreviewReinitializing, isPreviewing, isFirstVisualConfirmed })!;

        object CreateContext(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            string attemptLabel,
            string callerName,
            List<string> events)
        {
            var context = Activator.CreateInstance(contextType, nonPublic: true)!;
            SetPropertyOrBackingField(context, "IsPreviewReinitializing", isPreviewReinitializing);
            SetPropertyOrBackingField(context, "IsPreviewing", isPreviewing);
            SetPropertyOrBackingField(context, "IsFirstVisualConfirmed", isFirstVisualConfirmed);
            SetPropertyOrBackingField(context, "AttemptLabel", attemptLabel);
            SetPropertyOrBackingField(context, "CallerName", callerName);
            SetPropertyOrBackingField(context, "UpdateDeviceApplyButtonState", new Action(() => events.Add("update-apply")));
            SetPropertyOrBackingField(context, "RevealUnavailablePlaceholder", new Action(() => events.Add("reveal-unavailable")));
            SetPropertyOrBackingField(context, "StopPreviewStartupOverlay", new Action(() => events.Add("stop-overlay")));
            SetPropertyOrBackingField(context, "ResetPreviewContentTransform", new Action(() => events.Add("reset-transform")));
            SetPropertyOrBackingField(context, "ShowStartPreviewButtonPresentation", new Action(() => events.Add("show-start")));
            return context;
        }

        void HandleReinitializingChanged(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            List<string> events)
            => handleReinitializingChanged.Invoke(
                controller,
                new[]
                {
                    CreateContext(
                        isPreviewReinitializing,
                        isPreviewing,
                        isFirstVisualConfirmed,
                        "attempt-3",
                        "HandleViewModelPropertyChangedAsync",
                        events),
                });

        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "initial reinit animation inactive");
        AssertEqual(
            Presentation("ShowStartPreviewButton"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "idle stopped preview shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "begin reinit marks animation active");
        AssertEqual(
            Presentation("RevealUnavailablePlaceholder"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "completed reinit without preview reveals unavailable placeholder");
        AssertEqual(
            Presentation("ResetConfirmedVisual"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: true),
            "completed reinit after first visual resets presentation");
        AssertEqual(
            Presentation("None"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: false),
            "completed reinit before first visual keeps waiting");

        completeFirstVisualTransition.Invoke(controller, new object[] { "attempt-1", "ConfirmPreviewFirstVisual" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "first visual clears active reinit animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clearForStartupReset.Invoke(controller, new object[] { true, "ResetPreviewStartupTracking" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "startup reset can preserve reinit animation");
        clearForStartupReset.Invoke(controller, new object[] { false, "ResetPreviewStartupTracking" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "startup reset clears animation when not preserving");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        resetConfirmedVisualTransition.Invoke(controller, new object[] { "attempt-2", "reinit-stop-failed", "HandleViewModelPropertyChangedAsync" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual reset clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clear.Invoke(controller, new object?[] { "PreviewButton_Click", true, "PreviewButton_Click" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "explicit clear marks animation inactive");

        var idleStoppedEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            idleStoppedEvents);
        AssertEqual(
            "update-apply,show-start",
            string.Join(",", idleStoppedEvents),
            "idle stopped preview updates apply state then shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var stoppedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            stoppedReinitCompletionEvents);
        AssertEqual(
            "update-apply,reveal-unavailable",
            string.Join(",", stoppedReinitCompletionEvents),
            "completed reinit without preview updates apply state then reveals unavailable placeholder");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "unavailable placeholder completion clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var confirmedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: true,
            isFirstVisualConfirmed: true,
            confirmedReinitCompletionEvents);
        AssertEqual(
            "update-apply,stop-overlay,reset-transform",
            string.Join(",", confirmedReinitCompletionEvents),
            "confirmed visual completion updates apply state, stops overlay, and resets content transform");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual completion clears active animation");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelPreviewLifecycle_LivesInController()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var previewReinitializeControllerText = previewLifecycleControllerText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        if (File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewReinitialization.cs")))
        {
            throw new InvalidOperationException("Preview reinitialization should not live in a tiny pass-through partial.");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "ViewModel",
                "MainViewModelPreviewReinitializeController.cs")),
            "Preview reinitialize transaction controller lives with preview lifecycle owner");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(previewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(previewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(previewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(previewReinitializeControllerText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(previewReinitializeControllerText, "await _context.WaitReinitializeGateAsync();");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyRendererStopAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);");
        AssertContains(previewReinitializeControllerText, "catch (PreviewRendererReinitStopTimeoutException ex)");
        AssertContains(previewReinitializeControllerText, "REINIT_ABORT_RENDERER_STOP_TIMEOUT reason='{reason}'");
        var rendererStopTimeoutCatch = ExtractTextBetween(
            previewReinitializeControllerText,
            "catch (PreviewRendererReinitStopTimeoutException ex)",
            "        catch (Exception ex)");
        AssertDoesNotContain(rendererStopTimeoutCatch, "CleanupFailedPreviewRestartAsync");
        AssertContains(rendererStopTimeoutCatch, "success = false;");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.InitializeDeviceAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false);");
        AssertContains(previewReinitializeControllerText, "_context.ReleaseReinitializeGate();");
        AssertDoesNotContain(previewStateText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");

        return Task.CompletedTask;
    }

    internal static Task PreviewResizeTelemetry_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();

        AssertContains(previewRendererText, "private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewResizeTelemetryController()");
        AssertContains(previewRendererText, "private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(previewRendererText, "_previewResizeTelemetryController.HandleSizeChanged(");
        AssertContains(previewRendererText, "ViewModel.IsPreviewing,");
        AssertContains(previewRendererText, "_previewRendererHostController.HasD3DRenderer,");
        AssertContains(previewRendererText, "PreviewSwapChainPanel.Visibility);");
        AssertContains(previewRendererText, "private void ResetPreviewResizeTelemetry()");
        AssertContains(previewRendererText, "=> _previewResizeTelemetryController.Reset();");
        AssertContains(mainWindowText, "InitializePreviewResizeTelemetryController();");
        AssertContains(mainWindowText, "mainContent.SizeChanged += MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupText, "private void DetachMainContentSizeChanged()");
        AssertContains(shutdownCleanupText, "mainContent.SizeChanged -= MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMainContentSizeChanged();");
        AssertContains(previewRendererText, "ResetPreviewResizeTelemetry = ResetPreviewResizeTelemetry,");
        AssertContains(controllerText, "internal sealed class PreviewResizeTelemetryController");
        AssertContains(controllerText, "private long _previewLastResizeLogTick;");
        AssertContains(controllerText, "public void HandleSizeChanged(bool isPreviewing, bool hasD3dRenderer, Visibility previewVisibility)");
        AssertContains(controllerText, "if (!isPreviewing ||");
        AssertContains(controllerText, "!hasD3dRenderer ||");
        AssertContains(controllerText, "previewVisibility != Visibility.Visible");
        AssertContains(controllerText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertContains(controllerText, "Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick)");
        AssertContains(controllerText, "Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        AssertContains(controllerText, "public void Reset()");
        AssertContains(controllerText, "Interlocked.Exchange(ref _previewLastResizeLogTick, 0);");
        AssertDoesNotContain(mainWindowText, "private long _previewLastResizeLogTick;");
        AssertDoesNotContain(previewRendererText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertDoesNotContain(previewRendererText, "Logger.Log(\"Preview resize active.");
        AssertDoesNotContain(shutdownCleanupControllerText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(shutdownCleanupControllerText, "_previewLastResizeLogTick");

        return Task.CompletedTask;
    }

    internal static Task PreviewRendererStartupPlanBuilder_PreservesFallbackPolicy()
    {
        var builderType = RequireType("Sussudio.Controllers.PreviewRendererStartupPlanBuilder");
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var captureSettingsType = RequireType("Sussudio.Models.CaptureSettings");
        var sourceProbeType = RequireType("Sussudio.Models.VideoSourceProbeResult");
        var hdrOutputModeType = RequireType("Sussudio.Models.HdrOutputMode");
        var build = builderType.GetMethod("Build")
            ?? throw new InvalidOperationException("PreviewRendererStartupPlanBuilder.Build was not found.");
        var resolveExpectedIntervalMs = builderType.GetMethod("ResolveExpectedIntervalMs")
            ?? throw new InvalidOperationException("PreviewRendererStartupPlanBuilder.ResolveExpectedIntervalMs was not found.");

        var fallbackInterval = (double)(resolveExpectedIntervalMs.Invoke(null, new object?[] { null }) ?? 0.0);
        AssertNearlyEqual(1000.0 / 60.0, fallbackInterval, 0.0001, "default preview renderer interval");

        var selectedFormat = Activator.CreateInstance(mediaFormatType)!;
        SetPropertyOrBackingField(selectedFormat, "FrameRate", 30.0);

        var inactivePlan = build.Invoke(null, new object?[] { false, selectedFormat, null, null })!;
        AssertEqual(false, GetBoolProperty(inactivePlan, "UseD3DRenderer"), "inactive preview plan mode");
        AssertEqual(1920, GetIntProperty(inactivePlan, "RendererWidth"), "inactive default width");
        AssertEqual(1080, GetIntProperty(inactivePlan, "RendererHeight"), "inactive default height");
        AssertNearlyEqual(60.0, GetDoubleProperty(inactivePlan, "RendererFps"), 0.0001, "inactive default renderer FPS");
        AssertNearlyEqual(1000.0 / 30.0, GetDoubleProperty(inactivePlan, "PreviewMinPresentationIntervalMs"), 0.0001, "inactive selected-format interval");

        var settings = Activator.CreateInstance(captureSettingsType)!;
        SetPropertyOrBackingField(settings, "Width", (uint)2560);
        SetPropertyOrBackingField(settings, "Height", (uint)1440);
        SetPropertyOrBackingField(settings, "FrameRate", 144.0);
        var inactiveSourceProbe = Activator.CreateInstance(sourceProbeType)!;
        SetPropertyOrBackingField(inactiveSourceProbe, "SessionActive", false);

        var settingsPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
        AssertEqual(true, GetBoolProperty(settingsPlan, "UseD3DRenderer"), "active preview plan mode");
        AssertEqual(2560, GetIntProperty(settingsPlan, "RendererWidth"), "settings fallback width");
        AssertEqual(1440, GetIntProperty(settingsPlan, "RendererHeight"), "settings fallback height");
        AssertNearlyEqual(144.0, GetDoubleProperty(settingsPlan, "RendererFps"), 0.0001, "settings fallback FPS");
        AssertNearlyEqual(1000.0 / 144.0, GetDoubleProperty(settingsPlan, "PreviewMinPresentationIntervalMs"), 0.0001, "settings fallback interval");

        var activeSourceProbe = Activator.CreateInstance(sourceProbeType)!;
        SetPropertyOrBackingField(activeSourceProbe, "SessionActive", true);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentWidth", 3840);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentHeight", 2160);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentFrameRate", 119.88);
        var sourcePlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, activeSourceProbe })!;
        AssertEqual(3840, GetIntProperty(sourcePlan, "RendererWidth"), "active source width");
        AssertEqual(2160, GetIntProperty(sourcePlan, "RendererHeight"), "active source height");
        AssertNearlyEqual(119.88, GetDoubleProperty(sourcePlan, "RendererFps"), 0.0001, "active source FPS");
        AssertNearlyEqual(1000.0 / 119.88, GetDoubleProperty(sourcePlan, "PreviewMinPresentationIntervalMs"), 0.0001, "active source interval");

        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            SetPropertyOrBackingField(settings, "HdrEnabled", true);
            SetPropertyOrBackingField(settings, "HdrOutputMode", Enum.Parse(hdrOutputModeType, "Hdr10Pq"));
            var hdrPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
            AssertEqual(true, GetBoolProperty(hdrPlan, "IsHdr"), "HDR plan follows HDR output policy");

            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", "true");
            var forceOffPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
            AssertEqual(false, GetBoolProperty(forceOffPlan, "IsHdr"), "HDR plan honors force-off policy");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }

    internal static Task PreviewSurfacePresentationAndShadow_LiveInControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewSurface.cs")),
            "preview surface XAML adapter lives with preview renderer composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfaceShadowController.cs")),
            "preview surface shadow controller lives with preview surface presentation owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfacePresentationController.cs")),
            "preview surface presentation folded into PreviewLifecycleControllers.cs");
        AssertContains(previewRendererText, "XAML-facing preview surface adapter");
        AssertContains(previewRendererText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewRendererText, "private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewRendererText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewRendererText, "private void SetupVideoFrameShadow()");
        AssertContains(previewRendererText, "private void SetupControlBarShadow()");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupControlBarShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.ClearVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);");
        AssertContains(previewRendererText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewRendererText, "_previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfacePresentationController");
        AssertContains(previewSurfaceControllerText, "public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }");
        AssertContains(previewSurfaceControllerText, "private readonly PreviewSurfaceShadowController _shadowController;");
        AssertContains(previewSurfaceControllerText, "PreviewSurfaceShadowController shadowController)");
        AssertContains(previewSurfaceControllerText, "var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)");
        AssertContains(previewSurfaceControllerText, "_shadowController.ClearVideoFrameBounds();");
        AssertContains(previewSurfaceControllerText, "_shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);");

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameBounds()");
        AssertContains(previewSurfaceControllerText, "_videoShadowVisual.Size = Vector2.Zero;");
        AssertContains(previewSurfaceControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");

        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DProjection_OwnsPolicyGroups()
    {
        var previewRuntimeD3DProjectionText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotControllers.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRuntimeD3DProjectionText, "internal sealed class PreviewRuntimeD3DProjection");
        AssertContains(previewRuntimeD3DProjectionText, "public bool GpuActive { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DFramesDropped { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameCounters(PreviewRuntimeD3DFrameCounters frameCounters)");
        AssertContains(previewRuntimeD3DProjectionText, "FramesArrived = frameCounters.FramesArrived;");
        AssertContains(previewRuntimeD3DProjectionText, "public string RendererMode { get; private set; } = \"None\";");
        AssertContains(previewRuntimeD3DProjectionText, "public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; private set; } = Array.Empty<PreviewSlowFrameDiagnostic>();");
        AssertContains(previewRuntimeD3DProjectionText, "public string GpuPlaybackState { get; private set; } = \"None\";");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyRendererState(PreviewRuntimeD3DRendererState rendererState)");
        AssertContains(previewRuntimeD3DProjectionText, "GpuPlaybackState = rendererState.GpuPlaybackState;");
        AssertContains(previewRuntimeD3DProjectionText, "public double[] DisplayCadenceRecentIntervalsMs { get; private set; } = Array.Empty<double>();");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyDisplayCadence(PreviewRuntimeD3DDisplayCadence displayCadence)");
        AssertContains(previewRuntimeD3DProjectionText, "DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public double D3DInputUploadCpuAvgMs { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyRenderCpuTiming(PreviewRuntimeD3DRenderCpuTiming renderCpuTiming)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public double EstimatedPipelineLatencyMs { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyPipelineLatency(PreviewRuntimeD3DPipelineLatency pipelineLatency)");
        AssertContains(previewRuntimeD3DProjectionText, "EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DLastSubmittedPreviewPresentId { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameOwnership(PreviewRuntimeD3DFrameOwnership frameOwnership)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber;");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DFrameStatsPresentCount { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameStatistics(PreviewRuntimeD3DFrameStatistics frameStatistics)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DFrameStatsPresentCount = frameStatistics.PresentCount;");
        AssertContains(previewRuntimeD3DProjectionText, "public bool D3DFrameLatencyWaitEnabled { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameLatencyWait(PreviewRuntimeD3DFrameLatencyWait frameLatencyWait)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled;");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeD3DProjectionText, "var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);");
        AssertContains(previewRuntimeD3DProjectionText, "var d3d = input.D3DRenderer;");
        AssertContains(previewRuntimeD3DProjectionText, "var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.IsPreviewing);");
        AssertContains(previewRuntimeD3DProjectionText, "var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);");
        AssertContains(previewRuntimeD3DProjectionText, "var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var projection = new PreviewRuntimeD3DProjection();");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameCounters(frameCounters);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyRendererState(rendererState);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyDisplayCadence(displayCadence);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyRenderCpuTiming(renderCpuTiming);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyPipelineLatency(pipelineLatency);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameLatencyWait(frameLatencyWait);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameStatistics(frameStatistics);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameOwnership(frameOwnership);");
        AssertContains(previewRuntimeD3DProjectionText, "return projection;");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameCounterPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameCounters Evaluate(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeD3DProjectionText, "FramesArrived: gpuActive ? d3dFramesSubmitted : input.FramesArrived,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DRendererStatePolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DRendererState Evaluate(D3D11PreviewRenderer? d3d, bool isPreviewing)");
        AssertContains(previewRuntimeD3DProjectionText, "RendererMode: d3d?.RendererMode ?? (isPreviewing ? \"CpuSoftwareBitmap\" : \"None\"),");
        AssertContains(previewRuntimeD3DProjectionText, "RecentSlowFrames: d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DDisplayCadencePolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DDisplayCadence Evaluate(");
        AssertContains(previewRuntimeD3DProjectionText, "RecentIntervalsMs: displayCadence?.RecentIntervalsMs ?? Array.Empty<double>(),");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DRenderCpuTimingPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DRenderCpuTiming Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "SampleCount: renderCpuTiming?.TotalFrame.SampleCount ?? 0,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DPipelineLatencyPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DPipelineLatency Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "EstimatedPipelineLatencyMs: pipelineLatency?.AverageMs ?? 0);");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameStatisticsPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameStatistics Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "PresentCount: frameStats?.PresentCount ?? -1,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameLatencyWaitPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameLatencyWait Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "SampleCount: frameLatencyWait?.Timing.SampleCount ?? 0,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameOwnershipPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameOwnership Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "LastSubmittedSourceSequenceNumber: frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,");
        AssertContains(previewRuntimeD3DProjectionText, "LastDroppedSourceSequenceNumber: frameOwnership?.LastDroppedSourceSequenceNumber ?? -1,");

        AssertContains(agentMapText, "PreviewRuntimeSnapshotControllers.cs");
        AssertContains(agentMapText, "owns the renderer projection data contract, D3D policy records");
        AssertContains(agentMapText, "assignment from evaluated policy records");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotControllers.cs");
        AssertContains(cleanupPlanText, "renderer projection data contract, D3D policy records");
        AssertContains(cleanupPlanText, "evaluated policy records");
        foreach (var removedFile in new[]
        {
            "PreviewRuntimeD3DFrameCounterPolicy.cs",
            "PreviewRuntimeD3DRendererStatePolicy.cs",
            "PreviewRuntimeD3DDisplayCadencePolicy.cs",
            "PreviewRuntimeD3DRenderCpuTimingPolicy.cs",
            "PreviewRuntimeD3DPipelineLatencyPolicy.cs",
            "PreviewRuntimeD3DFrameOwnershipPolicy.cs",
            "PreviewRuntimeD3DFrameStatisticsPolicy.cs",
            "PreviewRuntimeD3DFrameLatencyWaitPolicy.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", removedFile)),
                $"{removedFile} folded into PreviewRuntimeSnapshotControllers.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task PreviewRendererHostController_OwnsRuntimeState()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var mainWindowFamilyText = string.Join(
                "\n",
                Directory.GetFiles(Path.Combine(GetRepoRoot(), "Sussudio"), "MainWindow*.cs")
                    .Select(File.ReadAllText))
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRendererHostControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var rendererReinitStop = ExtractMemberCode(previewRendererHostControllerText, "StopRendererForReinitTeardownAsync");
        var rendererReinitDispose = ExtractMemberCode(previewRendererHostControllerText, "DisposeD3DPreviewRendererForReinit");
        var previewRendererStartupPlanBuilderText = previewRendererHostControllerText;
        var statsSnapshotText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private PreviewRendererHostController _previewRendererHostController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewRendererHostController()");
        AssertContains(previewRendererText, "GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,");
        AssertContains(previewRendererText, "SetPreviewSwapChainPanel = panel => PreviewSwapChainPanel = panel,");
        AssertContains(previewRendererText, "PreviewContentGridSizeChangedHandler = OnPreviewContentGridSizeChanged,");
        AssertContains(previewRendererText, "PreviewSwapChainPanelSizeChangedHandler = OnPreviewSwapChainPanelSizeChanged,");
        AssertContains(previewRendererText, "ClearPreviewReinitAnimatingForShutdown = () =>");
        AssertContains(previewRendererText, "ConfirmPreviewFirstVisual = ConfirmPreviewFirstVisual,");
        AssertContains(previewRendererText, "MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),");
        AssertContains(previewRendererText, "ConfigurePreviewStartupSignals = ConfigurePreviewStartupSignals,");
        AssertContains(previewRendererText, "private Task StartPreviewRendererAsync()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StartAsync();");
        AssertContains(previewRendererText, "private Task StopPreviewRendererAsync()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StopAsync();");
        AssertContains(previewRendererText, "private void StopPreviewForShutdown()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StopForShutdown();");
        AssertContains(previewRendererText, "=> _previewRendererHostController.RendererReinitUnsafeWindows;");
        AssertContains(mainWindowText, "InitializePreviewRendererHostController();");
        AssertContains(previewRendererHostControllerText, "internal sealed class PreviewRendererHostControllerContext");
        AssertContains(previewRendererHostControllerText, "internal sealed class PreviewRendererHostController");
        AssertDoesNotContain(previewRendererHostControllerText, "partial class PreviewRendererHostController");
        AssertContains(previewRendererHostControllerText, "private SoftwareBitmapSource? _previewSource;");
        AssertContains(previewRendererHostControllerText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesArrived;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesDisplayed;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesDropped;");
        AssertContains(previewRendererHostControllerText, "private long _previewLastPresentedTick;");
        AssertContains(previewRendererHostControllerText, "private double _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "private long _lastRendererStopTick;");
        AssertContains(previewRendererHostControllerText, "private long _rendererReinitUnsafeWindows;");
        AssertContains(previewRendererHostControllerText, "public D3D11PreviewRenderer? Renderer => _d3dRenderer;");
        AssertContains(previewRendererHostControllerText, "public bool HasD3DRenderer => _d3dRenderer != null;");
        AssertContains(previewRendererHostControllerText, "public bool IsCpuPreviewSourceAttached => _previewSource != null;");
        AssertContains(previewRendererHostControllerText, "public double PreviewMinPresentationIntervalMs => _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertContains(previewRendererHostControllerText, "public int? PendingFrameCount => _d3dRenderer?.PendingFrameCount;");
        AssertContains(previewRendererHostControllerText, "public Task StartAsync()");
        AssertContains(previewRendererHostControllerText, "RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _context.IsPreviewReinitAnimating());");
        AssertContains(previewRendererHostControllerText, "var startupPlan = BuildPreviewRendererStartupPlan();");
        AssertContains(previewRendererHostControllerText, "_previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()");
        AssertContains(previewRendererHostControllerText, "PreviewRendererStartupPlanBuilder.Build(");
        AssertContains(previewRendererHostControllerText, "private void CleanupPreviewResources()");
        AssertContains(previewRendererHostControllerText, "_d3dRenderer = null;");
        AssertContains(previewRendererHostControllerText, "public Task StopAsync()");
        AssertContains(previewRendererHostControllerText, "private void StartD3DRenderer(PreviewRendererStartupPlan startupPlan)");
        AssertContains(previewRendererHostControllerText, "renderer.SetExpectedFrameRate(rendererFps);");
        AssertContains(previewRendererHostControllerText, "renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);");
        AssertContains(previewRendererHostControllerText, "_context.ViewModel.SetPreviewFrameSink(_d3dRenderer);");
        AssertContains(previewRendererHostControllerText, "PreviewStartupStrategy.D3D11VideoProcessor");
        AssertContains(previewRendererHostControllerText, "_context.MarkPreviewRendererAttached();");
        AssertContains(previewRendererHostControllerText, "private D3D11PreviewRenderer CreateFreshD3DPreviewRenderer(bool replaceSwapChainSurface)");
        AssertContains(previewRendererHostControllerText, "private void OnD3DRendererFirstFrameRendered()");
        AssertContains(previewRendererHostControllerText, "private void OnD3DRendererRenderThreadFailed(string reason)");
        AssertContains(previewRendererHostControllerText, "private void StartCpuRenderer()");
        AssertContains(previewRendererHostControllerText, "_context.ViewModel.SetPreviewFrameSink(null);");
        AssertContains(previewRendererHostControllerText, "_previewSource = new SoftwareBitmapSource();");
        AssertContains(previewRendererHostControllerText, "private void RecordPreviewRendererReinitUnsafeWindow(D3D11PreviewRenderer? previousRenderer, bool reinitAnimating)");
        AssertContains(previewRendererHostControllerText, "private void MarkPreviewRendererStopped()");
        AssertContains(previewRendererHostControllerText, "public Task StopRendererForReinitTeardownAsync()");
        AssertContains(rendererReinitStop, "PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
        AssertContains(rendererReinitStop, "catch (TimeoutException ex)");
        AssertContains(rendererReinitStop, "MarkPreviewRendererStopped();");
        AssertContains(rendererReinitStop, "PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; aborting reinit until renderer ownership is resolved.");
        AssertContains(rendererReinitStop, "throw new PreviewRendererReinitStopTimeoutException(");
        AssertDoesNotContain(rendererReinitStop, "_d3dRenderer = null;");
        AssertContains(previewRendererHostControllerText, "public void DisposeD3DPreviewRendererForReinit()");
        AssertContains(rendererReinitDispose, "renderer.Stop();");
        AssertContains(previewRendererHostControllerText, "renderer.RetireSharedDeviceReferenceForReinit();");
        AssertContains(rendererReinitDispose, "_context.ViewModel.SetPreviewFrameSink(null);");
        AssertContains(rendererReinitDispose, "renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;");
        AssertContains(rendererReinitDispose, "renderer.RenderThreadFailed -= OnD3DRendererRenderThreadFailed;");
        AssertContains(rendererReinitDispose, "_d3dRenderer = null;");
        AssertOccursBefore(rendererReinitDispose, "renderer.Stop();", "_context.ViewModel.SetPreviewFrameSink(null);");
        AssertOccursBefore(rendererReinitDispose, "renderer.Stop();", "renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;");
        AssertOccursBefore(rendererReinitDispose, "renderer.Stop();", "renderer.RenderThreadFailed -= OnD3DRendererRenderThreadFailed;");
        AssertOccursBefore(rendererReinitDispose, "renderer.Stop();", "_d3dRenderer = null;");
        AssertContains(previewRendererHostControllerText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertContains(previewRendererHostControllerText, "D3D11_RENDERER_REINIT_UNSAFE_WINDOW");
        AssertContains(previewRendererHostControllerText, "PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED");
        AssertContains(previewRendererStartupPlanBuilderText, "internal sealed record PreviewRendererStartupPlan(");
        AssertContains(previewRendererStartupPlanBuilderText, "internal static class PreviewRendererStartupPlanBuilder");
        AssertContains(previewRendererStartupPlanBuilderText, "private const int DefaultWidth = 1920;");
        AssertContains(previewRendererStartupPlanBuilderText, "private const int DefaultHeight = 1080;");
        AssertContains(previewRendererStartupPlanBuilderText, "private const double DefaultFps = 60.0;");
        AssertContains(previewRendererStartupPlanBuilderText, "public static double ResolveExpectedIntervalMs(MediaFormat? selectedFormat)");
        AssertContains(previewRendererStartupPlanBuilderText, "public static PreviewRendererStartupPlan Build(");
        AssertContains(previewRendererStartupPlanBuilderText, "var negotiatedWidth = sourceProbe?.SessionActive == true ? sourceProbe.CurrentWidth : 0;");
        AssertContains(previewRendererStartupPlanBuilderText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : settingsWidth;");
        AssertContains(previewRendererStartupPlanBuilderText, "var rendererFps = negotiatedFps > 0 ? negotiatedFps : settingsFps;");
        AssertContains(statsSnapshotText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(statsSnapshotText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderText, "GetPresentCadenceMetrics(previewMinPresentationIntervalMs)");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.Lifecycle.cs");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.D3D.cs");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.Reinit.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.Lifecycle.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.D3D.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.Reinit.cs");
        AssertDoesNotContain(previewRendererText, "DisposeD3DPreviewRendererForReinit");
        AssertDoesNotContain(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertDoesNotContain(previewRendererText, "var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;");
        AssertDoesNotContain(previewRendererText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;");
        AssertDoesNotContain(mainWindowFamilyText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowFamilyText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesArrived;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesDisplayed;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesDropped;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewLastPresentedTick;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(mainWindowFamilyText, "private double _previewMinPresentationIntervalMs;");
        AssertDoesNotContain(mainWindowFamilyText, "new D3D11PreviewRenderer(");
        AssertDoesNotContain(mainWindowFamilyText, "RetireSharedDeviceReferenceForReinit();");
        AssertDoesNotContain(mainWindowText, "PreviewRendererStartupPlanBuilder.ResolveExpectedIntervalMs");
        AssertDoesNotContain(mainWindowText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertDoesNotContain(previewRendererText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(previewRendererText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(previewRendererText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertDoesNotContain(previewRendererText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotController_OwnsSnapshotMapping()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = previewRendererText;
        var previewRuntimeSnapshotInitialization = ExtractMemberCode(previewRuntimeSnapshotText, "InitializePreviewRuntimeSnapshotSamplingController");
        var previewRuntimeSnapshotControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotControllers.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotControllerBuildText = ExtractMemberCode(previewRuntimeSnapshotControllerText, "Build");
        var previewRuntimeSnapshotSamplingControllerText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotMapperText = ExtractTextBetween(
            previewRuntimeSnapshotControllerText,
            "internal static class PreviewRuntimeSnapshotMapper",
            "internal sealed class PreviewRuntimeSnapshotHealthInput");
        var previewRuntimeSnapshotSurfaceProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotStartupProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotGpuPlaybackProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotHealthPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotHealthInputFactoryText = previewRuntimeSnapshotHealthPolicyText;
        var previewRuntimeSnapshotModelText = ReadRepoFile("Sussudio/Models/Automation/AutomationModels.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;");
        AssertContains(previewRuntimeSnapshotText, "private void InitializePreviewRuntimeSnapshotSamplingController()");
        AssertContains(previewRuntimeSnapshotText, "UiDispatchController = WindowUiDispatchController,");
        AssertContains(previewRuntimeSnapshotText, "RendererHostController = _previewRendererHostController,");
        AssertContains(previewRuntimeSnapshotText, "StartupSessionController = _previewStartupSessionController,");
        AssertContains(previewRuntimeSnapshotText, "StartupSignalCoordinator = _previewStartupSignalCoordinator,");
        AssertContains(previewRuntimeSnapshotText, "IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs");
        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "=> await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(mainWindowText, "InitializePreviewRuntimeSnapshotSamplingController();");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "internal sealed class PreviewRuntimeSnapshotSamplingControllerContext");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required WindowUiDispatchController UiDispatchController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewRendererHostController RendererHostController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewStartupSessionController StartupSessionController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewStartupSignalCoordinator StartupSignalCoordinator { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "internal sealed class PreviewRuntimeSnapshotSamplingController");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public Task<PreviewRuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "_context.UiDispatchController.InvokeWithRetryAsync(");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "BuildSnapshot,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "\"Failed to enqueue preview snapshot operation.\",");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "private PreviewRuntimeSnapshot BuildSnapshot()");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "private readonly object _previewRuntimeSnapshotEpochLock = new();");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "private PreviewRuntimeSnapshotSignature _lastPreviewRuntimeSnapshotSignature;");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "var startupSignalSnapshot = startupSignals.Snapshot;");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "startupSession.ShouldRefreshMissingSignalsForSnapshot");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "startupMissingSignals = startupSignals.BuildMissingSignals();");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "var signature = new PreviewRuntimeSnapshotSignature(");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "var previewRuntimeEpoch = PreviewRuntimeSnapshotEpoch(signature);");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "private long PreviewRuntimeSnapshotEpoch(PreviewRuntimeSnapshotSignature signature)");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "lock (_previewRuntimeSnapshotEpochLock)");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "Interlocked.Increment(ref _previewRuntimeSnapshotEpoch);");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "return Interlocked.Read(ref _previewRuntimeSnapshotEpoch);");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "internal readonly record struct PreviewRuntimeSnapshotSignature(");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "D3DRenderer = rendererHost.Renderer,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "PreviewRuntimeEpoch = previewRuntimeEpoch,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "PreviewSourceAttached = signature.PreviewSourceAttached,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "GpuElementVisible = signature.GpuElementVisible,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "FramesArrived = signature.FramesArrived,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "PreviewMinPresentationIntervalMs = signature.PreviewMinPresentationIntervalMs,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupState = signature.StartupState,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "IsStartupWaitingForFirstVisual = signature.IsStartupWaitingForFirstVisual,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupGpuSignalMediaOpened = signature.StartupGpuSignalMediaOpened,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "GpuPositionEventCount = signature.GpuPositionEventCount");
        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotControllerText, "public D3D11PreviewRenderer? D3DRenderer { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public long GpuPositionEventCount { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3dProjection = PreviewRuntimeD3DProjection.Build(input);");
        AssertContains(previewRuntimeSnapshotControllerText, "var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(");
        AssertContains(previewRuntimeSnapshotControllerText, "Environment.TickCount64,");
        AssertContains(previewRuntimeSnapshotControllerText, "var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);");
        AssertContains(previewRuntimeSnapshotControllerText, "return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);");
        AssertContains(previewRuntimeSnapshotMapperText, "internal static class PreviewRuntimeSnapshotMapper");
        AssertContains(previewRuntimeSnapshotMapperText, "public static PreviewRuntimeSnapshot Build(");
        AssertContains(previewRuntimeSnapshotMapperText, "var surface = PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate(input, d3dProjection, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var startup = PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate(input, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var gpuPlayback = PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate(input, d3dProjection);");
        AssertContains(previewRuntimeSnapshotMapperText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotMapperText, "TimestampUtc = timestampUtc,");
        AssertContains(previewRuntimeSnapshotMapperText, "IsPreviewing = surface.IsPreviewing,");
        AssertContains(previewRuntimeSnapshotMapperText, "FramesArrived = surface.FramesArrived,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupState = startup.State,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupElapsedMs = startup.ElapsedMs,");
        AssertContains(previewRuntimeSnapshotMapperText, "BlankSuspected = surface.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "StallSuspected = surface.StallSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "internal static class PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "public static PreviewRuntimeSnapshotSurfaceProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "GpuActive: d3dProjection.GpuActive,");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "BlankSuspected: health.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "internal static class PreviewRuntimeSnapshotStartupProjectionPolicy");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "public static PreviewRuntimeSnapshotStartupProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "ElapsedMs: health.StartupElapsedMs,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "RecoveryAttemptCount: input.StartupRecoveryAttemptCount,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "internal static class PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "public static PreviewRuntimeSnapshotGpuPlaybackProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PlaybackState: d3dProjection.GpuPlaybackState,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PositionEventCount: input.GpuPositionEventCount);");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "internal static class PreviewRuntimeSnapshotHealthInputFactory");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "public static PreviewRuntimeSnapshotHealthInput Build(");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "RendererAttached = d3dProjection.RendererAttached,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "CurrentTick = currentTick,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "UtcNow = utcNow");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "internal static class PreviewRuntimeSnapshotHealthPolicy");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "public static PreviewRuntimeSnapshotHealth Evaluate(PreviewRuntimeSnapshotHealthInput input)");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "var startupTimedOut = input.IsPreviewing");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.FramesArrived > 30");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.CurrentTick - input.LastPresentedTick > 3000");
        AssertContains(previewRuntimeSnapshotModelText, "public sealed class PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotModelText, "public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;");
        AssertContains(previewRuntimeSnapshotModelText, "public bool RendererAttached { get; init; }");
        AssertContains(previewRuntimeSnapshotModelText, "public string StartupState { get; init; } = \"Idle\";");
        AssertContains(previewRuntimeSnapshotModelText, "public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }");
        AssertContains(previewRuntimeSnapshotModelText, "public double[] DisplayCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(previewRuntimeSnapshotModelText, "public string RendererMode { get; init; } = \"None\";");
        AssertContains(previewRuntimeSnapshotModelText, "public string D3DSwapChainAddress { get; init; } = string.Empty;");
        AssertContains(previewRuntimeSnapshotModelText, "public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();");
        AssertContains(previewRuntimeSnapshotModelText, "public string GpuPlaybackState { get; init; } = \"None\";");
        AssertDoesNotContain(previewRuntimeSnapshotModelText, "partial class PreviewRuntimeSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation", "PreviewRuntimeSnapshot.cs")),
            "preview runtime DTO folded into AutomationModels.cs");
        AssertContains(agentMapText, "MainWindow.xaml.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotControllers.cs");
        AssertDoesNotContain(agentMapText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(agentMapText, "surface/startup/GPU playback projection policies");
        AssertContains(agentMapText, "health input factory");
        AssertContains(cleanupPlanText, "MainWindow.xaml.cs");
        AssertContains(cleanupPlanText, "surface/frame");
        AssertContains(cleanupPlanText, "display cadence");
        AssertContains(cleanupPlanText, "D3D renderer diagnostics");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotControllers.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(cleanupPlanText, "surface/startup/GPU playback projection policies");
        AssertContains(cleanupPlanText, "health input factory");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", "PreviewRuntimeSnapshotMapper.cs")),
            "Preview runtime snapshot mapper stays folded into the snapshot controller owner");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuActive = d3dProjection.GpuActive,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "FramesArrived = d3dProjection.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupElapsedMs = health.StartupElapsedMs,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPlaybackState = d3dProjection.GpuPlaybackState,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = input.GpuPositionEventCount");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "return new PreviewRuntimeSnapshot\n        {");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotText, "new PreviewRuntimeSnapshotInput");
        AssertDoesNotContain(previewRuntimeSnapshotInitialization, "BuildPreviewStartupMissingSignals()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "FramesArrived = _previewRendererHostController.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotSamplingControllerText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetRenderCpuTimingMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameOwnershipMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameLatencyWaitMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetPipelineLatencyMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "_dispatcherQueue.TryEnqueue");
        AssertDoesNotContain(previewRuntimeSnapshotText, "const int maxAttempts = 3;");
        AssertDoesNotContain(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertDoesNotContain(previewRuntimeSnapshotText, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(previewRuntimeSnapshotText, "CurrentPreviewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed");
        AssertDoesNotContain(previewRuntimeSnapshotText, "IsStartupWaitingForFirstVisual = CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", "PreviewRuntimeSnapshotSamplingController.cs")),
            "preview runtime snapshot sampling lives with the snapshot controller");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewRuntimeSnapshot.cs")),
            "preview runtime snapshot adapter lives with the preview renderer composition");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotEpoch_DoesNotAdvanceForUnchangedSignatures()
    {
        var contextType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSamplingControllerContext");
        var samplerType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSamplingController");
        var signatureType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSignature");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "MediaOpened");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var context = Activator.CreateInstance(contextType)
                      ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotSamplingControllerContext.");
        var sampler = Activator.CreateInstance(samplerType, context)
                      ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotSamplingController.");
        var epochMethod = samplerType.GetMethod("PreviewRuntimeSnapshotEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotEpoch method not found.");
        var signatureConstructor = signatureType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(constructor => constructor.GetParameters().Length == 26);
        var requestedUtc = DateTimeOffset.UtcNow.AddMilliseconds(-250);

        object CreateSignature(long framesDisplayed)
            => signatureConstructor.Invoke(new object?[]
            {
                true,
                true,
                true,
                false,
                false,
                12L,
                framesDisplayed,
                1L,
                12345L,
                16.67d,
                "WaitingForFirstVisual",
                true,
                "attempt-epoch",
                requestedUtc,
                1200,
                true,
                false,
                true,
                requiredSignals,
                receivedSignals,
                startupStrategy,
                "FirstVisual",
                2,
                "timeout",
                false,
                3L
            });

        var unchangedSignature = CreateSignature(framesDisplayed: 7);
        var changedSignature = CreateSignature(framesDisplayed: 8);

        AssertEqual(1L, Convert.ToInt64(epochMethod.Invoke(sampler, new[] { unchangedSignature })), "initial preview runtime epoch");
        AssertEqual(1L, Convert.ToInt64(epochMethod.Invoke(sampler, new[] { unchangedSignature })), "unchanged preview runtime signature must not advance epoch");
        AssertEqual(2L, Convert.ToInt64(epochMethod.Invoke(sampler, new[] { changedSignature })), "changed preview runtime signature advances epoch");

        return Task.CompletedTask;
    }


    internal static Task PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameCounterPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy.Evaluate not found.");

        var attachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(attachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(attachedInput, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(attachedInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(attachedInput, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(attachedInput, "FramesDropped", 4L);

        var attachedCounters = evaluate.Invoke(null, new[] { attachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(attachedCounters, "GpuActive"), "CPU fallback reports GPU inactive");
        AssertEqual(true, GetBoolProperty(attachedCounters, "RendererAttached"), "CPU fallback keeps renderer attached");
        AssertEqual(31L, GetLongProperty(attachedCounters, "FramesArrived"), "CPU fallback frames arrived");
        AssertEqual(17L, GetLongProperty(attachedCounters, "FramesDisplayed"), "CPU fallback frames displayed");
        AssertEqual(4L, GetLongProperty(attachedCounters, "FramesDropped"), "CPU fallback frames dropped");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesSubmitted"), "null D3D submitted counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesRendered"), "null D3D rendered counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesDropped"), "null D3D dropped counter");

        var detachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(detachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(detachedInput, "PreviewSourceAttached", false);

        var detachedCounters = evaluate.Invoke(null, new[] { detachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(detachedCounters, "RendererAttached"), "null D3D without CPU source is detached");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var build = projectionType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(input, "FramesDropped", 4L);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);

        var projection = build.Invoke(null, new[] { input })
                         ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build returned null.");
        AssertEqual(false, GetBoolProperty(projection, "GpuActive"), "builder applies frame-counter GPU state");
        AssertEqual(true, GetBoolProperty(projection, "RendererAttached"), "builder applies CPU fallback attachment");
        AssertEqual(31L, GetLongProperty(projection, "FramesArrived"), "builder applies frame-counter arrived value");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(projection, "RendererMode"), "builder applies renderer-state fallback");
        AssertEqual(0, GetIntProperty(projection, "DisplayCadenceSampleCount"), "builder applies display cadence defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "D3DInputUploadCpuAvgMs"), "builder applies render CPU timing defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "EstimatedPipelineLatencyMs"), "builder applies pipeline latency defaults");
        AssertEqual(false, GetBoolProperty(projection, "D3DFrameLatencyWaitEnabled"), "builder applies frame-latency wait defaults");
        AssertEqual(-1L, GetLongProperty(projection, "D3DFrameStatsPresentCount"), "builder applies frame-stat sentinels");
        AssertEqual(-1L, GetLongProperty(projection, "D3DLastSubmittedSourceSequenceNumber"), "builder applies frame-ownership sentinels");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameStatisticsPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate not found.");

        var frameStatistics = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SampleCount"), "null D3D frame-stat sample count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SuccessCount"), "null D3D frame-stat success count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "FailureCount"), "null D3D frame-stat failure count");
        AssertEqual(string.Empty, GetStringProperty(frameStatistics, "LastError"), "null D3D frame-stat last error");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentCount"), "null D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentRefreshCount"), "null D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "SyncRefreshCount"), "null D3D sync-refresh sentinel");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SyncQpcTime"), "null D3D sync QPC time");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentDelta"), "null D3D present delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentRefreshDelta"), "null D3D present-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastSyncRefreshDelta"), "null D3D sync-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "MissedRefreshCount"), "null D3D missed-refresh count");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameLatencyWaitPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate not found.");

        var frameLatencyWait = evaluate.Invoke(null, new object[] { null! })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy returned null.");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "Enabled"), "null D3D frame-latency wait enabled");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "HandleActive"), "null D3D frame-latency wait handle active");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "CallCount"), "null D3D frame-latency wait call count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "SignaledCount"), "null D3D frame-latency wait signaled count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "TimeoutCount"), "null D3D frame-latency wait timeout count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "UnexpectedResultCount"), "null D3D frame-latency wait unexpected-result count");
        AssertEqual(0u, GetPropertyValue(frameLatencyWait, "LastResult"), "null D3D frame-latency wait last result");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "LastWaitMs"), "null D3D frame-latency wait last wait");
        AssertEqual(0, GetIntProperty(frameLatencyWait, "SampleCount"), "null D3D frame-latency wait sample count");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "AverageMs"), "null D3D frame-latency wait average");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P95Ms"), "null D3D frame-latency wait p95");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P99Ms"), "null D3D frame-latency wait p99");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "MaxMs"), "null D3D frame-latency wait max");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameOwnershipPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate not found.");

        var frameOwnership = evaluate.Invoke(null, new object[] { null! })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedPreviewPresentId"), "null D3D submitted present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastSubmittedSourceSequenceNumber"), "null D3D submitted source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedSourcePtsTicks"), "null D3D submitted source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedQpc"), "null D3D submitted QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedUtcUnixMs"), "null D3D submitted UTC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedPreviewPresentId"), "null D3D rendered present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastRenderedSourceSequenceNumber"), "null D3D rendered source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedSourcePtsTicks"), "null D3D rendered source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedQpc"), "null D3D rendered QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedUtcUnixMs"), "null D3D rendered UTC");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedSchedulerToPresentMs"), "null D3D scheduler-to-present");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedPipelineLatencyMs"), "null D3D pipeline latency");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedPreviewPresentId"), "null D3D dropped present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastDroppedSourceSequenceNumber"), "null D3D dropped source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedSourcePtsTicks"), "null D3D dropped source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedQpc"), "null D3D dropped QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedUtcUnixMs"), "null D3D dropped UTC");
        AssertEqual(string.Empty, GetStringProperty(frameOwnership, "LastDropReason"), "null D3D drop reason");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRendererStatePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy.Evaluate not found.");

        var previewingState = evaluate.Invoke(null, new object[] { null!, true })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null.");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(previewingState, "RendererMode"), "null D3D previewing renderer mode");
        AssertEqual(0, GetIntProperty(previewingState, "PresentSyncInterval"), "null D3D present sync interval");
        AssertEqual(0, GetIntProperty(previewingState, "MaxFrameLatency"), "null D3D max frame latency");
        AssertEqual(0, GetIntProperty(previewingState, "SwapChainBufferCount"), "null D3D swap-chain buffer count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "SwapChainAddress"), "null D3D swap-chain address");
        AssertEqual(0L, GetLongProperty(previewingState, "RenderThreadFailureCount"), "null D3D render-thread failure count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureType"), "null D3D failure type");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureMessage"), "null D3D failure message");
        AssertEqual(0, GetIntProperty(previewingState, "LastRenderThreadFailureHResult"), "null D3D failure HRESULT");
        AssertEqual(0, GetIntProperty(previewingState, "PendingFrameCount"), "null D3D pending frame count");
        AssertEqual("None", GetStringProperty(previewingState, "InputColorSpace"), "null D3D input color space");
        AssertEqual("None", GetStringProperty(previewingState, "OutputColorSpace"), "null D3D output color space");
        var recentSlowFrames = GetPropertyValue(previewingState, "RecentSlowFrames") as Array
                               ?? throw new InvalidOperationException("RecentSlowFrames was not an array.");
        AssertEqual(0, recentSlowFrames.Length, "null D3D recent slow-frame count");
        AssertEqual("None", GetStringProperty(previewingState, "GpuPlaybackState"), "null D3D GPU playback state");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoWidth"), "null D3D natural video width");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoHeight"), "null D3D natural video height");
        AssertEqual(0d, GetDoubleProperty(previewingState, "PositionMs"), "null D3D GPU position");

        var idleState = evaluate.Invoke(null, new object[] { null!, false })
                        ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null for idle.");
        AssertEqual("None", GetStringProperty(idleState, "RendererMode"), "null D3D idle renderer mode");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DDisplayCadencePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy.Evaluate not found.");

        var displayCadence = evaluate.Invoke(null, new object[] { null!, 8.33d })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy returned null.");
        AssertEqual(0, GetIntProperty(displayCadence, "SampleCount"), "null D3D display cadence sample count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ObservedFps"), "null D3D display cadence observed fps");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ExpectedIntervalMs"), "null D3D display cadence expected interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "AverageIntervalMs"), "null D3D display cadence average interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P95IntervalMs"), "null D3D display cadence p95 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P99IntervalMs"), "null D3D display cadence p99 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "MaxIntervalMs"), "null D3D display cadence max interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "OnePercentLowFps"), "null D3D display cadence one-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "FivePercentLowFps"), "null D3D display cadence five-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SampleDurationMs"), "null D3D display cadence sample duration");
        var recentIntervals = GetPropertyValue(displayCadence, "RecentIntervalsMs") as Array
                              ?? throw new InvalidOperationException("RecentIntervalsMs was not an array.");
        AssertEqual(0, recentIntervals.Length, "null D3D display cadence recent interval count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "JitterStdDevMs"), "null D3D display cadence jitter");
        AssertEqual(0L, GetLongProperty(displayCadence, "SlowFrameCount"), "null D3D display cadence slow-frame count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SlowFramePercent"), "null D3D display cadence slow-frame percent");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRenderCpuTimingPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate not found.");

        var renderCpuTiming = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy returned null.");
        AssertEqual(0, GetIntProperty(renderCpuTiming, "SampleCount"), "null D3D render CPU timing sample count");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadAverageMs"), "null D3D input-upload average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP95Ms"), "null D3D input-upload p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP99Ms"), "null D3D input-upload p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadMaxMs"), "null D3D input-upload max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitAverageMs"), "null D3D render-submit average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP95Ms"), "null D3D render-submit p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP99Ms"), "null D3D render-submit p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitMaxMs"), "null D3D render-submit max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallAverageMs"), "null D3D present-call average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP95Ms"), "null D3D present-call p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP99Ms"), "null D3D present-call p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallMaxMs"), "null D3D present-call max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameAverageMs"), "null D3D total-frame average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP95Ms"), "null D3D total-frame p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP99Ms"), "null D3D total-frame p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameMaxMs"), "null D3D total-frame max");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DPipelineLatencyPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate not found.");

        var pipelineLatency = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy returned null.");
        AssertEqual(0, GetIntProperty(pipelineLatency, "SampleCount"), "null D3D pipeline latency sample count");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "AverageMs"), "null D3D pipeline latency average");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P95Ms"), "null D3D pipeline latency p95");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P99Ms"), "null D3D pipeline latency p99");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "MaxMs"), "null D3D pipeline latency max");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "EstimatedPipelineLatencyMs"), "null estimated pipeline latency");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy.Evaluate not found.");
        var now = DateTimeOffset.UtcNow;

        var cpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(cpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(cpuPathInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(cpuPathInput, "StartupRequestedUtc", now.AddMilliseconds(-2000));
        SetPropertyOrBackingField(cpuPathInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(cpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(cpuPathInput, "GpuActive", false);
        SetPropertyOrBackingField(cpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(cpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(cpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(cpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(cpuPathInput, "UtcNow", now);

        var cpuPathHealth = evaluate.Invoke(null, new[] { cpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetDoubleProperty(cpuPathHealth, "StartupElapsedMs") >= 2000, "startup elapsed uses supplied clock");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "BlankSuspected"), "CPU path blank suspected");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "StallSuspected"), "CPU path stall suspected");

        var gpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(gpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(gpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(gpuPathInput, "GpuActive", true);
        SetPropertyOrBackingField(gpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(gpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(gpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(gpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(gpuPathInput, "UtcNow", now);

        var gpuPathHealth = evaluate.Invoke(null, new[] { gpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "BlankSuspected"), "GPU path does not use CPU blank suspicion");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "StallSuspected"), "GPU path does not use CPU stall suspicion");

        var timeoutInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(timeoutInput, "IsPreviewing", true);
        SetPropertyOrBackingField(timeoutInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(timeoutInput, "StartupRequestedUtc", now.AddMilliseconds(-1500));
        SetPropertyOrBackingField(timeoutInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(timeoutInput, "RendererAttached", true);
        SetPropertyOrBackingField(timeoutInput, "GpuActive", false);
        SetPropertyOrBackingField(timeoutInput, "FramesArrived", 0L);
        SetPropertyOrBackingField(timeoutInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(timeoutInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(timeoutInput, "UtcNow", now);

        var timeoutHealth = evaluate.Invoke(null, new[] { timeoutInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetBoolProperty(timeoutHealth, "BlankSuspected"), "startup timeout marks blank suspected");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var factoryType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInputFactory");
        var build = factoryType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory.Build not found.");
        var now = DateTimeOffset.UtcNow;

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupRequestedUtc", now.AddMilliseconds(-2500));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1200);
        SetPropertyOrBackingField(input, "LastPresentedTick", 42L);

        var projection = Activator.CreateInstance(projectionType)
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(projection, "RendererAttached", true);
        SetPropertyOrBackingField(projection, "GpuActive", false);
        SetPropertyOrBackingField(projection, "FramesArrived", 55L);
        SetPropertyOrBackingField(projection, "FramesDisplayed", 6L);

        var healthInput = build.Invoke(null, new object[] { input, projection, 999L, now })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory returned null.");
        AssertEqual(true, GetBoolProperty(healthInput, "IsPreviewing"), "health input previewing");
        AssertEqual(true, GetBoolProperty(healthInput, "IsStartupWaitingForFirstVisual"), "health input waiting for first visual");
        AssertEqual(GetPropertyValue(input, "StartupRequestedUtc"), GetPropertyValue(healthInput, "StartupRequestedUtc"), "health input startup request time");
        AssertEqual(1200, GetIntProperty(healthInput, "StartupTimeoutMs"), "health input startup timeout");
        AssertEqual(true, GetBoolProperty(healthInput, "RendererAttached"), "health input renderer attached");
        AssertEqual(false, GetBoolProperty(healthInput, "GpuActive"), "health input GPU active");
        AssertEqual(55L, GetLongProperty(healthInput, "FramesArrived"), "health input frames arrived");
        AssertEqual(6L, GetLongProperty(healthInput, "FramesDisplayed"), "health input frames displayed");
        AssertEqual(42L, GetLongProperty(healthInput, "LastPresentedTick"), "health input last presented tick");
        AssertEqual(999L, GetLongProperty(healthInput, "CurrentTick"), "health input current tick");
        AssertEqual(now, GetPropertyValue(healthInput, "UtcNow"), "health input clock");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "GpuElementVisible", true);
        SetPropertyOrBackingField(input, "CpuElementVisible", false);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuActive", true);
        SetPropertyOrBackingField(d3dProjection, "RendererAttached", true);
        SetPropertyOrBackingField(d3dProjection, "FramesArrived", 101L);
        SetPropertyOrBackingField(d3dProjection, "FramesDisplayed", 99L);
        SetPropertyOrBackingField(d3dProjection, "FramesDropped", 2L);

        var health = Activator.CreateInstance(healthType, new object?[] { null, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var surface = evaluate.Invoke(null, new object?[] { input, d3dProjection, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy returned null.");

        AssertEqual(true, GetBoolProperty(surface, "IsPreviewing"), "surface projection previewing");
        AssertEqual(true, GetBoolProperty(surface, "GpuActive"), "surface projection GPU active");
        AssertEqual(false, GetBoolProperty(surface, "PlaceholderVisible"), "surface projection placeholder visible");
        AssertEqual(true, GetBoolProperty(surface, "GpuElementVisible"), "surface projection GPU element visible");
        AssertEqual(false, GetBoolProperty(surface, "CpuElementVisible"), "surface projection CPU element visible");
        AssertEqual(true, GetBoolProperty(surface, "RendererAttached"), "surface projection renderer attached");
        AssertEqual(101L, GetLongProperty(surface, "FramesArrived"), "surface projection frames arrived");
        AssertEqual(99L, GetLongProperty(surface, "FramesDisplayed"), "surface projection frames displayed");
        AssertEqual(2L, GetLongProperty(surface, "FramesDropped"), "surface projection frames dropped");
        AssertEqual(true, GetBoolProperty(surface, "BlankSuspected"), "surface projection blank suspected");
        AssertEqual(false, GetBoolProperty(surface, "StallSuspected"), "surface projection stall suspected");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotStartupProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "MediaOpened");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-42");
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1250);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", true);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 5);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "visual-timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", true);

        var health = Activator.CreateInstance(healthType, new object?[] { 456.25d, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var startup = evaluate.Invoke(null, new object?[] { input, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy returned null.");

        AssertEqual("WaitingForFirstVisual", GetStringProperty(startup, "State"), "startup projection state");
        AssertEqual("attempt-42", GetStringProperty(startup, "AttemptId"), "startup projection attempt id");
        AssertEqual(456.25d, GetDoubleProperty(startup, "ElapsedMs"), "startup projection elapsed");
        AssertEqual(1250, GetIntProperty(startup, "TimeoutMs"), "startup projection timeout");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalMediaOpened"), "startup projection media opened signal");
        AssertEqual(false, GetBoolProperty(startup, "GpuSignalFirstFrame"), "startup projection first frame signal");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalPlaybackAdvancing"), "startup projection playback signal");
        AssertEqual(requiredSignals, GetPropertyValue(startup, "RequiredSignals"), "startup projection required signals");
        AssertEqual(receivedSignals, GetPropertyValue(startup, "ReceivedSignals"), "startup projection received signals");
        AssertEqual(startupStrategy, GetPropertyValue(startup, "Strategy"), "startup projection strategy");
        AssertEqual("FirstVisual", GetStringProperty(startup, "MissingSignals"), "startup projection missing signals");
        AssertEqual(5, GetIntProperty(startup, "RecoveryAttemptCount"), "startup projection recovery count");
        AssertEqual("visual-timeout", GetStringProperty(startup, "LastFailureReason"), "startup projection failure reason");
        AssertEqual(true, GetBoolProperty(startup, "FirstVisualConfirmed"), "startup projection first visual confirmed");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 42L);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuPlaybackState", "Rendering");
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoWidth", 3840);
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoHeight", 2160);
        SetPropertyOrBackingField(d3dProjection, "GpuPositionMs", 1234.5d);

        var gpuPlayback = evaluate.Invoke(null, new object?[] { input, d3dProjection })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy returned null.");

        AssertEqual("Rendering", GetStringProperty(gpuPlayback, "PlaybackState"), "GPU playback projection state");
        AssertEqual(3840, GetIntProperty(gpuPlayback, "NaturalVideoWidth"), "GPU playback projection natural width");
        AssertEqual(2160, GetIntProperty(gpuPlayback, "NaturalVideoHeight"), "GPU playback projection natural height");
        AssertEqual(1234.5d, GetDoubleProperty(gpuPlayback, "PositionMs"), "GPU playback projection position");
        AssertEqual(42L, GetLongProperty(gpuPlayback, "PositionEventCount"), "GPU playback projection event count");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var controllerType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotController");
        var build = controllerType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "None");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "GpuElementVisible", false);
        SetPropertyOrBackingField(input, "CpuElementVisible", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(input, "FramesDropped", 2L);
        SetPropertyOrBackingField(input, "LastPresentedTick", Environment.TickCount64 - 4000);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-1");
        SetPropertyOrBackingField(input, "StartupRequestedUtc", DateTimeOffset.UtcNow.AddMilliseconds(-2000));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", false);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 3);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", false);
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 7L);

        var snapshot = build.Invoke(null, new[] { input })
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build returned null.");

        AssertEqual(true, GetBoolProperty(snapshot, "IsPreviewing"), "snapshot IsPreviewing");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuActive"), "snapshot GpuActive");
        AssertEqual(true, GetBoolProperty(snapshot, "RendererAttached"), "snapshot RendererAttached");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuElementVisible"), "snapshot GpuElementVisible");
        AssertEqual(true, GetBoolProperty(snapshot, "CpuElementVisible"), "snapshot CpuElementVisible");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(snapshot, "RendererMode"), "CPU renderer mode");
        AssertEqual("WaitingForFirstVisual", GetStringProperty(snapshot, "StartupState"), "startup state passthrough");
        AssertEqual("attempt-1", GetStringProperty(snapshot, "StartupAttemptId"), "startup attempt passthrough");
        AssertEqual("FirstVisual", GetStringProperty(snapshot, "StartupMissingSignals"), "missing signals passthrough");
        AssertEqual(requiredSignals, GetPropertyValue(snapshot, "StartupRequiredSignals"), "required startup signals");
        AssertEqual(receivedSignals, GetPropertyValue(snapshot, "StartupReceivedSignals"), "received startup signals");
        AssertEqual(startupStrategy, GetPropertyValue(snapshot, "StartupStrategy"), "startup strategy");
        AssertEqual(3, GetIntProperty(snapshot, "StartupRecoveryAttemptCount"), "startup recovery count");
        AssertEqual("timeout", GetStringProperty(snapshot, "StartupLastFailureReason"), "startup failure reason");
        AssertEqual(true, GetBoolProperty(snapshot, "StartupGpuSignalMediaOpened"), "media opened signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalFirstFrame"), "first-frame signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalPlaybackAdvancing"), "playback advancing signal");
        AssertEqual(true, GetDoubleProperty(snapshot, "StartupElapsedMs") >= 0, "startup elapsed is non-negative");
        AssertEqual(true, GetBoolProperty(snapshot, "BlankSuspected"), "blank suspected when CPU path receives frames but displays none");
        AssertEqual(true, GetBoolProperty(snapshot, "StallSuspected"), "stall suspected after stale last-presented tick");
        AssertEqual(31L, GetLongProperty(snapshot, "FramesArrived"), "frames arrived passthrough");
        AssertEqual(0L, GetLongProperty(snapshot, "FramesDisplayed"), "frames displayed passthrough");
        AssertEqual(2L, GetLongProperty(snapshot, "FramesDropped"), "frames dropped passthrough");
        AssertEqual(0, GetIntProperty(snapshot, "DisplayCadenceSampleCount"), "no D3D cadence samples");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentCount"), "D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentRefreshCount"), "D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsSyncRefreshCount"), "D3D sync-refresh sentinel");
        AssertEqual("None", GetStringProperty(snapshot, "D3DInputColorSpace"), "D3D input color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "D3DOutputColorSpace"), "D3D output color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "GpuPlaybackState"), "GPU playback fallback");
        AssertEqual(7L, GetLongProperty(snapshot, "GpuPositionEventCount"), "GPU position event count");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioControls_MapsAnalogGainCurveAndClamps()
    {
        var mapperType = RequireType("Sussudio.ViewModels.DeviceAudioGainMapper");
        var mapPercent = mapperType.GetMethod("PercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.PercentToGainByte was not found.");
        var mapByte = mapperType.GetMethod("GainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.GainByteToPercent was not found.");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceAudioModeText, "DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent)");
        AssertContains(deviceAudioStateText, "DeviceAudioGainMapper.PercentToGainByte(gainPercent)");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertDoesNotContain(deviceAudioStateText, "private static byte MapPercentToGainByte");
        AssertDoesNotContain(deviceAudioStateText, "private static double MapGainByteToPercent");
        AssertContains(deviceAudioStateText, "internal static class DeviceAudioGainMapper");
        AssertContains(deviceAudioStateText, "private const double GainCurveK = 4.0;");
        AssertContains(deviceAudioStateText, "internal static byte PercentToGainByte");
        AssertContains(deviceAudioStateText, "internal static double GainByteToPercent");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "DeviceAudioGainMapper.cs")), "DeviceAudioGainMapper folded into MainViewModel.AudioState.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "analog gain XU writes folded into MainViewModel.AudioState.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")), "device audio mode folded into MainViewModel.AudioState.cs");

        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { -25d })!, "PercentToGainByte clamps below zero");
        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { 0d })!, "PercentToGainByte zero");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 100d })!, "PercentToGainByte one hundred");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 150d })!, "PercentToGainByte clamps above one hundred");

        var gain25 = (byte)mapPercent.Invoke(null, new object[] { 25d })!;
        var gain50 = (byte)mapPercent.Invoke(null, new object[] { 50d })!;
        var gain75 = (byte)mapPercent.Invoke(null, new object[] { 75d })!;
        AssertEqual(true, gain25 > 0 && gain25 < gain50 && gain50 < gain75 && gain75 < 255, "PercentToGainByte monotonic curve");

        AssertNear(0d, (double)mapByte.Invoke(null, new object[] { (byte)0 })!, 0.0001d, "GainByteToPercent zero");
        AssertNear(100d, (double)mapByte.Invoke(null, new object[] { (byte)255 })!, 0.0001d, "GainByteToPercent max");
        AssertNear(50d, (double)mapByte.Invoke(null, new object[] { gain50 })!, 1.0d, "GainByteToPercent round-trip midpoint");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetProperty("SuppressVolumeSave", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SuppressVolumeSave");
        AssertNotNull(viewModelType.GetProperty("VolumeSaveOverride", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.VolumeSaveOverride");
        AssertNotNull(viewModelType.GetMethod("SavePreviewVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SavePreviewVolume");

        var audioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var transitionCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/PreviewAudioTransitionControllers.cs");
        var previewChanged = ExtractMemberCode(audioStateCode, "OnPreviewVolumeChanged");
        var handlePreviewChanged = ExtractMemberCode(transitionCode, "HandlePreviewVolumeChanged");
        var rampDown = ExtractMemberCode(transitionCode, "RampDownForAudioTransitionAsync");
        var rampUp = ExtractMemberCode(transitionCode, "RampUpForAudioTransitionAsync");
        var primeTransition = ExtractMemberCode(transitionCode, "PrimeForAudioTransition");
        var restoreTransition = ExtractMemberCode(transitionCode, "RestoreAfterUnavailableAudio");
        var monitoringTransition = ExtractMemberCode(audioStateCode, "SetAudioMonitoringEnabledWithVolumeTransitionAsync");
        var audioPreviewChanged = ExtractMemberCode(audioStateCode, "OnIsAudioPreviewEnabledChanged");
        var applyAudioInputSelection = ExtractMemberCode(audioStateCode, "ApplyAudioInputSelectionAsync");

        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.SuppressVolumeSave;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;");
        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.VolumeSaveOverride;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;");
        AssertContains(previewChanged, "_previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);");
        AssertContains(audioStateCode, "internal void SavePreviewVolume() => SaveSettings();");
        AssertContains(audioStateCode, "private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewVolumeTransitions.cs")), "MainViewModel.PreviewVolumeTransitions.cs folded into audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioMonitoring.cs")), "MainViewModel.AudioMonitoring.cs folded into audio state");
        AssertDoesNotContain(audioStateCode, "private const int PreviewAudioRampDownSteps");
        AssertContains(transitionCode, "internal sealed class PreviewAudioVolumeTransitionController");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "PreviewAudioVolumeTransitionController.Ramps.cs")), "PreviewAudioVolumeTransitionController.Ramps.cs folded into PreviewAudioTransitionControllers.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "PreviewAudioVolumeTransitionController.cs")), "preview audio volume transition controller folded into PreviewAudioTransitionControllers.cs");
        AssertContains(transitionCode, "private const int RampDownSteps = 18;");
        AssertContains(transitionCode, "private const int RampDownDelayMs = 25;");
        AssertContains(transitionCode, "private const int RampUpSteps = 30;");
        AssertContains(transitionCode, "private const int RampUpDelayMs = 30;");

        AssertContains(handlePreviewChanged, "if (!SuppressVolumeSave)");
        AssertContains(handlePreviewChanged, "VolumeSaveOverride = null;");
        AssertContains(handlePreviewChanged, "_context.SetSessionPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));");
        AssertOccursBefore(handlePreviewChanged, "VolumeSaveOverride = null;", "_context.SetSessionPreviewVolume");

        AssertContains(rampDown, "var persistedVolume = PersistedVolumeTarget;");
        AssertContains(rampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(rampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(rampDown, "_context.SetPreviewVolume(0);");
        AssertContains(rampUp, "VolumeSaveOverride = volumeTarget;");
        AssertContains(rampUp, "_context.SetPreviewVolume(volumeTarget * eased);");
        AssertContains(rampUp, "VolumeSaveOverride = null;");
        AssertContains(primeTransition, "var volumeTarget = PersistedVolumeTarget;");
        AssertContains(primeTransition, "_context.SetPreviewVolume(0);");
        AssertContains(restoreTransition, "_context.SetPreviewVolume(volumeTarget);");

        AssertContains(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertOccursBefore(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");

        AssertContains(audioPreviewChanged, "if (value && !IsAudioEnabled)");
        AssertContains(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)");
        AssertContains(audioPreviewChanged, "if (!value && !IsRecording)");
        AssertContains(audioPreviewChanged, "if (IsPreviewing && IsInitialized)");
        AssertContains(audioPreviewChanged, "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");
        AssertContains(audioStateCode, "private async Task ApplyAudioInputSelectionAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioInputSelection.cs")), "MainViewModel.AudioInputSelection.cs folded into audio state");
        AssertOccursBefore(audioPreviewChanged, "if (value && !IsAudioEnabled)", "IsAudioPreviewEnabled = false;");
        AssertOccursBefore(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)", "if (!value && !IsRecording)");
        AssertOccursBefore(audioPreviewChanged, "if (!value && !IsRecording)", "ResetAudioMeter();");
        AssertOccursBefore(audioPreviewChanged, "if (IsPreviewing && IsInitialized)", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");

        AssertContains(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedAudioInputDevice?.Id;");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedDevice?.AudioDeviceId;");
        AssertContains(applyAudioInputSelection, "var shouldRampMonitoring = IsPreviewing && _captureService.IsAudioPreviewActive;");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);");
        AssertContains(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");
        AssertOccursBefore(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioMeters_OwnCallbackMeterState()
    {
        var baseText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioStateText, "public double AudioMeterTarget;");
        AssertContains(audioStateText, "public double MicrophoneMeterTarget;");
        AssertContains(audioStateText, "public event Action? AudioMeterActivated;");
        AssertContains(audioStateText, "public event Action? MicrophoneMeterActivated;");
        AssertContains(audioStateText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(audioStateText, "private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(audioStateText, "private void ResetAudioMeter()");
        AssertContains(audioStateText, "public void ResetAudioMeterTimerFlag()");
        AssertContains(audioStateText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioMeters.cs")),
            "MainViewModel.AudioMeters.cs folded into MainViewModel.AudioState.cs");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertDoesNotContain(baseText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "private const double MeterFloorDb");
        AssertDoesNotContain(baseText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertDoesNotContain(baseText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");

        return Task.CompletedTask;
    }

    internal static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioMeterControllerRootText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();

        AssertContains(mainViewModelStateText, "IsAudioPreviewActive");
        AssertContains(propertyChangedText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(audioControlPresentationControllerText, "HandleAudioPreviewActiveChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);");
        AssertContains(audioMeterText, "private AudioMeterController _audioMeterController = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AudioMeter.cs")),
            "Audio meter adapter folded into MainWindow.xaml.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.MicrophoneControls.cs")),
            "Microphone controls adapter folded into MainWindow.xaml.cs");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerRootText, "internal sealed class AudioMeterController");
        AssertContains(audioMeterControllerRootText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerRootText, "internal sealed class AudioMeterControllerContext");
        AssertContains(audioMeterControllerRootText, "public required MainViewModel ViewModel { get; init; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Audio",
                "AudioMeterController.Context.cs")),
            "AudioMeterController context partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Audio",
                "Meter",
                "AudioMeterController.cs")),
            "audio meter controller folded into AudioControlBindingController.cs");
        AssertContains(audioMeterControllerRootText, "public void AnimateTick()");
        AssertContains(audioMeterControllerRootText, "public void ResetVisuals()");
        AssertContains(audioMeterControllerRootText, "public void ResetMicrophoneVisuals()");
        AssertContains(audioMeterControllerRootText, "public void SetAudioMeterTargetLevel(double targetLevel)");
        AssertContains(audioMeterControllerRootText, "public void EnsureTimerRunning()");
        AssertContains(audioMeterControllerRootText, "public void StopTimer()");
        AssertContains(audioMeterControllerRootText, "public static double TranslateMarker(double trackWidth, double level, double markerWidth)");
        AssertContains(audioMeterControllerRootText, "public void SetMonitoringState(bool isMonitoring)");
        AssertContains(audioMeterControllerRootText, "_audioMeterMonitoringStoryboard?.Stop();");
        AssertContains(audioMeterControllerRootText, "public void AnimateDisabled(bool isDisabled)");
        AssertContains(audioMeterControllerRootText, "private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2");
        AssertContains(audioMeterControllerRootText, "AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3");
        AssertContains(audioMeterControllerRootText, "private static void AddOpacityAnimation(");
        AssertDoesNotContain(audioMeterControllerRootText, "partial class AudioMeterController");

        return Task.CompletedTask;
    }

    internal static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioTransitionControllers.cs")
            .Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderRootText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioTransitionControllers.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderText = audioRampTraceRecorderRootText;
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = playbackText;
        var playbackVolumeText = playbackText;
        var runtimeContractsText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Models/Automation/AutomationModels.cs"))
            .Replace("\r\n", "\n");
        var runtimeSnapshotText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs"))
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText().Replace("\r\n", "\n");
        var automationInterfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs").Replace("\r\n", "\n");

        AssertContains(traceModelsText, "public sealed class AudioRampTraceSnapshot");
        AssertContains(traceModelsText, "public sealed class AudioRampTraceEntry");
        AssertContains(traceModelsText, "public double PlaybackOutputPeak { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackOutputRms { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackCurrentVolumePercent { get; init; }");
        AssertContains(traceModelsText, "public long PlaybackOutputAgeMs { get; init; }");

        AssertContains(audioRampTraceRecorderRootText, "internal sealed class AudioRampTraceRecorder");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "AudioRampTraceRecorder.cs")), "audio ramp trace recorder folded into PreviewAudioTransitionControllers.cs");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceCapacity = 2048;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTraceSampleIntervalMs = 10;");
        AssertContains(audioRampTraceRecorderText, "private const int AudioRampTracePostCompleteSampleMs = 250;");
        AssertContains(audioRampTraceRecorderText, "public long BeginSession(string reason, double targetVolume)");
        AssertContains(audioRampTraceRecorderText, "public void CompleteSession(long sessionId, string reason)");
        AssertContains(audioRampTraceRecorderText, "public void RecordPoint(");
        AssertContains(audioRampTraceRecorderText, "RunSamplerAsync");
        AssertContains(audioRampTraceRecorderText, "Task.Delay(AudioRampTraceSampleIntervalMs");
        AssertContains(audioRampTraceRecorderText, "AUDIO_RAMP_TRACE_SAMPLER_FAIL");
        AssertDoesNotContain(audioRampTraceRecorderRootText, "internal sealed partial class AudioRampTraceRecorder");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.GetSnapshot(maxEntries);");
        AssertContains(audioRampTraceText, "private AudioRampTraceRecorder CreateAudioRampTraceRecorder()");
        AssertContains(audioRampTraceText, "new AudioRampTraceRecorderContext");
        AssertContains(audioRampTraceText, "GetRuntimeSnapshot = () => _captureService.GetRuntimeSnapshot(),");
        AssertContains(audioRampTraceText, "GetPreviewVolume = () => PreviewVolume,");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.BeginSession(reason, targetVolume);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.CompleteSession(sessionId, reason);");
        AssertContains(audioRampTraceText, "=> _audioRampTraceRecorder.RecordPoint(kind, reason, targetVolume, note, sessionId);");
        AssertContains(audioRampTraceText, "private PreviewAudioVolumeTransitionController CreatePreviewAudioVolumeTransitionController()");
        AssertContains(audioRampTraceText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertContains(audioRampTraceText, "SetSessionPreviewVolume = volume => _sessionCoordinator.SetPreviewVolume(volume),");
        AssertContains(audioRampTraceText, "BeginTraceSession = BeginAudioRampTraceSession,");
        AssertDoesNotContain(audioRampTraceText, "private readonly AudioRampTraceEntry[]");
        AssertDoesNotContain(audioRampTraceText, "RunAudioRampTraceSamplerAsync");
        AssertDoesNotContain(rootText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(rootText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertContains(audioVolumeTransitionText, "BeginTraceSession(");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"volume-set\")");
        AssertContains(audioVolumeTransitionText, "RecordTracePoint(\"primed\"");
        AssertContains(audioVolumeTransitionText, "public Task RampDownForStopAsync(CancellationToken cancellationToken)");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "GetAudioRampTraceSnapshotAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioRampTrace.cs")),
            "MainViewModel.AudioRampTrace.cs folded into MainViewModel.AudioState.cs");

        AssertContains(playbackRenderText, "UpdateOutputLevel(destinationSpan);");
        AssertContains(playbackRenderText, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackVolumeText, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(playbackVolumeText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackVolumeText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackVolumeText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackVolumeText, "public float LastOutputRms => _lastOutputRms;");
        AssertContains(playbackVolumeText, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackVolumeText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Volume.cs")),
            "WASAPI playback render-side volume telemetry folded into the playback lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread folded into the playback lifecycle root");

        AssertContains(runtimeContractsText, "public double WasapiPlaybackTargetVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackCurrentVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputRms { get; init; }");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0,");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackOutputPeak = wasapiPlayback?.LastOutputPeak ?? 0,");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(automationInterfaceText, "Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync");

        return Task.CompletedTask;
    }

    private static void AssertNear(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected {expected:0.###} +/- {tolerance:0.###}, actual {actual:0.###}.");
        }
    }
    internal static Task MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetMethod("SaveMicrophoneVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SaveMicrophoneVolume");

        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var deviceAudioRefreshText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioRefreshCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var deviceAudioRequestControllerCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs");
        var deviceAudioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var microphoneVolumeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var audioCode = deviceAudioModeCode + "\n" + deviceAudioRefreshCode + "\n" + deviceAudioRequestControllerCode + "\n" + deviceAudioStateCode + "\n" + microphoneVolumeCode;
        var setMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "SetMicrophoneEndpointVolume");
        var getMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "GetMicrophoneEndpointVolume");
        var refreshDeviceAudioControls = ExtractMemberCode(audioCode, "RefreshDeviceAudioControlsAsync");
        var applyDeviceAudioMode = ExtractMemberCode(audioCode, "ApplyDeviceAudioModeAsync");
        var applyAnalogAudioGain = ExtractMemberCode(audioCode, "ApplyAnalogAudioGainAsync");
        var isCurrentSelectedDevice = ExtractMemberCode(audioCode, "IsCurrentSelectedDevice");
        var suppressedRefresh = ExtractMemberCode(audioCode, "WithAudioControlRefreshSuppressed");
        var normalizeDeviceAudioMode = ExtractMemberCode(audioCode, "NormalizeDeviceAudioMode");

        AssertContains(microphoneVolumeCode, "internal void SaveMicrophoneVolume() => SaveSettings();");
        AssertContains(microphoneVolumeCode, "partial void OnMicrophoneVolumeChanged(double value)");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.MicrophoneVolume.cs")), "MainViewModel.MicrophoneVolume.cs folded into audio state");
        AssertContains(deviceAudioStateCode, "SetMicrophoneEndpointVolume");
        AssertContains(deviceAudioStateCode, "GetMicrophoneEndpointVolume");
        AssertContains(deviceAudioStateCode, "private async Task RefreshDeviceAudioControlsAsync");
        AssertContains(deviceAudioRefreshText, "Device-native audio-control support probing and state readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "Device-native audio mode switching and failure readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRefresh.cs")), "MainViewModel.DeviceAudioRefresh.cs folded into audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")), "MainViewModel.DeviceAudioMode.cs folded into audio state");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertDoesNotContain(deviceAudioStateCode, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(deviceAudioStateCode, "SetInputSourceAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "MainViewModel analog gain writes folded into audio state");

        AssertContains(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)");
        AssertContains(setMicrophoneEndpointVolume, "WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));");
        AssertOccursBefore(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.SetEndpointVolume");

        AssertContains(getMicrophoneEndpointVolume, "return 100.0;");
        AssertContains(getMicrophoneEndpointVolume, "return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;");
        AssertOccursBefore(getMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.GetEndpointVolume");

        AssertContains(refreshDeviceAudioControls, "IsDeviceAudioControlSupported = false;");
        AssertContains(refreshDeviceAudioControls, "SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;");
        AssertContains(refreshDeviceAudioControls, "AnalogAudioGainPercent = 50;");
        AssertContains(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out _, out _)");
        AssertContains(refreshDeviceAudioControls, "await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedAnalogAudioGainPercent = null;");
        AssertOccursBefore(refreshDeviceAudioControls, "if (device == null)", "IsDeviceAudioControlSupported = false;");
        AssertOccursBefore(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds", "var state = await _deviceAudioControlService.ReadStateAsync");
        var refreshInitialReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var state = await _deviceAudioControlService.ReadStateAsync",
            "WithAudioControlRefreshSuppressed(() =>");
        AssertContains(refreshInitialReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshInitialReadback, "if (!IsCurrentSelectedDevice(device))");
        var refreshRestoreReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var refreshedState = await _deviceAudioControlService.ReadStateAsync",
            "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshRestoreReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshRestoreReadback, "if (!IsCurrentSelectedDevice(device))");

        AssertContains(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)");
        AssertContains(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))");
        AssertContains(applyDeviceAudioMode, "var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);");
        AssertContains(applyDeviceAudioMode, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent);");
        AssertContains(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte, cancellationToken)");
        AssertContains(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(applyDeviceAudioMode, "StatusText =");
        AssertContains(applyDeviceAudioMode, "if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))");
        AssertContains(applyDeviceAudioMode, "ApplyAnalogAudioGainAsync(");
        AssertContains(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);");
        AssertContains(applyDeviceAudioMode, "if (persistSettings)");
        AssertOccursBefore(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync", "var failureState = await _deviceAudioControlService.ReadStateAsync");
        AssertOccursBefore(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync", "StatusText =");
        AssertOccursBefore(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);", "if (persistSettings)");

        AssertContains(applyAnalogAudioGain, "var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);");
        AssertContains(applyAnalogAudioGain, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);");
        AssertContains(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(applyAnalogAudioGain, "StatusText =");
        AssertContains(applyAnalogAudioGain, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertContains(applyAnalogAudioGain, "SaveSettings();");
        AssertOccursBefore(applyAnalogAudioGain, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)", "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertOccursBefore(applyAnalogAudioGain, "if (persistSettings)", "SaveSettings();");

        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase)");
        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase)");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;");
        AssertContains(suppressedRefresh, "finally");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = false;");
        AssertOccursBefore(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;", "try");
        AssertOccursBefore(suppressedRefresh, "finally", "_isRefreshingDeviceAudioControls = false;");
        AssertContains(normalizeDeviceAudioMode, "string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)");
        AssertContains(normalizeDeviceAudioMode, "? DeviceAudioMode.Analog");
        AssertContains(normalizeDeviceAudioMode, ": DeviceAudioMode.Hdmi;");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime()
    {
        var deviceAudioRequestControllerCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");
        var deviceAudioStateCode = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var controllerStart = deviceAudioRequestControllerCode.IndexOf(
            "internal sealed class MainViewModelDeviceAudioRequestController",
            StringComparison.Ordinal);
        AssertEqual(true, controllerStart >= 0, "device audio request controller class marker");
        var controllerBody = deviceAudioRequestControllerCode[controllerStart..];
        var handleModeChange = ExtractMemberCode(controllerBody, "HandleSelectedDeviceAudioModeChanged");
        var refreshControls = ExtractMemberCode(controllerBody, "RequestDeviceAudioControlsRefresh");
        var cancelWork = ExtractMemberCode(controllerBody, "CancelPendingAudioControlWork");
        var handleGainChange = ExtractMemberCode(controllerBody, "HandleAnalogAudioGainPercentChanged");
        var flashPersist = ExtractMemberCode(controllerBody, "ScheduleAnalogGainFlashPersist");

        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainFlashDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainXuDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioModeCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioRefreshCts;");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioStateCode, "=> _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioRequestControllerCode, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerCode, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");

        AssertContains(refreshControls, "_deviceAudioRefreshCts = refreshCts;");
        AssertContains(refreshControls, "_context.RefreshDeviceAudioControlsAsync(targetDevice, true, refreshToken)");
        AssertContains(refreshControls, "\"device audio controls refresh\", true");
        AssertContains(refreshControls, "if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))");
        AssertContains(refreshControls, "refreshCts.Dispose();");

        AssertContains(handleModeChange, "oldCts?.Cancel();");
        AssertContains(handleModeChange, "_deviceAudioModeCts = cts;");
        AssertContains(handleModeChange, "_context.ApplyDeviceAudioModeAsync(\"device audio mode change\", targetDevice, token)");
        AssertContains(handleModeChange, "if (ReferenceEquals(_deviceAudioModeCts, cts))");
        AssertContains(handleModeChange, "cts.Dispose();");
        AssertContains(handleModeChange, "_context.SaveSettings();");

        AssertContains(handleGainChange, "oldCts?.Cancel();");
        AssertContains(handleGainChange, "_gainXuDebounceCts = cts;");
        AssertContains(handleGainChange, "await Task.Delay(200, token).ConfigureAwait(false);");
        AssertContains(handleGainChange, "_context.ApplyAnalogAudioGainAsync(\"analog audio gain change\", targetDevice, token)");
        AssertContains(handleGainChange, "if (ReferenceEquals(_gainXuDebounceCts, cts))");
        AssertContains(handleGainChange, "cts.Dispose();");
        AssertContains(handleGainChange, "_context.SaveSettings();");

        AssertContains(flashPersist, "oldCts?.Cancel();");
        AssertContains(flashPersist, "_gainFlashDebounceCts = cts;");
        AssertContains(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);");
        AssertContains(flashPersist, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertContains(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))");
        AssertContains(flashPersist, "cts.Dispose();");
        AssertOccursBefore(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);", "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertOccursBefore(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))", "cts.Dispose();");

        AssertContains(cancelWork, "var flashCts = _gainFlashDebounceCts;");
        AssertContains(cancelWork, "_gainFlashDebounceCts = null;");
        AssertContains(cancelWork, "flashCts?.Cancel();");
        AssertContains(cancelWork, "var xuCts = _gainXuDebounceCts;");
        AssertContains(cancelWork, "_gainXuDebounceCts = null;");
        AssertContains(cancelWork, "xuCts?.Cancel();");
        AssertContains(cancelWork, "var modeCts = _deviceAudioModeCts;");
        AssertContains(cancelWork, "_deviceAudioModeCts = null;");
        AssertContains(cancelWork, "modeCts?.Cancel();");
        AssertContains(cancelWork, "var refreshCts = _deviceAudioRefreshCts;");
        AssertContains(cancelWork, "_deviceAudioRefreshCts = null;");
        AssertContains(cancelWork, "refreshCts?.Cancel();");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAudioControlService_LivesInCohesiveServiceFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "internal sealed class NativeXuAudioControlService");
        AssertDoesNotContain(rootText, "partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static readonly int[] InputByteIndexes");
        AssertContains(rootText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(rootText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(rootText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(rootText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(rootText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(rootText, "private static byte[] ParseHex(string hex)");
        AssertContains(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(rootText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(rootText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(rootText, "private static bool TryReadRawPayload(");
        AssertContains(rootText, "private static bool TryWriteRawPayload(");
        AssertContains(rootText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(rootText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(rootText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(rootText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(rootText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(rootText, "private readonly record struct GainProfile");
        AssertContains(rootText, "private readonly record struct RawControlCandidate");
        AssertContains(rootText, "private readonly record struct RawPayloadSnapshot");
        AssertDoesNotContain(rootText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(");
        AssertContains(deviceSupportText, "public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuDeviceSupport.cs");
        foreach (var removedFile in new[]
        {
            "NativeXuAudioControlService.Profiles.cs",
            "NativeXuAudioControlService.Transport.cs",
            "NativeXuAudioControlService.RawTransport.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_LivesInFocusedHelper()
    {
        var adapterText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioDeviceDiscovery.cs")), "audio device discovery adapter stays folded into AudioState");
        AssertContains(policyText, "internal static class AudioDeviceSelectionPolicy");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(policyText, "internal static IReadOnlyList<AudioInputDevice> FilterOutCaptureCardAudio(");
        AssertContains(policyText, "SelectByPreviousSavedOrFirst(availableDevices, previousAudioId, savedAudioId)");
        AssertContains(policyText, "SelectByPreviousOrFirst(availableDevices, previousAudioId)");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(adapterText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "Logger.Log($\"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.\");");
        AssertContains(adapterText, "Logger.Log($\"Audio device list refreshed ({AudioInputDevices.Count} devices).\");");
        AssertDoesNotContain(policyText, "ReplaceCollection(");
        AssertDoesNotContain(policyText, "Logger.Log(");
        AssertDoesNotContain(policyText, "_pendingSaved");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("CAPTURE-AUDIO"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.CaptureDevice",
            CreateAudioDeviceSelectionPolicyCapture("video-first", "other-capture"),
            CreateAudioDeviceSelectionPolicyCapture("video-previous", "capture-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "video-previous",
            "missing-audio",
            "saved-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Startup audio list filters the capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Startup first filtered audio id");
        AssertEqual("saved-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup saved audio fallback");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup saved audio found");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup saved microphone found");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");

        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup preserves previous audio");
        AssertEqual("previous-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup preserves previous microphone");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup keeps existing saved-audio fallback log decision");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup keeps existing saved-microphone fallback log decision");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("capture-audio"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            "CAPTURE-AUDIO",
            "previous-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Refresh audio list filters selected capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Refresh first filtered audio id");
        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Refresh preserves previous audio");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Refresh saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Refresh does not log saved audio fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Refresh does not log saved microphone fallback");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.AudioInputDevice");
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var startupSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(startupSelection).Length, "Startup empty audio list");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedAudioInputDevice"), "Startup empty audio selection");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedMicrophoneDevice"), "Startup empty microphone selection");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedAudioFallback"), "Startup empty saved audio fallback log decision");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedMicrophoneFallback"), "Startup empty saved microphone fallback log decision");

        var refreshSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            null,
            "previous-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(refreshSelection).Length, "Refresh empty audio list");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedAudioInputDevice"), "Refresh empty audio selection");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedMicrophoneDevice"), "Refresh empty microphone selection");
        AssertEqual(false, GetBoolProperty(refreshSelection, "ShouldLogSavedMicrophoneFallback"), "Refresh empty saved microphone log decision");

        return Task.CompletedTask;
    }

    private static object InvokeAudioDeviceSelectionPolicy(string methodName, params object?[] arguments)
    {
        var policyType = RequireType("Sussudio.ViewModels.AudioDeviceSelectionPolicy");
        var method = policyType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing AudioDeviceSelectionPolicy.{methodName}.");
        return method.Invoke(null, arguments)
               ?? throw new InvalidOperationException($"AudioDeviceSelectionPolicy.{methodName} returned null.");
    }

    private static object CreateAudioDeviceSelectionPolicyAudio(string id)
    {
        var audioType = RequireType("Sussudio.Models.AudioInputDevice");
        var audio = Activator.CreateInstance(audioType)
            ?? throw new InvalidOperationException("Failed to create AudioInputDevice.");
        SetPropertyOrBackingField(audio, "Id", id);
        SetPropertyOrBackingField(audio, "Name", id);
        return audio;
    }

    private static object CreateAudioDeviceSelectionPolicyCapture(string id, string? audioDeviceId)
    {
        var captureType = RequireType("Sussudio.Models.CaptureDevice");
        var capture = Activator.CreateInstance(captureType)
            ?? throw new InvalidOperationException("Failed to create CaptureDevice.");
        SetPropertyOrBackingField(capture, "Id", id);
        SetPropertyOrBackingField(capture, "Name", id);
        SetPropertyOrBackingField(capture, "AudioDeviceId", audioDeviceId);
        return capture;
    }

    private static object CreateAudioDeviceSelectionPolicyList(string elementTypeName, params object[] items)
    {
        var elementType = RequireType(elementTypeName);
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
            ?? throw new InvalidOperationException($"Failed to create list for {elementTypeName}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static string? GetAudioDeviceSelectionId(object selection, string propertyName)
    {
        var device = GetPropertyValue(selection, propertyName);
        return device != null ? GetStringProperty(device, "Id") : null;
    }

    private static string[] GetAudioDeviceSelectionAvailableIds(object selection)
    {
        var devices = (IEnumerable)(GetPropertyValue(selection, "AvailableDevices")
            ?? throw new InvalidOperationException("AudioDeviceSelection.AvailableDevices was null."));
        return devices.Cast<object>().Select(device => GetStringProperty(device, "Id")).ToArray();
    }

    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var dependenciesText = compositionText;
        var constructorText = ExtractTextBetween(
            compositionText,
            "internal MainViewModel(MainViewModelDependencies dependencies)",
            "public Task InitializeAsync()");

        AssertContains(rootText, "public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel");
        AssertContains(rootText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "private readonly DeviceService _deviceService;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Composition.cs")),
            "MainViewModel.Composition.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelControllerGraph.cs")),
            "MainViewModelControllerGraph folded into MainViewModel.cs");
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
        AssertDoesNotContain(constructorText, "new MainViewModelUiDispatchController(");
        AssertDoesNotContain(constructorText, "new MainViewModelRecordingTransitionController(this)");
        AssertDoesNotContain(constructorText, "new MainViewModelRuntimeLifecycleController(this)");
        AssertDoesNotContain(constructorText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(constructorText, "_captureService = new CaptureService();");
        AssertDoesNotContain(constructorText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(constructorText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(constructorText, "_audioDeviceWatcher = new AudioDeviceWatcher();");
        AssertDoesNotContain(constructorText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(constructorText, "new PreviewAudioVolumeTransitionControllerContext");
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
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
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
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");

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
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");
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
        AssertContains(previewLifecycleControllerText, "public required Action<CaptureDevice?> SetSelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<MainViewModelCaptureSelectionSnapshot> CaptureSelectionSnapshot { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<MainViewModelCaptureSelectionSnapshot, MainViewModelCaptureSelectionSnapshot, bool> RestoreCaptureSelectionSnapshotIfUnchanged { get; init; }");
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
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = audioStateText;
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = captureSettingsAutomationControllerText;
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
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
        AssertContains(controllerGraphText, "CaptureSelectionSnapshot = viewModel.CaptureSelectionSnapshot,");
        AssertContains(controllerGraphText, "RestoreCaptureSelectionSnapshotIfUnchanged = viewModel.RestoreCaptureSelectionSnapshotIfUnchanged,");
        AssertContains(controllerGraphText, "SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,");
        AssertContains(controllerGraphText, "ReinitializeDeviceWithResultAsync = viewModel.ReinitializeDeviceWithResultAsync,");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelCaptureModeOptionRebuildController.cs")),
            "capture mode option rebuild controller folded into MainViewModelDeviceControllers.cs");
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
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
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


// MainWindow lifecycle and launch contracts live with the presentation-preview xUnit wrappers.
    internal static Task MainWindowNativeBootstrap_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var nativeWindowText = ReadMainWindowShellChromeAdapterSource();
        var nativeWindowControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var nativeBootstrapOwner = "Sussudio/Controllers/Window/WindowControllers.cs";

        AssertContains(agentMapText, nativeBootstrapOwner);
        AssertContains(cleanupPlanText, nativeBootstrapOwner);
        AssertContains(agentMapText, "Sussudio/MainWindow.xaml.cs");
        AssertContains(cleanupPlanText, "Sussudio/MainWindow.xaml.cs");
        AssertContains(agentMapText, "owns native window");
        AssertContains(cleanupPlanText, "DWM cloak/dark-mode setup");
        AssertContains(agentMapText, "first-composed-frame");
        AssertContains(cleanupPlanText, "first-composed-frame shell reveal");
        AssertContains(mainWindowText, "InitializeShellControllers();");
        AssertContains(mainWindowText, "var appWindow = InitializeNativeShellWindow();");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "var appWindow = InitializeNativeShellWindow();", "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "RegisterCloseLifecycle(appWindow);", "InitializeShellControllers();");
        AssertDoesNotContain(mainWindowText, "WindowNative.GetWindowHandle(this);");
        AssertDoesNotContain(mainWindowText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(mainWindowText, "MinSizeWindowSubclass.Install(");
        AssertDoesNotContain(mainWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");

        AssertContains(nativeWindowText, "private readonly NativeWindowBootstrapController _nativeWindowBootstrapController = new();");
        AssertContains(nativeWindowText, "private IntPtr _hwnd;");
        AssertContains(nativeWindowText, "private AppWindow InitializeNativeShellWindow()");
        AssertContains(nativeWindowText, "var result = _nativeWindowBootstrapController.Initialize(this, ViewModel.SetWindowHandle);");
        AssertContains(nativeWindowText, "_hwnd = result.Hwnd;");
        AssertContains(nativeWindowText, "return result.AppWindow;");
        AssertContains(nativeWindowText, "private AppWindow GetAppWindow()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.GetAppWindow(this);");
        AssertContains(nativeWindowText, "private void ScheduleNativeShellRevealAfterFirstFrame()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.ScheduleRevealAfterFirstComposedFrame(_hwnd);");
        AssertContains(nativeWindowText, "private void CancelNativeShellRevealAfterFirstFrame()");
        AssertContains(nativeWindowText, "=> _nativeWindowBootstrapController.CancelPendingFirstFrameReveal();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "native window adapter folded into MainWindow.xaml.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "NativeWindowBootstrapController.cs")),
            "native window bootstrap lives with the window lifecycle controller");
        AssertDoesNotContain(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertDoesNotContain(nativeWindowText, "MinSizeWindowSubclass.Install(");

        AssertContains(nativeWindowControllerText, "internal readonly record struct NativeWindowBootstrapResult(IntPtr Hwnd, AppWindow AppWindow);");
        AssertContains(nativeWindowControllerText, "internal sealed class NativeWindowBootstrapController");
        AssertContains(nativeWindowControllerText, "private const int MinWindowWidth = 1500;");
        AssertContains(nativeWindowControllerText, "private const int MinWindowHeight = 900;");
        AssertContains(nativeWindowControllerText, "private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;");
        AssertContains(nativeWindowControllerText, "private const int DWMWA_CLOAK = 13;");
        AssertContains(nativeWindowControllerText, "private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;");
        AssertContains(nativeWindowControllerText, "private EventHandler<object>? _pendingFirstFrameReveal;");
        AssertContains(nativeWindowControllerText, "public NativeWindowBootstrapResult Initialize(Window window, Action<IntPtr> setWindowHandle)");
        AssertContains(nativeWindowControllerText, "var hwnd = WindowNative.GetWindowHandle(window);");
        AssertContains(nativeWindowControllerText, "setWindowHandle(hwnd);");
        AssertContains(nativeWindowControllerText, "SetCloaked(hwnd, cloaked: true);");
        AssertContains(nativeWindowControllerText, "SetDarkMode(hwnd, enabled: true);");
        AssertContains(nativeWindowControllerText, "MinSizeWindowSubclass.Install(hwnd, MinWindowWidth, MinWindowHeight);");
        AssertContains(nativeWindowControllerText, "if (appWindow.Presenter is OverlappedPresenter presenter)");
        AssertContains(nativeWindowControllerText, "presenter.IsResizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.IsMaximizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.IsMinimizable = true;");
        AssertContains(nativeWindowControllerText, "presenter.Restore();");
        AssertContains(nativeWindowControllerText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertContains(nativeWindowControllerText, "appWindow.SetIcon(\"Assets\\\\AppIcon.ico\");");
        AssertContains(nativeWindowControllerText, "public AppWindow GetAppWindow(Window window)");
        AssertContains(nativeWindowControllerText, "var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);");
        AssertContains(nativeWindowControllerText, "return AppWindow.GetFromWindowId(windowId);");
        AssertContains(nativeWindowControllerText, "public void SetCloaked(IntPtr hwnd, bool cloaked)");
        AssertContains(nativeWindowControllerText, "DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));");
        AssertContains(nativeWindowControllerText, "public void ScheduleRevealAfterFirstComposedFrame(IntPtr hwnd)");
        AssertContains(nativeWindowControllerText, "CancelPendingFirstFrameReveal();");
        AssertContains(nativeWindowControllerText, "EventHandler<object>? revealOnFirstFrame = null;");
        AssertContains(nativeWindowControllerText, "_pendingFirstFrameReveal = revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "SetCloaked(hwnd, cloaked: false);");
        AssertContains(nativeWindowControllerText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "public void CancelPendingFirstFrameReveal()");
        AssertContains(nativeWindowControllerText, "var pending = _pendingFirstFrameReveal;");
        AssertContains(nativeWindowControllerText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= pending;");
        AssertContains(nativeWindowControllerText, "_pendingFirstFrameReveal = null;");
        AssertContains(nativeWindowControllerText, "private static void SetDarkMode(IntPtr hwnd, bool enabled)");
        AssertContains(nativeWindowControllerText, "DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));");
        AssertOccursBefore(
            nativeWindowControllerText,
            "CancelPendingFirstFrameReveal();",
            "SetCloaked(hwnd, cloaked: false);");
        AssertOccursBefore(
            nativeWindowControllerText,
            "_pendingFirstFrameReveal = revealOnFirstFrame;",
            "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;");
        AssertContains(nativeWindowControllerText, "private static extern int DwmSetWindowAttribute(");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseLifecycleAndShutdownCleanup_AreSplit()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var oldWindowManagementPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "MainWindow.WindowManagement.cs");
        var documentedOwners = new[]
        {
            "Sussudio/Controllers/Window/WindowControllers.cs",
            "Sussudio/Controllers/Window/WindowControllers.cs",
            "Sussudio/MainWindow.xaml.cs",
            "Sussudio/MainWindow.xaml.cs",
        };

        foreach (var owner in documentedOwners)
        {
            AssertContains(agentMapText, owner);
            AssertContains(cleanupPlanText, owner);
        }

        if (File.Exists(oldWindowManagementPath))
        {
            throw new InvalidOperationException("MainWindow.WindowManagement.cs should not return as a catch-all partial.");
        }

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShutdownCleanup.Composition.cs")),
            "shutdown cleanup adapter folded into MainWindow root composition");

        AssertContains(mainWindowText, "ViewModel = new MainViewModel();");
        AssertContains(mainWindowText, "InitializeWindowCloseRequestController();");
        AssertOccursBefore(mainWindowText, "ViewModel = new MainViewModel();", "InitializeWindowCloseRequestController();");
        AssertContains(mainWindowText, "InitializeWindowShutdownCleanupController();");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "_automationHostLifecycleController = new WindowAutomationHostLifecycleController(");
        AssertContains(mainWindowText, "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "InitializeWindowShutdownCleanupController();", "RegisterCloseLifecycle(appWindow);");
        AssertOccursBefore(mainWindowText, "InitializeWindowCloseRequestController();", "RegisterCloseLifecycle(appWindow);");
        AssertContains(mainWindowText, "Closed += MainWindow_Closed;");
        AssertDoesNotContain(mainWindowText, "WindowNative.GetWindowHandle(this);");
        AssertDoesNotContain(mainWindowText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(mainWindowText, "MinSizeWindowSubclass.Install(");
        AssertDoesNotContain(mainWindowText, "appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));");
        AssertDoesNotContain(mainWindowText, "private int _windowCloseRequested;");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseLifecycleControllers_OwnCloseRequestAndAppClosing()
    {
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var appClosingControllerText = closeLifecycleControllerText;
        var closeRequestControllerText = closeLifecycleControllerText;

        AssertContains(closeLifecycleText, "private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();");
        AssertContains(closeLifecycleText, "private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();");
        AssertContains(closeLifecycleText, "private WindowCloseRequestController _windowCloseRequestController = null!;");
        AssertContains(closeLifecycleText, "private WindowAppClosingController _windowAppClosingController = null!;");
        AssertContains(closeLifecycleText, "private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;");
        AssertContains(closeLifecycleText, "private void InitializeWindowCloseRequestController()");
        AssertContains(closeLifecycleText, "_windowAppClosingController = new WindowAppClosingController(new WindowAppClosingControllerContext");
        AssertContains(closeLifecycleText, "CloseWindow = Close,");
        AssertContains(closeLifecycleText, "ExitApplication = () => Application.Current.Exit(),");
        AssertContains(closeLifecycleText, "IsRecording = () => ViewModel.IsRecording,");
        AssertContains(closeLifecycleText, "IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning");
        AssertContains(closeLifecycleText, "GetStatusText = () => ViewModel.StatusText,");
        AssertContains(closeLifecycleText, "StopRecordingBeforeCloseAsync = TryStopRecordingBeforeCloseAsync,");
        AssertContains(closeLifecycleText, "RequestWindowClose = RequestWindowClose");
        AssertContains(closeLifecycleText, "private void RegisterCloseLifecycle(AppWindow appWindow)");
        AssertContains(closeLifecycleText, "=> appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "private async void MainWindow_Closing(");
        AssertContains(closeLifecycleText, "=> await _windowAppClosingController.HandleClosingAsync(args);");
        AssertContains(closeLifecycleText, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertContains(closeLifecycleText, "=> _windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken);");
        AssertContains(closeLifecycleText, "private void RequestWindowClose()");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");

        AssertContains(appClosingControllerText, "internal sealed class WindowAppClosingControllerContext");
        AssertContains(appClosingControllerText, "internal sealed class WindowAppClosingController");
        AssertContains(appClosingControllerText, "public async Task HandleClosingAsync(AppWindowClosingEventArgs args)");
        AssertContains(appClosingControllerText, "LogWindowClosingTrigger();");
        AssertContains(appClosingControllerText, "if (!_context.IsRecording() && !_context.IsRecordingTransitioning())");
        AssertContains(appClosingControllerText, "args.Cancel = true;");
        AssertContains(appClosingControllerText, "_context.LifecycleController.ClearRequested();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.TryBeginRecordingStop()");
        AssertContains(appClosingControllerText, "var stopped = await _context.StopRecordingBeforeCloseAsync();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()))");
        AssertContains(appClosingControllerText, "_context.LifecycleController.AllowAfterRecordingStop();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(appClosingControllerText, "_context.RequestWindowClose();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.EndRecordingStop();");
        AssertContains(appClosingControllerText, "WINDOW_CLOSING_TRIGGER ");

        AssertContains(closeLifecycleControllerText, "internal sealed class WindowCloseLifecycleController");
        AssertContains(closeLifecycleControllerText, "private int _closeRequested;");
        AssertContains(closeLifecycleControllerText, "private int _cleanupStarted;");
        AssertContains(closeLifecycleControllerText, "private int _recordingStopInProgress;");
        AssertContains(closeLifecycleControllerText, "private int _allowedAfterRecordingStop;");
        AssertContains(closeLifecycleControllerText, "private TaskCompletionSource<object?>? _completion;");
        AssertContains(closeLifecycleControllerText, "public bool TryBeginCleanup()");
        AssertContains(closeLifecycleControllerText, "public void MarkClosing()");
        AssertContains(closeLifecycleControllerText, "public Task CloseAsync(");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "public static bool IsCloseAlreadyInProgressException(Exception ex)");

        AssertContains(closeRequestControllerText, "internal sealed class WindowCloseRequestControllerContext");
        AssertContains(closeRequestControllerText, "public required WindowCloseLifecycleController LifecycleController { get; init; }");
        AssertContains(closeRequestControllerText, "public required Action CloseWindow { get; init; }");
        AssertContains(closeRequestControllerText, "public required Action ExitApplication { get; init; }");
        AssertContains(closeRequestControllerText, "public required Func<bool> IsRecording { get; init; }");
        AssertContains(closeRequestControllerText, "internal sealed class WindowCloseRequestController");
        AssertContains(closeRequestControllerText, "public void RequestClose()");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.TryMarkRequested()");
        AssertContains(closeRequestControllerText, "_context.CloseWindow();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.IsRecordingStopInProgress");
        AssertContains(closeRequestControllerText, "!_context.IsRecording()");
        AssertContains(closeRequestControllerText, "!_context.IsRecordingTransitioning()");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeRequestControllerText, "WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex)");
        AssertContains(closeRequestControllerText, "catch (COMException ex)");
        AssertContains(closeRequestControllerText, "_context.ExitApplication();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.ResetRequestedAfterFailure();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowCloseRequestController.cs")),
            "close request execution lives with close lifecycle policy");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowAppClosingController.cs")),
            "app closing choreography lives with close lifecycle policy");

        AssertDoesNotContain(closeLifecycleText, "args.Cancel = true;");
        AssertDoesNotContain(closeLifecycleText, "if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)");
        AssertDoesNotContain(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertDoesNotContain(closeLifecycleText, "private int _windowCloseRequested;");
        AssertDoesNotContain(closeLifecycleText, "private TaskCompletionSource<object?>? _windowCloseCompletion;");
        AssertDoesNotContain(closeLifecycleText, "private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)");
        AssertDoesNotContain(closeLifecycleText, "private static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertDoesNotContain(closeLifecycleText, "WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex)");
        AssertDoesNotContain(closeLifecycleText, "catch (System.Runtime.InteropServices.COMException ex)");
        AssertDoesNotContain(closeLifecycleText, "Window.Close COMException");
        AssertDoesNotContain(closeLifecycleText, "ResetRequestedAfterFailure();");
        AssertDoesNotContain(closeLifecycleText, "WINDOW_CLOSING_TRIGGER ");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");

        return Task.CompletedTask;
    }

    internal static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs")
            .Replace("\r\n", "\n");
        var appClosingControllerText = closeLifecycleControllerText;
        var closeRequestControllerText = closeLifecycleControllerText;
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "RegisterCloseLifecycle(appWindow);");
        AssertContains(closeLifecycleText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "_windowAppClosingController.HandleClosingAsync(args)");
        AssertContains(appClosingControllerText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(appClosingControllerText, "if (!_context.IsRecording() && !_context.IsRecordingTransitioning())");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(appClosingControllerText, "_context.RequestWindowClose();");
        AssertContains(closeLifecycleText, "_windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken)");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");
        AssertContains(closeRequestControllerText, "_context.CloseWindow();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeRequestControllerText, "_context.ExitApplication();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()))");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "var enqueueFailure = new InvalidOperationException(\"Failed to enqueue window close action on the UI thread.\");");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(closeRecordingFinalizationControllerText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");
        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(closeLifecycleText, "args.Cancel = true;");
        AssertDoesNotContain(closeLifecycleText, "MP4 may be truncated.");

        return Task.CompletedTask;
    }

    internal static Task MainWindowCloseRecordingFinalization_OwnsRecordingStopPolicy()
    {
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var stopBeforeCloseMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task<bool> StopBeforeCloseAsync(");
        var stopAfterClosedMethodOffset = closeRecordingFinalizationControllerText.IndexOf("public async Task<RecordingStopWaitResult> StopAfterClosedBestEffortAsync(");
        var waitForStopMethodOffset = closeRecordingFinalizationControllerText.IndexOf("private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(");

        if (stopBeforeCloseMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must expose pre-close recording stop.");
        }

        if (stopAfterClosedMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must expose post-close best-effort stop.");
        }

        if (waitForStopMethodOffset < 0)
        {
            throw new InvalidOperationException("Window close recording finalization controller must keep recording-stop wait mechanics in a helper.");
        }

        var stopBeforeCloseMethodText = closeRecordingFinalizationControllerText.Substring(
            stopBeforeCloseMethodOffset,
            stopAfterClosedMethodOffset - stopBeforeCloseMethodOffset);
        var stopAfterClosedMethodText = closeRecordingFinalizationControllerText.Substring(
            stopAfterClosedMethodOffset,
            waitForStopMethodOffset - stopAfterClosedMethodOffset);
        var waitForStopMethodText = closeRecordingFinalizationControllerText.Substring(waitForStopMethodOffset);

        AssertContains(closeLifecycleText, "private Task<bool> TryStopRecordingBeforeCloseAsync()");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(shutdownCleanupText, "StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
        AssertContains(closeRecordingFinalizationControllerText, "internal sealed class WindowCloseRecordingFinalizationController");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "public async Task<bool> StopBeforeCloseAsync(");
        AssertContains(closeRecordingFinalizationControllerText, "public async Task<RecordingStopWaitResult> StopAfterClosedBestEffortAsync(");
        AssertContains(closeRecordingFinalizationControllerText, "internal enum RecordingStopWaitStatus");
        AssertContains(closeRecordingFinalizationControllerText, "internal readonly record struct RecordingStopWaitResult");
        AssertContains(closeRecordingFinalizationControllerText, "private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(MainViewModel viewModel)");
        AssertContains(stopBeforeCloseMethodText, "var stopResult = await WaitForRecordingStopAsync(viewModel);");
        AssertContains(stopAfterClosedMethodText, "var stopResult = await WaitForRecordingStopAsync(viewModel);");
        AssertContains(stopAfterClosedMethodText, "viewModel.MarkRecordingFinalizationUnresolved(");
        AssertContains(stopAfterClosedMethodText, "return RecordingStopWaitResult.Failed(ex.Message);");
        AssertDoesNotContain(stopBeforeCloseMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertDoesNotContain(stopAfterClosedMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(waitForStopMethodText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(waitForStopMethodText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.IsHitTestVisible = false;");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.Opacity = 0.5;");
        AssertContains(closeRecordingFinalizationControllerText, "if (shutdownContent != null &&");
        AssertContains(closeRecordingFinalizationControllerText, "!isAllowedAfterRecordingStop())");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.IsHitTestVisible = true;");
        AssertContains(closeRecordingFinalizationControllerText, "shutdownContent.Opacity = 1;");
        AssertDoesNotContain(stopAfterClosedMethodText, "shutdownContent.IsHitTestVisible = true;");
        AssertDoesNotContain(stopAfterClosedMethodText, "shutdownContent.Opacity = 1;");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_TIMEOUT ");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording.");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "window already closed; continuing shutdown cleanup.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");

        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopBudgetMs");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(shutdownCleanupText, "Task.WhenAny(");
        AssertDoesNotContain(shutdownCleanupText, "StopBudgetMs");
        AssertDoesNotContain(shutdownCleanupText, "StopRecordingAndWaitAsync");

        return Task.CompletedTask;
    }

    internal static Task MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder()
    {
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");

        AssertContains(shutdownCleanupText, "private WindowShutdownCleanupController _windowShutdownCleanupController = null!;");
        AssertContains(shutdownCleanupText, "private WindowShutdownCleanupController _windowShutdownCleanupController = null!;");
        AssertContains(shutdownCleanupText, "private void InitializeWindowShutdownCleanupController()");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(shutdownCleanupText, "=> await _windowShutdownCleanupController.RunAsync();");
        AssertContains(shutdownCleanupText, "StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
        AssertContains(shutdownCleanupText, "DisposeAutomationHostAsync = () => _automationHostLifecycleController.DisposeAsync(),");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");

        AssertContains(automationHostControllerText, "public async ValueTask DisposeAsync()");
        AssertContains(automationHostControllerText, "await _pipeServer.DisposeAsync();");
        AssertContains(automationHostControllerText, "await _diagnosticsHub.DisposeAsync();");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation diagnostics shutdown cleanup failed: {ex.Message}\");");
        AssertOccursBefore(automationHostControllerText, "await _pipeServer.DisposeAsync();", "await _diagnosticsHub.DisposeAsync();");
        AssertOccursBefore(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");", "await _diagnosticsHub.DisposeAsync();");

        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupControllerContext");
        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupController");
        AssertContains(shutdownCleanupControllerText, "public async Task RunAsync()");
        AssertContains(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();");
        AssertContains(shutdownCleanupControllerText, "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertContains(shutdownCleanupControllerText, "LogWindowClosedTrigger();");
        AssertContains(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();");
        AssertContains(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMeterActivationHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopStatsOverlay();");
        AssertContains(shutdownCleanupControllerText, "_context.StopRecordingVisuals();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMainContentSizeChanged();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopPreviewForShutdown();");
        AssertContains(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();");
        AssertContains(shutdownCleanupControllerText, "var recordingStopResult = await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "WINDOW_CLOSE_RECORDING_STOP_UNRESOLVED ");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "_context.DisposeNvmlMonitor();");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeViewModelAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();", "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();", "_context.LifecycleController.MarkClosing();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();", "_context.DetachMeterActivationHandlers();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();", "_context.StopPreviewForShutdown();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();", "var recordingStopResult = await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "var recordingStopResult = await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);", "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);", "_context.DisposeNvmlMonitor();");

        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupText, "CancelNativeShellRevealAfterFirstFrame = CancelNativeShellRevealAfterFirstFrame,");
        AssertDoesNotContain(shutdownCleanupText, "WINDOW_CLOSED_TRIGGER ");
        AssertDoesNotContain(shutdownCleanupText, "_automationPipeServer.DisposeAsync()");
        AssertDoesNotContain(shutdownCleanupText, "_automationDiagnosticsHub.DisposeAsync()");

        return Task.CompletedTask;
    }

    internal static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var launchAdapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var catalogText = controllerText;
        var pacingPolicyText = controllerText;

        AssertContains(launchAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(launchAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(launchAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(launchAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "internal sealed class SplashLoadingPhraseController");
        AssertContains(controllerText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertContains(controllerText, "SplashLoadingPhraseCatalog.Load()");
        AssertContains(controllerText, "private readonly SplashLoadingPhrasePacingPolicy _pacingPolicy = new();");
        AssertContains(controllerText, "_pacingPolicy.Reset();");
        AssertContains(controllerText, "Interval = _pacingPolicy.NextInterval()");
        AssertContains(controllerText, "private void CyclePhrase()");
        AssertContains(controllerText, "storyboard.Begin();");
        AssertContains(pacingPolicyText, "internal sealed class SplashLoadingPhrasePacingPolicy");
        AssertContains(pacingPolicyText, "internal enum SplashLoadingPhrasePaceMode");
        AssertContains(pacingPolicyText, "public TimeSpan NextInterval()");
        AssertContains(pacingPolicyText, "internal TimeSpan NextInterval(Func<double> nextDouble, Func<int, int, int> nextInt)");
        AssertContains(catalogText, "internal static class SplashLoadingPhraseCatalog");
        AssertContains(catalogText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertContains(catalogText, "public static string[] Load()");
        AssertContains(catalogText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");
        AssertContains(catalogText, "if (line.StartsWith(\"##\"))");
        AssertContains(catalogText, "if (line.StartsWith('#')) continue;");
        AssertContains(catalogText, "if (line.StartsWith(\"<!--\")) continue;");
        AssertContains(catalogText, "line = line[2..].Trim();");
        AssertContains(catalogText, "while (line.EndsWith('.'))");
        AssertContains(catalogText, "_cachedSplashPhrases = DefaultSplashLoadingPhrases;");
        AssertDoesNotContain(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhraseCatalog.cs")),
            "splash phrase catalog folded into LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhrasePacingPolicy.cs")),
            "splash phrase pacing policy folded into LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Splash", "SplashLoadingPhraseController.cs")),
            "splash phrase controller folded into LaunchFlowController.cs");

        return Task.CompletedTask;
    }

    internal static Task SplashLoadingPhrasePacingPolicy_PreservesIntervalBands()
    {
        var policyType = RequireType("Sussudio.Controllers.SplashLoadingPhrasePacingPolicy");
        var policy = Activator.CreateInstance(policyType, nonPublic: true)
            ?? throw new InvalidOperationException("Failed to create SplashLoadingPhrasePacingPolicy.");
        var nextInterval = policyType.GetMethod(
                "NextInterval",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Func<double>), typeof(Func<int, int, int>) },
                modifiers: null)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.NextInterval test seam was not found.");
        var reset = policyType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.Reset was not found.");

        AssertEqual(
            TimeSpan.FromMilliseconds(319),
            InvokePolicy(policy, nextInterval, new[] { 0.10d }, (2, 6, 2), (280, 420, 319)),
            "burst first interval uses burst tick and interval ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(318),
            InvokePolicy(policy, nextInterval, Array.Empty<double>(), (280, 420, 318)),
            "burst keeps current mode while tick budget remains");
        AssertEqual(
            TimeSpan.FromMilliseconds(700),
            InvokePolicy(policy, nextInterval, new[] { 0.20d }, (1, 4, 1), (380, 900, 700)),
            "normal lower boundary uses normal ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(1200),
            InvokePolicy(policy, nextInterval, new[] { 0.70d }, (900, 1500, 1200)),
            "stuck lower boundary uses stuck interval range");
        AssertEqual(
            TimeSpan.FromMilliseconds(2000),
            InvokePolicy(policy, nextInterval, new[] { 0.90d }, (1500, 2500, 2000)),
            "long-stuck lower boundary uses long-stuck interval range");

        _ = InvokePolicy(policy, nextInterval, new[] { 0.05d }, (2, 6, 5), (280, 420, 300));
        reset.Invoke(policy, null);
        AssertEqual(
            TimeSpan.FromMilliseconds(1800),
            InvokePolicy(policy, nextInterval, new[] { 0.95d }, (1500, 2500, 1800)),
            "reset forces the next interval to choose a fresh mode");

        return Task.CompletedTask;
    }

    internal static Task LaunchEntranceAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;");
        AssertContains(adapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(adapterText, "private void InitializeLaunchEntranceAnimationController()");
        AssertContains(adapterText, "SplashContent = SplashContent,");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewBorderScale = PreviewBorderScale,");
        AssertContains(adapterText, "GetEntranceButtons = GetEntranceButtons,");
        AssertContains(adapterText, "IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,");
        AssertContains(adapterText, "FadeInControlBarShadow = () => FadeInControlBarShadow(delayMs: 400, durationMs: 500),");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PrepareInitialState();");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PlaySplashAndEntrance();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "launch entrance adapter folded into MainWindow.xaml.cs");
        AssertContains(controllerInitializationText, "InitializeLaunchEntranceAnimationController();");
        AssertContains(mainWindowText, "PrepareLaunchEntranceInitialState();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(controllerText, "internal sealed class LaunchEntranceAnimationController");
        AssertContains(controllerText, "private bool _played;");
        AssertContains(controllerText, "private Storyboard? _activeStoryboard;");
        AssertContains(controllerText, "public void PrepareInitialState()");
        AssertContains(controllerText, "_context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };");
        AssertContains(controllerText, "_context.PreviewBorderScale.ScaleX = 0.97;");
        AssertContains(controllerText, "foreach (var button in _context.GetEntranceButtons())");
        AssertContains(controllerText, "public void PlaySplashAndEntrance()");
        AssertContains(controllerText, "BeginTime = TimeSpan.FromMilliseconds(180)");
        AssertContains(controllerText, "BeginTime = TimeSpan.FromMilliseconds(3000)");
        AssertContains(controllerText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "PlayEntranceAnimation();");
        AssertOccursBefore(controllerText, "_context.StartSplashLoadingPhrases();", "splashStoryboard.Begin();");
        AssertOccursBefore(controllerText, "_context.StopSplashLoadingPhrases();", "PlayEntranceAnimation();");
        AssertContains(controllerText, "private void PlayEntranceAnimation()");
        AssertContains(controllerText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(controllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "_context.FadeInControlBarShadow();");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertDoesNotContain(agentMapText, "LaunchEntranceAnimationController.Splash.cs");
        AssertDoesNotContain(agentMapText, "LaunchEntranceAnimationController.Shell.cs");
        AssertDoesNotContain(cleanupPlanText, "LaunchEntranceAnimationController.Splash.cs");
        AssertDoesNotContain(cleanupPlanText, "LaunchEntranceAnimationController.Shell.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.Splash.cs")),
            "launch entrance splash phase is consolidated into the root controller");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.Shell.cs")),
            "launch entrance shell phase is consolidated into the root controller");
        AssertDoesNotContain(mainWindowText, "private bool _entranceAnimationPlayed;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _entranceStoryboard;");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Opacity = 0;");
        AssertDoesNotContain(mainWindowText, "var entranceButtons = GetEntranceButtons();");

        return Task.CompletedTask;
    }

    internal static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs").Replace("\r\n", "\n");
        var launchStartupHandleLoadedText = ExtractMemberCode(launchStartupControllerText, "HandleLoaded");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var refreshDevices = ExtractMemberCode(deviceRefreshControllerText, "RefreshDevicesAsync");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(startupText, "private LaunchStartupController _launchStartupController = null!;");
        AssertContains(startupText, "private void InitializeLaunchStartupController()");
        AssertContains(startupText, "new LaunchStartupControllerContext");
        AssertContains(startupText, "MainContent = (FrameworkElement)Content,");
        AssertContains(startupText, "LoadedHandler = MainWindow_Loaded,");
        AssertContains(startupText, "ScheduleNativeShellRevealAfterFirstFrame = ScheduleNativeShellRevealAfterFirstFrame,");
        AssertContains(startupText, "RunUiEventHandlerAsync = RunUiEventHandlerAsync,");
        AssertContains(startupText, "InitializeViewModelAsync = ViewModel.InitializeAsync,");
        AssertContains(startupText, "PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,");
        AssertContains(startupText, "RefreshDevicesAsync = () => ViewModel.RefreshDevicesForStartupAsync(),");
        AssertContains(startupText, "StartAutomationHost = _automationHostLifecycleController.Start,");
        AssertContains(startupText, "PlaySplashAndEntrance = PlaySplashAndEntrance,");
        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "=> _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));");
        AssertContains(launchStartupControllerText, "internal sealed class LaunchStartupControllerContext");
        AssertContains(launchStartupControllerText, "internal sealed class LaunchStartupController");
        AssertContains(launchStartupControllerText, "public void HandleLoaded(string operationName)");
        AssertContains(launchStartupControllerText, "_context.MainContent.Loaded -= _context.LoadedHandler;");
        AssertContains(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();");
        AssertContains(launchStartupControllerText, "_ = _context.RunUiEventHandlerAsync(async () =>");
        AssertContains(launchStartupControllerText, "await _context.InitializeViewModelAsync();");
        AssertContains(launchStartupControllerText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(launchStartupControllerText, "await _context.RefreshDevicesAsync();");
        AssertContains(launchStartupControllerText, "_context.RevealPreviewUnavailablePlaceholder();");
        AssertContains(launchStartupControllerText, "_context.StartAutomationHost();");
        AssertDoesNotContain(launchStartupHandleLoadedText, "finally");
        AssertContains(launchStartupControllerText, "_context.PlaySplashAndEntrance();");
        AssertContains(agentMapText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertContains(cleanupPlanText, "Sussudio/Controllers/Launch/LaunchFlowController.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "LaunchStartupController.cs")),
            "launch startup choreography lives with the launch flow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Launch", "Entrance", "LaunchEntranceAnimationController.cs")),
            "launch entrance choreography lives with the launch flow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "startup adapter folded into MainWindow.xaml.cs");
        AssertContains(mainWindowText, "private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");
        AssertContains(automationHostControllerText, "private int _started;");
        AssertContains(automationHostControllerText, "Interlocked.Exchange(ref _started, 1)");
        AssertContains(automationHostControllerText, "if (_pipeServer.Start())\n        {\n            _diagnosticsHub.Start();");
        AssertContains(automationHostControllerText, "Automation control ready on pipe");
        AssertContains(automationHostControllerText, "Automation control disabled on pipe");
        AssertContains(mainWindowText, "InitializeShellControllers();");
        AssertContains(controllerInitializationText, "private void InitializeShellControllers()");
        AssertContains(controllerInitializationText, "private void InitializeWindowAutomationControllers()");
        AssertContains(controllerInitializationText, "private void InitializeFlashbackControllers()");
        AssertContains(controllerInitializationText, "private void InitializeShellPresentationControllers()");
        AssertContains(controllerInitializationText, "private void InitializePreviewControllers()");
        AssertContains(controllerInitializationText, "private void InitializeRecordingControllers()");
        AssertContains(controllerInitializationText, "private void InitializeLaunchAndStatusControllers()");
        AssertContains(controllerInitializationText, "InitializeLaunchStartupController();");
        AssertContains(controllerInitializationText, "private void InitializePreviewActionControllers()");
        AssertContains(controllerInitializationText, "private void InitializeAudioControllers()");
        AssertContains(controllerInitializationText, "private void InitializeCaptureControllers()");
        AssertContains(controllerInitializationText, "private void InitializeOutputControllers()");
        AssertOccursBefore(controllerInitializationText, "InitializeWindowAutomationControllers();", "InitializeFlashbackControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeFlashbackControllers();", "InitializeShellPresentationControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeShellPresentationControllers();", "InitializePreviewControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializePreviewControllers();", "InitializeRecordingControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeRecordingControllers();", "InitializeLaunchAndStatusControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeLaunchAndStatusControllers();", "InitializePreviewActionControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializePreviewActionControllers();", "InitializeAudioControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeAudioControllers();", "InitializeResponsiveShellLayoutController();");
        AssertOccursBefore(controllerInitializationText, "InitializeResponsiveShellLayoutController();", "InitializeCaptureControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeCaptureControllers();", "InitializeOutputControllers();");
        AssertOccursBefore(controllerInitializationText, "InitializeOutputControllers();", "InitializePreviewScreenshotController();");
        AssertOccursBefore(controllerInitializationText, "private void InitializeWindowAutomationControllers()", "private void InitializeFlashbackControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializeFlashbackControllers()", "private void InitializeShellPresentationControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializeShellPresentationControllers()", "private void InitializePreviewControllers()");
        AssertOccursBefore(controllerInitializationText, "private void InitializePreviewControllers()", "private void InitializeRecordingControllers()");
        AssertOccursBefore(launchStartupControllerText, "await _context.InitializeViewModelAsync();", "_context.StartAutomationHost();");
        AssertOccursBefore(launchStartupHandleLoadedText, "await _context.RefreshDevicesAsync();", "_context.StartAutomationHost();");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "_ = _context.RunUiEventHandlerAsync(async () =>");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "await _context.InitializeViewModelAsync();");
        AssertOccursBefore(launchStartupControllerText, "_context.ScheduleNativeShellRevealAfterFirstFrame();", "_context.PlaySplashAndEntrance();");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
        AssertDoesNotContain(startupText, "await ViewModel.InitializeAsync();");
        AssertDoesNotContain(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertContains(rootText, "internal Task RefreshDevicesForStartupAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken, throwOnScanFailure: true);");
        AssertContains(deviceRefreshControllerText, "bool throwOnScanFailure = false");
        AssertContains(refreshDevices, "if (throwOnScanFailure)");
        AssertDoesNotContain(startupText, "_automationHostLifecycleController.Start();");
        AssertDoesNotContain(mainWindowText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "private int _automationServicesStarted;");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _automationServicesStarted");
        AssertDoesNotContain(startupText, "_automationDiagnosticsHub.Start();");
        AssertDoesNotContain(startupText, "CompositionTarget.Rendering");
        AssertDoesNotContain(startupText, "UncloakNativeShellWindow();");
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

        return Task.CompletedTask;
    }

    private static TimeSpan InvokePolicy(
        object policy,
        MethodInfo nextInterval,
        double[] rolls,
        params (int Min, int Max, int Value)[] integerResponses)
    {
        var rollQueue = new Queue<double>(rolls);
        var integerQueue = new Queue<(int Min, int Max, int Value)>(integerResponses);

        Func<double> nextDouble = () =>
        {
            if (rollQueue.Count == 0)
            {
                throw new InvalidOperationException("Policy requested an unexpected random roll.");
            }

            return rollQueue.Dequeue();
        };
        Func<int, int, int> nextInt = (min, max) =>
        {
            if (integerQueue.Count == 0)
            {
                throw new InvalidOperationException($"Policy requested unexpected integer range {min}..{max}.");
            }

            var expected = integerQueue.Dequeue();
            AssertEqual(expected.Min, min, "policy integer range minimum");
            AssertEqual(expected.Max, max, "policy integer range maximum");
            return expected.Value;
        };

        var result = (TimeSpan)(nextInterval.Invoke(policy, new object[] { nextDouble, nextInt })
                                ?? throw new InvalidOperationException("Policy returned null interval."));
        AssertEqual(0, rollQueue.Count, "unused policy random rolls");
        AssertEqual(0, integerQueue.Count, "unused policy integer responses");
        return result;
    }


// MainWindow capture option and selection contracts live with the presentation-preview xUnit wrappers.
    internal static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");
        var controllerText = controllerRootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var selectionBindingFamilyText = ExtractTextBetween(
            controllerRootText,
            "internal sealed class CaptureSelectionBindingController",
            "internal static class CaptureComboBoxSelectionNormalizer");
        var captureOptionBindingsWithoutVideoFormat = captureOptionBindingsText.Replace("VideoFormatComboBox.SelectionChanged +=", string.Empty);
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");

        AssertContains(captureOptionBindingsText, "private CaptureOptionBindingController _captureOptionBindingController = null!;");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionBindingController()");
        AssertContains(captureOptionBindingsText, "ResolutionComboBox = ResolutionComboBox,");
        AssertContains(captureOptionBindingsText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionBindingsText, "TrueHdrPreviewToggle = TrueHdrPreviewToggle,");
        AssertContains(captureOptionBindingsText, "ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,");
        AssertContains(captureOptionBindingsText, "ApplyAudioClipVisibility = ApplyAudioClipVisibility,");
        AssertContains(captureOptionBindingsText, "RefreshHdrHintText = RefreshHdrHintText,");
        AssertContains(captureOptionBindingsText, "UpdateFpsTelemetryTooltip = UpdateFpsTelemetryTooltip,");
        AssertContains(captureOptionBindingsText, "UpdateVideoContentOverlays = UpdateVideoContentOverlays,");
        AssertContains(captureOptionBindingsText, "SetHdrPassthroughEnabled = enabled => _previewRendererHostController.SetHdrPassthroughEnabled(enabled),");
        AssertContains(captureOptionBindingsText, "EnsureSplitEncodeModeSelection = EnsureSplitEncodeModeSelection");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionCollections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.InitializeCollections();");
        AssertContains(captureOptionBindingsText, "private void ApplyInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.ApplyInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void EnsureInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.EnsureInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void AttachCaptureModeSelectionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachCaptureModeSelectionBindings();");
        AssertContains(captureOptionBindingsText, "private void HandleCustomBitratePropertyChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleCustomBitratePropertyChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleHdrEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleHdrEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionPropertyChangedMethod, "=> _captureOptionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertContains(captureOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachRecordingOptionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureOptionBindingController();");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureOptionBindings.cs")), "MainWindow capture option adapter folded into MainWindow.xaml.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.xaml.cs");

        AssertContains(controllerRootText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerRootText, "internal sealed class CaptureOptionBindingController");
        AssertContains(controllerRootText, "private readonly CaptureOptionBindingControllerContext _context;");
        AssertContains(controllerRootText, "public CaptureOptionBindingController(CaptureOptionBindingControllerContext context)");
        AssertContains(controllerRootText, "public void InitializeCollections()");
        AssertContains(controllerRootText, "_context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;");
        AssertContains(controllerRootText, "for (var i = 1; i <= 8; i++)");
        AssertContains(controllerRootText, "_context.DecoderCountComboBox.Items.Add(i);");
        AssertContains(controllerRootText, "public void ApplyInitialSelections()");
        AssertContains(controllerRootText, "_context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;");
        AssertContains(controllerRootText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");
        AssertContains(controllerRootText, "_context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;");
        AssertContains(controllerRootText, "_context.ApplyInitialDecoderCountSelection();");
        AssertContains(controllerRootText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerRootText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerRootText, "public void EnsureInitialSelections()");
        AssertContains(controllerRootText, "_context.EnsureSplitEncodeModeSelection();");
        AssertContains(controllerRootText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerRootText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerRootText, "_context.ResolutionComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "_context.FrameRateComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "!string.Equals(resolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase)");
        AssertContains(controllerRootText, "if (CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(frameRate))");
        AssertContains(controllerRootText, "if (!_context.ViewModel.IsAutoFrameRateSelected)");
        AssertContains(controllerRootText, "else if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))");
        AssertContains(controllerRootText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerRootText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(controllerRootText, "_context.VideoFormatComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "_context.CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(controllerRootText, "_context.HdrToggle.Click +=");
        AssertContains(controllerRootText, "_context.TrueHdrPreviewToggle.Click +=");
        AssertContains(controllerRootText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerRootText, "case nameof(MainViewModel.AudioClipping):");
        AssertContains(controllerRootText, "_context.ApplyAudioClipVisibility();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsHdrAvailable):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceIsHdr):");
        AssertContains(controllerRootText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsHdrEnabled):");
        AssertContains(controllerRootText, "HandleHdrEnabledChanged();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsTrueHdrPreviewEnabled):");
        AssertContains(controllerRootText, "HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrResolutionSupportHint):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrReadinessReason):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrRuntimeState):");
        AssertContains(controllerRootText, "_context.RefreshHdrHintText();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceTelemetrySummaryText):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceTargetSummaryText):");
        AssertContains(controllerRootText, "_context.UpdateFpsTelemetryTooltip();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceWidth):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceHeight):");
        AssertContains(controllerRootText, "_context.UpdateVideoContentOverlays();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsCustomBitrateVisible):");
        AssertContains(controllerRootText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.CustomBitrateMbps):");
        AssertContains(controllerRootText, "public void HandleCustomBitratePropertyChanged()");
        AssertContains(controllerRootText, "public void HandleHdrEnabledChanged()");
        AssertContains(controllerRootText, "public void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(controllerRootText, "private void AttachHdrToggleBindings()");
        AssertContains(controllerRootText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");
        AssertDoesNotContain(controllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertContains(setupBindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(setupBindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(setupBindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(setupBindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(setupBindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(setupBindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(setupBindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(setupBindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(setupBindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertDoesNotContain(selectionBindingFamilyText, "public void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionBindingFamilyText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertDoesNotContain(selectionBindingFamilyText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertContains(agentMapText, "`Sussudio/Controllers/Capture/CaptureBindingControllers.cs` owns the");
        AssertContains(agentMapText, "capture option binding adapter context, setup, UI event attachment");
        AssertContains(agentMapText, "capture-option/source-signal property-change routing");
        AssertContains(agentMapText, "`Sussudio/MainWindow.xaml.cs` is the XAML-facing adapter");
        AssertContains(agentMapText, "option binding adapter context");
        AssertContains(agentMapText, "recording option event");
        AssertContains(agentMapText, "HDR/true-HDR click binding");
        AssertContains(agentMapText, "delegated presentation callbacks for option");
        AssertContains(agentMapText, "affordances, telemetry tooltips, and source overlay refreshes");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.Context.cs");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.Bindings.cs");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.PropertyChanges.cs");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/Capture/CaptureBindingControllers.cs`. It keeps the");
        AssertContains(cleanupPlanText, "capture-option binding adapter context, video-format and initial decoder");
        AssertContains(cleanupPlanText, "video-format and initial decoder");
        AssertContains(cleanupPlanText, "projection, initial selection projection");
        AssertContains(cleanupPlanText, "resolution/frame-rate selection");
        AssertContains(cleanupPlanText, "handlers, recording option event bindings for format, quality, preset");
        AssertContains(cleanupPlanText, "split-encode, video format, and custom bitrate");
        AssertContains(cleanupPlanText, "HDR/true-HDR click binding");
        AssertContains(cleanupPlanText, "`ShowAllCaptureOptionsToggle` click binding");
        AssertContains(cleanupPlanText, "capture-option/source-signal");
        AssertContains(cleanupPlanText, "property-change routing, custom-bitrate property-change value projection");
        AssertContains(cleanupPlanText, "preview HDR passthrough forwarding");
        AssertContains(cleanupPlanText, "presentation callback routing for option affordances, telemetry tooltips, and");
        AssertContains(cleanupPlanText, "source overlay refreshes");
        AssertContains(cleanupPlanText, "`Sussudio/MainWindow.xaml.cs` now owns the XAML-facing");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.Context.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.Bindings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.PropertyChanges.cs");

        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(captureOptionBindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedFrameRate =");
        AssertDoesNotContain(captureOptionBindingsWithoutVideoFormat, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(captureOptionBindingsText, "TrueHdrPreviewToggle.Click +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedRecordingFormat =");
        AssertDoesNotContain(captureOptionBindingsText, "QualityComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "PresetComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "SplitEncodeComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleCustomBitratePropertyChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleHdrEnabledChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleTrueHdrPreviewEnabledChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleShowAllCaptureOptionsChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HdrToggle.IsChecked = ViewModel.IsHdrEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "_previewRendererHostController.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");
        AssertDoesNotContain(propertyChangedText, "CustomBitrateNumberBox.Value");
        AssertDoesNotContain(propertyChangedText, "Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(propertyChangedText, "TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,");
        AssertDoesNotContain(bindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");

        return Task.CompletedTask;
    }

    internal static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureDeviceActionInit = ExtractMemberCode(adapterText, "InitializeCaptureDeviceActionController");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureDeviceActionController _captureDeviceActionController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureDeviceActionController()");
        AssertContains(adapterText, "RefreshButton = RefreshButton,");
        AssertContains(adapterText, "ApplyDeviceButton = ApplyDeviceButton,");
        AssertContains(adapterText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState");
        AssertContains(adapterText, "private Task RefreshDevicesFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.RefreshDevicesAsync();");
        AssertContains(adapterText, "private Task ApplySelectedDeviceFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.ApplySelectedDeviceAsync();");
        AssertContains(adapterText, "private void RefreshButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(adapterText, "private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(adapterText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(captureDeviceActionInit, "UpdateDeviceApplyButtonState();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureDeviceActions.cs")),
            "capture-device button adapter folded into MainWindow.xaml.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");
        var policyText = controllerText;
        const string tooltipFormatterMarker = "internal static class CaptureOptionTooltipFormatter";
        var tooltipFormatterStart = controllerText.IndexOf(tooltipFormatterMarker, System.StringComparison.Ordinal);
        if (tooltipFormatterStart < 0)
        {
            throw new System.InvalidOperationException("CaptureOptionTooltipFormatter was not found in CaptureBindingControllers.cs.");
        }

        var tooltipFormatterText = controllerText[tooltipFormatterStart..];
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");
        var outputPathDisplayText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private CaptureOptionPresentationController _captureOptionPresentationController = null!;");
        AssertContains(captureOptionText, "private void InitializeCaptureOptionPresentationController()");
        AssertContains(captureOptionText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionText, "AudioClipText = AudioClipText");
        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateDecoderCountVisibility();");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.HandleDecoderCountSelectionChanged();");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.RefreshHdrHintText();");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateFpsTelemetryTooltip();");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyHdrToggleEnabledState();");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyBitrateVisibility();");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyAudioClipVisibility();");

        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationController");
        AssertContains(controllerText, "private int _selectedDecoderCount = 4;");
        AssertContains(controllerText, "public void ApplyInitialDecoderCountSelection()");
        AssertContains(controllerText, "_selectedDecoderCount = affordances.InitialDecoderCount;");
        AssertContains(controllerText, "_context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;");
        AssertContains(controllerText, "public void UpdateDecoderCountVisibility()");
        AssertContains(policyText, "InitialDecoderCount: Math.Clamp(input.MjpegDecoderCount, 1, 8)");
        AssertContains(controllerText, "public void HandleDecoderCountSelectionChanged()");
        AssertContains(controllerText, "_context.ViewModel.MjpegDecoderCount = count;");
        AssertContains(controllerText, "public void RefreshHdrHintText()");
        AssertContains(controllerText, "public void UpdateFpsTelemetryTooltip()");
        AssertContains(controllerText, "public void ApplyHdrToggleEnabledState()");
        AssertContains(controllerText, "public void ApplyBitrateVisibility()");
        AssertContains(controllerText, "public void ApplyAudioClipVisibility()");
        AssertContains(controllerText, "_context.ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(controllerText, "CaptureOptionPresentationPolicy.Build(BuildPolicyInput())");
        AssertContains(controllerText, "private CaptureOptionPresentationInput BuildPolicyInput()");
        AssertContains(controllerText, "private static Visibility ToVisibility(bool isVisible)");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildHdrHintText(");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip(");
        AssertContains(policyText, "internal static class CaptureOptionPresentationPolicy");
        AssertContains(policyText, "internal static CaptureOptionPresentationAffordances Build(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationInput(");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationAffordances(");
        AssertContains(policyText, "private static double ResolveSelectedFrameRate(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "private static bool ShouldShowDecoderCount(");
        AssertContains(policyText, "selectedFrameRate >= 90");
        AssertContains(tooltipFormatterText, "internal static class CaptureOptionTooltipFormatter");
        AssertContains(tooltipFormatterText, "public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)");
        AssertContains(tooltipFormatterText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(tooltipFormatterText, "public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)");
        AssertDoesNotContain(captureOptionText, "var combinedHint =");
        AssertDoesNotContain(controllerText, "var parts = new List<string>();");
        AssertContains(controllerText, "_context.ViewModel.SourceTelemetrySummaryText");
        AssertContains(controllerText, "_context.ViewModel.SourceTargetSummaryText");
        AssertContains(mainWindowText, "InitializeCaptureOptionPresentationController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureOptionPresentationController.cs")),
            "capture option presentation policy and controller folded into CaptureBindingControllers.cs");
        AssertContains(propertyChangedText, "TryHandleOutput = TryHandleOutputPropertyChanged,");
        AssertContains(propertyChangedText, "TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,");
        AssertContains(outputPathDisplayText, "=> _outputPathController.TryHandlePropertyChanged(propertyName);");
        AssertContains(captureOptionBindingsText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionPropertyChangedMethod, "=> _captureOptionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyAudioClipVisibility();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "RefreshHdrHintText();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "UpdateFpsTelemetryTooltip();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyBitrateVisibility();");
        AssertDoesNotContain(setupBindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(setupBindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(setupBindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(setupBindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(setupBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(ReadMainWindowCompositionSource(), "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertDoesNotContain(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertDoesNotContain(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertDoesNotContain(captureOptionText, "var isExplicitMjpg =");
        AssertDoesNotContain(captureOptionText, "var isAutoWithMjpgDevice =");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsHdrAvailable &&");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsCustomBitrateVisible ? Visibility.Visible");
        AssertDoesNotContain(controllerText, "_context.ViewModel.AudioClipping ? Visibility.Visible");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionPresentationPolicy_PreservesAffordanceRules()
    {
        var policyType = RequireType("Sussudio.Controllers.CaptureOptionPresentationPolicy");
        var inputType = RequireType("Sussudio.Controllers.CaptureOptionPresentationInput");
        var build = policyType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build was not found.");
        var constructor = inputType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 12);

        object Build(
            string? selectedVideoFormat,
            string? selectedFormatPixelFormat,
            double? selectedFrameRateOptionFriendlyValue,
            double? selectedFrameRateOptionValue,
            double selectedFrameRateFallback,
            int mjpegDecoderCount = 4,
            bool isHdrAvailable = true,
            bool isRecording = false,
            bool? sourceIsHdr = true,
            bool isHdrEnabled = true,
            bool isCustomBitrateVisible = false,
            bool audioClipping = false)
        {
            var input = constructor.Invoke(new object?[]
            {
                selectedVideoFormat,
                selectedFormatPixelFormat,
                selectedFrameRateOptionFriendlyValue,
                selectedFrameRateOptionValue,
                selectedFrameRateFallback,
                mjpegDecoderCount,
                isHdrAvailable,
                isRecording,
                sourceIsHdr,
                isHdrEnabled,
                isCustomBitrateVisible,
                audioClipping
            });

            return build.Invoke(null, new[] { input })
                ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build returned null.");
        }

        var explicitMjpgHighFps = Build("MJPG", null, 90d, null, 60d);
        AssertEqual(true, GetBoolProperty(explicitMjpgHighFps, "ShowDecoderCount"), "explicit MJPG at 90 FPS shows decoder count");

        var explicitMjpgLowFps = Build("MJPG", null, 89.99d, null, 120d);
        AssertEqual(false, GetBoolProperty(explicitMjpgLowFps, "ShowDecoderCount"), "explicit MJPG below 90 FPS hides decoder count");

        var autoMjpgValueFps = Build("Auto", "MJPG", 0d, 120d, 60d);
        AssertEqual(true, GetBoolProperty(autoMjpgValueFps, "ShowDecoderCount"), "Auto with MJPG device format uses frame-rate option value fallback");

        var autoNonMjpgHighFps = Build("Auto", "NV12", null, 120d, 60d);
        AssertEqual(false, GetBoolProperty(autoNonMjpgHighFps, "ShowDecoderCount"), "Auto with non-MJPG device format hides decoder count");

        var fallbackFrameRate = Build("MJPG", null, null, null, 120d);
        AssertEqual(true, GetBoolProperty(fallbackFrameRate, "ShowDecoderCount"), "missing frame-rate option falls back to selected frame rate");

        var sourceUnknown = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: null);
        AssertEqual(true, GetBoolProperty(sourceUnknown, "EnableHdrToggle"), "unknown source HDR state does not disable HDR toggle");

        var sdrSource = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: false);
        AssertEqual(false, GetBoolProperty(sdrSource, "EnableHdrToggle"), "SDR source disables HDR toggle");

        var recording = Build("Auto", "NV12", null, null, 60d, isRecording: true);
        AssertEqual(false, GetBoolProperty(recording, "EnableHdrToggle"), "recording disables HDR toggle");
        AssertEqual(false, GetBoolProperty(recording, "EnableTrueHdrPreviewToggle"), "recording disables true-HDR preview toggle");

        var unavailableHdr = Build("Auto", "NV12", null, null, 60d, isHdrAvailable: false);
        AssertEqual(false, GetBoolProperty(unavailableHdr, "EnableHdrToggle"), "HDR unavailable disables HDR toggle");

        var customBitrate = Build("Auto", "NV12", null, null, 60d, isCustomBitrateVisible: true, audioClipping: true);
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowCustomBitrate"), "custom bitrate shows custom panel");
        AssertEqual(false, GetBoolProperty(customBitrate, "ShowPreset"), "custom bitrate hides preset panel");
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowAudioClip"), "audio clipping shows warning text");

        var lowDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 0);
        var highDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 9);
        var normalDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 5);
        AssertEqual(1, GetIntProperty(lowDecoderCount, "InitialDecoderCount"), "decoder count clamps low");
        AssertEqual(8, GetIntProperty(highDecoderCount, "InitialDecoderCount"), "decoder count clamps high");
        AssertEqual(5, GetIntProperty(normalDecoderCount, "InitialDecoderCount"), "decoder count preserves valid values");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.CaptureOptionTooltipFormatter");
        var buildHdrHintText = formatterType.GetMethod("BuildHdrHintText", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildHdrHintText was not found.");
        var buildFpsTelemetryTooltip = formatterType.GetMethod("BuildFpsTelemetryTooltip", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip was not found.");

        string? Hdr(string? resolutionHint, string? readinessHint, bool isRecording)
            => buildHdrHintText.Invoke(null, new object?[] { resolutionHint, readinessHint, isRecording })?.ToString();

        string? Fps(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)
            => buildFpsTelemetryTooltip.Invoke(null, new object?[] { sourceTelemetrySummaryText, sourceTargetSummaryText })?.ToString();

        var stopRecordingText = "Stop recording before switching between HDR and SDR pipelines.";
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower",
            Hdr("  4K HDR requires 59.94 or lower ", " Source is SDR ", isRecording: false),
            "HDR hint trims and combines readiness before resolution support");
        AssertEqual(
            "4K HDR requires 59.94 or lower",
            Hdr("4K HDR requires 59.94 or lower", null, isRecording: false),
            "HDR hint uses resolution when readiness is empty");
        AssertEqual(
            stopRecordingText,
            Hdr(null, null, isRecording: true),
            "HDR hint uses recording guard when no other hint exists");
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower{System.Environment.NewLine}{stopRecordingText}",
            Hdr("4K HDR requires 59.94 or lower", "Source is SDR", isRecording: true),
            "HDR hint appends recording guard after existing hints");
        AssertEqual(
            null,
            Hdr(" ", null, isRecording: false),
            "HDR hint returns null when no hint text exists");

        AssertEqual(
            $"Telemetry: NativeXu{System.Environment.NewLine}Target: 3840 x 2160",
            Fps("Telemetry: NativeXu", "Target: 3840 x 2160"),
            "FPS tooltip combines telemetry and target summaries");
        AssertEqual(
            "  Telemetry: NativeXu  ",
            Fps("  Telemetry: NativeXu  ", null),
            "FPS tooltip preserves existing telemetry summary whitespace");
        AssertEqual(
            "Target: 3840 x 2160",
            Fps(null, "Target: 3840 x 2160"),
            "FPS tooltip uses target summary when telemetry is empty");
        AssertEqual(
            null,
            Fps(" ", null),
            "FPS tooltip returns null when both summaries are empty");

        return Task.CompletedTask;
    }
    internal static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void AttachDeviceSelectionChangedBinding()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.AttachDeviceSelectionChangedBinding();");
        AssertContains(adapterText, "private void HandleSelectedDevicePropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailablePresetsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(adapterText, "private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.xaml.cs");

        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(bindingsText, "AttachDeviceSelectionChangedBinding();");
        AssertContains(propertyChangedText, "TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "private readonly CaptureSelectionBindingControllerContext _context;");
        AssertContains(controllerText, "public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)");
        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingControllerContext");

        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(propertyChangedText, "DEVICE_SELECTION_SYNC");
        AssertDoesNotContain(propertyChangedText, "DeviceComboBox.SelectedItem");
        AssertDoesNotContain(propertyChangedText, "ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;");
        AssertDoesNotContain(propertyChangedText, "FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;");
        AssertDoesNotContain(propertyChangedText, "PresetComboBox.ItemsSource = ViewModel.AvailablePresets;");
        AssertDoesNotContain(propertyChangedText, "SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertContains(controllerText, "public void EnsureDeviceAudioModeSelection()");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingCollectionSync_LivesInControllerPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)");
        AssertContains(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertContains(controllerText, "public void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(controllerText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;");
        AssertContains(controllerText, "EnsureResolutionSelection();");
        AssertContains(controllerText, "public void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(controllerText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;");
        AssertContains(controllerText, "EnsureFrameRateSelection();");
        AssertContains(controllerText, "public void HandleAvailablePresetsPropertyChanged()");
        AssertContains(controllerText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;");
        AssertContains(controllerText, "EnsurePresetSelection();");
        AssertContains(controllerText, "public void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(controllerText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;");
        AssertContains(controllerText, "EnsureSplitEncodeModeSelection();");
        AssertOccursBefore(
            ExtractMemberCode(controllerText, "HandleAvailableResolutionsPropertyChanged"),
            "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;",
            "EnsureResolutionSelection();");
        AssertOccursBefore(
            ExtractMemberCode(controllerText, "HandleAvailableFrameRatesPropertyChanged"),
            "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;",
            "EnsureFrameRateSelection();");
        AssertOccursBefore(
            ExtractMemberCode(controllerText, "HandleAvailablePresetsPropertyChanged"),
            "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;",
            "EnsurePresetSelection();");
        AssertOccursBefore(
            ExtractMemberCode(controllerText, "HandleAvailableSplitEncodeModesPropertyChanged"),
            "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;",
            "EnsureSplitEncodeModeSelection();");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureSelectionBindingController.SelectionState.cs")),
            "empty selection-state marker partial should stay removed");
        AssertDoesNotContain(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(adapterText, "_captureSelectionBindingController.AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingPropertyRouter_LivesInController()
    {
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedRouteText = ExtractMemberCode(propertyChangedText, "RouteAsync");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");

        AssertContains(propertyChangesText, "public bool TryHandlePropertyChanged(string? propertyName)");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedFrameRate):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDeviceAudioMode):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AnalogAudioGainPercent):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):", "case nameof(MainViewModel.SelectedResolution):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):", "case nameof(MainViewModel.SelectedFrameRate):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):", "case nameof(MainViewModel.AvailableResolutions):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):", "case nameof(MainViewModel.AvailableFrameRates):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):", "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):", "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):", "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):", "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):", "case nameof(MainViewModel.SelectedQuality):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):", "case nameof(MainViewModel.AvailablePresets):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):", "case nameof(MainViewModel.SelectedPreset):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):", "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):", "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertContains(propertyChangesText, "HandleSelectedDevicePropertyChanged();");
        AssertContains(propertyChangesText, "EnsureResolutionSelection();");
        AssertContains(propertyChangesText, "EnsureFrameRateSelection();");
        AssertContains(propertyChangesText, "HandleAvailableResolutionsPropertyChanged();");
        AssertContains(propertyChangesText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(propertyChangesText, "ApplyDeviceAudioControlState();");
        AssertContains(propertyChangesText, "EnsureAudioInputSelection();");
        AssertContains(propertyChangesText, "EnsureMicrophoneSelection();");
        AssertContains(propertyChangesText, "EnsureFormatSelection();");
        AssertContains(propertyChangesText, "EnsureQualitySelection();");
        AssertContains(propertyChangesText, "HandleAvailablePresetsPropertyChanged();");
        AssertContains(propertyChangesText, "EnsurePresetSelection();");
        AssertContains(propertyChangesText, "HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(propertyChangesText, "EnsureSplitEncodeModeSelection();");

        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(propertyChangedRouteText, "HandleSelectedDevicePropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableResolutionsPropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "ApplyDeviceAudioControlState();");
        AssertDoesNotContain(propertyChangedRouteText, "EnsureFormatSelection();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableSplitEncodeModesPropertyChanged();");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureBindingControllers.cs").Replace("\r\n", "\n");
        var selectionNormalizerText = controllerText.Substring(
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));
        var bindingControllerText = controllerText.Substring(
            0,
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void EnsureDeviceSelection()");
        AssertContains(controllerText, "public void AttachDeviceSelectionChangedBinding()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public void HandleSelectedDevicePropertyChanged()");
        AssertContains(controllerText, "DEVICE_SELECTION_SYNC");
        AssertContains(controllerText, "EnsureDeviceSelection();");
        AssertContains(controllerText, "UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(controllerText, "public void UpdateDeviceApplyButtonState()");
        var selectedDevicePropertyChangedText = controllerText.Substring(
            controllerText.IndexOf("public void HandleSelectedDevicePropertyChanged()", System.StringComparison.Ordinal));
        AssertOccursBefore(selectedDevicePropertyChangedText, "DEVICE_SELECTION_SYNC", "EnsureDeviceSelection();");
        AssertOccursBefore(selectedDevicePropertyChangedText, "EnsureDeviceSelection();", "UpdateDeviceApplyButtonState();");
        AssertOccursBefore(controllerText, "public void EnsureDeviceSelection()", "public void HandleSelectedDevicePropertyChanged()");

        AssertContains(controllerText, "public void EnsureAudioInputSelection()");
        AssertContains(controllerText, "public void EnsureMicrophoneSelection()");
        AssertOccursBefore(controllerText, "public void EnsureAudioInputSelection()", "public void EnsureMicrophoneSelection()");

        AssertContains(controllerText, "public void EnsureResolutionSelection()");
        AssertContains(controllerText, "public void EnsureFrameRateSelection()");
        AssertOccursBefore(controllerText, "public void EnsureResolutionSelection()", "public void EnsureFrameRateSelection()");

        AssertContains(controllerText, "public void EnsureFormatSelection()");
        AssertContains(controllerText, "public void EnsureQualitySelection()");
        AssertContains(controllerText, "public void EnsurePresetSelection()");
        AssertContains(controllerText, "public void EnsureSplitEncodeModeSelection()");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);");
        AssertOccursBefore(controllerText, "public void EnsureFormatSelection()", "public void EnsureQualitySelection()");
        AssertOccursBefore(controllerText, "public void EnsureQualitySelection()", "public void EnsurePresetSelection()");
        AssertOccursBefore(controllerText, "public void EnsurePresetSelection()", "public void EnsureSplitEncodeModeSelection()");

        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "internal static class CaptureComboBoxSelectionNormalizer");
        AssertContains(selectionNormalizerText, "public static CaptureDevice? ResolveCaptureDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static AudioInputDevice? ResolveAudioInputDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static ResolutionOption? ResolveResolutionSelection(");
        AssertContains(selectionNormalizerText, "public static FrameRateOption? ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "public static string? ResolveStringSelection(");
        AssertContains(selectionNormalizerText, "public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(selectionNormalizerText, "public static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertDoesNotContain(bindingsText, "DeviceComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingControllerText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(bindingControllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(bindingControllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(bindingControllerText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(bindingControllerText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(bindingControllerText, "AvailableFrameRates.FirstOrDefault(option =>");

        return Task.CompletedTask;
    }

    internal static Task CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks()
    {
        var normalizerType = RequireType("Sussudio.Controllers.CaptureComboBoxSelectionNormalizer");
        var captureDeviceType = RequireType("Sussudio.Models.CaptureDevice");
        var audioInputDeviceType = RequireType("Sussudio.Models.AudioInputDevice");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var resolveCaptureDevice = RequireNormalizerMethod(normalizerType, "ResolveCaptureDeviceSelection");
        var resolveAudioInputDevice = RequireNormalizerMethod(normalizerType, "ResolveAudioInputDeviceSelection");
        var resolveResolution = RequireNormalizerMethod(normalizerType, "ResolveResolutionSelection");
        var resolveFrameRate = RequireNormalizerMethod(normalizerType, "ResolveFrameRateSelection");
        var resolveString = RequireNormalizerMethod(normalizerType, "ResolveStringSelection");

        var staleCaptureDevice = CreateNormalizerDevice(captureDeviceType, "DEVICE-A", "old device");
        var firstCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-b", "first device");
        var liveCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-a", "live device");
        var captureDevices = CreateNormalizerList(captureDeviceType, firstCaptureDevice, liveCaptureDevice);
        AssertEqual(
            liveCaptureDevice,
            resolveCaptureDevice.Invoke(null, new[] { captureDevices, staleCaptureDevice }),
            "capture-device matching returns live collection instance by case-insensitive id");

        var staleAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "MIC-1", "old mic");
        var firstAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "line-1", "first input");
        var liveAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "mic-1", "live mic");
        var audioDevices = CreateNormalizerList(audioInputDeviceType, firstAudioDevice, liveAudioDevice);
        AssertEqual(
            liveAudioDevice,
            resolveAudioInputDevice.Invoke(null, new[] { audioDevices, staleAudioDevice }),
            "audio-device matching returns live collection instance by case-insensitive id");

        var disabledExactResolution = CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: false);
        var enabledFallbackResolution = CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true);
        var resolutionOptions = CreateResolutionOptionList(resolutionType, disabledExactResolution, enabledFallbackResolution);
        AssertEqual(
            disabledExactResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "3840X2160" }),
            "resolution exact selected value wins before enabled fallback");
        AssertEqual(
            enabledFallbackResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "1280x720" }),
            "resolution falls back to first enabled value");

        var disabledExactFrameRate = CreateFrameRateOption(frameRateType, 60d, 59.94d, "60000/1001", isEnabled: false);
        var autoFrameRate = CreateFrameRateOption(frameRateType, 0d, 0d, string.Empty, isEnabled: true);
        var enabledFrameRate = CreateFrameRateOption(frameRateType, 120d, 120d, "120/1", isEnabled: true);
        var frameRateOptions = CreateFrameRateOptionList(frameRateType, disabledExactFrameRate, autoFrameRate, enabledFrameRate);
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, true }),
            "auto frame-rate item wins when auto frame-rate is selected");
        AssertEqual(
            disabledExactFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, false }),
            "frame-rate exact selected value wins before enabled fallback");
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 30d, false }),
            "frame-rate fallback preserves first enabled item ordering");

        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "quality" }),
            "string fallback is case-insensitive");
        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "Missing" }),
            "string fallback uses the first item when no case-insensitive match exists");

        return Task.CompletedTask;
    }

    private static MethodInfo RequireNormalizerMethod(Type normalizerType, string methodName)
        => normalizerType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"CaptureComboBoxSelectionNormalizer.{methodName} was not found.");

    private static object CreateNormalizerDevice(Type deviceType, string id, string name)
    {
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException($"Failed to create {deviceType.Name}.");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", name);
        return device;
    }

    private static object CreateNormalizerList(Type elementType, params object[] items)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
                           ?? throw new InvalidOperationException($"Failed to create list for {elementType.Name}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    internal static Task ModeSelectionState_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var frameRateOptionsText = resolutionOptionsText;
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = captureModeOptionsControllerText;
        var resolutionOptionRebuildControllerText = captureModeOptionsControllerText;
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "private void RebuildResolutionOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildResolutionOptions();");
        AssertContains(resolutionOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertContains(resolutionOptionsText, "private static bool IsAutoResolutionValue(");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selection.Selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);");
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertDoesNotContain(frameRateRebuildControllerText, "_viewModel.");
        AssertContains(modeSelectionText, "private void ResetFrameRateSelectionState()");
        AssertContains(modeSelectionText, "_hasUserOverriddenFrameRateForCurrentMode = false;");
        AssertContains(modeSelectionText, "IsAutoFrameRateSelected = true;");
        AssertContains(modeSelectionText, "private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)");
        AssertContains(modeSelectionText, "_isApplyingAutomaticFrameRateSelection = true;\n        try\n        {\n            SelectedFrameRate = selected?.Value ?? fallbackRate;\n        }\n        finally\n        {\n            _isApplyingAutomaticFrameRateSelection = false;\n        }");
        AssertContains(modeSelectionText, "SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);");
        AssertContains(modeSelectionText, "SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "SelectedExactFrameRateArg = selected?.Rational;");
        AssertContains(modeSelectionText, "if (IsAutoResolutionValue(SelectedResolution))\n        {\n            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;\n        }");
        AssertContains(modeSelectionText, "AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "DisabledFrameRateReason = selected is { IsEnabled: false }\n            ? selected.DisableReason\n            : string.Empty;");
        AssertContains(modeSelectionText, "private void ResetModeSelectionState()");
        AssertContains(modeSelectionText, "ResetFrameRateSelectionState();");
        AssertContains(modeSelectionText, "_hasUserOverriddenResolutionForCurrentMode = false;");
        AssertContains(modeSelectionText, "_forceSourceAutoRetarget = false;");
        AssertContains(modeSelectionText, "_lastSourceModeKey = null;");
        AssertContains(modeSelectionText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(modeSelectionText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ModeSelectionState.cs")),
            "MainViewModel.ModeSelectionState.cs folded into MainViewModel.cs");

        return Task.CompletedTask;
    }

    internal static Task RecordingSettingsSelectionPolicy_LivesInFocusedHelper()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationRecordingControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var recordingSettingsPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs").Replace("\r\n", "\n");

        AssertContains(recordingRuntimeText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingRuntimeText, "=> _recordingCapabilityController.RebuildRecordingFormatOptions();");
        AssertDoesNotContain(recordingCapabilityControllerText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "public void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(_context.GetSelectedRecordingFormat())");
        AssertContains(recordingCapabilityControllerText, "_context.NotifySelectedRecordingFormatChanged();");
        AssertContains(recordingCapabilityControllerText, "Logger.Log($\"Selected recording format: {_context.GetSelectedRecordingFormat()}\");");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertDoesNotContain(captureModeTransactionsText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(automationSettingsText, "=> RunPersistedSettingsAutomationAsync(");
        AssertMemberContains(automationSettingsText, "SetRecordingFormatAsync", "_recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken)");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(matched)");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ParseVideoQuality(_context.GetSelectedQuality())");
        AssertContains(automationRecordingControllerText, "namespace Sussudio.Controllers;");
        AssertContains(automationRecordingControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(automationRecordingControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(automationRecordingControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(automationRecordingControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(automationRecordingControllerText, "_viewModel.");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps)");
        AssertContains(automationRecordingControllerText, "public async Task SetRecordingFormatAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationRecordingFormat.cs")),
            "stale recording format automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationRecordingSettings.cs")),
            "stale recording settings automation facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingFormatOptions.cs")),
            "stale recording format options partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingCapabilityRefresh.cs")),
            "stale recording capability refresh partial");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsHdrCompatibleRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static class RecordingSettingsSelectionPolicy");
        AssertContains(recordingSettingsPolicyText, "internal static bool IsHdrCompatible(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormat ParseRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static VideoQuality ParseVideoQuality(");
        AssertContains(recordingSettingsPolicyText, "internal static double ClampCustomBitrateMbps(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormatSelection Select(");
        AssertContains(recordingSettingsPolicyText, "internal sealed record RecordingFormatSelection(");
        AssertContains(recordingSettingsPolicyText, "Keep the last known real formats visible if capability refresh temporarily produced none.");

        return Task.CompletedTask;
    }

    internal static Task CaptureFormatSelectionPolicy_LivesInFocusedHelper()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(captureModeTransactionsText, "private void UpdateSelectedFormat()");
        AssertContains(captureModeTransactionsText, "private void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.UpdateSelectedFormat();");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildVideoFormatOptions();");
        AssertContains(captureModeOptionsControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionsControllerText, "CaptureFormatSelectionPolicy.Select(");
        AssertContains(captureModeOptionsControllerText, ".SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(");
        AssertContains(captureModeOptionsControllerText, "_context.AvailableVideoFormats.Clear();");
        AssertContains(captureModeOptionsControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertDoesNotContain(captureModeOptionsControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeTransactionsText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsHdrModeCandidate(");
        AssertDoesNotContain(captureModeTransactionsText, "ShouldPreserveMjpegHighFrameRateMode(");
        AssertContains(policyText, "internal static class CaptureFormatSelectionPolicy");
        AssertContains(policyText, "internal static MediaFormat? Select(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "internal static IReadOnlyList<MediaFormat> SelectModeTupleFormats(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(policyText, "CaptureModeOptionsBuilder.IsHdrModeCandidate(format)");
        AssertContains(policyText, "internal sealed record CaptureFormatSelectionRequest(");
        AssertEqual(
            true,
            policyText.Split('\n').Length >= 100,
            "capture format selection policy is a substantial ownership file");

        return Task.CompletedTask;
    }

    internal static Task CaptureFormatSelectionPolicy_PreservesSelectionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        var sdrNv12 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "NV12", isHdr: false);
        var sdrMjpg = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "MJPG", isHdr: false);
        var hdrP010 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "P010", isHdr: true);
        var ntsc119 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120000d / 1001d, 120000, 1001, "NV12", isHdr: false);
        var otherResolution = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 120, 120, 1, "NV12", isHdr: false);
        var formats = CreateMediaFormatList(mediaFormatType, hdrP010, sdrNv12, sdrMjpg, ntsc119, otherResolution);
        var frameRates = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120000d / 1001d, "120000/1001", isEnabled: true));

        var sdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(false, GetBoolProperty(sdrAuto!, "IsHdr"), "SDR selected format excludes HDR when SDR alternatives exist");
        AssertEqual("NV12", GetStringProperty(sdrAuto!, "PixelFormat"), "4K HFR SDR auto preserves existing source-order tie");

        var hdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            preferredTimingFamilyName: "Integer");
        AssertEqual(true, GetBoolProperty(hdrAuto!, "IsHdr"), "HDR selected format uses HDR candidates");
        AssertEqual("P010", GetStringProperty(hdrAuto!, "PixelFormat"), "HDR selected format keeps P010 candidate");

        var explicitNv12 = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual("NV12", GetStringProperty(explicitNv12!, "PixelFormat"), "explicit selected pixel format narrows candidates");
        AssertEqual(120u, (uint)GetPropertyValue(explicitNv12!, "FrameRateNumerator")!, "integer timing family wins for explicit NV12");

        var ntscPreferred = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120000d / 1001d,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Ntsc1001");
        AssertEqual(120000u, (uint)GetPropertyValue(ntscPreferred!, "FrameRateNumerator")!, "friendly bucket selection preserves NTSC timing");

        var unavailablePixelFormat = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "YUY2",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(null, unavailablePixelFormat, "unavailable explicit pixel format returns no selected format");

        var tupleFormats = InvokeCaptureFormatModeTupleFormats(
                formats,
                frameRates,
                width: 3840,
                height: 2160,
                selectedFrameRate: 120000d / 1001d,
                selectedVideoFormat: "Auto",
                isHdrEnabled: false,
                preferredTimingFamilyName: "Ntsc1001")
            .Cast<object>()
            .ToArray();
        AssertEqual(3, tupleFormats.Length, "friendly 119.88/120 mode tuple includes SDR bucket variants");
        AssertEqual(
            false,
            tupleFormats.Any(format => GetBoolProperty(format, "IsHdr")),
            "mode tuple formats exclude HDR while SDR is selected");

        return Task.CompletedTask;
    }

    private static object? InvokeCaptureFormatSelection(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request });
    }

    private static IEnumerable InvokeCaptureFormatModeTupleFormats(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var selectModeTupleFormats = policyType.GetMethod("SelectModeTupleFormats", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats missing.");
        return (IEnumerable)(selectModeTupleFormats.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats returned null."));
    }

    private static object CreateCaptureFormatSelectionRequest(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionRequest");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", preferredTimingFamilyName);
        var constructor = FindConstructor(requestType, parameterCount: 8);
        return constructor.Invoke(new[]
        {
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            timingFamily
        });
    }

    private static object InvokeDeviceFormatProbeRetargetDecision(
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        bool isHdrEnabled,
        bool modeChanged,
        string? previousResolution,
        double previousFrameRate,
        string? selectedResolution,
        double selectedFrameRate,
        object? selectedFormat,
        object supportedFormats,
        bool previousResolutionAvailable,
        bool includeSessionMismatchCheck,
        uint? sessionActualWidth,
        uint? sessionActualHeight)
    {
        var requestType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetRequest");
        var policyType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 14);
        var request = constructor.Invoke(new object?[]
        {
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            isHdrEnabled,
            modeChanged,
            previousResolution,
            previousFrameRate,
            selectedResolution,
            selectedFrameRate,
            selectedFormat,
            supportedFormats,
            previousResolutionAvailable,
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight
        });
        var decide = policyType.GetMethod("Decide", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide missing.");
        return decide.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide returned null.");
    }

    private static string GetEnumName(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName)!.GetValue(instance)?.ToString()
           ?? throw new InvalidOperationException($"{propertyName} returned null.");

    private static object InvokeCaptureResolutionSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        string? preferredSelection,
        double previousFrameRate,
        bool isHdrEnabled,
        bool allowSourceAutoSelect,
        bool pendingSdrAutoSelectionForDeviceChange)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 8);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            preferredSelection,
            previousFrameRate,
            isHdrEnabled,
            allowSourceAutoSelect,
            pendingSdrAutoSelectionForDeviceChange
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select returned null.");
    }

    private static object InvokeAutoCaptureSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        bool isHdrEnabled)
    {
        var requestType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 4);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            isHdrEnabled
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select returned null.");
    }

    private static object InvokeFrameRateAutoSelection(
        object options,
        bool autoFrameRateOptionAvailable,
        bool forceAutoSelection,
        bool isAutoFrameRateSelected,
        bool hasUserOverriddenFrameRateForCurrentMode,
        bool isHdrEnabled,
        bool pendingSdrAutoSelectionForDeviceChange,
        int? pendingSdrAutoFriendlyFrameRateBucket,
        double? sourceRate,
        bool sourceTimingFamilyKnown,
        string sourceTimingFamilyName,
        double previousRate)
    {
        var sourceType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionSource");
        var requestType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionPolicy");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", sourceTimingFamilyName);
        var sourceConstructor = FindConstructor(sourceType, parameterCount: 3);
        var source = sourceConstructor.Invoke(new object?[]
        {
            sourceRate,
            sourceTimingFamilyKnown,
            timingFamily
        });
        var requestConstructor = FindConstructor(requestType, parameterCount: 10);
        var request = requestConstructor.Invoke(new object?[]
        {
            options,
            autoFrameRateOptionAvailable,
            forceAutoSelection,
            isAutoFrameRateSelected,
            hasUserOverriddenFrameRateForCurrentMode,
            isHdrEnabled,
            pendingSdrAutoSelectionForDeviceChange,
            pendingSdrAutoFriendlyFrameRateBucket,
            source,
            previousRate
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select returned null.");
    }

    private static ConstructorInfo FindConstructor(Type type, int parameterCount)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (constructor.GetParameters().Length == parameterCount)
            {
                return constructor;
            }
        }

        throw new InvalidOperationException($"{type.Name} constructor with {parameterCount} parameters was not found.");
    }

    private static object CreateResolutionOptionList(Type resolutionType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(resolutionType))
                           ?? throw new InvalidOperationException("Failed to create resolution option list."));
        foreach (var option in options)
        {
            list.Add(option);
        }

        return list;
    }

    private static object CreateFrameRateOptionList(Type frameRateType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(frameRateType))
                           ?? throw new InvalidOperationException("Failed to create frame-rate option list."));
        foreach (var option in options)
        {
            list.Add(option);
        }

        return list;
    }

    private static object CreateResolutionOption(
        Type resolutionType,
        string value,
        uint width,
        uint height,
        bool isEnabled)
    {
        var option = CreateConfigInstance(resolutionType);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Width", width);
        SetPropertyOrBackingField(option, "Height", height);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
    }

    private static object CreateFrameRateOption(
        Type frameRateType,
        double friendlyValue,
        double value,
        string rational,
        bool isEnabled)
    {
        var option = CreateConfigInstance(frameRateType);
        SetPropertyOrBackingField(option, "FriendlyValue", friendlyValue);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Rational", rational);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
    }

    internal static Task DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var retargetApplierText = probeControllerText;
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertDoesNotContain(probeControllerText, "var nv12Candidates = target.SupportedFormats");
        AssertDoesNotContain(probeControllerText, "ShouldPreserveMjpegHighFrameRateMode(_viewModel.SelectedFormat)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(retargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "EnqueueUiOperation(");
        AssertContains(retargetPolicyText, "internal static class DeviceFormatProbeRetargetPolicy");
        AssertContains(retargetPolicyText, "internal static DeviceFormatProbeRetargetDecision Decide(DeviceFormatProbeRetargetRequest request)");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetDecision(");
        AssertContains(retargetPolicyText, "CaptureSettings.IsMjpegHighFrameRateMode(");
        AssertContains(retargetPolicyText, "\"format probe (HDR retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (SDR nv12 retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (session mismatch)\"");
        AssertDoesNotContain(retargetPolicyText, "Logger.Log(");
        AssertDoesNotContain(retargetPolicyText, "ReinitializeDeviceAsync(");
        AssertDoesNotContain(retargetPolicyText, "SelectedResolution =");
        AssertDoesNotContain(retargetPolicyText, "RebuildFrameRateOptions(");

        return Task.CompletedTask;
    }

    internal static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var retargetApplierText = probeControllerText;
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "target.SupportedFormats.Clear();");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(retargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(retargetDecision.TargetResolution);");
        AssertContains(retargetApplierText, "_context.RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(previousResolution);");
        AssertContains(retargetApplierText, "_context.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }

    internal static Task DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");

        var hdrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: true,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "1920x1080",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "P010", isHdr: true),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("HdrRetarget", GetEnumName(hdrDecision, "Kind"), "HDR retarget decision");
        AssertEqual("format probe (HDR retarget)", GetStringProperty(hdrDecision, "ReinitializeReason"), "HDR retarget reason");
        AssertEqual("format probe hdr retarget", GetStringProperty(hdrDecision, "UiOperationName"), "HDR retarget UI operation");

        var mjpgHfrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "NV12", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("PreserveMjpegHighFrameRate", GetEnumName(mjpgHfrDecision, "Kind"), "MJPG HFR preserve decision");

        var sdrNv12Decision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1280x720",
            previousFrameRate: 60,
            selectedResolution: "1280x720",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 3840, 2160, 30, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("SdrNv12Retarget", GetEnumName(sdrNv12Decision, "Kind"), "SDR NV12 retarget decision");
        AssertEqual("1920x1080", GetStringProperty(sdrNv12Decision, "TargetResolution"), "SDR NV12 target resolution");
        AssertEqual(60d, sdrNv12Decision.GetType().GetProperty("TargetFrameRate")!.GetValue(sdrNv12Decision), "SDR NV12 target frame rate");
        AssertEqual("format probe (SDR nv12 retarget)", GetStringProperty(sdrNv12Decision, "ReinitializeReason"), "SDR NV12 reason");
        AssertEqual("format probe sdr retarget", GetStringProperty(sdrNv12Decision, "UiOperationName"), "SDR NV12 UI operation");

        var sessionMismatchDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1920x1080",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: true,
            sessionActualWidth: 1280,
            sessionActualHeight: 720);
        AssertEqual("SessionMismatch", GetEnumName(sessionMismatchDecision, "Kind"), "session mismatch decision");
        AssertEqual("format probe (session mismatch)", GetStringProperty(sessionMismatchDecision, "ReinitializeReason"), "session mismatch reason");
        AssertEqual("format probe session mismatch", GetStringProperty(sessionMismatchDecision, "UiOperationName"), "session mismatch UI operation");

        var restoreDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: false,
            isHdrEnabled: false,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("RestoreActiveSelection", GetEnumName(restoreDecision, "Kind"), "recording-time restore decision");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText =
            ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
                .Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var captureSettingsBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureSettings.cs")),
            "MainViewModel capture-settings adapter folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureSettingsProjectionBuilder.cs")),
            "capture settings projection builder folded into ViewModelBuilders.cs");
        AssertContains(captureStateText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureStateText, "var runtime = _captureService.GetRuntimeSnapshot();");
        AssertContains(captureStateText, "var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(captureStateText, "return CaptureSettingsProjectionBuilder.Build(new CaptureSettingsProjectionInput");
        AssertContains(captureStateText, "AvailableFrameRates = AvailableFrameRates.ToArray(),");
        AssertContains(captureStateText, "SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,");
        AssertContains(captureStateText, "SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,");
        AssertContains(captureSettingsBuilderText, "internal static class CaptureSettingsProjectionBuilder");
        AssertDoesNotContain(captureSettingsBuilderText, "partial class CaptureSettingsProjectionBuilder");
        AssertContains(captureSettingsBuilderText, "public static CaptureSettings Build(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "FrameRate = frameRateProjection.EffectiveFrameRate,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,");
        AssertContains(captureSettingsBuilderText, "RequestedPixelFormat = ResolveRequestedPixelFormat(input)");
        AssertContains(captureSettingsBuilderText, "ForceMjpegDecode = ShouldForceMjpegDecode(input)");
        AssertContains(captureSettingsBuilderText, "settings.UseCustomAudioInput = input.IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsBuilderText, "settings.MicrophoneEnabled = input.IsMicrophoneEnabled;");
        AssertContains(captureSettingsBuilderText, "private static CaptureSettingsFrameRateProjection ProjectFrameRate(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "private static string? ResolveRequestedPixelFormat(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "private static bool ShouldForceMjpegDecode(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "internal sealed class CaptureSettingsProjectionInput");
        AssertContains(captureSettingsBuilderText, "var selectedFrameRateOption = input.AvailableFrameRates");
        AssertContains(captureSettingsBuilderText, "var effectiveFrameRate = input.IsAutoResolutionSelected && input.AutoResolvedFrameRate.HasValue && input.AutoResolvedFrameRate.Value > 0");
        AssertContains(captureSettingsBuilderText, "runtimeMatchesResolution");
        AssertContains(captureSettingsBuilderText, "input.Runtime.NegotiatedFrameRateNumerator");
        AssertContains(captureSettingsBuilderText, "input.SourceTelemetry.HasFrameRate");
        AssertContains(captureSettingsBuilderText, "TryParseFrameRateRational(requestedFrameRateArg");
        AssertContains(captureSettingsBuilderText, "input.SelectedFormat?.FrameRateNumerator > 0 && input.SelectedFormat.FrameRateDenominator > 0");
        AssertContains(captureSettingsBuilderText, "requestedFrameRateArg = effectiveFrameRate.ToString(\"0.###\");");
        AssertContains(captureSettingsBuilderText, "internal readonly record struct CaptureSettingsFrameRateProjection(");
        AssertDoesNotContain(captureStateText, "ProjectCaptureSettingsFrameRate");
        AssertDoesNotContain(captureStateText, "private string? ResolveRequestedPixelFormat()");
        AssertDoesNotContain(captureStateText, "private bool ShouldForceMjpegDecode()");
        AssertContains(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(previewLifecycleControllerText, "await _context.SessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingTransitionControllerText, "await _context.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence()
    {
        var settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1),
            runtime: CreateRuntimeSnapshot(
                actualWidth: 1920,
                actualHeight: 1080,
                actualFrameRate: 60000d / 1001d,
                actualFrameRateArg: "60000/1001",
                negotiatedNumerator: 60000,
                negotiatedDenominator: 1001),
            sourceTelemetry: CreateSourceTelemetry(frameRateExact: 60, frameRateArg: "60/1"),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60000d / 1001d,
                "60000/1001",
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "source-over-runtime effective frame rate");
        AssertEqual("60/1", GetStringProperty(settings, "RequestedFrameRateArg"), "source telemetry frame-rate arg wins after runtime");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "source telemetry numerator wins after runtime");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "source telemetry denominator wins after runtime");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 59.94, numerator: 60000, denominator: 1001),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60,
                string.Empty,
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "selected frame-rate effective value");
        AssertEqual("60000/1001", GetStringProperty(settings, "RequestedFrameRateArg"), "selected format rational fallback");
        AssertEqual(60000, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "selected format fallback numerator");
        AssertEqual(1001, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "selected format fallback denominator");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "Source",
            selectedFrameRate: 0,
            autoResolvedFrameRate: 119.88,
            selectedFormat: null,
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry());

        AssertNearlyEqual(119.88, GetDoubleProperty(settings, "FrameRate"), 0.001, "auto-resolved effective frame rate");
        AssertEqual("119.88", GetStringProperty(settings, "RequestedFrameRateArg"), "decimal frame-rate fallback");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateNumerator"), "decimal fallback numerator remains unset");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateDenominator"), "decimal fallback denominator remains unset");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            mjpegDecoderCount: 99);

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "auto SDR 4K HFR requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "auto SDR 4K HFR forces MJPEG decode");
        AssertEqual(8, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps high");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "P010"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            isTrueHdrPreviewEnabled: true,
            mjpegDecoderCount: 0);

        AssertEqual("P010", GetStringProperty(settings, "RequestedPixelFormat"), "HDR auto keeps selected format pixel format");
        AssertEqual(false, GetBoolProperty(settings, "ForceMjpegDecode"), "HDR auto does not force MJPEG decode");
        AssertEqual("Hdr10Pq", GetPropertyValue(settings, "HdrOutputMode")?.ToString(), "HDR output mode");
        AssertEqual("TrueHdr", GetPropertyValue(settings, "PreviewMode")?.ToString(), "true HDR preview mode");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps low");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "MJPG",
            isHdrEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-1",
            selectedAudioInputDeviceName: "Capture Audio",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-1",
            selectedMicrophoneDeviceName: "Mic");

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "explicit MJPG requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "explicit MJPG forces MJPEG decode");
        AssertEqual(true, GetBoolProperty(settings, "UseCustomAudioInput"), "custom audio flag copied");
        AssertEqual("audio-1", GetStringProperty(settings, "AudioDeviceId"), "custom audio id copied");
        AssertEqual("Capture Audio", GetStringProperty(settings, "AudioDeviceName"), "custom audio name copied");
        AssertEqual(true, GetBoolProperty(settings, "MicrophoneEnabled"), "microphone flag copied");
        AssertEqual("mic-1", GetStringProperty(settings, "MicrophoneDeviceId"), "microphone id copied");
        AssertEqual("Mic", GetStringProperty(settings, "MicrophoneDeviceName"), "microphone name copied");

        return Task.CompletedTask;
    }

    private static object InvokeCaptureSettingsProjection(
        string selectedResolution,
        double selectedFrameRate,
        double? autoResolvedFrameRate,
        object? selectedFormat,
        object runtime,
        object sourceTelemetry,
        string? selectedVideoFormat = "Auto",
        bool isHdrEnabled = false,
        bool isTrueHdrPreviewEnabled = false,
        int mjpegDecoderCount = 6,
        bool isCustomAudioInputEnabled = false,
        string? selectedAudioInputDeviceId = null,
        string? selectedAudioInputDeviceName = null,
        bool isMicrophoneEnabled = false,
        string? selectedMicrophoneDeviceId = null,
        string? selectedMicrophoneDeviceName = null,
        params object[] frameRateOptions)
    {
        var inputType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionInput");
        var input = CreateConfigInstance(inputType);
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var availableFrameRates = Array.CreateInstance(frameRateType, frameRateOptions.Length);
        for (var i = 0; i < frameRateOptions.Length; i++)
        {
            availableFrameRates.SetValue(frameRateOptions[i], i);
        }

        SetPropertyOrBackingField(input, "EffectiveResolutionKnown", true);
        SetPropertyOrBackingField(input, "EffectiveWidth", 1920u);
        SetPropertyOrBackingField(input, "EffectiveHeight", 1080u);
        SetPropertyOrBackingField(input, "SelectedResolution", selectedResolution);
        SetPropertyOrBackingField(input, "SelectedFrameRate", selectedFrameRate);
        SetPropertyOrBackingField(input, "AutoResolvedFrameRate", autoResolvedFrameRate);
        SetPropertyOrBackingField(input, "IsAutoResolutionSelected", string.Equals(selectedResolution, "Source", StringComparison.OrdinalIgnoreCase));
        SetPropertyOrBackingField(input, "SelectedFormat", selectedFormat);
        SetPropertyOrBackingField(input, "AvailableFrameRates", availableFrameRates);
        SetPropertyOrBackingField(input, "Runtime", runtime);
        SetPropertyOrBackingField(input, "SourceTelemetry", sourceTelemetry);
        SetPropertyOrBackingField(input, "SelectedVideoFormat", selectedVideoFormat);
        SetPropertyOrBackingField(input, "IsHdrEnabled", isHdrEnabled);
        SetPropertyOrBackingField(input, "IsTrueHdrPreviewEnabled", isTrueHdrPreviewEnabled);
        SetPropertyOrBackingField(input, "MjpegDecoderCount", mjpegDecoderCount);
        SetPropertyOrBackingField(input, "SelectedRecordingFormat", "HEVC");
        SetPropertyOrBackingField(input, "SelectedQuality", "High");
        SetPropertyOrBackingField(input, "SelectedPreset", "P5");
        SetPropertyOrBackingField(input, "SelectedSplitEncodeMode", "Auto");
        SetPropertyOrBackingField(input, "CustomBitrateMbps", 42d);
        SetPropertyOrBackingField(input, "OutputPath", "C:\\Capture");
        SetPropertyOrBackingField(input, "FlashbackGpuDecode", true);
        SetPropertyOrBackingField(input, "FlashbackBufferMinutes", 5);
        SetPropertyOrBackingField(input, "IsAudioEnabled", true);
        SetPropertyOrBackingField(input, "IsCustomAudioInputEnabled", isCustomAudioInputEnabled);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceId", selectedAudioInputDeviceId);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceName", selectedAudioInputDeviceName);
        SetPropertyOrBackingField(input, "IsMicrophoneEnabled", isMicrophoneEnabled);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceId", selectedMicrophoneDeviceId);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceName", selectedMicrophoneDeviceName);

        var builderType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build was not found.");
        return build.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build returned null.");
    }

    private static object CreateRuntimeSnapshot(
        uint? actualWidth = null,
        uint? actualHeight = null,
        double? actualFrameRate = null,
        string? actualFrameRateArg = null,
        uint? negotiatedNumerator = null,
        uint? negotiatedDenominator = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
        SetPropertyOrBackingField(snapshot, "ActualWidth", actualWidth);
        SetPropertyOrBackingField(snapshot, "ActualHeight", actualHeight);
        SetPropertyOrBackingField(snapshot, "ActualFrameRate", actualFrameRate);
        SetPropertyOrBackingField(snapshot, "ActualFrameRateArg", actualFrameRateArg);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedDenominator);
        return snapshot;
    }

    private static object CreateSourceTelemetry(double? frameRateExact = null, string? frameRateArg = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot"));
        SetPropertyOrBackingField(snapshot, "FrameRateExact", frameRateExact);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", frameRateArg);
        return snapshot;
    }

    private static object CreateMediaFormat(
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator,
        string pixelFormat = "NV12")
    {
        var format = CreateConfigInstance(RequireType("Sussudio.Models.MediaFormat"));
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        return format;
    }

    internal static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var resolutionOptionRebuildControllerText = captureModeOptionsControllerText;
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var autoCaptureSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");
        var helperText = autoCaptureSelectionPolicyText;

        AssertContains(captureModeTransactionsText, "private void RebuildResolutionOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildResolutionOptions();");
        AssertContains(resolutionOptionRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(resolutionOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(resolutionOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(resolutionOptionRebuildControllerText, "private AutoCaptureSelection? ResolveAutoCaptureSelection(");
        AssertContains(resolutionOptionRebuildControllerText, "AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(");
        AssertContains(resolutionOptionRebuildControllerText, "CaptureModeOptionsBuilder.BuildResolutionOptions(");
        AssertContains(resolutionOptionRebuildControllerText, "_context.AvailableResolutions.Clear();");
        AssertContains(resolutionOptionRebuildControllerText, "_context.AvailableResolutions.Add(option);");
        AssertDoesNotContain(captureModeOptionsControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(resolutionOptionRebuildControllerText, "_viewModel.");
        AssertContains(resolutionOptionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertContains(captureModeOptionsControllerText, "public void RebuildResolutionOptions()");
        AssertContains(resolutionOptionRebuildControllerText, "var allowSourceAutoSelect =\n            string.Equals(previousSelection, _context.AutoResolutionValue, StringComparison.OrdinalIgnoreCase) ||");
        AssertDoesNotContain(captureModeOptionsControllerText, "_viewModel.AvailableResolutions.Clear();");
        AssertContains(resolutionOptionRebuildControllerText, "private ResolutionOption CreateAutoResolutionOption()");
        AssertContains(resolutionOptionRebuildControllerText, "Value = _context.AutoResolutionValue,");
        AssertContains(resolutionOptionRebuildControllerText, "private bool ShouldSelectAutoResolutionOption(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectBestAutoResolutionCandidate(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "ViewModels",
                "MainViewModel.AutoResolutionSelection.cs")),
            "MainViewModel auto resolution selection adapter partial");
        AssertContains(autoCaptureSelectionPolicyText, "internal sealed record AutoCaptureSelection(");
        AssertContains(autoCaptureSelectionPolicyText, "internal sealed record AutoCaptureSelectionRequest(");
        AssertContains(autoCaptureSelectionPolicyText, "internal static class CaptureModeOptionsBuilder");
        AssertContains(autoCaptureSelectionPolicyText, "internal static class CaptureFormatSelectionPolicy");
        AssertContains(autoCaptureSelectionPolicyText, "internal static class AutoCaptureSelectionPolicy");
        AssertContains(autoCaptureSelectionPolicyText, "internal static AutoCaptureSelection? Select(AutoCaptureSelectionRequest request)");
        AssertContains(autoCaptureSelectionPolicyText, "private static ResolutionOption? SelectBestResolutionCandidate(");
        AssertContains(autoCaptureSelectionPolicyText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ResolutionOptions.cs")), "old resolution options partial folded into capture selection owner");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "SelectedResolution =");
        AssertContains(resolutionOptionsText, "/// Capture-device, resolution, and frame-rate selection reactions.");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(resolutionOptionRebuildControllerText, "private void UpdateAutoResolutionState(AutoCaptureSelection? selection)");
        AssertContains(resolutionOptionRebuildControllerText, "_context.SetAutoResolvedWidth(selection?.Resolution.Width);");
        AssertContains(resolutionOptionRebuildControllerText, "private void ClearAutoResolutionState()");
        AssertContains(capturePresentationText, "// Capture presentation adapters that apply runtime/source state to ViewModel labels.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into MainViewModel.cs");
        AssertContains(capturePresentationText, "private string GetSelectedResolutionDisplayText()");
        AssertContains(capturePresentationText, "return $\"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)\";");
        AssertContains(resolutionOptionsText, "private static bool IsAutoResolutionValue(");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertContains(resolutionOptionsText, "var preserveSourceTelemetryForActiveSourceSelection =\n            resetTelemetryState &&\n            device != null &&\n            IsPreviewing &&\n            IsAutoResolutionValue(SelectedResolution) &&\n            _latestSourceTelemetry.HasDimensions;");
        AssertContains(resolutionOptionsText, "private void RefreshAutoResolvedResolutionFromLatestSource()");
        AssertContains(resolutionOptionsText, "if (IsAutoResolutionValue(value))\n        {\n            RefreshAutoResolvedResolutionFromLatestSource();\n        }");
        AssertContains(resolutionOptionsText, "if (!IsAutoResolutionValue(value) &&\n            TryResolveResolutionKey(value, out var resolvedResolutionKey))");
        AssertContains(resolutionOptionsText, "private string? GetEffectiveResolutionKey(");
        AssertContains(resolutionOptionsText, "private bool TryGetEffectiveResolutionSelection(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(resolutionOptionRebuildControllerText, "CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.TryParseResolutionKey(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.BuildHdrSupportHint(");
        AssertDoesNotContain(resolutionOptionsText, "SelectNearestResolution(");
        AssertDoesNotContain(resolutionOptionsText, "sdrFriendlyBucketsByResolution");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionOptions.cs")),
            "old auto resolution options partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionState.cs")),
            "old auto resolution state partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ResolutionSelectionPolicy.cs")),
            "old resolution selection policy adapter partial removed");
        AssertContains(helperText, "internal static class CaptureResolutionSelectionPolicy");
        AssertDoesNotContain(helperText, "partial class CaptureResolutionSelectionPolicy");
        AssertContains(helperText, "internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)");
        AssertContains(helperText, "internal static bool TryParseResolutionKey(");
        AssertContains(helperText, "internal static string BuildHdrSupportHint(");
        AssertContains(helperText, "private static ResolutionOption? SelectSourceResolutionOption(");
        AssertContains(helperText, "SelectNearestResolution(sourceKey, enabled)");
        AssertContains(helperText, "private static HdrResolutionSelection SelectHdrResolutionOption(");
        AssertContains(helperText, "SelectNearestResolution(previousSelection, sameFpsCandidates)");
        AssertContains(helperText, "private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(");
        AssertContains(helperText, "sdrFriendlyBucketsByResolution");
        AssertContains(helperText, "internal static bool ResolutionSupportsFriendlyFrameRate(");
        AssertContains(helperText, "private static ResolutionOption? SelectNearestResolution(");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelectionRequest(");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelection(");
        AssertDoesNotContain(helperText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(helperText, "OnPropertyChanged(");
        AssertDoesNotContain(helperText, "SelectedResolution =");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.cs")),
            "resolution selection policy folded into ViewModelSelectionPolicies.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Source.cs")),
            "old source resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Hdr.cs")),
            "old HDR resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Sdr.cs")),
            "old SDR resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Support.cs")),
            "old resolution support policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Ranking.cs")),
            "old resolution ranking policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Models.cs")),
            "old resolution policy models partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureResolutionSelectionPolicy_PreservesHdrSourceRetargetBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 120, "P010", isHdr: true));

        var options = CreateResolutionOptionList(
            resolutionType,
            CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
            CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
            CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true));
        var telemetry = CreateConfigInstance(telemetryType);
        SetPropertyOrBackingField(telemetry, "Width", 3840);
        SetPropertyOrBackingField(telemetry, "Height", 2160);

        var selection = InvokeCaptureResolutionSelection(
            options,
            formatsByResolution,
            telemetry,
            preferredSelection: "3840x2160",
            previousFrameRate: 120,
            isHdrEnabled: true,
            allowSourceAutoSelect: true,
            pendingSdrAutoSelectionForDeviceChange: false);
        var selected = selection.GetType().GetProperty("Selected")!.GetValue(selection)
            ?? throw new InvalidOperationException("HDR source retarget returned no selection.");

        AssertEqual("1920x1080", GetStringProperty(selected, "Value"), "HDR source retarget preserves frame-rate bucket before resolution");
        AssertEqual(
            "HDR at 3840x2160 supported up to 60 fps; switched to 1920x1080 to keep 120 fps.",
            selection.GetType().GetProperty("HdrHint")!.GetValue(selection) as string,
            "HDR source retarget hint");

        var retained = InvokeCaptureResolutionSelection(
            options,
            formatsByResolution,
            telemetry,
            preferredSelection: "3840x2160",
            previousFrameRate: 60,
            isHdrEnabled: true,
            allowSourceAutoSelect: true,
            pendingSdrAutoSelectionForDeviceChange: false);
        var retainedSelected = retained.GetType().GetProperty("Selected")!.GetValue(retained)
            ?? throw new InvalidOperationException("HDR exact match retention returned no selection.");

        AssertEqual("3840x2160", GetStringProperty(retainedSelected, "Value"), "HDR exact source match remains selected when it supports the current rate");
        AssertEqual(null, retained.GetType().GetProperty("HdrHint")!.GetValue(retained) as string, "HDR retained exact match defers support hint fallback to ResolutionOptions");

        return Task.CompletedTask;
    }

    internal static Task CaptureResolutionSelectionPolicy_PreservesSdrAutoBucketPreference()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 30, "NV12", isHdr: false));

        var selection = InvokeCaptureResolutionSelection(
            CreateResolutionOptionList(
                resolutionType,
                CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
                CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
                CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true)),
            formatsByResolution,
            CreateConfigInstance(telemetryType),
            preferredSelection: "3840x2160",
            previousFrameRate: 120,
            isHdrEnabled: false,
            allowSourceAutoSelect: false,
            pendingSdrAutoSelectionForDeviceChange: true);
        var selected = selection.GetType().GetProperty("Selected")!.GetValue(selection)
            ?? throw new InvalidOperationException("SDR auto selection returned no selection.");

        AssertEqual("1920x1080", GetStringProperty(selected, "Value"), "SDR auto prefers a 60 fps bucket before largest 120-only resolution");
        AssertEqual(60, selection.GetType().GetProperty("SdrAutoFriendlyFrameRateBucket")!.GetValue(selection), "SDR auto selected friendly bucket");

        return Task.CompletedTask;
    }

    internal static Task AutoCaptureSelectionPolicy_PreservesSourceBoundedSelection()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 30, "NV12", isHdr: false));

        var telemetry = CreateConfigInstance(telemetryType);
        SetPropertyOrBackingField(telemetry, "Width", 1920);
        SetPropertyOrBackingField(telemetry, "Height", 1080);
        SetPropertyOrBackingField(telemetry, "FrameRateExact", 60d);

        var selection = InvokeAutoCaptureSelection(
            CreateResolutionOptionList(
                resolutionType,
                CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
                CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
                CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true)),
            formatsByResolution,
            telemetry,
            isHdrEnabled: false);
        var selectedResolution = selection.GetType().GetProperty("Resolution")!.GetValue(selection)
            ?? throw new InvalidOperationException("Auto capture selection returned no resolution.");

        AssertEqual("1920x1080", GetStringProperty(selectedResolution, "Value"), "Auto capture selection caps resolution to source dimensions");
        AssertEqual(60, selection.GetType().GetProperty("FriendlyFrameRate")!.GetValue(selection), "Auto capture selection keeps source-friendly frame-rate bucket");
        AssertEqual(60d, GetDoubleProperty(selection, "ExactFrameRate"), "Auto capture selection keeps exact frame rate");

        return Task.CompletedTask;
    }

    internal static Task SourceFilteredFrameRatesAreAlwaysUnlocked()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(frameRateRebuildControllerText, "FrameRateSourceFilterPolicy.Apply(");
        AssertContains(frameRateRebuildControllerText, "true);");
        AssertContains(mainViewModelText, "RebuildFrameRateOptions();");
        AssertContains(sourceFilterPolicyText, "showAllCaptureOptions");
        AssertContains(sourceFilterPolicyText, "!IsSourceFilteredFrameRateDisableReason(option.DisableReason)");
        AssertContains(sourceFilterPolicyText, "CloneOption(option, isEnabled: true, disableReason: string.Empty)");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsSourceFilteredFrameRateDisableReason(");
        AssertDoesNotContain(captureModeTransactionsText, "higher capture fps duplicates frames");

        return Task.CompletedTask;
    }

    internal static Task FrameRateSourceFilterPolicy_LivesInFocusedHelper()
    {
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = captureModeOptionsControllerText;
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var repoRoot = GetRepoRoot();

        AssertContains(frameRateOptionsText, "/// Capture-device, resolution, and frame-rate selection reactions.");
        AssertContains(frameRateOptionsText, "private void SelectAutoFrameRate(bool rebuildOptions)");
        AssertContains(frameRateOptionsText, "private void RebuildFrameRateOptions()");
        AssertContains(frameRateOptionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(captureModeTransactionsText, "private void RebuildFrameRateOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(captureModeOptionsControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionsControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionsControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(frameRateRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(frameRateRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(frameRateRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(frameRateRebuildControllerText, "var sourceRate = _frameRateTimingResolver.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Clear();");
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertDoesNotContain(captureModeOptionsControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(frameRateRebuildControllerText, "_viewModel.");
        AssertContains(modeSelectionText, "private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)");
        AssertContains(sourceFilterPolicyText, "internal static class FrameRateSourceFilterPolicy");
        AssertContains(sourceFilterPolicyText, "internal static FrameRateSourceFilterResult Apply(");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateOptions.cs")), "old frame-rate options partial folded into capture selection owner");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateSourceFilterPolicy.cs")), "old nested frame-rate source-filter partial removed");
        AssertContains(sourceFilterPolicyText, "IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants");
        AssertContains(sourceFilterPolicyText, "option.FriendlyValue > sourceFriendlyRate.Value + 0.01");
        AssertContains(sourceFilterPolicyText, "option.Value > sourceRate.Value + 0.03");
        AssertContains(sourceFilterPolicyText, "higher capture fps duplicates frames");
        AssertContains(sourceFilterPolicyText, "duplicate variant is hidden");
        AssertContains(sourceFilterPolicyText, "not a clean divisor");
        AssertDoesNotContain(sourceFilterPolicyText, "private readonly record struct FrameRateTimingVariant(");
        AssertDoesNotContain(sourceFilterPolicyText, "private IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertDoesNotContain(sourceFilterPolicyText, "AvailableFrameRates.Clear();");
        AssertDoesNotContain(sourceFilterPolicyText, "ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(sourceFilterPolicyText, "DetectedSourceFrameRate =");

        return Task.CompletedTask;
    }

    internal static Task FrameRateAutoSelectionPolicy_LivesInFocusedHelper()
    {
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var autoSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var repoRoot = GetRepoRoot();

        AssertContains(frameRateOptionsText, "FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(");
        AssertContains(frameRateRebuildControllerText, "FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Clear();");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Add(option);");
        AssertContains(frameRateRebuildControllerText, "_context.SetIsAutoFrameRateSelected(selection.SelectAutoOption);");
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertContains(frameRateRebuildControllerText, "_context.SetPendingSdrAutoSelectionForDeviceChange(false);");
        AssertDoesNotContain(frameRateOptionsText, "OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))");
        AssertDoesNotContain(captureModeTransactionsText, "OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))");
        AssertContains(autoSelectionPolicyText, "internal static class FrameRateAutoSelectionPolicy");
        AssertContains(autoSelectionPolicyText, "internal readonly record struct FrameRateAutoSelectionSource(");
        AssertContains(autoSelectionPolicyText, "internal sealed record FrameRateAutoSelectionRequest(");
        AssertContains(autoSelectionPolicyText, "internal sealed record FrameRateAutoSelection(");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateAutoSelectionPolicy.cs")), "old nested frame-rate auto-selection partial removed");
        AssertContains(autoSelectionPolicyText, "internal static FrameRateAutoSelection Select(FrameRateAutoSelectionRequest request)");
        AssertContains(autoSelectionPolicyText, "request.PendingSdrAutoSelectionForDeviceChange");
        AssertContains(autoSelectionPolicyText, "request.PendingSdrAutoFriendlyFrameRateBucket.Value");
        AssertContains(autoSelectionPolicyText, ".OrderBy(option => Math.Abs(option.Value - source.Rate.Value))");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily)");
        AssertContains(autoSelectionPolicyText, "optionFamily == source.TimingFamily");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFrameRateMatch(option.Value, previousRate)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 60)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 30)");
        AssertDoesNotContain(autoSelectionPolicyText, "AvailableFrameRates.Clear();");
        AssertDoesNotContain(autoSelectionPolicyText, "ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(autoSelectionPolicyText, "SelectedFrameRate =");
        AssertContains(modeSelectionText, "SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);");
        AssertContains(modeSelectionText, "SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "SelectedExactFrameRateArg = selected?.Rational;");

        return Task.CompletedTask;
    }

    internal static Task FrameRateTimingPolicy_LivesWithViewModelSelectionPolicies()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var timingResolverText = captureModeOptionsControllerText;
        var controllerGraphText = ReadMainViewModelControllerGraphSource();
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var timingPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "private void UpdateSelectedFormat()");
        AssertContains(captureModeTransactionsText, "private void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.UpdateSelectedFormat();");
        AssertContains(captureModeOptionsControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds, HDR/SDR changes,");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertDoesNotContain(captureModeTransactionsText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool TryInferFrameRateTimingFamily(");
        AssertDoesNotContain(captureModeTransactionsText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(
            ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n"),
            "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(rootText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertContains(compositionText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertContains(controllerGraphText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(timingResolverText, "namespace Sussudio.Controllers;");
        AssertContains(timingResolverText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertContains(timingResolverText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(timingResolverText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(timingResolverText, "internal sealed class MainViewModelFrameRateTimingResolver");
        AssertContains(timingResolverText, "public FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(timingResolverText, "public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(timingResolverText, "public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertContains(timingResolverText, "FrameRateTimingPolicy.BuildTimingVariants(formats)");
        AssertContains(timingResolverText, "FrameRateTimingPolicy.TryInferFrameRateTimingFamily(");
        AssertContains(timingResolverText, "CaptureResolutionSelectionPolicy.TryParseResolutionKey(");
        AssertDoesNotContain(timingResolverText, "private readonly record struct FrameRateTimingVariant(");
        AssertDoesNotContain(timingResolverText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(timingResolverText, "private static bool TryInferFrameRateTimingFamily(");
        AssertDoesNotContain(timingResolverText, "private static bool TryParseFrameRateRational(");
        AssertDoesNotContain(timingResolverText, "private static int GetFriendlyFrameRateBucket(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FrameRateTiming.cs")),
            "old MainViewModel frame-rate timing partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelFrameRateTimingResolver.cs")),
            "frame-rate timing resolver lives with capture mode option rebuild owner");
        AssertContains(timingPolicyText, "internal enum FrameRateTimingFamily");
        AssertContains(timingPolicyText, "internal readonly record struct FrameRateTimingVariant(int FriendlyBucket, FrameRateTimingFamily Family);");
        AssertContains(timingPolicyText, "internal static IReadOnlyList<FrameRateTimingVariant> BuildTimingVariants(IEnumerable<MediaFormat> formats)");
        AssertContains(timingPolicyText, "internal static MediaFormat SelectPreferredFrameRateFormat(");
        AssertContains(timingPolicyText, "internal static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingPolicyText, "internal static bool TryParseFrameRateRational(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "FrameRateTimingPolicy.cs")),
            "pure frame-rate timing policy folded into ViewModelSelectionPolicies.cs");

        return Task.CompletedTask;
    }

    internal static Task FrameRateAutoSelectionPolicy_PreservesSelectionBehavior()
    {
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        var sourceNearestOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 60, 60000d / 1001d, "60000/1001", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var sourceNearest = InvokeFrameRateAutoSelection(
            sourceNearestOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: 59.94,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Ntsc1001",
            previousRate: 30);
        AssertEqual(60000d / 1001d, GetDoubleProperty(GetPropertyValue(sourceNearest, "Selected")!, "Value"), "Frame-rate auto source nearest selection");
        AssertEqual(true, GetBoolProperty(sourceNearest, "SelectAutoOption"), "Frame-rate source nearest keeps auto selected");

        var pendingBucketOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 60, 60000d / 1001d, "60000/1001", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var pendingBucket = InvokeFrameRateAutoSelection(
            pendingBucketOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: true,
            pendingSdrAutoFriendlyFrameRateBucket: 60,
            sourceRate: 120,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 120);
        AssertEqual(60d, GetDoubleProperty(GetPropertyValue(pendingBucket, "Selected")!, "FriendlyValue"), "Frame-rate auto pending SDR bucket selection");

        var hdrSkipsPendingBucket = InvokeFrameRateAutoSelection(
            pendingBucketOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: true,
            pendingSdrAutoSelectionForDeviceChange: true,
            pendingSdrAutoFriendlyFrameRateBucket: 60,
            sourceRate: 120,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 60);
        AssertEqual(120d, GetDoubleProperty(GetPropertyValue(hdrSkipsPendingBucket, "Selected")!, "Value"), "Frame-rate auto HDR skips pending SDR bucket");

        var manualFallbackOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 60, 60, "60/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var manualFallback = InvokeFrameRateAutoSelection(
            manualFallbackOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: false,
            hasUserOverriddenFrameRateForCurrentMode: true,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: 60,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 119.88);
        AssertEqual(120d, GetDoubleProperty(GetPropertyValue(manualFallback, "Selected")!, "Value"), "Frame-rate manual previous friendly fallback");
        AssertEqual(false, GetBoolProperty(manualFallback, "SelectAutoOption"), "Frame-rate manual fallback leaves auto deselected");

        var autoFallbackOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: false),
            CreateFrameRateOption(frameRateType, 60, 60, "60/1", isEnabled: true));
        var autoFallback = InvokeFrameRateAutoSelection(
            autoFallbackOptions,
            autoFrameRateOptionAvailable: false,
            forceAutoSelection: true,
            isAutoFrameRateSelected: false,
            hasUserOverriddenFrameRateForCurrentMode: true,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: null,
            sourceTimingFamilyKnown: false,
            sourceTimingFamilyName: "Unknown",
            previousRate: 30);
        AssertEqual(60d, GetDoubleProperty(GetPropertyValue(autoFallback, "Selected")!, "Value"), "Frame-rate forced auto fallback chooses first enabled option");
        AssertEqual(true, GetBoolProperty(autoFallback, "SelectAutoOption"), "Frame-rate forced auto fallback selects auto");

        return Task.CompletedTask;
    }

    internal static Task FrameRateTimingPolicy_PreservesPureTimingBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var policyType = RequireType("Sussudio.ViewModels.FrameRateTimingPolicy");
        var ntscFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", "Ntsc1001");
        var integerFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", "Integer");

        var integer60 = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 60, 60, 1, "NV12", isHdr: false);
        var ntsc60 = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 60000d / 1001d, 60000, 1001, "NV12", isHdr: false);
        var selectPreferred = policyType.GetMethod("SelectPreferredFrameRateFormat", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.SelectPreferredFrameRateFormat missing.");

        var ntscSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, integer60, ntsc60),
                60,
                ntscFamily
            })
            ?? throw new InvalidOperationException("NTSC preferred selection returned null.");
        AssertEqual(60000u, (uint)GetPropertyValue(ntscSelected, "FrameRateNumerator")!, "NTSC timing-family rank numerator");

        var integerSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, ntsc60, integer60),
                60,
                integerFamily
            })
            ?? throw new InvalidOperationException("Integer preferred selection returned null.");
        AssertEqual(1u, (uint)GetPropertyValue(integerSelected, "FrameRateDenominator")!, "Integer timing-family rank denominator");

        var hfrMjpg = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "MJPG", isHdr: false);
        var hfrNv12 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "NV12", isHdr: false);
        var hfrSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, hfrMjpg, hfrNv12),
                120,
                integerFamily
            })
            ?? throw new InvalidOperationException("4K HFR preferred selection returned null.");
        AssertEqual("MJPG", GetStringProperty(hfrSelected, "PixelFormat"), "4K HFR MJPG keeps top pixel-format priority");
        var hfrSourceOrderSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, hfrNv12, hfrMjpg),
                120,
                integerFamily
            })
            ?? throw new InvalidOperationException("4K HFR source-order selection returned null.");
        AssertEqual("NV12", GetStringProperty(hfrSourceOrderSelected, "PixelFormat"), "4K HFR top priority preserves source order tie");

        var buildTimingVariants = policyType.GetMethod("BuildTimingVariants", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.BuildTimingVariants missing.");
        var variants = ((IEnumerable)buildTimingVariants.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, ntsc60, integer60)
            })!)
            .Cast<object>()
            .ToArray();
        AssertEqual(2, variants.Length, "Friendly bucket timing variant count");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(variants[0], "FriendlyBucket")), "NTSC friendly bucket");
        AssertEqual("Ntsc1001", GetPropertyValue(variants[0], "Family")?.ToString(), "NTSC family variant");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(variants[1], "FriendlyBucket")), "Integer friendly bucket");
        AssertEqual("Integer", GetPropertyValue(variants[1], "Family")?.ToString(), "Integer family variant");

        var inferFamily = policyType.GetMethod("TryInferFrameRateTimingFamily", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.TryInferFrameRateTimingFamily missing.");
        var inferArgs = new object?[] { "not/rational", 60000d / 1001d, null };
        AssertEqual(true, (bool)inferFamily.Invoke(null, inferArgs)!, "Timing-family rational parse fallback return");
        AssertEqual("Ntsc1001", inferArgs[2]?.ToString(), "Timing-family rational parse fallback value");

        var friendlyMatch = policyType.GetMethod("IsFriendlyFrameRateMatch", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.IsFriendlyFrameRateMatch missing.");
        AssertEqual(true, (bool)friendlyMatch.Invoke(null, new object[] { 60d, 60000d / 1001d })!, "Friendly bucket grouping");

        return Task.CompletedTask;
    }

    private static object CreateFrameRateTimingFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator,
        string pixelFormat,
        bool isHdr)
    {
        var format = CreateTestMediaFormat(mediaFormatType, width, height, frameRate, pixelFormat, isHdr);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        return format;
    }
}
