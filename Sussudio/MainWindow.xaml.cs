using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
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
    private FlashbackCommandController _flashbackCommandController = null!;
    private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;
    private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;
    private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;
    private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;
    private FlashbackPlaybackUiCoordinator _flashbackPlaybackUiCoordinator = null!;
    private FlashbackPollingController _flashbackPollingController = null!;
    private FlashbackScrubInteractionController _flashbackScrubInteractionController = null!;
    private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;
    private FlashbackTimelineController _flashbackTimelineController = null!;

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
        InitializeWindowAutomationControllers();
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

    private void InitializeWindowAutomationControllers()
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

    // XAML-facing Flashback interaction adapter. Behavior lives in focused controllers.
    private void InitializeFlashbackCommandController()
    {
        _flashbackCommandController = new FlashbackCommandController(new FlashbackCommandControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });
    }

    private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetInPointAtPlayhead();

    private void FlashbackOutButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetOutPointAtPlayhead();

    private void FlashbackClearButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ClearInOutPoints();

    private void FlashbackPlayPauseButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.TogglePlayPause();

    private void FlashbackGoLiveButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.GoLive();

    private void FlashbackExportButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));

    private void FlashbackSaveLast5mButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));

    private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));

    private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));

    private void InitializeFlashbackPollingController()
    {
        _flashbackPollingController = new FlashbackPollingController(new FlashbackPollingControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            IsWindowClosing = () => _isWindowClosing,
        });
    }

    private void StartFlashbackStatusPolling()
        => _flashbackPollingController.StartStatusPolling();

    private void StopFlashbackStatusPolling()
    {
        _flashbackPollingController.StopStatusPolling();
        StopFlashbackCtiAnchorTimer();
    }

    private void StartFlashbackPlaybackPolling()
        => _flashbackPollingController.StartPlaybackPolling();

    private void StopFlashbackPlaybackPolling()
        => _flashbackPollingController.StopPlaybackPolling();

    // XAML-facing Flashback playhead motion adapter.
    private void InitializeFlashbackPlayheadMotionController()
    {
        _flashbackPlayheadMotionController = new FlashbackPlayheadMotionController(new FlashbackPlayheadMotionControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            IsWindowClosing = () => _isWindowClosing,
            IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,
            ScrubArea = FlashbackScrubArea,
            Playhead = FlashbackPlayhead,
            PlayheadHandle = FlashbackPlayheadHandle,
            PlayheadTimeBorder = FlashbackPlayheadTimeBorder,
        });
    }

    private void RequestFlashbackPlayheadSnapOnNextUpdate()
        => _flashbackPlayheadMotionController.RequestSnapOnNextUpdate();

    private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)
        => _flashbackPlayheadMotionController.PositionMagneticPlayhead(x, trackWidth);

    private void RefreshFlashbackCtiMotion(string reason)
        => _flashbackPlayheadMotionController.RefreshCtiMotion(reason);

    private void StopFlashbackCtiAnchorTimer()
        => _flashbackPlayheadMotionController.StopCtiAnchorTimer();

    // XAML-facing Flashback pointer scrub adapter.
    private void InitializeFlashbackScrubInteractionController()
    {
        _flashbackScrubInteractionController = new FlashbackScrubInteractionController(new FlashbackScrubInteractionControllerContext
        {
            ViewModel = ViewModel,
            ScrubArea = FlashbackScrubArea,
            PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,
            RefreshCtiMotion = RefreshFlashbackCtiMotion,
            GetTickCount64 = () => Environment.TickCount64,
        });
    }

    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerPressed(sender as UIElement, e);

    private void FlashbackScrubArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerMoved(sender as UIElement, e);

    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerReleased(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCanceled(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCaptureLost(sender as UIElement, e);

    private void ClearFlashbackScrubInteractionForLockout()
        => _flashbackScrubInteractionController.ClearForLockout();

    private void InitializeFlashbackSettingsBindingController()
    {
        _flashbackSettingsBindingController = new FlashbackSettingsBindingController(new FlashbackSettingsBindingControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,
            FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,
            ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout
        });
    }

    private void ApplyInitialFlashbackSettings()
        => _flashbackSettingsBindingController.ApplyInitialSettings();

    private void AttachFlashbackSettingsBindings()
        => _flashbackSettingsBindingController.AttachBindings();

    private void SyncFlashbackGpuDecodeSetting()
        => _flashbackSettingsBindingController.SyncGpuDecodeToggle();

    private void SyncFlashbackBufferDurationSetting()
        => _flashbackSettingsBindingController.SyncBufferDurationSelection();

    private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || _flashbackSettingsBindingController == null)
        {
            return;
        }

        _flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();
    }

    private void InitializeFlashbackTimelineController()
    {
        _flashbackTimelineController = new FlashbackTimelineController(new FlashbackTimelineControllerContext
        {
            ViewModel = ViewModel,
            FlashbackToggle = FlashbackToggle,
            FlashbackTimelinePanel = FlashbackTimelinePanel,
            FlashbackTrackBackground = FlashbackTrackBackground,
            FlashbackScrubArea = FlashbackScrubArea,
            FlashbackPlayhead = FlashbackPlayhead,
            FlashbackLiveEdge = FlashbackLiveEdge,
            SnapPlayheadOnNextOpen = RequestFlashbackPlayheadSnapOnNextUpdate,
            StartStatusPolling = StartFlashbackStatusPolling,
            StopStatusPolling = StopFlashbackStatusPolling,
            ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,
        });
    }

    private void FlashbackToggle_Checked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleChecked();

    private void FlashbackToggle_Unchecked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleUnchecked();

    private void ApplyFlashbackTimelineVisibility(bool show)
        => _flashbackTimelineController.ApplyVisibility(show);

    private void ApplyFlashbackTimelineLockout()
        => _flashbackTimelineController.ApplyLockout();

    private void CollapseFlashbackTimelineImmediately()
        => _flashbackTimelineController.CollapseImmediately();

    private void InitializeFlashbackMarkerPresentationController()
    {
        _flashbackMarkerPresentationController = new FlashbackMarkerPresentationController(new FlashbackMarkerPresentationControllerContext
        {
            ScrubArea = FlashbackScrubArea,
            InPointMarker = FlashbackInPointMarker,
            OutPointMarker = FlashbackOutPointMarker,
            SelectionRegion = FlashbackSelectionRegion,
        });
    }

    private void UpdateFlashbackMarkers()
        => _flashbackMarkerPresentationController.UpdateMarkers(
            ViewModel.FlashbackBufferFilledDuration,
            ViewModel.FlashbackInPoint,
            ViewModel.FlashbackOutPoint);

    private void InitializeFlashbackPlaybackPresentationController()
    {
        _flashbackPlaybackPresentationController = new FlashbackPlaybackPresentationController(new FlashbackPlaybackPresentationControllerContext
        {
            PlayPauseIcon = FlashbackPlayPauseIcon,
            GoLiveButton = FlashbackGoLiveButton,
            BufferDurationText = FlashbackBufferDurationText,
            PlayheadTimeText = FlashbackPlayheadTimeText,
        });
    }

    private void InitializeFlashbackPlaybackUiCoordinator()
    {
        _flashbackPlaybackUiCoordinator = new FlashbackPlaybackUiCoordinator(new FlashbackPlaybackUiCoordinatorContext
        {
            ViewModel = ViewModel,
            ApplyTrackSize = _flashbackTimelineController.ApplyTrackSize,
            RequestPlayheadSnapOnNextUpdate = RequestFlashbackPlayheadSnapOnNextUpdate,
            UpdateMarkers = UpdateFlashbackMarkers,
            RefreshCtiMotion = RefreshFlashbackCtiMotion,
            IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,
            StartPlaybackPolling = StartFlashbackPlaybackPolling,
            StopPlaybackPolling = StopFlashbackPlaybackPolling,
            PlaybackPresentation = _flashbackPlaybackPresentationController,
        });
    }

    private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)
        => _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);

    private void UpdateFlashbackStateUI()
        => _flashbackPlaybackUiCoordinator.UpdateState();

    private void UpdateFlashbackBufferFill()
        => _flashbackPlaybackUiCoordinator.UpdateBufferFill();

    private void UpdateFlashbackPositionUI()
        => _flashbackPlaybackUiCoordinator.UpdatePosition();

    private void UpdateFlashbackBufferPresentation()
        => _flashbackPlaybackUiCoordinator.UpdateBufferPresentation();

    private void InitializeFlashbackExportProgressPresentationController()
    {
        _flashbackExportProgressPresentationController = new FlashbackExportProgressPresentationController(
            new FlashbackExportProgressPresentationControllerContext
            {
                FlashbackExportProgressBar = FlashbackExportProgressBar,
            });
    }

    private void UpdateFlashbackExportProgress(double progress)
        => _flashbackExportProgressPresentationController.UpdateProgress(progress);

    private void UpdateFlashbackExportingPresentation(bool isExporting)
        => _flashbackExportProgressPresentationController.UpdateExporting(isExporting);

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

internal sealed class MainWindowPropertyChangedRouterContext
{
    public required Func<string, bool> TryHandleCaptureSelection { get; init; }
    public required Func<string, bool> TryHandleStatusStrip { get; init; }
    public required Func<string, Task<bool>> TryHandlePreviewAsync { get; init; }
    public required Func<string, bool> TryHandleRecording { get; init; }
    public required Func<string, bool> TryHandleOutput { get; init; }
    public required Func<string, bool> TryHandleCaptureOption { get; init; }
    public required Func<string, bool> TryHandleAudio { get; init; }
    public required Func<string, bool> TryHandleShell { get; init; }
    public required Func<string, bool> TryHandleLiveSignal { get; init; }
    public required Func<string, bool> TryHandleFlashback { get; init; }
}

internal sealed class MainWindowPropertyChangedRouter
{
    private readonly MainWindowPropertyChangedRouterContext _context;

    public MainWindowPropertyChangedRouter(MainWindowPropertyChangedRouterContext context)
    {
        _context = context;
    }

    public async Task RouteAsync(string? propertyNameValue)
    {
        var propertyName = propertyNameValue ?? string.Empty;

        if (_context.TryHandleCaptureSelection(propertyName))
        {
            return;
        }

        if (_context.TryHandleStatusStrip(propertyName))
        {
            return;
        }

        if (await _context.TryHandlePreviewAsync(propertyName))
        {
            return;
        }

        if (_context.TryHandleRecording(propertyName))
        {
            return;
        }

        if (_context.TryHandleOutput(propertyName))
        {
            return;
        }

        if (_context.TryHandleCaptureOption(propertyName))
        {
            return;
        }

        if (_context.TryHandleAudio(propertyName))
        {
            return;
        }

        if (_context.TryHandleShell(propertyName))
        {
            return;
        }

        if (_context.TryHandleLiveSignal(propertyName))
        {
            return;
        }

        if (_context.TryHandleFlashback(propertyName))
        {
            return;
        }
    }
}
