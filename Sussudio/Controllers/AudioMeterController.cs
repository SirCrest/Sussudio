using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed partial class AudioMeterController
{
    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;

    private readonly AudioMeterControllerContext _context;
    private double _audioPeakHoldLevel;
    private long _audioPeakHoldTimestamp;
    private double _audioRangeMin = 1.0;
    private double _audioRangeMax;
    private long _audioRangeResetTimestamp;
    private double _audioMeterDisplayLevel;
    private double _audioMeterTargetLevel;
    private double _micMeterDisplayLevel;
    private double _micMeterTargetLevel;
    private LinearGradientBrush? _audioMeterColorBrush;
    private DispatcherQueueTimer? _audioMeterAnimationTimer;
    private Storyboard? _audioMeterMonitoringStoryboard;

    public AudioMeterController(AudioMeterControllerContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        _audioMeterAnimationTimer = _context.DispatcherQueue.CreateTimer();
        _audioMeterAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _audioMeterAnimationTimer.IsRepeating = true;
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateTick();

        _audioMeterColorBrush = (LinearGradientBrush)_context.AudioMeterFill.Background;

        SetupRoundedContentClip(_context.AudioMeterContent, 3f);
        SetupRoundedContentClip(_context.MicMeterContent, 3f);
    }
}
