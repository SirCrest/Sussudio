using System;
using System.Collections.Generic;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing shell launch/chrome adapter. Focused controllers own native
// window bootstrap, launch entrance choreography, splash phrases, control-bar
// animations, settings shelf presentation, and static shell elevation.
public sealed partial class MainWindow
{
    private ControlBarAnimationController _controlBarAnimationController = null!;
    private readonly NativeWindowBootstrapController _nativeWindowBootstrapController = new();
    private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;
    private LaunchStartupController _launchStartupController = null!;
    private SettingsShelfController _settingsShelfController = null!;
    private ShellElevationController _shellElevationController = null!;
    private SplashLoadingPhraseController _splashLoadingPhraseController = null!;
    private IntPtr _hwnd;

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

    private void InitializeShellElevationController()
    {
        _shellElevationController = new ShellElevationController(new ShellElevationControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            SettingsOverlayPanel = SettingsOverlayPanel,
            RecordButton = RecordButton,
        });
    }

    private void InitializeSettingsShelfController()
    {
        _settingsShelfController = new SettingsShelfController(new SettingsShelfControllerContext
        {
            SettingsOverlayPanel = SettingsOverlayPanel,
        });
    }

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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        => _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));

    private void SetupButtonHoverAnimations()
        => _controlBarAnimationController.AttachHoverAnimations();

    private IReadOnlyList<FrameworkElement> GetEntranceButtons()
        => _controlBarAnimationController.EntranceButtons;

    private void ApplyShellElevation()
        => _shellElevationController.Apply();

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
        => _settingsShelfController.Toggle();

    private void ApplySettingsVisibility(bool visible)
        => _settingsShelfController.ApplyVisibility(visible);

    private void ShowSettingsShelf()
        => _settingsShelfController.Show();

    private void HideSettingsShelf()
        => _settingsShelfController.Hide();

    private void ResetSettingsShelfAnimationForFullScreen()
        => _settingsShelfController.ResetAnimationState();

    private void PrepareLaunchEntranceInitialState()
        => _launchEntranceAnimationController.PrepareInitialState();

    private void PlaySplashAndEntrance()
        => _launchEntranceAnimationController.PlaySplashAndEntrance();

    private void StartSplashLoadingPhrases()
        => _splashLoadingPhraseController.Start();

    private void StopSplashLoadingPhrases()
        => _splashLoadingPhraseController.Stop();

    private bool TryHandleShellPropertyChanged(string propertyName)
    {
        if (_statsOverlayCompositionController.TryHandlePropertyChanged(propertyName, ViewModel.IsStatsVisible))
        {
            return true;
        }

        if (_settingsShelfController.TryHandlePropertyChanged(propertyName, ViewModel.IsSettingsVisible))
        {
            return true;
        }

        return false;
    }
}
