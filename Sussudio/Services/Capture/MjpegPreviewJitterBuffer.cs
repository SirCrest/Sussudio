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

    public readonly record struct Metrics(
        bool Enabled,
        int TargetDepth,
        int MaxDepth,
        int QueueDepth,
        long TotalQueued,
        long TotalSubmitted,
        long TotalDropped,
        long UnderflowCount,
        long ResumeReprimeCount,
        int InputIntervalSampleCount,
        double InputIntervalAvgMs,
        double InputIntervalP95Ms,
        double InputIntervalMaxMs,
        int OutputIntervalSampleCount,
        double OutputIntervalAvgMs,
        double OutputIntervalP95Ms,
        double OutputIntervalMaxMs,
        int QueueLatencySampleCount,
        double QueueLatencyAvgMs,
        double QueueLatencyP95Ms,
        double QueueLatencyMaxMs,
        long DeadlineDropCount,
        long ClearedDropCount,
        long TargetIncreaseCount,
        long TargetDecreaseCount,
        long LastSelectedPreviewPresentId,
        long LastSelectedSourceSequenceNumber,
        long LastSelectedQpc,
        double LastSelectedSourceLatencyMs,
        long LastDroppedSourceSequenceNumber,
        long LastDropQpc,
        string LastDropReason,
        long LastUnderflowQpc,
        string LastUnderflowReason,
        int LastUnderflowQueueDepth,
        double LastUnderflowInputAgeMs,
        double LastUnderflowOutputAgeMs,
        double LastScheduleLateMs,
        double MaxScheduleLateMs,
        long ScheduleLateCount);

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

    private void EmitLoop()
    {
        using var mmcss = MmcssThreadRegistration.TryRegister(_mmcssTask, _mmcssPriority, message => Logger.Log(message));
        var primed = false;
        var nextDueTick = 0L;

        while (Volatile.Read(ref _disposed) == 0)
        {
            if (!primed)
            {
                var targetDepth = Volatile.Read(ref _targetDepth);
                if (GetDepth() < targetDepth)
                {
                    _signal.WaitOne(2);
                    continue;
                }

                primed = true;
                nextDueTick = Stopwatch.GetTimestamp();
            }

            var now = Stopwatch.GetTimestamp();
            var clockSink = _displayClockPacingEnabled ? _getPreviewSink() : null;
            nextDueTick = AlignDueTickToDisplayClock(clockSink, nextDueTick, now);
            var remainingTicks = nextDueTick - now;
            if (remainingTicks > 0)
            {
                WaitForTicks(remainingTicks);
                continue;
            }

            var scheduleLateTicks = Math.Max(0, now - nextDueTick);
            RecordScheduleLate(scheduleLateTicks);
            var sink = clockSink ?? _getPreviewSink();
            if (sink == null)
            {
                ClearQueue();
                primed = false;
                continue;
            }

            if (_isPreviewSuppressed())
            {
                ResetForPreviewSuppression();
                primed = false;
                continue;
            }

            DropDeadlineExpiredFrames(now);
            DropLatencyOverflowFrames(now);
            MaybeDecreaseTargetDepth(now);

            var frame = TryDequeueCore(out var dequeueMissReason);
            if (frame == null)
            {
                if (dequeueMissReason == DequeueMissReason.WaitingForSequence)
                {
                    _signal.WaitOne(1);
                    continue;
                }

                if (dequeueMissReason == DequeueMissReason.EmptyQueue &&
                    TryRecordResumeReprimeMiss(now))
                {
                    primed = false;
                    continue;
                }

                Interlocked.Increment(ref _underflowCount);
                RecordUnderflow(now);
                IncreaseTargetDepth(now);
                primed = false;
                continue;
            }

            using (frame)
            {
                SubmitFrame(sink, frame);
            }

            var outputIntervalTicks = GetAdjustedOutputIntervalTicks();
            var submittedTick = Stopwatch.GetTimestamp();
            if (_displayClockPacingEnabled)
            {
                nextDueTick = AlignDueTickToDisplayClock(sink, submittedTick + outputIntervalTicks, submittedTick);
            }
            else if (scheduleLateTicks > _frameIntervalTicks * LateScheduleResetFrames)
            {
                nextDueTick = submittedTick + outputIntervalTicks;
            }
            else
            {
                nextDueTick += outputIntervalTicks;
            }
        }
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

    public Metrics GetMetrics()
    {
        int depth;
        double[] input;
        double[] output;
        double[] latency;
        lock (_sync)
        {
            depth = _frames.Count;
            input = RingBufferHelpers.Copy(_inputIntervalsMs, _inputIntervalCount, _inputIntervalIndex);
            output = RingBufferHelpers.Copy(_outputIntervalsMs, _outputIntervalCount, _outputIntervalIndex);
            latency = RingBufferHelpers.Copy(_queueLatencyMs, _queueLatencyCount, _queueLatencyIndex);
        }

        var inputMetrics = ComputeTimingMetrics(input);
        var outputMetrics = ComputeTimingMetrics(output);
        var latencyMetrics = ComputeTimingMetrics(latency);
        return new Metrics(
            Enabled: true,
            TargetDepth: Volatile.Read(ref _targetDepth),
            MaxDepth: _maxDepth,
            QueueDepth: depth,
            TotalQueued: Interlocked.Read(ref _totalQueued),
            TotalSubmitted: Interlocked.Read(ref _totalSubmitted),
            TotalDropped: Interlocked.Read(ref _totalDropped),
            UnderflowCount: Interlocked.Read(ref _underflowCount),
            ResumeReprimeCount: Interlocked.Read(ref _resumeReprimeCount),
            InputIntervalSampleCount: inputMetrics.SampleCount,
            InputIntervalAvgMs: inputMetrics.AverageMs,
            InputIntervalP95Ms: inputMetrics.P95Ms,
            InputIntervalMaxMs: inputMetrics.MaxMs,
            OutputIntervalSampleCount: outputMetrics.SampleCount,
            OutputIntervalAvgMs: outputMetrics.AverageMs,
            OutputIntervalP95Ms: outputMetrics.P95Ms,
            OutputIntervalMaxMs: outputMetrics.MaxMs,
            QueueLatencySampleCount: latencyMetrics.SampleCount,
            QueueLatencyAvgMs: latencyMetrics.AverageMs,
            QueueLatencyP95Ms: latencyMetrics.P95Ms,
            QueueLatencyMaxMs: latencyMetrics.MaxMs,
            DeadlineDropCount: Interlocked.Read(ref _deadlineDropCount),
            ClearedDropCount: Interlocked.Read(ref _clearedDropCount),
            TargetIncreaseCount: Interlocked.Read(ref _targetIncreaseCount),
            TargetDecreaseCount: Interlocked.Read(ref _targetDecreaseCount),
            LastSelectedPreviewPresentId: Interlocked.Read(ref _lastSelectedPreviewPresentId),
            LastSelectedSourceSequenceNumber: Interlocked.Read(ref _lastSelectedSourceSequenceNumber),
            LastSelectedQpc: Interlocked.Read(ref _lastSelectedQpc),
            LastSelectedSourceLatencyMs: TicksToMs(Interlocked.Read(ref _lastSelectedSourceLatencyTicks)),
            LastDroppedSourceSequenceNumber: Interlocked.Read(ref _lastDroppedSourceSequenceNumber),
            LastDropQpc: Interlocked.Read(ref _lastDropQpc),
            LastDropReason: Volatile.Read(ref _lastDropReason),
            LastUnderflowQpc: Interlocked.Read(ref _lastUnderflowQpc),
            LastUnderflowReason: Volatile.Read(ref _lastUnderflowReason),
            LastUnderflowQueueDepth: Volatile.Read(ref _lastUnderflowQueueDepth),
            LastUnderflowInputAgeMs: TicksToMs(Interlocked.Read(ref _lastUnderflowInputAgeTicks)),
            LastUnderflowOutputAgeMs: TicksToMs(Interlocked.Read(ref _lastUnderflowOutputAgeTicks)),
            LastScheduleLateMs: TicksToMs(Interlocked.Read(ref _lastScheduleLateTicks)),
            MaxScheduleLateMs: TicksToMs(Interlocked.Read(ref _maxScheduleLateTicks)),
            ScheduleLateCount: Interlocked.Read(ref _scheduleLateCount));
    }

    private void RecordInputInterval(long nowTick)
    {
        var previous = Interlocked.Exchange(ref _lastInputTick, nowTick);
        if (previous > 0)
        {
            RecordTimingSample(_inputIntervalsMs, ref _inputIntervalCount, ref _inputIntervalIndex, ElapsedMs(previous, nowTick));
        }
    }

    private void RecordOutputInterval(long nowTick)
    {
        var previous = Interlocked.Exchange(ref _lastOutputTick, nowTick);
        if (previous > 0)
        {
            RecordTimingSample(_outputIntervalsMs, ref _outputIntervalCount, ref _outputIntervalIndex, ElapsedMs(previous, nowTick));
        }
    }

    private void RecordQueueLatency(long startTick, long endTick)
        => RecordTimingSample(_queueLatencyMs, ref _queueLatencyCount, ref _queueLatencyIndex, ElapsedMs(startTick, endTick));

    private void RecordSelectedFrame(BufferedFrame frame, long previewPresentId, long submitTick)
    {
        Interlocked.Exchange(ref _lastSelectedPreviewPresentId, previewPresentId);
        Interlocked.Exchange(ref _lastSelectedSourceSequenceNumber, frame.SequenceNumber);
        Interlocked.Exchange(ref _lastSelectedQpc, submitTick);
        var latencyTicks = frame.ArrivalTick > 0 && submitTick > frame.ArrivalTick
            ? submitTick - frame.ArrivalTick
            : 0;
        Interlocked.Exchange(ref _lastSelectedSourceLatencyTicks, latencyTicks);
    }

    private void RecordDroppedFrame(long sourceSequenceNumber, string reason)
    {
        Interlocked.Exchange(ref _lastDroppedSourceSequenceNumber, sourceSequenceNumber);
        Interlocked.Exchange(ref _lastDropQpc, Stopwatch.GetTimestamp());
        Volatile.Write(ref _lastDropReason, reason);
    }

    private void RecordUnderflow(long nowTick)
    {
        string reason;
        int depth;
        lock (_sync)
        {
            depth = _frames.Count;
            reason = depth == 0
                ? "empty-queue"
                : _nextPreviewSequence >= 0 &&
                  _frames.FindIndex(frame => frame.SequenceNumber == _nextPreviewSequence) < 0
                    ? "waiting-for-sequence"
                    : "selection-blocked";
        }

        Interlocked.Exchange(ref _lastUnderflowQpc, nowTick);
        Volatile.Write(ref _lastUnderflowQueueDepth, depth);
        Volatile.Write(ref _lastUnderflowReason, reason);

        var lastInputTick = Interlocked.Read(ref _lastInputTick);
        var inputAgeTicks = lastInputTick > 0 && nowTick > lastInputTick ? nowTick - lastInputTick : 0;
        Interlocked.Exchange(ref _lastUnderflowInputAgeTicks, inputAgeTicks);

        var lastOutputTick = Interlocked.Read(ref _lastOutputTick);
        var outputAgeTicks = lastOutputTick > 0 && nowTick > lastOutputTick ? nowTick - lastOutputTick : 0;
        Interlocked.Exchange(ref _lastUnderflowOutputAgeTicks, outputAgeTicks);
    }

    private void RecordScheduleLate(long scheduleLateTicks)
    {
        Interlocked.Exchange(ref _lastScheduleLateTicks, scheduleLateTicks);
        if (scheduleLateTicks <= _frameIntervalTicks / 2)
        {
            return;
        }

        Interlocked.Increment(ref _scheduleLateCount);
        while (true)
        {
            var current = Interlocked.Read(ref _maxScheduleLateTicks);
            if (scheduleLateTicks <= current ||
                Interlocked.CompareExchange(ref _maxScheduleLateTicks, scheduleLateTicks, current) == current)
            {
                return;
            }
        }
    }

    private void RecordTimingSample(double[] window, ref int count, ref int index, double valueMs)
    {
        if (valueMs <= 0 || valueMs > 5000)
        {
            return;
        }

        lock (_sync)
        {
            RingBufferHelpers.Add(window, ref count, ref index, valueMs);
        }
    }

    private static (int SampleCount, double AverageMs, double P95Ms, double MaxMs) ComputeTimingMetrics(double[] samples)
    {
        if (samples.Length == 0)
        {
            return (0, 0, 0, 0);
        }

        var sorted = (double[])samples.Clone();
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sorted.Length; i++)
        {
            sum += sorted[i];
            if (sorted[i] > max)
            {
                max = sorted[i];
            }
        }

        Array.Sort(sorted);
        var p95Index = Math.Min((int)(sorted.Length * 0.95), sorted.Length - 1);
        return (sorted.Length, sum / sorted.Length, sorted[p95Index], max);
    }

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;

    private static double TicksToMs(long ticks)
        => ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;

    private static long MsToTicks(double ms)
        => Math.Max(0, (long)Math.Round(ms * Stopwatch.Frequency / 1000.0));

}
