using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
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
}
