using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;

namespace Sussudio.Tests
{

[Collection(RecoveryEnvironmentCollection.Name)]
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

public sealed class PresentationPreviewCapturePreviewLifecycleContractsTests
{
    public PresentationPreviewCapturePreviewLifecycleContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AudioPreviewRemainsInactiveWhenNoAudioCaptureDeviceExists()
        => global::Program.AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists();
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
    public Task LeasedSubmissionPreservesTracking()
        => global::Program.D3D11PreviewRenderer_LeasedSubmissionPreservesTracking();

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
    public Task OutputSizingRoundsVisiblePanelToStableBucket()
        => global::Program.PreviewOutputSizePolicy_RoundsVisiblePanelToStableBucket();

    [Fact]
    public Task OutputSizingPreservesVisiblePanelAspectRatio()
        => global::Program.PreviewOutputSizePolicy_PreservesVisiblePanelAspectRatio();

    [Fact]
    public Task OutputSizingIgnoresSubHysteresisChanges()
        => global::Program.PreviewOutputSizePolicy_IgnoresSubHysteresisChanges();

    [Fact]
    public Task OutputSizingNeverUpscalesPastSource()
        => global::Program.PreviewOutputSizePolicy_NeverUpscalesPastSource();

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

}

public sealed class PresentationPreviewD3DDiagnosticsContractsTests
{
    public PresentationPreviewD3DDiagnosticsContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SnapshotModelsExposeExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_SnapshotModelsExposeExpectedProperties();

    [Fact]
    public Task PerformanceTimelineExposesExpectedProperties()
        => global::Program.D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties();
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

}

public sealed class PresentationPreviewMainViewModelDependencyCompositionContractsTests
{
    public PresentationPreviewMainViewModelDependencyCompositionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PresentationControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelPresentationControllers_UseDependencyCompositionContexts();

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
    public Task OutputDriveFreeSpacePresentationHandlesInvalidPaths()
        => global::Program.OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty();
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
    public Task AutomationHdrEnablementLivesInCaptureSelection()
        => global::Program.MainViewModelAutomation_HdrEnablementLivesInCaptureSelection();

    [Fact]
    public Task CaptureRoutesAudioMonitoringThroughCoordinator()
        => global::Program.MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator();

    [Fact]
    public Task CaptureSettingsFrameRateProjectionPreservesPrecedence()
        => global::Program.MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence();

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
    public Task FrameRateAutoSelectionPolicyPreservesSelectionBehavior()
        => global::Program.FrameRateAutoSelectionPolicy_PreservesSelectionBehavior();

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
    public Task DeviceFormatProbeRetargetPolicyPreservesRetargetDecisionBehavior()
        => global::Program.DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior();

}

public sealed class PresentationPreviewCaptureSelectionPolicyContractsTests
{
    public PresentationPreviewCaptureSelectionPolicyContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task CaptureFormatSelectionPolicyPreservesSelectionBehavior()
        => global::Program.CaptureFormatSelectionPolicy_PreservesSelectionBehavior();

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
    public Task SplashLoadingPhrasePacingPolicyPreservesIntervalBands()
        => global::Program.SplashLoadingPhrasePacingPolicy_PreservesIntervalBands();

}

public sealed class PresentationPreviewRecordingContractsTests
{
    public PresentationPreviewRecordingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingStatePresentationPolicyPreservesLockoutRules()
        => global::Program.RecordingStatePresentationPolicy_PreservesLockoutRules();
}

public sealed class PresentationPreviewResolutionSelectionContractsTests
{
    public PresentationPreviewResolutionSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

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
    public Task WindowTitleControllerFormatsBuildStampAndRecordingSuffix()
        => global::Program.WindowTitleController_FormatsBuildStampAndRecordingSuffix();
}

public sealed class PresentationPreviewMainWindowInitialContractsTests
{
    public PresentationPreviewMainWindowInitialContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task WindowScreenshotImageEncodingLivesInFocusedHelper()
        => global::Program.WindowScreenshotImageEncoding_LivesInFocusedHelper();
}

[Collection(RecoveryEnvironmentCollection.Name)]
public sealed class PresentationPreviewRuntimeShellContractsTests
{
    public PresentationPreviewRuntimeShellContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewRuntimeSnapshotEpochDoesNotAdvanceForUnchangedSignatures()
        => global::Program.PreviewRuntimeSnapshotEpoch_DoesNotAdvanceForUnchangedSignatures();

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
    public Task PreviewRuntimeSnapshotMapperPreservesVisibilityAndHealthFields()
        => global::Program.PreviewRuntimeSnapshotMapper_PreservesVisibilityAndHealthFields();

    [Fact]
    public Task PreviewRuntimeSnapshotMapperPreservesSampledStartupFields()
        => global::Program.PreviewRuntimeSnapshotMapper_PreservesSampledStartupFields();

    [Fact]
    public Task PreviewRuntimeSnapshotMapperPreservesRendererAndEventFields()
        => global::Program.PreviewRuntimeSnapshotMapper_PreservesRendererAndEventFields();

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
    public Task PreviewRuntimeSnapshotProjectionPreservesDisplayCadenceMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesDisplayCadenceMetrics();

    [Fact]
    public Task PreviewRuntimeSnapshotProjectionPreservesRenderCpuTimingMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesRenderCpuTimingMetrics();

    [Fact]
    public Task PreviewRuntimeSnapshotProjectionPreservesPipelineLatencyMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesPipelineLatencyMetrics();

    [Fact]
    public Task PreviewRuntimeSnapshotProjectionPreservesFrameStatisticsMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesFrameStatisticsMetrics();

    [Fact]
    public Task PreviewRuntimeSnapshotProjectionPreservesFrameLatencyWaitMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesFrameLatencyWaitMetrics();

    [Fact]
    public Task PreviewRuntimeSnapshotProjectionPreservesFrameOwnershipMetrics()
        => global::Program.PreviewRuntimeSnapshotProjection_PreservesFrameOwnershipMetrics();
}

public sealed class PresentationPreviewCaptureOptionContractsTests
{
    public PresentationPreviewCaptureOptionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task CaptureOptionPresentationPolicyPreservesAffordanceRules()
        => global::Program.CaptureOptionPresentationPolicy_PreservesAffordanceRules();

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
    public Task OutputPathDisplayTextFormatterPreservesTruncationPolicy()
        => global::Program.OutputPathDisplayTextFormatter_PreservesTruncationPolicy();
}

}

static partial class Program
{

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

    internal static Task PreviewOutputSizePolicy_RoundsVisiblePanelToStableBucket()
    {
        var type = RequireType("Sussudio.Services.Preview.PreviewOutputSizePolicy");
        var resolve = type.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewOutputSizePolicy.Resolve not found.");

        var target = resolve.Invoke(null, new object[] { 1345, 756, 3840, 2160 })!;
        AssertEqual(1344, GetIntProperty(target, "Width"), "visible panel width rounds to the nearest 64-pixel bucket");

        return Task.CompletedTask;
    }

    internal static Task PreviewOutputSizePolicy_PreservesVisiblePanelAspectRatio()
    {
        var type = RequireType("Sussudio.Services.Preview.PreviewOutputSizePolicy");
        var resolve = type.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewOutputSizePolicy.Resolve not found.");

        var target = resolve.Invoke(null, new object[] { 1279, 719, 3840, 2160 })!;
        AssertEqual(1280, GetIntProperty(target, "Width"), "HD output width");
        AssertEqual(720, GetIntProperty(target, "Height"), "HD output height");

        return Task.CompletedTask;
    }

    internal static Task PreviewOutputSizePolicy_IgnoresSubHysteresisChanges()
    {
        var policyType = RequireType("Sussudio.Services.Preview.PreviewOutputSizePolicy");
        var outputSizeType = RequireType("Sussudio.Services.Preview.PreviewOutputSize");
        var shouldResize = policyType.GetMethod("ShouldResize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewOutputSizePolicy.ShouldResize not found.");

        var current = Activator.CreateInstance(outputSizeType, 1280, 720)!;
        var subHysteresisTarget = Activator.CreateInstance(outputSizeType, 1344, 756)!;
        AssertEqual(false, (bool)shouldResize.Invoke(null, new[] { current, subHysteresisTarget })!, "a 64-pixel bucket change remains below resize hysteresis");

        return Task.CompletedTask;
    }

    internal static Task PreviewOutputSizePolicy_NeverUpscalesPastSource()
    {
        var type = RequireType("Sussudio.Services.Preview.PreviewOutputSizePolicy");
        var resolve = type.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewOutputSizePolicy.Resolve not found.");

        var sourceCapped = resolve.Invoke(null, new object[] { 3840, 2160, 1920, 1080 })!;
        AssertEqual(1920, GetIntProperty(sourceCapped, "Width"), "output does not upscale beyond source width");
        AssertEqual(1080, GetIntProperty(sourceCapped, "Height"), "output does not upscale beyond source height");

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

    internal static Task D3D11PreviewRenderer_LeasedSubmissionPreservesTracking()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = RequireNestedType(rendererType, "PendingFrame");
        var queueType = typeof(System.Collections.Concurrent.ConcurrentQueue<>).MakeGenericType(pendingFrameType);
        var queue = Activator.CreateInstance(queueType)!;
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        using var frameReady = new System.Threading.ManualResetEventSlim(false);
        SetPrivateField(renderer, "_lifecycleLock", new object());
        SetPrivateField(renderer, "_pendingFrames", queue);
        SetPrivateField(renderer, "_frameReadyEvent", frameReady);
        SetPrivateField(renderer, "_renderThread", System.Threading.Thread.CurrentThread);
        SetPrivateField(renderer, "_maxPendingFrames", 4);

        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var pool = new TrackingArrayPool();
        using var owner = (IDisposable)CreatePooledVideoFrame(
            frameType, Enum.Parse(formatType, "Nv12"), 77L, 100L, 110L, 16, 16, 384, pool);
        using var lease = (IDisposable)frameType.GetMethod("AddLease")!.Invoke(owner, null)!;
        var trackingType = RequireType("Sussudio.Services.Contracts.PreviewFrameTracking");
        var tracking = Activator.CreateInstance(trackingType, 999L, 888L, 55L, 200L, 123456L, false)!;
        var drop = rendererType.GetMethod("DropPendingFrames")!;
        try
        {
            rendererType.GetMethod("SubmitRawFrameLease")!.Invoke(renderer, new[] { (object)lease, false, tracking });
            var pending = ((IEnumerable)queue).Cast<object>().Single();
            AssertEqual(123456L, GetLongProperty(pending, "SourcePtsTicks"), "leased source PTS");
            AssertEqual(100L, GetLongProperty(pending, "ArrivalTick"), "lease arrival is authoritative");
            AssertEqual(77L, GetLongProperty(pending, "SourceSequenceNumber"), "lease sequence is authoritative");
            AssertEqual(55L, GetLongProperty(pending, "PreviewPresentId"), "tracking present identity");
            AssertEqual(200L, GetLongProperty(pending, "SchedulerSubmitTick"), "tracking scheduler tick");
            AssertEqual(false, GetBoolProperty(pending, "CountForPresentCadence"), "tracking cadence policy");
            Assert.Same(lease, GetPropertyValue(pending, "FrameLease"));
            AssertEqual(123456L, GetLongPrivateField(renderer, "_lastSubmittedSourcePtsTicks"), "submitted PTS diagnostic");
            owner.Dispose();
            AssertEqual(0, pool.ReturnCount, "queue retains leased buffer");
            AssertEqual(1, (int)drop.Invoke(renderer, new object[] { "test-drain" })!, "queued lease drained");
            AssertEqual(123456L, GetLongPrivateField(renderer, "_lastDroppedSourcePtsTicks"), "dropped PTS diagnostic");
            AssertEqual(1, pool.ReturnCount, "drain returns buffer once");
        }
        finally
        {
            // The thread is a queue-fixture sentinel; never stop this renderer.
            drop.Invoke(renderer, new object[] { "test-cleanup" });
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

    internal static Task D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_presentCadenceLock", new object());
        SetPrivateField(renderer, "_presentIntervalWindowMs", new double[8]);
        SetPrivateField(renderer, "_presentFrameTimeHistory", Activator.CreateInstance(
            RequireType("Sussudio.Services.Preview.PreviewFrameTimeHistory"), new object?[] { 4096, null }));

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
        var watchdogType = RequireType("Sussudio.Controllers.PreviewStartupSessionController");
        var formatTimeoutReason = watchdogType.GetMethod("FormatTimeoutReason", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSessionController.FormatTimeoutReason was not found.");
        var formatTimeoutStatusText = watchdogType.GetMethod("FormatTimeoutStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSessionController.FormatTimeoutStatusText was not found.");
        var formatFailureStopStatusText = watchdogType.GetMethod("FormatFailureStopStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSessionController.FormatFailureStopStatusText was not found.");

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
            "Preview failed: no visual confirmation",
            formatTimeoutStatusText.Invoke(null, new object?[] { null })?.ToString(),
            "timeout status without missing signals");
        AssertEqual(
            "Preview failed: no visual confirmation",
            formatTimeoutStatusText.Invoke(null, new object?[] { "   " })?.ToString(),
            "timeout status with whitespace missing signals");
        AssertEqual(
            "Preview failed: missing readiness signal (FirstCaptureFrame+FirstVisual)",
            formatTimeoutStatusText.Invoke(null, new object?[] { "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout status with missing signals");
        AssertEqual(
            "Preview failed: missing readiness signal (FirstCaptureFrame+FirstVisual)",
            formatFailureStopStatusText.Invoke(null, new object?[] { "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual" })?.ToString(),
            "failure stop status for a timeout with missing signals repeats the readable timeout text");
        AssertEqual(
            "Preview failed: no visual confirmation",
            formatFailureStopStatusText.Invoke(null, new object?[] { "no-visual-confirmation-within-10000ms" })?.ToString(),
            "failure stop status for a timeout without missing signals repeats the readable timeout text");
        AssertEqual(
            "Preview failed: renderer lost",
            formatFailureStopStatusText.Invoke(null, new object?[] { "renderer lost" })?.ToString(),
            "failure stop status shows a non-timeout reason as given");

        return Task.CompletedTask;
    }

    internal static async Task PreviewStartupWatchdogController_PreservesTimeoutContracts()
    {
        const string environmentName = "SUSSUDIO_PREVIEW_START_TIMEOUT_MS";
        var previousTimeout = Environment.GetEnvironmentVariable(environmentName);
        try
        {
            foreach (var (setting, expected) in new (string? Setting, int Expected)[]
            {
                (null, 10000), ("invalid", 10000), ("1", 1000), ("20000", 15000), ("4500", 4500)
            })
            {
                Environment.SetEnvironmentVariable(environmentName, setting);
                var candidate = CreatePreviewStartupSessionForTest(out _);
                AssertEqual(expected, GetIntProperty(candidate, "VisualTimeoutMs"), $"timeout setting {setting ?? "default"}");
            }

            Environment.SetEnvironmentVariable(environmentName, "1000");
            var lazyController = CreatePreviewStartupSessionForTest(out _);
            Environment.SetEnvironmentVariable(environmentName, "6500");
            AssertEqual(6500, GetIntProperty(lazyController, "VisualTimeoutMs"), "timeout reads environment on first access");
            Environment.SetEnvironmentVariable(environmentName, "10000");
            AssertEqual(6500, GetIntProperty(lazyController, "VisualTimeoutMs"), "timeout caches first access for the owner lifetime");

            var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
            var formatPayload = formatterType.GetMethod("FormatTimeoutDiagnosticPayload", BindingFlags.Public | BindingFlags.Static)!;
            AssertEqual(
                "placeholder=False gpuVisible=True cpuVisible=False strategy=D3D11VideoProcessor required=FirstCaptureFrame+FirstVisual received=None missing=FirstCaptureFrame+FirstVisual",
                formatPayload.Invoke(null, new[] { CreatePreviewStartupTimeoutDiagnosticSnapshot() }),
                "timeout diagnostic payload formatting");

            var controller = CreatePreviewStartupSessionForTest(out var recorder);
            BeginWaitingPreviewStartup(controller);
            recorder.Now = recorder.Now.AddMilliseconds(1234);
            recorder.Events.Clear();
            await ((Task)InvokeNonPublicInstanceMethod(controller, "HandleTimeoutAsync", null)!).ConfigureAwait(false);

            const string reason = "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual";
            AssertEqual("Failed", GetPropertyValue(controller, "State")?.ToString(), "timeout marks the same startup attempt failed");
            AssertEqual("FirstCaptureFrame+FirstVisual", GetStringProperty(controller, "MissingSignals"), "timeout caches owned missing signals");
            AssertEqual(reason, GetStringProperty(controller, "LastFailureReason"), "timeout failure reason");
            AssertEqual(reason, recorder.StopPreviewReasons.Single(), "timeout forces teardown");
            AssertEqual("Preview failed: missing readiness signal (FirstCaptureFrame+FirstVisual)", recorder.StatusTexts[0], "timeout status");
            AssertEqual(recorder.StatusTexts[0], recorder.StatusTexts[1], "failure stop republishes the readable timeout status");
            var timeoutEvents = string.Join("|", recorder.Events);
            AssertOccursBefore(timeoutEvents, "log:PREVIEW_START_STATE state=Failed", "reason=timeout renderer=null");
            AssertOccursBefore(timeoutEvents, "reason=timeout renderer=null", "stop-overlay");
            AssertOccursBefore(timeoutEvents, "stop-overlay", "status:Preview failed");
            AssertOccursBefore(timeoutEvents, "status:Preview failed", $"stop-preview:{reason}");

            foreach (var guard in new[] { "closing", "user-stop", "not-previewing", "not-waiting" })
            {
                var ignored = CreatePreviewStartupSessionForTest(out var ignoredRecorder);
                BeginWaitingPreviewStartup(ignored);
                ignoredRecorder.IsWindowClosing = guard == "closing";
                ignoredRecorder.IsStopRequested = guard == "user-stop";
                ignoredRecorder.IsPreviewing = guard != "not-previewing";
                if (guard == "not-waiting")
                {
                    InvokePreviewStartup(ignored, "SetStartupState", ParseEnum("Sussudio.Models.PreviewStartupState", "RendererAttaching"), null);
                }

                var previousState = GetPropertyValue(ignored, "State");
                ignoredRecorder.Events.Clear();
                await ((Task)InvokeNonPublicInstanceMethod(ignored, "HandleTimeoutAsync", null)!).ConfigureAwait(false);
                AssertEqual(previousState, GetPropertyValue(ignored, "State"), $"{guard} timeout preserves state");
                AssertEqual(null, GetPropertyValue(ignored, "LastFailureReason"), $"{guard} timeout preserves failure reason");
                AssertEqual(0, ignoredRecorder.StatusTexts.Count, $"{guard} timeout does not publish status");
                AssertEqual(0, ignoredRecorder.StopPreviewReasons.Count, $"{guard} timeout does not stop preview");
                AssertDoesNotContain(string.Join("|", ignoredRecorder.Events), "stop-overlay");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentName, previousTimeout);
        }
    }

    internal static async Task PreviewStartupWatchdogController_GatesFailureStopScheduling()
    {
        var pending = new List<(Func<Task> Operation, string Name)>();
        var controller = CreatePreviewStartupSessionForTest(out var recorder, (operation, name) =>
        {
            pending.Add((operation, name));
            return Task.CompletedTask;
        });

        recorder.IsWindowClosing = true;
        InvokePreviewStartup(controller, "ScheduleFailureStop", "closing");
        AssertEqual(0, pending.Count, "closing window does not schedule teardown");
        recorder.IsWindowClosing = false;
        InvokePreviewStartup(controller, "ScheduleFailureStop", "first");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "duplicate");
        AssertEqual(1, pending.Count, "only one failure stop is pending");
        AssertEqual("PreviewStartupFailureStop", pending[0].Name, "failure stop operation name");
        await pending[0].Operation().ConfigureAwait(false);
        AssertEqual("first", recorder.StopPreviewReasons.Single(), "scheduled stop uses the first reason");
        AssertEqual("Preview failed: first", recorder.StatusTexts.Single(), "completed stop publishes failure status");

        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-completion");
        AssertEqual(2, pending.Count, "completed teardown releases the scheduling gate");
        recorder.IsPreviewing = false;
        await pending[1].Operation().ConfigureAwait(false);
        AssertEqual(1, recorder.StopPreviewReasons.Count, "preview already stopped before dispatch skips teardown");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-skipped-stop");
        AssertEqual(3, pending.Count, "skipped teardown releases the scheduling gate");

        recorder.IsPreviewing = true;
        var stopFailure = new InvalidOperationException("test teardown failure");
        recorder.StopFailure = stopFailure;
        var thrownStopFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => pending[2].Operation()).ConfigureAwait(false);
        Assert.Same(stopFailure, thrownStopFailure);

        AssertEqual(1, recorder.StatusTexts.Count, "throwing teardown does not publish completion status");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-throw");
        AssertEqual(4, pending.Count, "finally releases the gate after throwing teardown");
        InvokePreviewStartup(controller, "ResetFailureStopSchedule");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-explicit-reset");
        AssertEqual(5, pending.Count, "explicit reset releases the scheduling gate");
    }

    private static object CreatePreviewStartupSessionForTest(
        out PreviewStartupSessionTestRecorder recorder,
        Func<Func<Task>, string, Task>? runUiEventHandlerAsync = null)
    {
        var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.PreviewStartupSessionControllerContext"), nonPublic: true)!;
        recorder = new PreviewStartupSessionTestRecorder();
        var state = recorder;
        SetPropertyOrBackingField(context, "DispatcherQueue", null);
        SetPropertyOrBackingField(context, "IsPreviewing", new Func<bool>(() => state.IsPreviewing));
        SetPropertyOrBackingField(context, "IsPreviewStopRequestedByUser", new Func<bool>(() => state.IsStopRequested));
        SetPropertyOrBackingField(context, "IsWindowClosing", new Func<bool>(() => state.IsWindowClosing));
        SetPropertyOrBackingField(context, "GetSelectedDeviceName", new Func<string?>(() => "Cam Link 4K"));
        SetPropertyOrBackingField(context, "StopOverlay", new Action(() => state.Events.Add("stop-overlay")));
        SetPropertyOrBackingField(context, "StopFadeInTimer", new Action(() => state.Events.Add("stop-fade-timer")));
        SetPropertyOrBackingField(context, "ScheduleFadeIn", new Action(() => state.Events.Add("schedule-fade")));
        SetPropertyOrBackingField(context, "CompleteFirstVisualTransition", new Action<string, string>((attempt, caller) =>
        {
            state.InspectFirstVisualTransition?.Invoke();
            state.Events.Add($"complete-reinit:{attempt}:{caller}");
        }));
        SetPropertyOrBackingField(context, "ClearReinitTransitionForStartupReset", new Action<bool, string>((preserve, caller) => state.Events.Add($"clear-reinit:{preserve}:{caller}")));
        SetPropertyOrBackingField(context, "Log", new Action<string>(message => state.Events.Add($"log:{message}")));
        SetPropertyOrBackingField(context, "CreateAttemptId", new Func<string>(() => state.AttemptId));
        SetPropertyOrBackingField(context, "GetUtcNow", new Func<DateTimeOffset>(() => state.Now));
        SetPropertyOrBackingField(context, "GetTimeoutDiagnosticSnapshot", new Func<(string PlaceholderVisibility, string GpuVisibility, string CpuVisibility)>(() => ("False", "True", "False")));
        var playbackType = RequireType("Sussudio.Controllers.PreviewStartupPlaybackSnapshotState");
        var playback = Activator.CreateInstance(playbackType, false, false, "Collapsed")!;
        SetPropertyOrBackingField(context, "GetPlaybackSnapshotState", Expression.Lambda(
            typeof(Func<>).MakeGenericType(playbackType), Expression.Constant(playback, playbackType)).Compile());
        SetPropertyOrBackingField(context, "GetStatusText", new Func<string>(() => state.CurrentStatus));
        SetPropertyOrBackingField(context, "SetStatusText", new Action<string>(value =>
        {
            state.StatusTexts.Add(value);
            state.CurrentStatus = value;
            state.Events.Add($"status:{value}");
        }));
        SetPropertyOrBackingField(context, "StopPreviewForFailureAsync", new Func<string, Task>(reason =>
        {
            state.StopPreviewReasons.Add(reason);
            state.Events.Add($"stop-preview:{reason}");
            return state.StopFailure == null ? Task.CompletedTask : Task.FromException(state.StopFailure);
        }));
        SetPropertyOrBackingField(context, "RunUiEventHandlerAsync",
            runUiEventHandlerAsync ?? new Func<Func<Task>, string, Task>((operation, _) => operation()));
        return Activator.CreateInstance(RequireType("Sussudio.Controllers.PreviewStartupSessionController"),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
    }

    private static object? InvokePreviewStartup(object controller, string methodName, params object?[] arguments)
        => (controller.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"PreviewStartupSessionController.{methodName} was not found."))
            .Invoke(controller, arguments);

    private static void BeginWaitingPreviewStartup(object controller)
    {
        InvokePreviewStartup(controller, "BeginStartupAttempt");
        InvokePreviewStartup(controller, "ConfigureSignals",
            ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor"),
            ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstCaptureFrame, FirstVisual"));
        InvokePreviewStartup(controller, "SetStartupState", ParseEnum("Sussudio.Models.PreviewStartupState", "WaitingForFirstVisual"), null);
    }

    private sealed class PreviewStartupSessionTestRecorder
    {
        public bool IsPreviewing { get; set; } = true;
        public bool IsStopRequested { get; set; }
        public bool IsWindowClosing { get; set; }
        public string AttemptId { get; set; } = "attempt-1";
        public DateTimeOffset Now { get; set; } = new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        public Exception? StopFailure { get; set; }
        public Action? InspectFirstVisualTransition { get; set; }
        public List<string> Events { get; } = [];
        public List<string> StatusTexts { get; } = [];
        public string CurrentStatus { get; set; } = string.Empty;
        public List<string> StopPreviewReasons { get; } = [];
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

    internal static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
    {
        var scheduled = new List<Func<Task>>();
        var controller = CreatePreviewStartupSessionForTest(out var recorder, (operation, _) =>
        {
            scheduled.Add(operation);
            return Task.CompletedTask;
        });
        object State(string name) => ParseEnum("Sussudio.Models.PreviewStartupState", name);
        object Signals(string name) => ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", name);
        object SignalSnapshot() => GetPropertyValue(controller, "SignalSnapshot")!;
        bool SignalWindowActive(bool previewing) => (bool)InvokePreviewStartup(controller, "IsSignalWindowActive", previewing)!;

        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");
        AssertEqual(false, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "idle does not refresh missing signals");
        AssertEqual(false, SignalWindowActive(true), "idle signal window inactive");
        InvokePreviewStartup(controller, "StartWatchdog");
        InvokePreviewStartup(controller, "StopWatchdog");

        InvokePreviewStartup(controller, "BeginStartupAttempt");
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin");
        AssertEqual("attempt-1", GetStringProperty(controller, "AttemptId"), "attempt identity");
        AssertEqual(recorder.Now, GetPropertyValue(controller, "RequestedUtc"), "attempt timestamp");
        AssertEqual(true, SignalWindowActive(true), "starting session signal window active");
        AssertEqual(false, SignalWindowActive(false), "stopped preview signal window inactive");
        AssertEqual(false, GetBoolProperty(controller, "ShouldBeginAttempt"), "active attempt gate");
        AssertEqual(1250.0, InvokePreviewStartup(controller, "GetElapsedMilliseconds", recorder.Now.AddMilliseconds(1250)), "elapsed milliseconds");
        AssertEqual(
            "log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K",
            string.Join("|", recorder.Events), "begin publishes state before request");
        recorder.Events.Clear();
        InvokePreviewStartup(controller, "SetStartupState", State("StartingSession"), null);
        AssertEqual(0, recorder.Events.Count, "duplicate state suppresses log");

        BeginWaitingPreviewStartup(controller);
        AssertEqual("FirstCaptureFrame+FirstVisual", GetStringProperty(controller, "MissingSignals"), "configuration caches required signals");
        AssertEqual(Signals("FirstCaptureFrame, FirstVisual"), GetPropertyValue(SignalSnapshot(), "RequiredSignals"), "configured readiness requirements");
        AssertEqual(Signals("None"), GetPropertyValue(SignalSnapshot(), "ReceivedSignals"), "configuration starts without signals");
        InvokePreviewStartup(controller, "MarkRendererAttached", recorder.Now.AddMilliseconds(100));
        AssertEqual(recorder.Now.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached timestamp");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "waiting refreshes missing signals");
        AssertEqual(true, SignalWindowActive(true), "waiting signal window active");
        recorder.InspectFirstVisualTransition = () =>
        {
            AssertEqual(State("Rendering"), GetPropertyValue(controller, "State"), "rendering state precedes visual transition callback");
            AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "confirmation precedes visual transition callback");
            AssertEqual(Signals("FirstVisual"), GetPropertyValue(SignalSnapshot(), "ReceivedSignals"), "readiness signal precedes visual transition callback");
            AssertEqual(recorder.Now, GetPropertyValue(controller, "FirstVisualUtc"), "visual timestamp precedes transition callback");
        };
        recorder.Events.Clear();
        recorder.CurrentStatus = "Preview starting...";
        recorder.Now = recorder.Now.AddMilliseconds(250);
        InvokePreviewStartup(controller, "ConfirmFirstVisual", "D3D11FirstFrame");
        AssertEqual("Preview started", recorder.CurrentStatus, "first visual completes the pending preview status");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "confirmation clears cached missing signals");
        AssertEqual(false, SignalWindowActive(true), "confirmed visual closes signal window");
        AssertEqual(false, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "rendering does not refresh missing signals");
        AssertEqual(
            "log:PREVIEW_START_STATE state=Rendering attempt=attempt-1 recovery=0 reason=-|stop-overlay|status:Preview started|schedule-fade|complete-reinit:attempt-1:ConfirmPreviewFirstVisual|log:PREVIEW_FIRST_VISUAL_CONFIRMED attempt=attempt-1 source=D3D11FirstFrame elapsedMs=250 recovery=0",
            string.Join("|", recorder.Events), "first visual presentation order");
        var firstVisualUtc = GetPropertyValue(controller, "FirstVisualUtc");
        recorder.Events.Clear();
        recorder.Now = recorder.Now.AddMilliseconds(100);
        InvokePreviewStartup(controller, "ConfirmFirstVisual", "duplicate");
        AssertEqual(false, InvokePreviewStartup(controller, "MarkFirstVisualConfirmed", recorder.Now), "low-level duplicate confirmation suppressed");
        AssertEqual(firstVisualUtc, GetPropertyValue(controller, "FirstVisualUtc"), "duplicate confirmation preserves timestamp");
        AssertEqual(0, recorder.Events.Count, "duplicate confirmation does not repeat presentation");
        recorder.InspectFirstVisualTransition = null;

        var attaching = CreatePreviewStartupSessionForTest(out var attachingRecorder);
        InvokePreviewStartup(attaching, "BeginStartupAttempt");
        InvokePreviewStartup(attaching, "SetStartupState", State("RendererAttaching"), null);
        InvokePreviewStartup(attaching, "ConfigureSignals",
            ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor"), Signals("FirstVisual"));
        InvokePreviewStartup(attaching, "MarkGpuStartupSignal", Signals("MediaOpened"), "MediaOpened");
        InvokePreviewStartup(attaching, "MarkGpuStartupSignalPlaybackAdvancing", TimeSpan.FromMilliseconds(100));
        AssertEqual(Signals("None"), GetPropertyValue(GetPropertyValue(attaching, "SignalSnapshot")!, "ReceivedSignals"), "configuration keeps dual GPU signal handling disabled");
        AssertEqual(false, GetBoolProperty(attaching, "FirstVisualConfirmed"), "GPU startup signals do not confirm visual readiness");
        attachingRecorder.Events.Clear();
        InvokePreviewStartup(attaching, "ConfirmFirstVisual", "during-renderer-attach");
        AssertEqual(State("Rendering"), GetPropertyValue(attaching, "State"), "first visual can complete during renderer attach before waiting");
        AssertEqual(string.Empty, GetStringProperty(attaching, "MissingSignals"), "renderer attach confirmation clears configured missing signals");
        AssertEqual(1, attachingRecorder.Events.Count(value => value.StartsWith("complete-reinit:", StringComparison.Ordinal)), "renderer attach confirmation presents once");
        InvokePreviewStartup(attaching, "StartWatchdog");

        SetPropertyOrBackingField(controller, "RecoveryAttemptCount", 3);
        SetPrivateField(controller, "_positionEventCount", 9L);
        InvokePreviewStartup(controller, "ScheduleFailureStop", "before-reset");
        recorder.Events.Clear();
        InvokePreviewStartup(controller, "ResetStartupTracking", true, true);
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "terminal reset returns idle");
        AssertEqual(3, GetIntProperty(controller, "RecoveryAttemptCount"), "reset can preserve recovery count");
        AssertEqual(null, GetPropertyValue(controller, "AttemptId"), "reset clears attempt identity");
        AssertEqual(null, GetPropertyValue(controller, "RequestedUtc"), "reset clears request timestamp");
        AssertEqual(null, GetPropertyValue(controller, "RendererAttachedUtc"), "reset clears renderer timestamp");
        AssertEqual(null, GetPropertyValue(controller, "FirstVisualUtc"), "reset clears visual timestamp");
        AssertEqual(null, GetPropertyValue(controller, "MissingSignals"), "reset clears cached missing signals");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "reset clears first visual flag");
        AssertEqual(0L, GetPropertyValue(controller, "PositionEventCount"), "reset clears position count");
        AssertEqual(Signals("None"), GetPropertyValue(SignalSnapshot(), "RequiredSignals"), "reset clears required signals");
        AssertEqual(Signals("None"), GetPropertyValue(SignalSnapshot(), "ReceivedSignals"), "reset clears received signals");
        AssertEqual("None", GetPropertyValue(SignalSnapshot(), "Strategy")?.ToString(), "reset clears strategy");
        AssertEqual(
            "stop-overlay|stop-fade-timer|clear-reinit:True:ResetPreviewStartupTracking",
            string.Join("|", recorder.Events), "terminal reset preserves reinit animation without duplicate idle log");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-reset");
        AssertEqual(2, scheduled.Count, "tracking reset releases pending failure stop gate");

        InvokePreviewStartup(controller, "SetStartupState", State("Failed"), "renderer-failed");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "failed state allows new attempt");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "failed state refreshes missing signals");
        recorder.AttemptId = "attempt-2";
        InvokePreviewStartup(controller, "BeginStartupAttempt");
        AssertEqual("attempt-2", GetStringProperty(controller, "AttemptId"), "new attempt takes fresh identity");
        AssertEqual(0, GetIntProperty(controller, "RecoveryAttemptCount"), "new attempt clears preserved recovery count");
        AssertEqual(null, GetPropertyValue(controller, "LastFailureReason"), "new attempt clears failure reason");
        InvokePreviewStartup(controller, "ScheduleFailureStop", "after-begin");
        AssertEqual(3, scheduled.Count, "new attempt releases failure stop gate");

        BeginWaitingPreviewStartup(controller);
        recorder.IsStopRequested = true;
        recorder.Events.Clear();
        InvokePreviewStartup(controller, "ConfirmFirstVisual", "D3D11FirstFrame");
        AssertEqual(State("WaitingForFirstVisual"), GetPropertyValue(controller, "State"), "user stop preserves waiting state");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "user stop suppresses confirmation");
        AssertEqual(Signals("None"), GetPropertyValue(SignalSnapshot(), "ReceivedSignals"), "user stop does not record visual readiness");
        AssertEqual("log:PREVIEW_FIRST_VISUAL_IGNORED attempt=attempt-2 source=D3D11FirstFrame reason=stop-requested", string.Join("|", recorder.Events), "user stop logs only ignored confirmation");
        recorder.IsStopRequested = false;
        recorder.IsPreviewing = false;
        recorder.Events.Clear();
        InvokePreviewStartup(controller, "ConfirmFirstVisual", "after-stop");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "stopped preview suppresses confirmation");
        AssertEqual(0, recorder.Events.Count, "stopped preview does not present first visual");

        SetPropertyOrBackingField(controller, "RecoveryAttemptCount", 4);
        InvokePreviewStartup(controller, "ResetStartupTracking", false, false);
        AssertEqual(0, GetIntProperty(controller, "RecoveryAttemptCount"), "normal reset clears recovery count");
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "nonterminal reset returns idle");
        AssertEqual(
            "stop-overlay|stop-fade-timer|clear-reinit:False:ResetPreviewStartupTracking|log:PREVIEW_START_STATE state=Idle attempt=none recovery=0 reason=-",
            string.Join("|", recorder.Events), "nonterminal reset presentation order");
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

    internal static Task PreviewRuntimeSnapshotEpoch_DoesNotAdvanceForUnchangedSignatures()
    {
        var contextType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSamplingControllerContext");
        var samplerType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSamplingController");
        var signatureType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSignature");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "MediaOpened");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");
        var startupState = ParseEnum("Sussudio.Models.PreviewStartupState", "WaitingForFirstVisual");

        var context = Activator.CreateInstance(contextType)
                      ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotSamplingControllerContext.");
        var sampler = Activator.CreateInstance(samplerType, context)
                      ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotSamplingController.");
        var epochMethod = samplerType.GetMethod("GetOrAdvancePreviewRuntimeSnapshotEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException("GetOrAdvancePreviewRuntimeSnapshotEpoch method not found.");
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
                startupState,
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

    internal static Task PreviewRuntimeSnapshotProjection_PreservesFrameStatisticsMetrics()
    {
        var metrics = CreatePreviewRendererMetric("DxgiFrameStatisticsMetrics",
            11L, 12L, 13L, "DXGI_ERROR_FRAME_STATISTICS_DISJOINT", 14L, 15L, 16L, 17L, 18L, 19L, 20L, 21L);
        AssertPreviewMetricSnapshots("ApplyFrameStatistics", metrics,
            ("D3DFrameStatsSampleCount", 0L, 11L),
            ("D3DFrameStatsSuccessCount", 0L, 12L),
            ("D3DFrameStatsFailureCount", 0L, 13L),
            ("D3DFrameStatsLastError", string.Empty, "DXGI_ERROR_FRAME_STATISTICS_DISJOINT"),
            ("D3DFrameStatsPresentCount", -1L, 14L),
            ("D3DFrameStatsPresentRefreshCount", -1L, 15L),
            ("D3DFrameStatsSyncRefreshCount", -1L, 16L),
            ("D3DFrameStatsSyncQpcTime", 0L, 17L),
            ("D3DFrameStatsLastPresentDelta", 0L, 18L),
            ("D3DFrameStatsLastPresentRefreshDelta", 0L, 19L),
            ("D3DFrameStatsLastSyncRefreshDelta", 0L, 20L),
            ("D3DFrameStatsMissedRefreshCount", 0L, 21L));

        AssertPreviewMetricSnapshots("ApplyFrameStatistics", CreatePreviewRendererMetric("DxgiFrameStatisticsMetrics"),
            ("D3DFrameStatsPresentCount", -1L, 0L),
            ("D3DFrameStatsPresentRefreshCount", -1L, 0L),
            ("D3DFrameStatsSyncRefreshCount", -1L, 0L),
            ("D3DFrameStatsLastError", string.Empty, string.Empty));
        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotProjection_PreservesFrameLatencyWaitMetrics()
    {
        var timing = CreatePreviewRendererMetric("CpuStageTimingMetrics", 66, 6.7d, 6.8d, 6.9d, 7d);
        var metrics = CreatePreviewRendererMetric("FrameLatencyWaitMetrics",
            true, false, 61L, 62L, 63L, 64L, 0xFFFFFFF0u, 6.5d, timing);
        AssertPreviewMetricSnapshots("ApplyFrameLatencyWait", metrics,
            ("D3DFrameLatencyWaitEnabled", false, true),
            ("D3DFrameLatencyWaitHandleActive", false, false),
            ("D3DFrameLatencyWaitCallCount", 0L, 61L),
            ("D3DFrameLatencyWaitSignaledCount", 0L, 62L),
            ("D3DFrameLatencyWaitTimeoutCount", 0L, 63L),
            ("D3DFrameLatencyWaitUnexpectedResultCount", 0L, 64L),
            ("D3DFrameLatencyWaitLastResult", 0u, 0xFFFFFFF0u),
            ("D3DFrameLatencyWaitLastMs", 0d, 6.5d),
            ("D3DFrameLatencyWaitSampleCount", 0, 66),
            ("D3DFrameLatencyWaitAvgMs", 0d, 6.7d),
            ("D3DFrameLatencyWaitP95Ms", 0d, 6.8d),
            ("D3DFrameLatencyWaitP99Ms", 0d, 6.9d),
            ("D3DFrameLatencyWaitMaxMs", 0d, 7d));

        var activeHandleMetrics = CreatePreviewRendererMetric("FrameLatencyWaitMetrics",
            false, true, 0L, 0L, 0L, 0L, 0u, 0d, timing);
        AssertPreviewMetricSnapshots("ApplyFrameLatencyWait", activeHandleMetrics,
            ("D3DFrameLatencyWaitEnabled", false, false),
            ("D3DFrameLatencyWaitHandleActive", false, true));
        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotProjection_PreservesFrameOwnershipMetrics()
    {
        var metrics = CreatePreviewRendererMetric("FrameOwnershipMetrics",
            1001L, 1002L, 1003L, 1004L, 1005L,
            2001L, 2002L, 2003L, 2004L, 2005L, 2.6d, 2.7d,
            3001L, 3002L, 3003L, 3004L, 3005L, "replaced by newer frame");
        AssertPreviewMetricSnapshots("ApplyFrameOwnership", metrics,
            ("D3DLastSubmittedPreviewPresentId", 0L, 1001L),
            ("D3DLastSubmittedSourceSequenceNumber", -1L, 1002L),
            ("D3DLastSubmittedSourcePtsTicks", 0L, 1003L),
            ("D3DLastSubmittedQpc", 0L, 1004L),
            ("D3DLastSubmittedUtcUnixMs", 0L, 1005L),
            ("D3DLastRenderedPreviewPresentId", 0L, 2001L),
            ("D3DLastRenderedSourceSequenceNumber", -1L, 2002L),
            ("D3DLastRenderedSourcePtsTicks", 0L, 2003L),
            ("D3DLastRenderedQpc", 0L, 2004L),
            ("D3DLastRenderedUtcUnixMs", 0L, 2005L),
            ("D3DLastRenderedSchedulerToPresentMs", 0d, 2.6d),
            ("D3DLastRenderedPipelineLatencyMs", 0d, 2.7d),
            ("D3DLastDroppedPreviewPresentId", 0L, 3001L),
            ("D3DLastDroppedSourceSequenceNumber", -1L, 3002L),
            ("D3DLastDroppedSourcePtsTicks", 0L, 3003L),
            ("D3DLastDroppedQpc", 0L, 3004L),
            ("D3DLastDroppedUtcUnixMs", 0L, 3005L),
            ("D3DLastDropReason", string.Empty, "replaced by newer frame"));

        AssertPreviewMetricSnapshots("ApplyFrameOwnership", CreatePreviewRendererMetric("FrameOwnershipMetrics"),
            ("D3DLastSubmittedSourceSequenceNumber", -1L, 0L),
            ("D3DLastRenderedSourceSequenceNumber", -1L, 0L),
            ("D3DLastDroppedSourceSequenceNumber", -1L, 0L),
            ("D3DLastDropReason", string.Empty, string.Empty));
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

    internal static Task PreviewRuntimeSnapshotProjection_PreservesDisplayCadenceMetrics()
    {
        var intervals = new[] { 8.1d, 8.2d };
        var metrics = CreatePreviewRendererMetric("PresentCadenceMetrics",
            19, 118.5d, 8.33d, 8.44d, 8.55d, 8.66d, 8.77d, 110.1d, 111.2d, 159.3d, intervals, 0.45d, 7L, 6.7d);
        AssertPreviewMetricSnapshots("ApplyDisplayCadence", metrics,
            ("DisplayCadenceSampleCount", 0, 19),
            ("DisplayCadenceObservedFps", 0d, 118.5d),
            ("DisplayCadenceExpectedIntervalMs", 0d, 8.33d),
            ("DisplayCadenceAverageIntervalMs", 0d, 8.44d),
            ("DisplayCadenceP95IntervalMs", 0d, 8.55d),
            ("DisplayCadenceP99IntervalMs", 0d, 8.66d),
            ("DisplayCadenceMaxIntervalMs", 0d, 8.77d),
            ("DisplayCadenceOnePercentLowFps", 0d, 110.1d),
            ("DisplayCadenceFivePercentLowFps", 0d, 111.2d),
            ("DisplayCadenceSampleDurationMs", 0d, 159.3d),
            ("DisplayCadenceRecentIntervalsMs", Array.Empty<double>(), intervals),
            ("DisplayCadenceJitterStdDevMs", 0d, 0.45d),
            ("DisplayCadenceSlowFrameCount", 0L, 7L),
            ("DisplayCadenceSlowFramePercent", 0d, 6.7d));

        AssertPreviewMetricSnapshots("ApplyDisplayCadence", CreatePreviewRendererMetric("PresentCadenceMetrics"),
            ("DisplayCadenceRecentIntervalsMs", Array.Empty<double>(), Array.Empty<double>()));
        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotProjection_PreservesRenderCpuTimingMetrics()
    {
        var inputUpload = CreatePreviewRendererMetric("CpuStageTimingMetrics", 11, 1.1d, 1.2d, 1.3d, 1.4d);
        var renderSubmit = CreatePreviewRendererMetric("CpuStageTimingMetrics", 22, 2.1d, 2.2d, 2.3d, 2.4d);
        var presentCall = CreatePreviewRendererMetric("CpuStageTimingMetrics", 33, 3.1d, 3.2d, 3.3d, 3.4d);
        var totalFrame = CreatePreviewRendererMetric("CpuStageTimingMetrics", 44, 4.1d, 4.2d, 4.3d, 4.4d);
        var metrics = CreatePreviewRendererMetric("RenderCpuTimingMetrics", inputUpload, renderSubmit, presentCall, totalFrame);
        AssertPreviewMetricSnapshots("ApplyRenderCpuTiming", metrics,
            ("D3DCpuTimingSampleCount", 0, 44),
            ("D3DInputUploadCpuAvgMs", 0d, 1.1d),
            ("D3DInputUploadCpuP95Ms", 0d, 1.2d),
            ("D3DInputUploadCpuP99Ms", 0d, 1.3d),
            ("D3DInputUploadCpuMaxMs", 0d, 1.4d),
            ("D3DRenderSubmitCpuAvgMs", 0d, 2.1d),
            ("D3DRenderSubmitCpuP95Ms", 0d, 2.2d),
            ("D3DRenderSubmitCpuP99Ms", 0d, 2.3d),
            ("D3DRenderSubmitCpuMaxMs", 0d, 2.4d),
            ("D3DPresentCallAvgMs", 0d, 3.1d),
            ("D3DPresentCallP95Ms", 0d, 3.2d),
            ("D3DPresentCallP99Ms", 0d, 3.3d),
            ("D3DPresentCallMaxMs", 0d, 3.4d),
            ("D3DTotalFrameCpuAvgMs", 0d, 4.1d),
            ("D3DTotalFrameCpuP95Ms", 0d, 4.2d),
            ("D3DTotalFrameCpuP99Ms", 0d, 4.3d),
            ("D3DTotalFrameCpuMaxMs", 0d, 4.4d));
        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotProjection_PreservesPipelineLatencyMetrics()
    {
        var metrics = CreatePreviewRendererMetric("PipelineLatencyMetrics", 51, 5.1d, 5.2d, 5.3d, 5.4d);
        AssertPreviewMetricSnapshots("ApplyPipelineLatency", metrics,
            ("D3DPipelineLatencySampleCount", 0, 51),
            ("D3DPipelineLatencyAvgMs", 0d, 5.1d),
            ("D3DPipelineLatencyP95Ms", 0d, 5.2d),
            ("D3DPipelineLatencyP99Ms", 0d, 5.3d),
            ("D3DPipelineLatencyMaxMs", 0d, 5.4d),
            ("EstimatedPipelineLatencyMs", 0d, 5.1d));
        return Task.CompletedTask;
    }

    private static object CreatePreviewRendererMetric(string typeName, params object?[] arguments)
        => Activator.CreateInstance(RequireType($"Sussudio.Services.Preview.D3D11PreviewRenderer+{typeName}"), arguments)
           ?? throw new InvalidOperationException($"Failed to create renderer metric {typeName}.");

    private static void AssertPreviewMetricSnapshots(
        string applyMethod,
        object metrics,
        params (string Field, object Absent, object Present)[] expectedFields)
    {
        var absentSnapshot = BuildPreviewMetricSnapshot();
        var presentSnapshot = BuildPreviewMetricSnapshot(applyMethod, metrics);
        foreach (var expected in expectedFields)
        {
            // Object equality for arrays also verifies that projection preserves the sampled array reference.
            AssertEqual(expected.Absent, GetPropertyValue(absentSnapshot, expected.Field), $"{expected.Field} without renderer metrics");
            AssertEqual(expected.Present, GetPropertyValue(presentSnapshot, expected.Field), $"{expected.Field} with renderer metrics");
        }
    }

    private static object BuildPreviewMetricSnapshot(string? applyMethod = null, object? metrics = null)
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);

        if (applyMethod == null)
        {
            var controller = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotController");
            return controller.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new[] { input })
                   ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build returned null.");
        }

        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var projection = projectionType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new[] { input })
                         ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build returned null.");
        InvokeNonPublicInstanceMethod(projection, applyMethod, new[] { metrics });
        var health = Activator.CreateInstance(RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth"), new object?[] { null, false, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var mapper = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotMapper");
        return mapper.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)!.Invoke(
            null, new object[] { input, projection, health, DateTimeOffset.UnixEpoch })
            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build returned null.");
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

    internal static Task PreviewRuntimeSnapshotMapper_PreservesVisibilityAndHealthFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var mapperType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotMapper");
        var build = mapperType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build not found.");
        var timestamp = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

        foreach (var active in new[] { true, false })
        {
            var input = Activator.CreateInstance(inputType)
                        ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
            SetPropertyOrBackingField(input, "PreviewRuntimeEpoch", 73L);
            SetPropertyOrBackingField(input, "IsPreviewing", active);
            SetPropertyOrBackingField(input, "PlaceholderVisible", !active);
            SetPropertyOrBackingField(input, "GpuElementVisible", active);
            SetPropertyOrBackingField(input, "CpuElementVisible", !active);
            // The final counters must come from the resolved projection, not the sampled CPU input.
            SetPropertyOrBackingField(input, "FramesArrived", 901L);
            SetPropertyOrBackingField(input, "FramesDisplayed", 807L);
            SetPropertyOrBackingField(input, "FramesDropped", 94L);

            var d3dProjection = Activator.CreateInstance(projectionType)
                                ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
            SetPropertyOrBackingField(d3dProjection, "GpuActive", !active);
            SetPropertyOrBackingField(d3dProjection, "RendererAttached", active);
            SetPropertyOrBackingField(d3dProjection, "FramesArrived", 101L);
            SetPropertyOrBackingField(d3dProjection, "FramesDisplayed", 99L);
            SetPropertyOrBackingField(d3dProjection, "FramesDropped", 2L);

            var health = Activator.CreateInstance(healthType, new object?[] { null, active, !active })
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
            var snapshot = build.Invoke(null, new object?[] { input, d3dProjection, health, timestamp })
                           ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build returned null.");

            AssertEqual(timestamp, GetPropertyValue(snapshot, "TimestampUtc"), "snapshot supplied timestamp");
            AssertEqual(73L, GetLongProperty(snapshot, "PreviewRuntimeEpoch"), "snapshot sampled epoch");
            AssertEqual(active, GetBoolProperty(snapshot, "IsPreviewing"), "snapshot previewing");
            AssertEqual(!active, GetBoolProperty(snapshot, "GpuActive"), "snapshot GPU active");
            AssertEqual(!active, GetBoolProperty(snapshot, "PlaceholderVisible"), "snapshot placeholder visible");
            AssertEqual(active, GetBoolProperty(snapshot, "GpuElementVisible"), "snapshot GPU element visible");
            AssertEqual(!active, GetBoolProperty(snapshot, "CpuElementVisible"), "snapshot CPU element visible");
            AssertEqual(active, GetBoolProperty(snapshot, "RendererAttached"), "snapshot renderer attached");
            AssertEqual(101L, GetLongProperty(snapshot, "FramesArrived"), "snapshot projected frames arrived");
            AssertEqual(99L, GetLongProperty(snapshot, "FramesDisplayed"), "snapshot projected frames displayed");
            AssertEqual(2L, GetLongProperty(snapshot, "FramesDropped"), "snapshot projected frames dropped");
            AssertEqual(active, GetBoolProperty(snapshot, "BlankSuspected"), "snapshot blank suspected");
            AssertEqual(!active, GetBoolProperty(snapshot, "StallSuspected"), "snapshot stall suspected");
        }

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotMapper_PreservesSampledStartupFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var mapperType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotMapper");
        var build = mapperType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build not found.");

        foreach (var hasAttempt in new[] { true, false })
        {
            var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", hasAttempt ? "FirstVisual" : "MediaOpened");
            var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", hasAttempt ? "MediaOpened" : "None");
            var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", hasAttempt ? "D3D11VideoProcessor" : "CpuSoftwareBitmap");
            var state = ParseEnum(
                "Sussudio.Models.PreviewStartupState",
                hasAttempt ? "WaitingForFirstVisual" : "Idle");
            var attemptId = hasAttempt ? "attempt-42" : null;
            var missingSignals = hasAttempt ? "FirstVisual" : null;
            var failureReason = hasAttempt ? "visual-timeout" : null;
            double? elapsedMs = hasAttempt ? 456.25d : null;
            var timeoutMs = hasAttempt ? 1250 : 730;
            var recoveryCount = hasAttempt ? 5 : 0;

            var input = Activator.CreateInstance(inputType)
                        ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
            SetPropertyOrBackingField(input, "StartupState", state);
            SetPropertyOrBackingField(input, "StartupAttemptId", attemptId);
            SetPropertyOrBackingField(input, "StartupTimeoutMs", timeoutMs);
            SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", hasAttempt);
            SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", !hasAttempt);
            SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", hasAttempt);
            SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
            SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
            SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
            SetPropertyOrBackingField(input, "StartupMissingSignals", missingSignals);
            SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", recoveryCount);
            SetPropertyOrBackingField(input, "StartupLastFailureReason", failureReason);
            SetPropertyOrBackingField(input, "FirstVisualConfirmed", !hasAttempt);

            var d3dProjection = Activator.CreateInstance(projectionType)
                                ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
            var health = Activator.CreateInstance(healthType, new object?[] { elapsedMs, true, false })
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
            var snapshot = build.Invoke(null, new object?[] { input, d3dProjection, health, DateTimeOffset.UnixEpoch })
                           ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build returned null.");

            AssertEqual(state, GetPropertyValue(snapshot, "StartupState"), "snapshot startup state");
            AssertEqual(attemptId, GetPropertyValue(snapshot, "StartupAttemptId"), "snapshot startup attempt id");
            AssertEqual(elapsedMs, GetPropertyValue(snapshot, "StartupElapsedMs"), "snapshot startup elapsed");
            AssertEqual(timeoutMs, GetIntProperty(snapshot, "StartupTimeoutMs"), "snapshot startup timeout");
            AssertEqual(hasAttempt, GetBoolProperty(snapshot, "StartupGpuSignalMediaOpened"), "snapshot media opened signal");
            AssertEqual(!hasAttempt, GetBoolProperty(snapshot, "StartupGpuSignalFirstFrame"), "snapshot first frame signal");
            AssertEqual(hasAttempt, GetBoolProperty(snapshot, "StartupGpuSignalPlaybackAdvancing"), "snapshot playback signal");
            AssertEqual(requiredSignals, GetPropertyValue(snapshot, "StartupRequiredSignals"), "snapshot required signals");
            AssertEqual(receivedSignals, GetPropertyValue(snapshot, "StartupReceivedSignals"), "snapshot received signals");
            AssertEqual(startupStrategy, GetPropertyValue(snapshot, "StartupStrategy"), "snapshot startup strategy");
            AssertEqual(missingSignals, GetPropertyValue(snapshot, "StartupMissingSignals"), "snapshot missing signals");
            AssertEqual(recoveryCount, GetIntProperty(snapshot, "StartupRecoveryAttemptCount"), "snapshot recovery count");
            AssertEqual(failureReason, GetPropertyValue(snapshot, "StartupLastFailureReason"), "snapshot failure reason");
            AssertEqual(!hasAttempt, GetBoolProperty(snapshot, "FirstVisualConfirmed"), "snapshot first visual confirmed");
        }

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotMapper_PreservesRendererAndEventFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var mapperType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotMapper");
        var build = mapperType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build not found.");

        foreach (var sample in new[]
        {
            (State: "Rendering", Width: 3840, Height: 2160, PositionMs: 1234.5d, EventCount: 42L),
            (State: "Idle", Width: 1920, Height: 1080, PositionMs: 987.25d, EventCount: 17L)
        })
        {
            var input = Activator.CreateInstance(inputType)
                        ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
            SetPropertyOrBackingField(input, "GpuPositionEventCount", sample.EventCount);

            var d3dProjection = Activator.CreateInstance(projectionType)
                                ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
            SetPropertyOrBackingField(d3dProjection, "GpuPlaybackState", sample.State);
            SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoWidth", sample.Width);
            SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoHeight", sample.Height);
            SetPropertyOrBackingField(d3dProjection, "GpuPositionMs", sample.PositionMs);

            var health = Activator.CreateInstance(healthType, new object?[] { null, false, false })
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
            var snapshot = build.Invoke(null, new object?[] { input, d3dProjection, health, DateTimeOffset.UnixEpoch })
                           ?? throw new InvalidOperationException("PreviewRuntimeSnapshotMapper.Build returned null.");

            AssertEqual(sample.State, GetStringProperty(snapshot, "GpuPlaybackState"), "snapshot GPU playback state");
            AssertEqual(sample.Width, GetIntProperty(snapshot, "GpuNaturalVideoWidth"), "snapshot GPU natural width");
            AssertEqual(sample.Height, GetIntProperty(snapshot, "GpuNaturalVideoHeight"), "snapshot GPU natural height");
            AssertEqual(sample.PositionMs, GetDoubleProperty(snapshot, "GpuPositionMs"), "snapshot GPU position");
            AssertEqual(sample.EventCount, GetLongProperty(snapshot, "GpuPositionEventCount"), "snapshot GPU event count");
        }

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
        var startupState = ParseEnum("Sussudio.Models.PreviewStartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "StartupState", startupState);
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
        AssertEqual(startupState, GetPropertyValue(snapshot, "StartupState"), "startup state passthrough");
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
        var mapperType = RequireType("Sussudio.Services.Audio.DeviceAudioGainMapper");
        var mapPercent = mapperType.GetMethod("PercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.PercentToGainByte was not found.");
        var mapByte = mapperType.GetMethod("GainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.GainByteToPercent was not found.");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceAudioModeText, "_deviceAudioControlService.SetAudioModeAsync(device, mode, cancellationToken)");
        AssertContains(deviceAudioStateText, "DeviceAudioGainMapper.PercentToGainByte(gainPercent)");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertDoesNotContain(deviceAudioStateText, "private static byte MapPercentToGainByte");
        AssertDoesNotContain(deviceAudioStateText, "private static double MapGainByteToPercent");
        var deviceAudioControlServiceText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        AssertContains(deviceAudioControlServiceText, "internal static class DeviceAudioGainMapper");
        AssertContains(deviceAudioControlServiceText, "private const double GainCurveK = 4.0;");
        AssertContains(deviceAudioControlServiceText, "internal static byte PercentToGainByte");
        AssertContains(deviceAudioControlServiceText, "internal static double GainByteToPercent");

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

    internal static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/Controllers/ViewModel/PreviewAudioTransitionControllers.cs")
            .Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var audioRampTraceRecorderRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/PreviewAudioTransitionControllers.cs").Replace("\r\n", "\n");
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
        AssertContains(audioVolumeTransitionText, "public async Task<PreviewAudioVolumeOperation> RampDownForStopAsync(CancellationToken cancellationToken)");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "GetAudioRampTraceSnapshotAsync");

        AssertContains(playbackRenderText, "UpdateOutputLevel(destinationSpan);");
        AssertContains(playbackRenderText, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackVolumeText, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(playbackVolumeText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackVolumeText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackVolumeText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackVolumeText, "public float LastOutputRms => _lastOutputRms;");
        AssertContains(playbackVolumeText, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackVolumeText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");

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
        AssertDoesNotContain(controllerGraphText, "IncrementReinitializeGeneration =");
        AssertDoesNotContain(controllerGraphText, "ReadReinitializeGeneration =");
        AssertContains(controllerGraphText, "PreviewReinitializeDebounceMs = PreviewReinitializeDebounceMs,");
        AssertContains(controllerGraphText, "PendingFlashbackCycleTask = () => viewModel._recordingSettingsController.PendingApplication,");
        AssertContains(controllerGraphText, "ClearPendingFlashbackCycleIfSameAndCompleted = task => viewModel._recordingSettingsController.ClearPendingIfSameAndCompleted(task),");
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
        AssertDoesNotContain(previewStateText, "private readonly SemaphoreSlim _previewReinitializeGate");
        AssertDoesNotContain(previewStateText, "private int _previewReinitializeGeneration;");
        AssertDoesNotContain(previewStateText, "private bool _cancelPreviewRestartAfterReinitialize;");
        AssertContains(previewReinitializeControllerText, "private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);");
        AssertContains(previewReinitializeControllerText, "private int _previewReinitializeGeneration;");
        AssertContains(previewReinitializeControllerText, "private bool _cancelPreviewRestartAfterReinitialize;");
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
        var recordingSettingsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsController.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var frameRateTimingResolverText = captureModeOptionRebuildControllerText;
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = deviceFormatProbeControllerText;

        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");

        AssertContains(controllerGraphText, "var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);");
        AssertContains(controllerGraphText, "var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);");
        AssertContains(controllerGraphText, "var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var recordingSettingsController = CreateRecordingSettingsController(viewModel);");
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
        AssertContains(controllerGraphText, "ApplyCaptureSelectionWithoutReinitialize = viewModel.ApplyCaptureSelectionWithoutReinitialize,");
        foreach (var source in new[] { controllerGraphText, captureSettingsAutomationControllerText, deviceFormatProbeControllerText })
        {
            AssertDoesNotContain(source, "SetSuppressFormatChangeReinitialize");
            AssertDoesNotContain(source, "IsSuppressFormatChangeReinitialize");
        }
        AssertContains(captureSettingsAutomationControllerText, "Action<Action> ApplyCaptureSelectionWithoutReinitialize");
        AssertContains(deviceFormatProbeControllerText, "Action<Action> ApplyCaptureSelectionWithoutReinitialize");
        AssertContains(controllerGraphText, "ReinitializeDeviceWithResultAsync = viewModel.ReinitializeDeviceWithResultAsync,");

        AssertContains(recordingSettingsControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsControllerText, "internal sealed class MainViewModelRecordingSettingsController");
        AssertContains(recordingSettingsControllerText, "public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsControllerText, "internal sealed class MainViewModelRecordingSettingsControllerContext");
        AssertContains(recordingSettingsControllerText, "private readonly MainViewModelRecordingSettingsControllerContext _context;");
        AssertDoesNotContain(recordingSettingsControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsControllerText, "_viewModel.");
        AssertContains(recordingSettingsControllerText, "_context.ApplyAsync(_context.CaptureSelection(), kind, cancellationToken)");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingSettingsController CreateRecordingSettingsController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsControllerContext");
        AssertMemberContains(controllerGraphText, "CreateRecordingSettingsController", "ApplyAsync = viewModel._sessionCoordinator.ApplyRecordingSettingsAsync,");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "class MainViewModelRecordingSettingsController");

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
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions(bool forceSourceAutoRetarget)");
        AssertContains(captureModeOptionRebuildControllerText, "=> RebuildFrameRateOptions();");

        AssertContains(deviceFormatProbeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
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
        AssertContains(sourceTelemetryControllerText, "public required Action<bool> RebuildResolutionOptions { get; init; }");
        AssertContains(sourceTelemetryControllerText, "_context.RebuildResolutionOptions(forceSourceAutoRetarget);");
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
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateLiveCaptureInfoWithoutSnapshot();");
        AssertContains(runtimeLifecycleControllerText, "SetupTimer();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateDiskSpace();");

        AssertContains(runtimeEventIngressControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressController");
        AssertDoesNotContain(runtimeEventIngressControllerText, "partial class MainViewModelRuntimeEventIngressController");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "private readonly MainViewModelRuntimeEventIngressControllerContext _context;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.");
        AssertContains(runtimeEventIngressControllerText, "_context.RecoverCaptureErrorAsync(error.Origin)");
        AssertContains(runtimeEventIngressControllerText, "if (!_context.IsCaptureErrorCurrent(error.Origin))");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(controllerGraphText, "private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(runtimeEventIngressControllerText, "public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.RecoverCaptureErrorAsync(");
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
        AssertDoesNotContain(runtimeEventIngressControllerText, "OnFrameCaptured");
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
        var cancelPendingAudioControlWork = ExtractTextBetween(
            controllerGraphText,
            "CancelPendingAudioControlWork = () =>",
            "StopRuntimeForDispose = runtimeLifecycleController.StopForDispose,");
        AssertContains(cancelPendingAudioControlWork, "viewModel.DisposePreviewAudioVolume();");
        AssertContains(cancelPendingAudioControlWork, "deviceAudioRequestController.CancelPendingAudioControlWork();");
        AssertOccursBefore(
            cancelPendingAudioControlWork,
            "viewModel.DisposePreviewAudioVolume();",
            "deviceAudioRequestController.CancelPendingAudioControlWork();");
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
        AssertContains(disposalControllerText, "GetOrStartDisposalTask()");
        AssertContains(disposalControllerText, "_context.CompleteRuntimeDispose();");
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

        var stopRecordingText = "Stop recording before switching between HDR and SDR pipelines";
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
