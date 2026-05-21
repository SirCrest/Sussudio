using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FullScreenControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Grid RootGrid { get; init; }
    public required UIElement RootElement { get; init; }
    public required FrameworkElement RootFrameworkElement { get; init; }
    public required Border PreviewBorder { get; init; }
    public required Border PreviewShadowHost { get; init; }
    public required Grid PreviewContentGrid { get; init; }
    public required Border VideoShadowHost { get; init; }
    public required Border SettingsOverlayPanel { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required Border FlashbackTimelinePanel { get; init; }
    public required Border ControlBarShadowHost { get; init; }
    public required Border ControlBarBorder { get; init; }
    public required StackPanel FullScreenControlsOverlay { get; init; }
    public required Button FullScreenButton { get; init; }
    public required FontIcon FullScreenButtonIcon { get; init; }
    public required MenuFlyoutItem FullScreenMenuItem { get; init; }
    public required Func<AppWindow> GetAppWindow { get; init; }
    public required Func<Windows.System.VirtualKey, bool> HandleFlashbackKeyboardCommand { get; init; }
    public required Action EndFlashbackScrubForFullScreen { get; init; }
    public required Action ResetFlashbackTimelineAnimation { get; init; }
    public required Action ResetSettingsShelfAnimation { get; init; }
    public required Func<bool> ShouldShowFlashbackTimeline { get; init; }
    public required Action<bool> SyncFlashbackTimelineToggle { get; init; }
    public required Action HideStatsDockPanelImmediate { get; init; }
    public required Action ShowStatsDockPanel { get; init; }
    public required Action UpdateVideoContentOverlays { get; init; }
    public required Action FadeInVideoShadow { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
}

internal sealed partial class FullScreenController
{
    private const int AutoHideDelayMs = 3000;
    private const double HotZoneHeight = 150;

    private readonly FullScreenControllerContext _context;
    private bool _isFullScreen;
    private bool _isTransitioning;
    private Windows.Graphics.RectInt32 _preFullScreenBounds;
    private Windows.Graphics.PointInt32 _preFullScreenPosition;
    private bool _preFullScreenSettingsVisible;
    private bool _preFullScreenStatsDockVisible;
    private bool _controlsVisible;
    private DispatcherQueueTimer? _autoHideTimer;
    private bool _pointerOverControls;
    private Brush? _preFullScreenControlBarBackground;
    private Brush? _preFullScreenFlashbackTimelineBackground;

    public FullScreenController(FullScreenControllerContext context)
    {
        _context = context;
    }

    public bool IsFullScreen => _isFullScreen;

    public void Toggle()
    {
        if (_isFullScreen)
        {
            Exit();
        }
        else
        {
            Enter();
        }
    }

    public Task SetEnabledAsync(bool enabled)
        => enabled ? EnterAsync() : ExitAsync();

    public void Enter()
        => _ = RunTransitionAsync(EnterAsync, "enter");

    public void Exit()
        => _ = RunTransitionAsync(ExitAsync, "exit");
}
