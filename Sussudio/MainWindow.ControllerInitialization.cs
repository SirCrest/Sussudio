namespace Sussudio;

// Phased controller initialization for the MainWindow composition root. Keep
// these groups ordered by runtime surface so startup wiring stays auditable.
public sealed partial class MainWindow
{
    private void InitializeShellControllers()
    {
        InitializeWindowShellControllers();
        InitializeFlashbackControllers();
        InitializeShellPresentationControllers();
        InitializePreviewControllers();
        InitializeRecordingControllers();
        InitializeLaunchAndStatusControllers();
        InitializePreviewActionControllers();
        InitializeAudioControllers();
        InitializeResponsiveShellLayoutController();
        InitializeCaptureControllers();
        InitializeOutputControllers();
        InitializePreviewScreenshotController();
        InitializeMainWindowPropertyChangedRouter();
    }

    private void InitializeWindowShellControllers()
    {
        InitializeWindowAutomationController();
        InitializeWindowScreenshotController();
    }

    private void InitializeFlashbackControllers()
    {
        InitializeFlashbackPollingController();
        InitializeFlashbackScrubInteractionController();
        InitializeFlashbackPlayheadMotionController();
        InitializeFlashbackTimelineController();
        InitializeFlashbackSettingsBindingController();
        InitializeFlashbackCommandController();
        InitializeFlashbackMarkerPresentationController();
        InitializeFlashbackPlaybackPresentationController();
        InitializeFlashbackPlaybackUiCoordinator();
        InitializeFlashbackExportProgressPresentationController();
        InitializeFlashbackPropertyChangedController();
    }

    private void InitializeShellPresentationControllers()
    {
        InitializeSettingsShelfController();
        InitializeSplashLoadingPhraseController();
        InitializeControlBarAnimationController();
        InitializeShellElevationController();
        InitializeShellPropertyChangedController();
    }

    private void InitializePreviewControllers()
    {
        InitializePreviewResizeTelemetryController();
        InitializePreviewSurfacePresentationController();
        InitializePreviewStartupSessionController();
        InitializePreviewLifecycleEventController();
        InitializePreviewStartupSignalCoordinator();
        InitializePreviewStartupWatchdogController();
        InitializePreviewRuntimeSnapshotSamplingController();
        InitializePreviewStartupOverlayController();
        InitializePreviewFadeInController();
        InitializePreviewTransitionAnimationController();
        InitializePreviewButtonPresentationController();
    }

    private void InitializeRecordingControllers()
    {
        InitializeRecordingButtonChromeController();
        InitializeRecordingStatePresentationController();
        InitializeRecordingButtonActionController();
    }

    private void InitializeLaunchAndStatusControllers()
    {
        InitializeLaunchEntranceAnimationController();
        InitializeLaunchStartupController();
        InitializeLiveSignalInfoController();
        InitializeStatusStripPresentationController();
    }

    private void InitializePreviewActionControllers()
    {
        InitializePreviewAudioFadeController();
        InitializePreviewButtonActionController();
    }

    private void InitializeAudioControllers()
    {
        InitializeMicrophoneControlsController();
        InitializeAudioControlBindingController();
        InitializeAudioControlPresentationController();
    }

    private void InitializeCaptureControllers()
    {
        InitializeCaptureSelectionBindingController();
        InitializeCaptureDeviceActionController();
        InitializeCaptureOptionPresentationController();
        InitializeCaptureOptionBindingController();
    }

    private void InitializeOutputControllers()
        => InitializeOutputPathController();
}
