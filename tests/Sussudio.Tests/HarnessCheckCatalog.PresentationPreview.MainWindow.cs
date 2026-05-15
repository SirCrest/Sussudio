using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewMainWindowInitialChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Window close cancels until recording stop completes",
            MainWindowClose_CancelsCloseUntilRecordingStopCompletes);
        await AddCheckAsync(results,
            "Window screenshot capture completes on dispatcher failure and cancellation",
            MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation);
        await AddCheckAsync(results,
            "Window screenshot native capture lives in focused helper",
            WindowScreenshotNativeCapture_LivesInFocusedHelper);
        await AddCheckAsync(results,
            "Window screenshot image encoding lives in focused helper",
            WindowScreenshotImageEncoding_LivesInFocusedHelper);
    }

    private static async Task AddPresentationPreviewMainWindowChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Settings shelf lifecycle lives in controller",
            SettingsShelfLifecycle_LivesInController);
        await AddCheckAsync(results,
            "Splash loading phrase catalog and animation ownership are split",
            SplashLoadingPhrases_LiveInController);
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
            "Preview renderer runtime state lives in renderer partial",
            PreviewRendererRuntimeState_LivesInRendererPartial);
        await AddCheckAsync(results,
            "Preview runtime snapshot controller preserves null-D3D policy",
            PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy);
        await AddCheckAsync(results,
            "Window title formatting lives in controller and MainWindow is the adapter",
            MainWindowTitlePresentation_LivesInController);
        await AddCheckAsync(results,
            "Window title controller formats build stamp and recording suffix",
            WindowTitleController_FormatsBuildStampAndRecordingSuffix);
        await AddCheckAsync(results,
            "MainWindow close lifecycle and native bootstrap are split",
            MainWindowCloseLifecycleAndNativeHelpers_AreSplit);
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
            "Record button width animation lives in controller",
            RecordButtonWidthAnimation_LivesInController);
        await AddCheckAsync(results,
            "Recording state presentation lives in controller",
            RecordingStatePresentation_LivesInController);
        await AddCheckAsync(results,
            "Recording button action lives in controller",
            RecordingButtonAction_LivesInController);
        await AddCheckAsync(results,
            "Live signal info presentation lives in controller",
            LiveSignalInfoPresentation_LivesInController);
        await AddCheckAsync(results,
            "Status strip presentation lives in controller",
            StatusStripPresentation_LivesInController);
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
            "Capture device button actions live in controller",
            CaptureDeviceButtonActions_LiveInController);
        await AddCheckAsync(results,
            "Capture option presentation lives in controller",
            CaptureOptionPresentation_LivesInController);
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
        await AddCheckAsync(results,
            "Preview screenshot button workflow lives in controller",
            PreviewScreenshotButtonWorkflow_LivesInController);
    }
}
