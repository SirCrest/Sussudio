using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
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
