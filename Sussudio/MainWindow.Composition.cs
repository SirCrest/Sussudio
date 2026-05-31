using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio;

public sealed partial class MainWindow
{
    private readonly NativeWindowBootstrapController _nativeWindowBootstrapController = new();
    private IntPtr _hwnd;
    private ControlBarAnimationController _controlBarAnimationController = null!;
    private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;
    private LaunchStartupController _launchStartupController = null!;
    private SettingsShelfController _settingsShelfController = null!;
    private ShellElevationController _shellElevationController = null!;
    private ShellPropertyChangedController _shellPropertyChangedController = null!;
    private SplashLoadingPhraseController _splashLoadingPhraseController = null!;
    private ControlBarLabelVisibilityController _controlBarLabelVisibilityController = null!;
    private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;
    private LiveSignalInfoController _liveSignalInfoController = null!;
    private StatsOverlayCompositionController _statsOverlayCompositionController = null!;
    private WindowTitleController _windowTitleController = null!;
    private StatusStripPresentationController _statusStripPresentationController = null!;
    private WindowUiDispatchController? _windowUiDispatchController;
    private WindowAutomationController _windowAutomationController = null!;
    private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();
    private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();
    private WindowCloseRequestController _windowCloseRequestController = null!;
    private WindowAppClosingController _windowAppClosingController = null!;
    private WindowScreenshotController _windowScreenshotController = null!;
    private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;

    private WindowUiDispatchController WindowUiDispatchController =>
        _windowUiDispatchController ??= new WindowUiDispatchController(
            new WindowUiDispatchControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                CompleteWindowCloseRequest = CompleteWindowCloseRequest
            });

    private AppWindow InitializeNativeShellWindow()
    {
        var result = _nativeWindowBootstrapController.Initialize(this, ViewModel.SetWindowHandle);
        _hwnd = result.Hwnd;
        return result.AppWindow;
    }

    private AppWindow GetAppWindow()
        => _nativeWindowBootstrapController.GetAppWindow(this);

    private void ScheduleNativeShellRevealAfterFirstFrame()
        => _nativeWindowBootstrapController.ScheduleRevealAfterFirstComposedFrame(_hwnd);

    private void CancelNativeShellRevealAfterFirstFrame()
        => _nativeWindowBootstrapController.CancelPendingFirstFrameReveal();

    private void InitializeWindowCloseRequestController()
    {
        _windowCloseRequestController = new WindowCloseRequestController(new WindowCloseRequestControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            CloseWindow = Close,
            ExitApplication = () => Application.Current.Exit(),
            IsRecording = () => ViewModel.IsRecording,
            IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning
        });

        _windowAppClosingController = new WindowAppClosingController(new WindowAppClosingControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            IsRecording = () => ViewModel.IsRecording,
            IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning,
            GetStatusText = () => ViewModel.StatusText,
            StopRecordingBeforeCloseAsync = TryStopRecordingBeforeCloseAsync,
            RequestWindowClose = RequestWindowClose
        });
    }

    private void InitializeWindowAutomationController()
    {
        _windowAutomationController = new WindowAutomationController(
            new WindowAutomationControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                GetAppWindow = GetAppWindow,
                GetWindowHandle = () => _hwnd,
                InvokeOnUiThreadAsync = InvokeOnUiThreadAsync
            });
    }

    private void InitializeWindowScreenshotController()
    {
        _windowScreenshotController = new WindowScreenshotController(
            _dispatcherQueue,
            () => _hwnd);
    }

    private void RegisterCloseLifecycle(AppWindow appWindow)
        => appWindow.Closing += MainWindow_Closing;

    private async void MainWindow_Closing(
        AppWindow sender,
        AppWindowClosingEventArgs args)
        => await _windowAppClosingController.HandleClosingAsync(args);

    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
        => WindowUiDispatchController.RunUiEventHandlerAsync(operation, operationName);

    private Task<bool> TryStopRecordingBeforeCloseAsync()
        => _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(
            ViewModel,
            Content as FrameworkElement,
            () => _windowCloseLifecycleController.IsAllowedAfterRecordingStop);

    public Task CloseAsync(CancellationToken cancellationToken = default)
        => _windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken);

    private void CompleteWindowCloseRequest(Exception? exception = null)
        => _windowCloseLifecycleController.CompleteRequest(exception);

    private void RequestWindowClose()
        => _windowCloseRequestController.RequestClose();

    public Task MinimizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MinimizeAsync(cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MaximizeAsync(cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.RestoreAsync(cancellationToken);

    public Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
        => _windowAutomationController.MoveToAsync(x, y, cancellationToken);

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
        => _windowAutomationController.ResizeToAsync(width, height, cancellationToken);

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
        => _windowAutomationController.SnapToRegionAsync(region, cancellationToken);

    public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
        => _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);

    private void InitializeControlBarAnimationController()
    {
        _controlBarAnimationController = new ControlBarAnimationController(new ControlBarAnimationControllerContext
        {
            ControlBarButtons = new FrameworkElement[]
            {
                SettingsToggleButton,
                OpenRecordingsButton,
                ScreenshotButton,
                RecordButton,
                PreviewButton,
                HdrToggle,
                AudioRecordToggle,
                TrueHdrPreviewToggle,
                AudioPreviewToggle,
                StatsToggle,
                FrameTimeOverlayToggle,
            },
        });
    }

    private void InitializeResponsiveShellLayoutController()
    {
        var controlBarLabels = new UIElement[]
        {
            HdrToggleLabel,
            AudioRecordToggleLabel,
            PreviewButtonLabel,
            HdrPreviewToggleLabel,
            AudioPreviewToggleLabel,
            StatsToggleLabel,
            FrameTimeOverlayToggleLabel,
            FlashbackToggleLabel,
        };

        _controlBarLabelVisibilityController = new ControlBarLabelVisibilityController(new ControlBarLabelVisibilityControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            ControlBarLabels = controlBarLabels,
        });

        _responsiveShellLayoutController = new ResponsiveShellLayoutController(new ResponsiveShellLayoutControllerContext
        {
            CaptureSettingsGrid = CaptureSettingsGrid,
            VideoFormatColumn = VideoFormatColumn,
            PresetColumn = PresetColumn,
            SplitColumn = SplitColumn,
            VideoFormatPanel = VideoFormatPanel,
            PresetPanel = PresetPanel,
            SplitPanel = SplitPanel,
            CustomBitratePanel = CustomBitratePanel,
        });
    }

    private void SetupResponsiveShellLayoutBindings()
    {
        _controlBarLabelVisibilityController.Attach();
        _responsiveShellLayoutController.Attach();
    }

    private void SetupButtonHoverAnimations()
        => _controlBarAnimationController.AttachHoverAnimations();

    private IReadOnlyList<FrameworkElement> GetEntranceButtons()
        => _controlBarAnimationController.EntranceButtons;

    private void InitializeLaunchEntranceAnimationController()
    {
        _launchEntranceAnimationController = new LaunchEntranceAnimationController(new LaunchEntranceAnimationControllerContext
        {
            SplashContent = SplashContent,
            SplashOverlay = SplashOverlay,
            SplashScale = SplashScale,
            ControlBarBorder = ControlBarBorder,
            StatsRow = StatsRow,
            PreviewBorder = PreviewBorder,
            PreviewBorderScale = PreviewBorderScale,
            GetEntranceButtons = GetEntranceButtons,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            StartSplashLoadingPhrases = StartSplashLoadingPhrases,
            StopSplashLoadingPhrases = StopSplashLoadingPhrases,
            AddPreviewShellEntranceAnimations = AddPreviewShellEntranceAnimations,
            FadeInControlBarShadow = () => FadeInControlBarShadow(delayMs: 400, durationMs: 500),
        });
    }

    private void PrepareLaunchEntranceInitialState()
        => _launchEntranceAnimationController.PrepareInitialState();

    private void PlaySplashAndEntrance()
        => _launchEntranceAnimationController.PlaySplashAndEntrance();

    private void InitializeLaunchStartupController()
    {
        _launchStartupController = new LaunchStartupController(new LaunchStartupControllerContext
        {
            MainContent = (FrameworkElement)Content,
            LoadedHandler = MainWindow_Loaded,
            ScheduleNativeShellRevealAfterFirstFrame = ScheduleNativeShellRevealAfterFirstFrame,
            RunUiEventHandlerAsync = RunUiEventHandlerAsync,
            InitializeViewModelAsync = ViewModel.InitializeAsync,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            RefreshDevicesAsync = () => ViewModel.RefreshDevicesAsync(),
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            StartAutomationHost = _automationHostLifecycleController.Start,
            PlaySplashAndEntrance = PlaySplashAndEntrance,
            Log = message => Logger.Log(message),
        });
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        => _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));

    private void InitializeSettingsShelfController()
    {
        _settingsShelfController = new SettingsShelfController(new SettingsShelfControllerContext
        {
            SettingsOverlayPanel = SettingsOverlayPanel,
        });
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
        => _settingsShelfController.Toggle();

    private void ApplySettingsVisibility(bool visible)
        => _settingsShelfController.ApplyVisibility(visible);

    private void ShowSettingsShelf()
        => _settingsShelfController.Show();

    private void HideSettingsShelf()
        => _settingsShelfController.Hide();

    private void InitializeShellElevationController()
    {
        _shellElevationController = new ShellElevationController(new ShellElevationControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            SettingsOverlayPanel = SettingsOverlayPanel,
            RecordButton = RecordButton,
        });
    }

    private void ApplyShellElevation()
        => _shellElevationController.Apply();

    private void InitializeShellPropertyChangedController()
    {
        _shellPropertyChangedController = new ShellPropertyChangedController(new ShellPropertyChangedControllerContext
        {
            StatsOverlayComposition = _statsOverlayCompositionController,
            SettingsShelf = _settingsShelfController,
            IsStatsVisible = () => ViewModel.IsStatsVisible,
            IsSettingsVisible = () => ViewModel.IsSettingsVisible,
        });
    }

    private bool TryHandleShellPropertyChanged(string propertyName)
        => _shellPropertyChangedController.TryHandlePropertyChanged(propertyName);

    private void InitializeStatsOverlayCompositionController()
    {
        _statsOverlayCompositionController = new StatsOverlayCompositionController(new StatsOverlayCompositionControllerContext
        {
            Shell = CreateStatsOverlayShellContext(),
            SnapshotSources = CreateStatsOverlaySnapshotSourceContext(),
            DockTargets = CreateStatsOverlayDockTargetsContext(),
            HardwareSources = CreateStatsOverlayHardwareSourceContext(),
            FrameTimeTargets = CreateStatsOverlayFrameTimeTargetsContext(),
        });
    }

    private StatsOverlayShellContext CreateStatsOverlayShellContext()
        => new()
        {
            DispatcherQueue = _dispatcherQueue,
            StatsToggle = StatsToggle,
            StatsDockPanel = StatsDockPanel,
            FrameTimeOverlay = FrameTimeOverlay,
            FrameTimeOverlayToggle = FrameTimeOverlayToggle,
            IsWindowClosing = () => _isWindowClosing,
            SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,
            Log = message => Logger.Log(message),
        };

    private StatsOverlaySnapshotSourceContext CreateStatsOverlaySnapshotSourceContext()
        => new()
        {
            GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,
            GetRenderer = () => _previewRendererHostController.Renderer,
            GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs,
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsRecording = () => ViewModel.IsRecording,
        };

    private StatsSnapshot GetStatsSnapshot()
        => _statsOverlayCompositionController.GetStatsSnapshot();

    private StatsOverlayDockTargetsContext CreateStatsOverlayDockTargetsContext()
        => new()
        {
            DiagnosticsContent = Diagnostics_Content,
            SessionStateValue = Stats_SessionStateValue,
            SummaryCaptureValue = Stats_SummaryCaptureValue,
            SummaryPreviewValue = Stats_SummaryPreviewValue,
            SummaryRendererFpsValue = Stats_SummaryRendererFpsValue,
            SummaryVisualFpsValue = Stats_SummaryVisualFpsValue,
            SummaryLatencyValue = Stats_SummaryLatencyValue,
            SourceResolutionValue = Stats_SourceResolutionValue,
            SourceFrameRateValue = Stats_SourceFrameRateValue,
            SourceHdrValue = Stats_SourceHdrValue,
            SourceFormatValue = Stats_SourceFormatValue,
            TelemetryOriginValue = Stats_TelemetryOriginValue,
            AdcOnOffValue = Stats_AdcOnOffValue,
            AdcGainValue = Stats_AdcGainValue,
            SourceFpsValue = Stats_SourceFpsValue,
            SourceExpectedFpsValue = Stats_SourceExpectedFpsValue,
            SourceAvgValue = Stats_SourceAvgValue,
            SourceP95Value = Stats_SourceP95Value,
            SourceJitterValue = Stats_SourceJitterValue,
            SourceGapsValue = Stats_SourceGapsValue,
            SourceDropsValue = Stats_SourceDropsValue,
            PreviewFpsValue = Stats_PreviewFpsValue,
            PreviewAvgValue = Stats_PreviewAvgValue,
            PreviewP95Value = Stats_PreviewP95Value,
            PreviewSlowValue = Stats_PreviewSlowValue,
            VisualFpsValue = Stats_VisualFpsValue,
            VisualMotionValue = Stats_VisualMotionValue,
            PipelineLatencyValue = Stats_PipelineLatencyValue,
            SourceDeliveredValue = Stats_SourceDeliveredValue,
            SourceDroppedValue = Stats_SourceDroppedValue,
            RendererRenderedValue = Stats_RendererRenderedValue,
            RendererDroppedValue = Stats_RendererDroppedValue,
            PerformanceScoreValue = Stats_PerfScoreValue,
            AvSyncDriftValue = Stats_AvSyncDriftValue,
            AvSyncDriftRateValue = Stats_AvSyncDriftRateValue,
            AvSyncEncoderRow = Stats_AvSyncEncoderRow,
            AvSyncEncoderValue = Stats_AvSyncEncoderValue,
            EncoderSection = EncoderSection,
            EncoderCodecValue = Stats_EncoderCodecValue,
            EncoderResolutionValue = Stats_EncoderResolutionValue,
            EncoderFrameRateValue = Stats_EncoderFrameRateValue,
            EncoderBitrateValue = Stats_EncoderBitrateValue,
            DecodeSection = DecodeSection,
            DecodeContent = Decode_Content,
            GpuContent = GPU_Content,
        };

    private StatsOverlayHardwareSourceContext CreateStatsOverlayHardwareSourceContext()
        => new()
        {
            GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = () => _previewRendererHostController.PendingFrameCount,
            GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot(),
        };

    private StatsOverlayFrameTimeTargetsContext CreateStatsOverlayFrameTimeTargetsContext()
        => new()
        {
            FrameTimeSourceValue = FrameTime_SourceValue,
            FrameTimeVisualValue = FrameTime_VisualValue,
            FrameTimePreviewValue = FrameTime_PreviewValue,
            FrameTimeLatencyValue = FrameTime_LatencyValue,
            FrameTimeStatusValue = FrameTime_StatusValue,
            FrameTimeCanvas = FrameTime_Canvas,
            FrameTimeVisualLine = FrameTime_VisualLine,
            FrameTimePreviewLine = FrameTime_PreviewLine,
            FrameTimeExpectedLine = FrameTime_ExpectedLine,
        };

    private void AttachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.AttachToggleBindings();

    private void DetachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.DetachToggleBindings();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayCompositionController.ApplyStatsVisibility(visible, immediate);

    private void StartStatsDockPolling()
        => _statsOverlayCompositionController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayCompositionController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayCompositionController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayCompositionController.HideDockPanel(immediate);

    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
        => _statsOverlayCompositionController.ToggleSectionFromHeader(sender);

    private void SetStatsSectionVisible(string section, bool visible)
        => _statsOverlayCompositionController.SetSectionVisible(section, visible);

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayCompositionController.SetFrameTimeOverlayVisible(visible);

    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayCompositionController.IsFrameTimeOverlayVisible;

    private void InitializeLiveSignalInfoController()
    {
        _liveSignalInfoController = new LiveSignalInfoController(new LiveSignalInfoControllerContext
        {
            DispatcherQueue = DispatcherQueue,
            LiveSignalInfoPanel = LiveSignalInfoPanel,
            LiveSignalInfoScale = LiveSignalInfoScale,
            LiveResolutionTextBlock = LiveResolutionTextBlock,
            LiveFrameRateTextBlock = LiveFrameRateTextBlock,
            LivePixelFormatTextBlock = LivePixelFormatTextBlock,
        });
    }

    private void InitializeWindowTitleController()
        => _windowTitleController = new WindowTitleController();

    private void InitializeStatusStripPresentationController()
    {
        _statusStripPresentationController = new StatusStripPresentationController(new StatusStripPresentationControllerContext
        {
            DiskWarningInfoBar = DiskWarningInfoBar,
            StatusTextBlock = StatusTextBlock,
            RecordingTimeTextBlock = RecordingTimeTextBlock,
            DiskSpaceTextBlock = DiskSpaceTextBlock,
            RecordingSizeTextBlock = RecordingSizeTextBlock,
            RecordingBitrateTextBlock = RecordingBitrateTextBlock,
        });
    }

    private void ApplyInitialStatusStripPresentation()
        => _statusStripPresentationController.ApplyInitial(BuildStatusStripPresentationSnapshot());

    private void UpdateLiveSignalInfoVisibility()
        => _liveSignalInfoController.Update(
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);

    private void StopLiveSignalInfoTimers()
        => _liveSignalInfoController.StopTimers();

    private bool TryHandleLiveSignalPropertyChanged(string propertyName)
        => _liveSignalInfoController.TryHandlePropertyChanged(
            propertyName,
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);

    private bool TryHandleStatusStripPropertyChanged(string? propertyName)
        => _statusStripPresentationController.TryHandlePropertyChanged(
            propertyName,
            BuildStatusStripPresentationSnapshot(),
            ApplyWindowTitle);

    private StatusStripPresentationSnapshot BuildStatusStripPresentationSnapshot()
        => new(
            ViewModel.StatusText,
            ViewModel.RecordingTime,
            ViewModel.DiskSpaceInfo,
            ViewModel.RecordingSizeInfo,
            ViewModel.RecordingBitrateInfo,
            ViewModel.FlashbackBitrateInfo,
            ViewModel.IsDiskWarningActive,
            ViewModel.IsRecording,
            ViewModel.IsFlashbackEnabled);

    private void UpdateStatusTextPresentation()
        => _statusStripPresentationController.UpdateStatusText(ViewModel.StatusText);

    private void UpdateRecordingTimePresentation()
        => _statusStripPresentationController.UpdateRecordingTime(ViewModel.RecordingTime);

    private void UpdateDiskSpacePresentation()
        => _statusStripPresentationController.UpdateDiskSpace(ViewModel.DiskSpaceInfo);

    private void UpdateRecordingSizePresentation()
        => _statusStripPresentationController.UpdateRecordingSize(ViewModel.RecordingSizeInfo);

    private void UpdateRecordingBitratePresentation()
        => _statusStripPresentationController.UpdateRecordingBitrate(ViewModel.RecordingBitrateInfo);

    private void UpdateDiskWarningPresentation()
        => _statusStripPresentationController.UpdateDiskWarning(ViewModel.IsDiskWarningActive);

    private void ApplyWindowTitle()
        => Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);

    private void InitializeSplashLoadingPhraseController()
    {
        _splashLoadingPhraseController = new SplashLoadingPhraseController(new SplashLoadingPhraseControllerContext
        {
            SplashLoadingTextA = SplashLoadingTextA,
            SplashLoadingTextB = SplashLoadingTextB,
            SplashLoadingTransformA = SplashLoadingTransformA,
            SplashLoadingTransformB = SplashLoadingTransformB,
        });
    }

    private void StartSplashLoadingPhrases()
        => _splashLoadingPhraseController.Start();

    private void StopSplashLoadingPhrases()
        => _splashLoadingPhraseController.Stop();

    private void InitializeFullScreenController()
    {
        ElementCompositionPreview.SetIsTranslationEnabled(FullScreenControlsOverlay, true);

        _fullScreenController = new FullScreenController(new FullScreenControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            RootGrid = (Grid)Content,
            RootElement = (UIElement)Content,
            RootFrameworkElement = (FrameworkElement)Content,
            PreviewBorder = PreviewBorder,
            PreviewShadowHost = PreviewShadowHost,
            PreviewContentGrid = PreviewContentGrid,
            VideoShadowHost = VideoShadowHost,
            SettingsOverlayPanel = SettingsOverlayPanel,
            StatsDockPanel = StatsDockPanel,
            FlashbackTimelinePanel = FlashbackTimelinePanel,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
            FullScreenControlsOverlay = FullScreenControlsOverlay,
            FullScreenButton = FullScreenButton,
            FullScreenButtonIcon = FullScreenButtonIcon,
            FullScreenMenuItem = FullScreenMenuItem,
            GetAppWindow = GetAppWindow,
            HandleFlashbackKeyboardCommand = _flashbackCommandController.HandleFullScreenKeyboardCommand,
            EndFlashbackScrubForFullScreen = _flashbackScrubInteractionController.EndForFullScreen,
            ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,
            ResetSettingsShelfAnimation = _settingsShelfController.ResetAnimationState,
            SyncFlashbackTimelineToggle = _flashbackTimelineController.SyncToggle,
            HideStatsDockPanelImmediate = () => HideStatsDockPanel(immediate: true),
            ShowStatsDockPanel = ShowStatsDockPanel,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            FadeInVideoShadow = () => FadeInVideoFrameShadow(delayMs: 0, durationMs: 400),
            IsWindowClosing = () => _isWindowClosing,
        });
    }

    private void PreviewBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
        => _fullScreenController.Toggle();

    public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(
            () => _fullScreenController.SetEnabledAsync(enabled),
            cancellationToken);

    private void EnterFullScreen()
        => _fullScreenController.Enter();

    private void ExitFullScreen()
        => _fullScreenController.Exit();

    private Task EnterFullScreenAsync()
        => _fullScreenController.EnterAsync();

    private Task ExitFullScreenAsync()
        => _fullScreenController.ExitAsync();

    private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
        => _fullScreenController.OnKeyDown(e);

    private void OnFullScreenPointerActivity(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnPointerActivity(e);

    private void OnFullScreenControlsPointerEntered(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerEntered();

    private void OnFullScreenControlsPointerExited(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerExited(e);

    private void StopFullScreenAutoHideTimer()
        => _fullScreenController.StopAutoHideTimer();

    // Preview lifecycle, renderer, startup, and transition adapter wiring shares the MainWindow composition surface; behavior stays in named controllers.
private PreviewAudioFadeController _previewAudioFadeController = null!;
    private PreviewButtonActionController _previewButtonActionController = null!;
    private PreviewButtonPresentationController _previewButtonPresentationController = null!;
    private PreviewFadeInController _previewFadeInController = null!;
    private PreviewLifecycleEventController _previewLifecycleEventController = null!;
    private PreviewRendererHostController _previewRendererHostController = null!;
    private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;
    private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;
    private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;
    private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;
    private PreviewStartupSessionController _previewStartupSessionController = null!;
    private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;
    private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;

    private void InitializePreviewButtonPresentationController()
    {
        _previewButtonPresentationController = new PreviewButtonPresentationController(new PreviewButtonPresentationControllerContext
        {
            PreviewButton = PreviewButton,
            PreviewButtonIcon = PreviewButtonIcon,
        });
    }

    private void ShowStopPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStopPreview();

    private void ShowStartPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStartPreview();

    // XAML-facing preview surface adapter. Surface and shadow behavior stays in
    // focused controllers; MainWindow only wires XAML elements to them.
    private void InitializePreviewSurfacePresentationController()
    {
        _previewSurfaceShadowController = new PreviewSurfaceShadowController(new PreviewSurfaceShadowControllerContext
        {
            VideoShadowHost = VideoShadowHost,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
        });

        _previewSurfacePresentationController = new PreviewSurfacePresentationController(
            new PreviewSurfacePresentationControllerContext
            {
                GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
                PreviewContentGrid = PreviewContentGrid,
                RecordingGlowBorder = RecordingGlowBorder,
            },
            _previewSurfaceShadowController);
    }

    private void InitializePreviewLifecycleEventController()
    {
        _previewLifecycleEventController = new PreviewLifecycleEventController(new PreviewLifecycleEventControllerContext
        {
            ViewModel = ViewModel,
            ShouldBeginPreviewStartupAttempt = () => ShouldBeginPreviewStartupAttempt,
            BeginPreviewStartupAttempt = BeginPreviewStartupAttempt,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            PreparePreviewStartupPresentation = PreparePreviewStartupPresentation,
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            StartPreviewStartupWatchdog = StartPreviewStartupWatchdog,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
            SetPreviewStartupState = SetPreviewStartupState,
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            StartPreviewRendererAsync = StartPreviewRendererAsync,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ShowStopPreviewButtonPresentation = ShowStopPreviewButtonPresentation,
            ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            StopPreviewRendererAsync = StopPreviewRendererAsync,
            ResetPreviewStartupTracking = preserveReinitAnimation => ResetPreviewStartupTracking(
                preserveReinitAnimation: preserveReinitAnimation),
            HandlePreviewReinitializingChanged = HandlePreviewReinitializingChanged,
        });
    }

    private void InitializePreviewRendererHostController()
    {
        _previewRendererHostController = new PreviewRendererHostController(new PreviewRendererHostControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
            SetPreviewSwapChainPanel = panel => PreviewSwapChainPanel = panel,
            PreviewContentGrid = PreviewContentGrid,
            PreviewImage = PreviewImage,
            PreviewContentGridSizeChangedHandler = OnPreviewContentGridSizeChanged,
            PreviewSwapChainPanelSizeChangedHandler = OnPreviewSwapChainPanelSizeChanged,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            ClearPreviewReinitAnimatingForShutdown = () =>
            {
                _previewReinitTransitionController.Clear(nameof(StopPreviewForShutdown));
            },
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            ConfirmPreviewFirstVisual = ConfirmPreviewFirstVisual,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ClearVideoFrameShadow = ClearVideoFrameShadow,
            SetupVideoFrameShadow = SetupVideoFrameShadow,
            SetGpuPreviewVisibility = SetGpuPreviewVisibility,
            ResetPreviewSignalState = ResetPreviewSignalState,
            ResetPreviewResizeTelemetry = ResetPreviewResizeTelemetry,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            MarkPreviewRendererAttached = MarkPreviewRendererAttached,
            ConfigurePreviewStartupSignals = ConfigurePreviewStartupSignals,
            Log = message => Logger.Log(message)
        });
    }

    private void InitializePreviewResizeTelemetryController()
    {
        _previewResizeTelemetryController = new PreviewResizeTelemetryController();
    }

    private void InitializePreviewRuntimeSnapshotSamplingController()
    {
        _previewRuntimeSnapshotSamplingController = new PreviewRuntimeSnapshotSamplingController(new PreviewRuntimeSnapshotSamplingControllerContext
        {
            UiDispatchController = WindowUiDispatchController,
            ViewModel = ViewModel,
            RendererHostController = _previewRendererHostController,
            StartupSessionController = _previewStartupSessionController,
            StartupSignalCoordinator = _previewStartupSignalCoordinator,
            IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,
            IsCpuElementVisible = () => PreviewImage.Visibility == Visibility.Visible,
            IsPlaceholderVisible = () => NoDevicePlaceholder.Visibility == Visibility.Visible,
            GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs
        });
    }

    private Task StartPreviewRendererAsync()
        => _previewRendererHostController.StartAsync();

    private Task StopPreviewRendererAsync()
        => _previewRendererHostController.StopAsync();

    private void StopPreviewForShutdown()
        => _previewRendererHostController.StopForShutdown();

    public long RendererReinitUnsafeWindows
        => _previewRendererHostController.RendererReinitUnsafeWindows;

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _previewResizeTelemetryController.HandleSizeChanged(
            ViewModel.IsPreviewing,
            _previewRendererHostController.HasD3DRenderer,
            PreviewSwapChainPanel.Visibility);
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateVideoContentOverlays();

    private void UpdateVideoContentOverlays()
        => _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);

    private void SetupVideoFrameShadow()
        => _previewSurfaceShadowController.SetupVideoFrameShadow();

    private void SetupControlBarShadow()
        => _previewSurfaceShadowController.SetupControlBarShadow();

    private void SetGpuPreviewVisibility(Visibility visibility)
        => _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);

    private void ClearVideoFrameShadow()
        => _previewSurfaceShadowController.ClearVideoFrameShadow();

    private void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);

    private void FadeOutVideoFrameShadow(int durationMs)
        => _previewSurfaceShadowController.FadeOutVideoFrameShadow(durationMs);

    private void FadeInControlBarShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInControlBarShadow(delayMs, durationMs);

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();

    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

    private bool IsPreviewStopRequestedByUser
        => _previewLifecycleEventController.StopRequestedByUser;

    private void SetPreviewStopRequestedByUser(bool value)
        => _previewLifecycleEventController.SetStopRequestedByUser(value);

    private Task<bool> TryHandlePreviewPropertyChangedAsync(string propertyName)
        => _previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);

    private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStartRequested();

    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStopRequested();

    private void InitializePreviewAudioFadeController()
    {
        _previewAudioFadeController = new PreviewAudioFadeController(new PreviewAudioFadeControllerContext
        {
            ViewModel = ViewModel,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
        });
    }

    private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;

    private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;

    private void PrimePreviewAudioFadeIn()
        => _previewAudioFadeController.PrimeFadeIn();

    private void StartPreviewAudioFadeIn(int durationMs = 900)
        => _previewAudioFadeController.StartFadeIn(durationMs);

    private Task StartPreviewAudioFadeOutAsync(int durationMs = 450)
        => _previewAudioFadeController.StartFadeOutAsync(durationMs);

    private void CancelPreviewAudioFadeInForUser()
        => _previewAudioFadeController.CancelFadeInForUser();

    private void InitializePreviewButtonActionController()
    {
        _previewButtonActionController = new PreviewButtonActionController(new PreviewButtonActionControllerContext
        {
            ViewModel = ViewModel,
            SetPreviewStopRequestedByUser = SetPreviewStopRequestedByUser,
            GetPreviewStartupAttemptId = () => PreviewStartupAttemptId,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewAudioFadeOutAsync = () => StartPreviewAudioFadeOutAsync(),
            AnimatePreviewOutAsync = AnimatePreviewOutAsync,
            ClearPreviewReinitAnimation = operationName =>
            {
                _previewReinitTransitionController.Clear(operationName, operationName: operationName);
            },
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
        });
    }

    private Task TogglePreviewFromButtonAsync()
        => _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));
    }

    private void InitializePreviewFadeInController()
    {
        _previewFadeInController = new PreviewFadeInController(new PreviewFadeInControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            GetRenderer = () => _previewRendererHostController.Renderer,
            AnimatePreviewInAsync = AnimatePreviewInAsync,
            StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),
        });
    }

    private void SchedulePreviewFadeIn()
        => _previewFadeInController.Schedule();

    private void StopPreviewFadeInTimer()
        => _previewFadeInController.Stop();

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
        });
    }

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);

    private void InitializePreviewTransitionAnimationController()
    {
        _previewTransitionAnimationController = new PreviewTransitionAnimationController(new PreviewTransitionAnimationControllerContext
        {
            PreviewBorder = PreviewBorder,
            PreviewBorderScale = PreviewBorderScale,
            PreviewContentGrid = PreviewContentGrid,
            PreviewContentScale = PreviewContentScale,
            NoDevicePlaceholder = NoDevicePlaceholder,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewStartupOverlay = StartPreviewStartupOverlay,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
            FadeOutVideoFrameShadow = FadeOutVideoFrameShadow,
            FadeInVideoFrameShadow = FadeInVideoFrameShadow,
        });
    }

    private void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
        => _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);

    private void ResetPreviewContentTransform()
        => _previewTransitionAnimationController.ResetPreviewContentTransform();

    private Task AnimatePreviewOutAsync()
        => _previewTransitionAnimationController.AnimatePreviewOutAsync();

    private Task AnimatePreviewInAsync()
        => _previewTransitionAnimationController.AnimatePreviewInAsync();

    private void PreparePreviewStartupPresentation()
        => _previewTransitionAnimationController.PrepareStartupPresentation();

    private void RevealPreviewUnavailablePlaceholder()
        => _previewTransitionAnimationController.RevealUnavailablePlaceholder();

    private void InitializePreviewReinitTransitionController()
        => _previewReinitTransitionController = new PreviewReinitTransitionController();

    private bool IsPreviewReinitAnimating
        => _previewReinitTransitionController.IsAnimating;

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _previewReinitTransitionController.BeginAnimateOut(reason, nameof(ViewModel_PreviewReinitRequested));
        await AnimatePreviewOutAsync();
    }

    private Task ViewModel_PreviewRendererStopRequested()
        => _previewRendererHostController.StopRendererForReinitTeardownAsync();

    private void HandlePreviewReinitializingChanged()
        => _previewReinitTransitionController.HandleReinitializingChanged(
            new PreviewReinitCompletionPresentationContext
            {
                IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,
                IsPreviewing = ViewModel.IsPreviewing,
                IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
                AttemptLabel = PreviewStartupAttemptLabel,
                CallerName = nameof(HandleViewModelPropertyChangedAsync),
                UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
                RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
                StopPreviewStartupOverlay = StopPreviewStartupOverlay,
                ResetPreviewContentTransform = ResetPreviewContentTransform,
                ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            });

    private void InitializePreviewStartupSessionController()
        => _previewStartupSessionController = new PreviewStartupSessionController(new PreviewStartupSessionControllerContext
        {
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            GetSelectedDeviceName = () => ViewModel.SelectedDevice?.Name,
            ResetSignalState = ResetPreviewSignalState,
            ResetFailureStopSchedule = ResetPreviewStartupFailureStopSchedule,
            MarkFirstVisualSignalConfirmed = MarkPreviewStartupFirstVisualConfirmed,
            StopWatchdog = StopPreviewStartupWatchdog,
            StopOverlay = StopPreviewStartupOverlay,
            StopFadeInTimer = StopPreviewFadeInTimer,
            ScheduleFadeIn = SchedulePreviewFadeIn,
            CompleteFirstVisualTransition = (attemptLabel, callerName) =>
                _previewReinitTransitionController.CompleteFirstVisualTransition(attemptLabel, callerName),
            ClearReinitTransitionForStartupReset = (preserveReinitAnimation, callerName) =>
                _previewReinitTransitionController.ClearForStartupReset(preserveReinitAnimation, callerName),
            Log = message => Logger.Log(message),
            CreateAttemptId = () => Guid.NewGuid().ToString("N"),
            GetUtcNow = () => DateTimeOffset.UtcNow
        });

    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptLabel;

    private string? PreviewStartupAttemptId
        => _previewStartupSessionController.AttemptId;

    private DateTimeOffset? PreviewStartupRequestedUtc
        => _previewStartupSessionController.RequestedUtc;

    private string? PreviewStartupMissingSignals
    {
        get => _previewStartupSessionController.MissingSignals;
        set => _previewStartupSessionController.SetMissingSignals(value);
    }

    private int PreviewStartupRecoveryAttemptCount
        => _previewStartupSessionController.RecoveryAttemptCount;

    private string? PreviewStartupLastFailureReason
        => _previewStartupSessionController.LastFailureReason;

    private bool IsPreviewFirstVisualConfirmed
        => _previewStartupSessionController.FirstVisualConfirmed;

    private bool ShouldBeginPreviewStartupAttempt
        => _previewStartupSessionController.ShouldBeginAttempt;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
        => _previewStartupSessionController.SetStartupState(state, reason);

    private void MarkPreviewRendererAttached()
        => _previewStartupSessionController.MarkRendererAttached(DateTimeOffset.UtcNow);

    private void BeginPreviewStartupAttempt()
        => _previewStartupSessionController.BeginStartupAttempt();

    private void ConfirmPreviewFirstVisual(string source)
        => _previewStartupSessionController.ConfirmFirstVisual(source);

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
        => _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);

    private void InitializePreviewStartupSignalCoordinator()
        => _previewStartupSignalCoordinator = new PreviewStartupSignalCoordinator(new PreviewStartupSignalCoordinatorContext
        {
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            Log = message => Logger.Log(message),
            ConfirmFirstVisual = ConfirmPreviewFirstVisual,
            GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState
        });

    private PreviewStartupReadinessSignalSnapshot PreviewStartupSignalSnapshot
        => _previewStartupSignalCoordinator.Snapshot;

    private bool _previewGpuSignalMediaOpened => PreviewStartupSignalSnapshot.GpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame => PreviewStartupSignalSnapshot.GpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing => PreviewStartupSignalSnapshot.GpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals => PreviewStartupSignalSnapshot.RequiredSignals;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals => PreviewStartupSignalSnapshot.ReceivedSignals;
    private PreviewStartupStrategy _previewStartupStrategy => PreviewStartupSignalSnapshot.Strategy;
    private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;

    private bool IsPreviewStartupSignalWindowActive()
        => _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);

    private void ResetPreviewSignalState()
        => _previewStartupSignalCoordinator.Reset();

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
        => _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);

    private string BuildPreviewStartupMissingSignals()
        => _previewStartupSignalCoordinator.BuildMissingSignals();

    private void MarkPreviewStartupFirstVisualConfirmed()
        => _previewStartupSignalCoordinator.MarkFirstVisualConfirmed();

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignal(signal, signalName);

    private void MarkGpuStartupSignalMediaOpened()
        => MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");

    private void MarkGpuStartupSignalFirstFrame()
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalFirstFrame();

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalPlaybackAdvancing(position);

    private void LogPreviewStartupPlaybackSnapshot(string reason)
        => _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);

    private PreviewStartupPlaybackSnapshotState GetPreviewStartupPlaybackSnapshotState()
    {
        var renderer = _previewRendererHostController.Renderer;
        return new PreviewStartupPlaybackSnapshotState(
            renderer != null,
            renderer?.IsRendering == true,
            PreviewSwapChainPanel.Visibility.ToString());
    }

    private int PreviewStartupVisualTimeoutMs => _previewStartupWatchdogController.VisualTimeoutMs;

    private void InitializePreviewStartupWatchdogController()
        => _previewStartupWatchdogController = new PreviewStartupWatchdogController(new PreviewStartupWatchdogControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            IsWaitingForFirstVisual = () => _previewStartupSessionController.IsWaitingForFirstVisual,
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsWindowClosing = () => _isWindowClosing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            IsPreviewing = () => ViewModel.IsPreviewing,
            GetElapsedMilliseconds = () => _previewStartupSessionController.GetElapsedMilliseconds(DateTimeOffset.UtcNow),
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            BuildMissingSignals = BuildPreviewStartupMissingSignals,
            GetMissingSignals = () => PreviewStartupMissingSignals,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            GetTimeoutDiagnosticSnapshot = GetPreviewStartupTimeoutDiagnosticSnapshot,
            LogPlaybackSnapshot = LogPreviewStartupPlaybackSnapshot,
            StopStartupOverlay = StopPreviewStartupOverlay,
            SetStatusText = value => ViewModel.StatusText = value,
            StopPreviewForFailureAsync = _ => ViewModel.StopPreviewAsync(userInitiated: true, teardownPipeline: true),
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });

    private void StopPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Stop();

    private void StartPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Start();

    private void SchedulePreviewStartupFailureStop(string reason)
        => _previewStartupWatchdogController.ScheduleFailureStop(reason);

    private void ResetPreviewStartupFailureStopSchedule()
        => _previewStartupWatchdogController.ResetFailureStopSchedule();

    private PreviewStartupTimeoutDiagnosticSnapshot GetPreviewStartupTimeoutDiagnosticSnapshot()
        => new(
            NoDevicePlaceholder.Visibility.ToString(),
            PreviewSwapChainPanel.Visibility.ToString(),
            PreviewImage.Visibility.ToString(),
            _previewStartupStrategy,
            _previewStartupRequiredSignals,
            _previewStartupReceivedSignals,
            PreviewStartupMissingSignals);
}
