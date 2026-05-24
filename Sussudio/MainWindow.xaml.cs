using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio;

// Main window composition root. This partial owns construction and service
// wiring; phased controller initialization and feature-specific UI behavior
// live in sibling partials/controllers.
public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;
    private NvmlMonitor? _nvmlMonitor;
    private FullScreenController _fullScreenController = null!;
    private WindowShutdownCleanupController _windowShutdownCleanupController = null!;
    private MainWindowPropertyChangedRouter _propertyChangedRouter = null!;
    private FlashbackPropertyChangedController _flashbackPropertyChangedController = null!;

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();
        InitializeWindowCloseRequestController();
        ViewModel.StatsSectionVisibilityHandler = SetStatsSectionVisible;
        ViewModel.FrameTimeOverlayVisibilityHandler = SetFrameTimeOverlayVisible;
        InitializeWindowTitleController();
        ApplyWindowTitle();
        _nvmlMonitor = new NvmlMonitor();
        _automationHostLifecycleController = new WindowAutomationHostLifecycleController(
            ViewModel,
            GetPreviewRuntimeSnapshotAsync,
            this);
        InitializePreviewReinitTransitionController();
        InitializePreviewRendererHostController();
        InitializeStatsOverlayCompositionController();
        InitializeWindowShutdownCleanupController();

        var appWindow = InitializeNativeShellWindow();
        RegisterCloseLifecycle(appWindow);
        InitializeShellControllers();

        // Subscribe to ViewModel changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested += ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested += ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested += ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested += ViewModel_PreviewRendererStopRequested;

        // Wire up UI controls to ViewModel
        SetupBindings();
        SetupButtonHoverAnimations();
        SetupControlBarShadow();

        // ESC key exits fullscreen
        ((FrameworkElement)Content).KeyDown += OnContentKeyDown;

        InitializeFullScreenController();

        // Fullscreen overlay: show controls when mouse enters bottom hot zone
        ((UIElement)Content).PointerMoved += OnFullScreenPointerActivity;
        FullScreenControlsOverlay.PointerEntered += OnFullScreenControlsPointerEntered;
        FullScreenControlsOverlay.PointerExited += OnFullScreenControlsPointerExited;

        PrepareLaunchEntranceInitialState();
        ApplyShellElevation();

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        var mainContent = (FrameworkElement)this.Content;
        mainContent.Loaded += MainWindow_Loaded;
        mainContent.SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;
    }

    // Phased controller initialization and property-change routing for the
    // MainWindow composition root. Keep these groups ordered by runtime surface
    // so startup wiring stays auditable.
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

    private void InitializeWindowShutdownCleanupController()
    {
        _windowShutdownCleanupController = new WindowShutdownCleanupController(new WindowShutdownCleanupControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            IsRecording = () => ViewModel.IsRecording,
            IsPreviewing = () => ViewModel.IsPreviewing,
            CancelNativeShellRevealAfterFirstFrame = CancelNativeShellRevealAfterFirstFrame,
            CompleteWindowCloseRequest = () => CompleteWindowCloseRequest(),
            DetachMeterActivationHandlers = DetachMeterActivationHandlers,
            StopTimers = StopShutdownTimers,
            StopStatsOverlay = StopStatsOverlayForShutdown,
            StopRecordingVisuals = StopRecordingVisualsForShutdown,
            DetachMainContentSizeChanged = DetachMainContentSizeChanged,
            DetachViewModelEventHandlers = DetachViewModelEventHandlers,
            StopPreviewForShutdown = StopPreviewForShutdown,
            ResetPreviewStartupTracking = () => ResetPreviewStartupTracking(),
            StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(
                ViewModel,
                Content as FrameworkElement),
            DisposeAutomationHostAsync = () => _automationHostLifecycleController.DisposeAsync(),
            DisposeNvmlMonitor = () => _nvmlMonitor?.Dispose(),
            DisposeViewModelAsync = ViewModel.DisposeAsync
        });
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
        => await _windowShutdownCleanupController.RunAsync();

    private void DetachMeterActivationHandlers()
    {
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
    }

    private void StopShutdownTimers()
    {
        StopAudioMeterTimer();
        StopLiveSignalInfoTimers();
        StopFullScreenAutoHideTimer();
        StopFlashbackStatusPolling();
    }

    private void StopStatsOverlayForShutdown()
    {
        DetachStatsOverlayToggleBindings();
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
    }

    private void StopRecordingVisualsForShutdown()
    {
        StopMicMeterRowAnimation();
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();
    }

    private void DetachMainContentSizeChanged()
    {
        if (Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }
    }

    private void DetachViewModelEventHandlers()
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;
    }

    // Manual binding layer for WinUI controls. The app deliberately avoids
    // x:Bind, so startup maps view-model state to concrete UI updates here.
    private void SetupBindings()
    {
        AttachAudioMeterActivationBindings();

        ApplyInitialFlashbackSettings();

        // Bind all collections to ComboBoxes
        AttachCaptureSelectionBindings();
        InitializeCaptureOptionCollections();

        // Set initial values
        UpdateOutputPathDisplay();
        ApplyInitialStatusStripPresentation();
        UpdateLiveSignalInfoVisibility();
        ApplyInitialAudioControlBindings();
        ApplyInitialCaptureOptionSelections();
        ApplyInitialAudioMeterPresentation();
        ApplyAudioClipVisibility();
        ApplyInitialRecordingStatePresentation();
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioControlSelections();
        EnsureInitialCaptureOptionSelections();

        AttachDeviceSelectionChangedBinding();
        AttachAudioSelectionBindings();
        AttachCaptureModeSelectionBindings();

        AttachRecordingOptionBindings();
        AttachAudioRecordPreviewToggleBindings();
        AttachStatsOverlayToggleBindings();
        AttachAudioInputToggleBindings();
        AttachFlashbackSettingsBindings();
        AttachDeviceAudioGainAndMeterBindings();
        SetupResponsiveShellLayoutBindings();
        AttachOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
    }
}
