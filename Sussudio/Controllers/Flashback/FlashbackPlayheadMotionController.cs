using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackPlayheadMotionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsScrubbing { get; init; }
    public required FrameworkElement ScrubArea { get; init; }
    public required FrameworkElement Playhead { get; init; }
    public required FrameworkElement PlayheadHandle { get; init; }
    public required FrameworkElement PlayheadTimeBorder { get; init; }
}

internal sealed partial class FlashbackPlayheadMotionController
{
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private readonly FlashbackPlayheadMotionControllerContext _context;
    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection = TimeSpan.FromMilliseconds(1000);

    public FlashbackPlayheadMotionController(FlashbackPlayheadMotionControllerContext context)
    {
        _context = context;
    }

    public void RequestSnapOnNextUpdate()
    {
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    public void PositionMagneticPlayhead(double x, double trackWidth)
    {
        PositionFlashbackPlayhead(x, trackWidth, FlashbackPlayheadMotion.Magnetic);
    }
}
