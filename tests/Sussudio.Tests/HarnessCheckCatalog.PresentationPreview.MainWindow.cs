using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainWindowChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainWindow property changed routing delegates to focused controllers",
            MainWindowPropertyChangedRouting_DelegatesToFocusedControllers);
        await AddCheckAsync(results,
            "Splash loading phrase catalog and animation ownership are split",
            SplashLoadingPhrases_LiveInController);
        await AddCheckAsync(results,
            "Splash loading phrase pacing policy preserves interval bands",
            SplashLoadingPhrasePacingPolicy_PreservesIntervalBands);
        await AddCheckAsync(results,
            "Launch entrance animation lives in controller",
            LaunchEntranceAnimation_LivesInController);
        await AddCheckAsync(results,
            "MainWindow startup hosting lives in startup partial",
            MainWindowStartupHosting_LivesInStartupPartial);
        await AddCheckAsync(results,
            "Preview resize telemetry lives in controller",
            PreviewResizeTelemetry_LivesInController);
        await AddCheckAsync(results,
            "Preview renderer runtime state lives in host controller",
            PreviewRendererHostController_OwnsRuntimeState);
        await AddCheckAsync(results,
            "Preview runtime snapshot ownership lives in controller and mapper",
            PreviewRuntimeSnapshotController_OwnsSnapshotMapping);
        await AddCheckAsync(results,
            "Preview runtime D3D projection ownership lives in policy groups",
            PreviewRuntimeD3DProjection_OwnsPolicyGroups);
        await AddCheckAsync(results,
            "Preview surface presentation and shadow live in controllers",
            PreviewSurfacePresentationAndShadow_LiveInControllers);
        await AddCheckAsync(results,
            "Preview renderer startup plan builder preserves fallback policy",
            PreviewRendererStartupPlanBuilder_PreservesFallbackPolicy);
        await AddCheckAsync(results,
            "Preview runtime snapshot controller preserves null-D3D policy",
            PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy);
        await AddCheckAsync(results,
            "Preview runtime snapshot health policy preserves suspicion rules",
            PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules);
        await AddCheckAsync(results,
            "Preview runtime snapshot health input factory projects controller inputs",
            PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs);
        await AddCheckAsync(results,
            "Preview runtime snapshot surface projection preserves visibility and health fields",
            PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields);
        await AddCheckAsync(results,
            "Preview runtime snapshot startup projection preserves sampled startup fields",
            PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields);
        await AddCheckAsync(results,
            "Preview runtime snapshot GPU playback projection preserves renderer and event fields",
            PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-counter policy preserves CPU fallback counters",
            PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters);
        await AddCheckAsync(results,
            "Preview runtime D3D projection builder applies policy groups",
            PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups);
        await AddCheckAsync(results,
            "Preview runtime D3D renderer state policy preserves null renderer defaults",
            PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D display cadence policy preserves null renderer defaults",
            PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D render CPU timing policy preserves null renderer defaults",
            PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D pipeline latency policy preserves null renderer defaults",
            PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-statistics policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-latency wait policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "Preview runtime D3D frame-ownership policy preserves null renderer defaults",
            PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults);
        await AddCheckAsync(results,
            "MainWindow native bootstrap lives in controller",
            MainWindowNativeBootstrap_LivesInFocusedController);
        await AddCheckAsync(results,
            "MainWindow close lifecycle and shutdown cleanup are split",
            MainWindowCloseLifecycleAndShutdownCleanup_AreSplit);
        await AddCheckAsync(results,
            "MainWindow close lifecycle controllers own close request and app closing",
            MainWindowCloseLifecycleControllers_OwnCloseRequestAndAppClosing);
        await AddCheckAsync(results,
            "MainWindow close recording finalization owns recording stop policy",
            MainWindowCloseRecordingFinalization_OwnsRecordingStopPolicy);
        await AddCheckAsync(results,
            "MainWindow shutdown cleanup owns post-close cleanup order",
            MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder);
        await AddCheckAsync(results,
            "Control bar hover animations live in controller",
            ControlBarHoverAnimations_LiveInController);
        await AddCheckAsync(results,
            "Shell elevation setup lives in controller",
            ShellElevationSetup_LivesInController);
        await AddCheckAsync(results,
            "Preview transition animations live in controller",
            PreviewTransitionAnimations_LiveInController);
        await AddCheckAsync(results,
            "Preview startup overlay lives in controller",
            PreviewStartupOverlay_LivesInController);
        await AddCheckAsync(results,
            "Preview fade-in reveal lives in controller",
            PreviewFadeInReveal_LivesInController);
        await AddCheckAsync(results,
            "Preview audio fade state lives in controller",
            PreviewAudioFadeState_LivesInController);
        await AddCheckAsync(results,
            "Audio control presentation lives in controller",
            AudioControlPresentation_LivesInController);
        await AddCheckAsync(results,
            "Preview button presentation lives in controller",
            PreviewButtonPresentation_LivesInController);
        await AddCheckAsync(results,
            "Microphone controls live in controller",
            MicrophoneControls_LiveInController);
        await AddCheckAsync(results,
            "Responsive shell layout lives in controller",
            ResponsiveShellLayout_LivesInController);
        await AddCheckAsync(results,
            "Responsive shell layout policy preserves breakpoints and placements",
            ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements);
        await AddCheckAsync(results,
            "Capture selection binding sync lives in controller",
            CaptureSelectionBindingSync_LivesInController);
        await AddCheckAsync(results,
            "Capture selection property-change router lives in controller",
            CaptureSelectionBindingPropertyRouter_LivesInController);
        await AddCheckAsync(results,
            "Capture selection collection sync lives in controller partial",
            CaptureSelectionBindingCollectionSync_LivesInControllerPartial);
        await AddCheckAsync(results,
            "Capture selection owners live in focused partials",
            CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials);
        await AddCheckAsync(results,
            "Capture selection device-audio projection lives in focused partial",
            CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture combo-box selection normalizer preserves fallback rules",
            CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks);
        await AddCheckAsync(results,
            "Capture device button actions live in controller",
            CaptureDeviceButtonActions_LiveInController);
        await AddCheckAsync(results,
            "Capture option presentation lives in controller",
            CaptureOptionPresentation_LivesInController);
        await AddCheckAsync(results,
            "Capture option presentation policy preserves affordance rules",
            CaptureOptionPresentationPolicy_PreservesAffordanceRules);
        await AddCheckAsync(results,
            "Capture option bindings live in controller",
            CaptureOptionBindings_LiveInController);
        await AddCheckAsync(results,
            "Capture option tooltip formatter preserves text policy",
            CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy);
        await AddCheckAsync(results,
            "Output path display lives in controller",
            OutputPathDisplay_LivesInController);
        await AddCheckAsync(results,
            "Output path display text formatter preserves truncation policy",
            OutputPathDisplayTextFormatter_PreservesTruncationPolicy);
        await AddCheckAsync(results,
            "Output path button actions live in controller",
            OutputPathButtonActions_LiveInController);
    }
}
