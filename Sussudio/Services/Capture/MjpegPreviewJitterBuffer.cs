using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Smooths decoded MJPEG preview frames before they hit the renderer. 4K120
// decode workers can emit in bursts, so this thread paces frame submission
// against the expected display cadence and drops stale frames instead of
// letting preview latency grow without bound.
internal sealed partial class MjpegPreviewJitterBuffer : IDisposable
{
    private enum DequeueMissReason
    {
        None,
        EmptyQueue,
        WaitingForSequence
    }

    public delegate void PreviewFrameProbe(
        ReadOnlySpan<byte> frame,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        long arrivalTick,
        long sequenceNumber);

    private readonly object _sync = new();
    private readonly List<BufferedFrame> _frames = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Func<IPreviewFrameSink?> _getPreviewSink;
    private readonly Func<bool> _isPreviewSuppressed;
    private readonly PreviewFrameProbe? _previewFrameProbe;
    private readonly double _fps;
    private readonly long _frameIntervalTicks;
    private const int DefaultMinAdaptiveTargetDepth = 2;
    private const int DefaultMaxAdaptiveTargetDepth = 8;
    private const int DefaultExtraQueueDepth = 4;
    private const int SoftDeadlineExtraFrames = 2;
    private const int HardDeadlineExtraFrames = 4;
    private const int FastCatchUpSurplusFrames = 2;
    private const int AggressiveCatchUpSurplusFrames = 4;
    private const double LateScheduleResetFrames = 0.5;

    // The adaptive target is a small queue-depth range, not a general-purpose
    // video buffer. It only absorbs decode jitter; hard deadlines below keep
    // preview real-time when the decoder falls behind.
    private int _minAdaptiveTargetDepth;
    private int _maxAdaptiveTargetDepth;
    private int _targetDepth;
    private readonly int _maxDepth;
    private readonly bool _timerResolutionRaised;
    private readonly Thread _thread;
    private readonly double[] _inputIntervalsMs = new double[600];
    private readonly double[] _outputIntervalsMs = new double[600];
    private readonly double[] _queueLatencyMs = new double[600];
    private int _inputIntervalCount;
    private int _inputIntervalIndex;
    private int _outputIntervalCount;
    private int _outputIntervalIndex;
    private int _queueLatencyCount;
    private int _queueLatencyIndex;
    private long _lastInputTick;
    private long _lastOutputTick;
    private long _nextPreviewSequence = -1;
    private long _totalQueued;
    private long _totalSubmitted;
    private long _totalDropped;
    private long _deadlineDropCount;
    private long _clearedDropCount;
    private long _underflowCount;
    private long _resumeReprimeCount;
    private long _targetIncreaseCount;
    private long _targetDecreaseCount;
    private long _lastAdaptiveIssueTick;
    private long _lastTargetDecreaseTick;
    private long _nextPreviewPresentId;
    private long _lastSelectedPreviewPresentId;
    private long _lastSelectedSourceSequenceNumber = -1;
    private long _lastSelectedQpc;
    private long _lastSelectedSourceLatencyTicks;
    private long _lastDroppedSourceSequenceNumber = -1;
    private long _lastDropQpc;
    private long _lastDisplayClockPacedPresentTick;
    private long _lastUnderflowQpc;
    private long _lastUnderflowInputAgeTicks;
    private long _lastUnderflowOutputAgeTicks;
    private long _lastScheduleLateTicks;
    private long _maxScheduleLateTicks;
    private long _scheduleLateCount;
    private long _resumeReprimeStartTick;
    private int _lastUnderflowQueueDepth;
    private int _previewSubmissionSuppressed;
    private int _resumeReprimeMissBudget;
    private string _lastDropReason = string.Empty;
    private string _lastUnderflowReason = string.Empty;
    private int _disposed;
    private readonly string _mmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_PREVIEW_JITTER_MMCSS_TASK") ?? "Playback";
    private readonly int _mmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_JITTER_MMCSS_PRIORITY", 1, -2, 2);
    private readonly bool _displayClockPacingEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING", 1, 0, 1) != 0;
    private readonly double _displayClockSubmitDelayMs = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_SUBMIT_DELAY_MS", 0.25, 0.0, 4.0);
    private readonly double _displayClockMinLeadMs = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_MIN_LEAD_MS", 2.0, 0.25, 6.0);

    public MjpegPreviewJitterBuffer(
        double fps,
        Func<IPreviewFrameSink?> getPreviewSink,
        Func<bool> isPreviewSuppressed,
        PreviewFrameProbe? previewFrameProbe = null,
        int targetDepth = 3)
    {
        if (fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fps));
        }

        _fps = fps;
        _frameIntervalTicks = Math.Max(1, (long)Math.Round(Stopwatch.Frequency / fps));
        _minAdaptiveTargetDepth = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_PREVIEW_JITTER_MIN_TARGET_DEPTH",
            DefaultMinAdaptiveTargetDepth,
            1,
            60);
        _maxAdaptiveTargetDepth = Math.Max(
            _minAdaptiveTargetDepth,
            EnvironmentHelpers.GetIntFromEnv(
                "SUSSUDIO_PREVIEW_JITTER_MAX_TARGET_DEPTH",
                DefaultMaxAdaptiveTargetDepth,
                1,
                60));
        var requestedTargetDepth = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH",
            targetDepth,
            1,
            60);
        _targetDepth = Math.Clamp(requestedTargetDepth, _minAdaptiveTargetDepth, _maxAdaptiveTargetDepth);
        _maxDepth = Math.Max(
            _targetDepth + 1,
            EnvironmentHelpers.GetIntFromEnv(
                "SUSSUDIO_PREVIEW_JITTER_MAX_DEPTH",
                _maxAdaptiveTargetDepth + DefaultExtraQueueDepth,
                _targetDepth + 1,
                90));
        _lastAdaptiveIssueTick = Stopwatch.GetTimestamp();
        _lastTargetDecreaseTick = _lastAdaptiveIssueTick;
        _getPreviewSink = getPreviewSink ?? throw new ArgumentNullException(nameof(getPreviewSink));
        _isPreviewSuppressed = isPreviewSuppressed ?? throw new ArgumentNullException(nameof(isPreviewSuppressed));
        _previewFrameProbe = previewFrameProbe;
        _timerResolutionRaised = fps >= 100.0 && timeBeginPeriod(1) == 0;
        _thread = new Thread(EmitLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
            Name = "MjpegPreviewJitter"
        };
        _thread.Start();
        Logger.Log(
            $"MJPEG_PREVIEW_JITTER_INIT fps={fps:0.###} target={_targetDepth} " +
            $"targetRange={_minAdaptiveTargetDepth}-{_maxAdaptiveTargetDepth} max={_maxDepth} " +
            $"timerResolutionRaised={_timerResolutionRaised} displayClockPacing={_displayClockPacingEnabled} " +
            $"displayClockDelayMs={_displayClockSubmitDelayMs:0.###} displayClockMinLeadMs={_displayClockMinLeadMs:0.###}");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _signal.Set();
        if (!_thread.Join(TimeSpan.FromSeconds(2)))
        {
            Logger.Log("MJPEG_PREVIEW_JITTER_STOP_TIMEOUT");
        }

        lock (_sync)
        {
            foreach (var frame in _frames)
            {
                frame.Dispose();
            }

            _frames.Clear();
        }

        if (_timerResolutionRaised)
        {
            timeEndPeriod(1);
        }

        _signal.Dispose();
        Logger.Log(
            $"MJPEG_PREVIEW_JITTER_DISPOSED queued={_totalQueued} submitted={_totalSubmitted} " +
            $"dropped={_totalDropped} underflows={_underflowCount} resumeReprimes={_resumeReprimeCount}");
    }

    public void Clear()
    {
        ClearQueue();
    }

    public void ResetForPreviewSuppression()
    {
        Volatile.Write(ref _previewSubmissionSuppressed, 1);
        Interlocked.Exchange(ref _resumeReprimeMissBudget, 1);
        Interlocked.Exchange(ref _resumeReprimeStartTick, Stopwatch.GetTimestamp());
        ClearQueue("suppressed");
    }

    public void ReprimeAfterPreviewResume()
    {
        Volatile.Write(ref _previewSubmissionSuppressed, 0);
        Interlocked.Exchange(ref _resumeReprimeMissBudget, 1);
        Interlocked.Exchange(ref _resumeReprimeStartTick, Stopwatch.GetTimestamp());
        if (Volatile.Read(ref _disposed) == 0)
        {
            try
            {
                _signal.Set();
            }
            catch (ObjectDisposedException)
            {
                // Dispose won the race; the resume marker is irrelevant now.
            }
        }
    }

}
