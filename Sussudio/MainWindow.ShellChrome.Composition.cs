using System;
using System.Collections.Generic;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

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
}
