using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
