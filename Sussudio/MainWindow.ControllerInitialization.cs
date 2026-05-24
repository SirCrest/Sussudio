using System.ComponentModel;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// Phased controller initialization and property-change routing for the MainWindow
// composition root. Keep these groups ordered by runtime surface so startup
// wiring stays auditable.
public sealed partial class MainWindow
{
    private MainWindowPropertyChangedRouter _propertyChangedRouter = null!;
    private FlashbackPropertyChangedController _flashbackPropertyChangedController = null!;

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

    private void InitializeMainWindowPropertyChangedRouter()
    {
        _propertyChangedRouter = new MainWindowPropertyChangedRouter(new MainWindowPropertyChangedRouterContext
        {
            TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,
            TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,
            TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,
            TryHandleRecording = TryHandleRecordingPropertyChanged,
            TryHandleOutput = TryHandleOutputPropertyChanged,
            TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,
            TryHandleAudio = TryHandleAudioPropertyChanged,
            TryHandleShell = TryHandleShellPropertyChanged,
            TryHandleLiveSignal = TryHandleLiveSignalPropertyChanged,
            TryHandleFlashback = TryHandleFlashbackPropertyChanged
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }

    private Task HandleViewModelPropertyChangedAsync(PropertyChangedEventArgs e)
        => _propertyChangedRouter.RouteAsync(e.PropertyName);

    private void InitializeFlashbackPropertyChangedController()
    {
        _flashbackPropertyChangedController = new FlashbackPropertyChangedController(new FlashbackPropertyChangedControllerContext
        {
            IsTimelineVisible = () => ViewModel.IsFlashbackTimelineVisible,
            GetExportProgress = () => ViewModel.FlashbackExportProgress,
            IsExporting = () => ViewModel.IsFlashbackExporting,
            ApplyTimelineVisibility = ApplyFlashbackTimelineVisibility,
            ApplyTimelineLockout = ApplyFlashbackTimelineLockout,
            UpdateState = UpdateFlashbackStateUI,
            UpdateBuffer = UpdateFlashbackBufferPresentation,
            UpdatePlaybackPosition = UpdateFlashbackPositionUI,
            UpdateRangeMarkers = UpdateFlashbackMarkers,
            UpdateExportProgress = UpdateFlashbackExportProgress,
            UpdateExportingPresentation = UpdateFlashbackExportingPresentation,
            SyncGpuDecodeSetting = SyncFlashbackGpuDecodeSetting,
            SyncBufferDurationSetting = SyncFlashbackBufferDurationSetting
        });
    }

    private bool TryHandleFlashbackPropertyChanged(string propertyName)
        => _flashbackPropertyChangedController.TryHandlePropertyChanged(propertyName);
}
