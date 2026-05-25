using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
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
}
