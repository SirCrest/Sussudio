using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Smooths decoded MJPEG preview frames before they hit the renderer. 4K120
// decode workers can emit in bursts, so this thread paces frame submission
// against the expected display cadence and drops stale frames instead of
// letting preview latency grow without bound.
internal sealed class MjpegPreviewJitterBuffer : IDisposable
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

    private sealed class BufferedFrame : IDisposable
    {
        public BufferedFrame(byte[] buffer, int length, int width, int height, long arrivalTick, long enqueueTick)
        {
            Buffer = buffer;
            SequenceNumber = -1;
            Length = length;
            Width = width;
            Height = height;
            PixelFormat = PooledVideoPixelFormat.Nv12;
            ArrivalTick = arrivalTick;
            EnqueueTick = enqueueTick;
        }

        public BufferedFrame(PooledVideoFrameLease lease, long enqueueTick)
        {
            Lease = lease ?? throw new ArgumentNullException(nameof(lease));
            Buffer = Array.Empty<byte>();
            SequenceNumber = lease.SequenceNumber;
            Length = lease.Length;
            Width = lease.Width;
            Height = lease.Height;
            PixelFormat = lease.PixelFormat;
            ArrivalTick = lease.ArrivalTick;
            EnqueueTick = enqueueTick;
        }

        public byte[] Buffer { get; private set; }
        public PooledVideoFrameLease? Lease { get; set; }
        public long SequenceNumber { get; }
        public int Length { get; }
        public int Width { get; }
        public int Height { get; }
        public PooledVideoPixelFormat PixelFormat { get; }
        public long ArrivalTick { get; }
        public long EnqueueTick { get; }

        public void Dispose()
        {
            var lease = Lease;
            if (lease != null)
            {
                Lease = null;
                lease.Dispose();
            }

            var buffer = Buffer;
            if (buffer.Length != 0)
            {
                Buffer = Array.Empty<byte>();
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

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

    public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)
    {
        if (Volatile.Read(ref _disposed) != 0 ||
            Volatile.Read(ref _previewSubmissionSuppressed) != 0 ||
            nv12Data.IsEmpty ||
            width <= 0 ||
            height <= 0)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        RecordInputInterval(now);

        var buffer = ArrayPool<byte>.Shared.Rent(nv12Data.Length);
        nv12Data.CopyTo(buffer);
        var frame = new BufferedFrame(buffer, nv12Data.Length, width, height, arrivalTick, now);
        EnqueueBufferedFrame(frame);
    }

    public void Enqueue(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 ||
            Volatile.Read(ref _previewSubmissionSuppressed) != 0 ||
            frame.Length <= 0 ||
            frame.Width <= 0 ||
            frame.Height <= 0)
        {
            frame.Dispose();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        RecordInputInterval(now);
        EnqueueBufferedFrame(new BufferedFrame(frame, now));
    }

    private void EnqueueBufferedFrame(BufferedFrame frame)
    {
        var shouldSignal = false;

        lock (_sync)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                frame.Dispose();
                return;
            }

            while (_frames.Count >= _maxDepth)
            {
                var dropped = RemoveOldestFrame();
                RecordDroppedFrame(dropped.SequenceNumber, "queue-full");
                dropped.Dispose();
                Interlocked.Increment(ref _totalDropped);
            }

            if (AddFrameInOrder(frame))
            {
                Interlocked.Increment(ref _totalQueued);
                shouldSignal = true;
            }
        }

        if (shouldSignal && Volatile.Read(ref _disposed) == 0)
        {
            try
            {
                _signal.Set();
            }
            catch (ObjectDisposedException)
            {
                // Dispose won the race after the frame was queued; Dispose drains the queue.
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
            input = CopyRing(_inputIntervalsMs, _inputIntervalCount, _inputIntervalIndex);
            output = CopyRing(_outputIntervalsMs, _outputIntervalCount, _outputIntervalIndex);
            latency = CopyRing(_queueLatencyMs, _queueLatencyCount, _queueLatencyIndex);
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

    private long AlignDueTickToDisplayClock(IPreviewFrameSink? sink, long currentDueTick, long nowTick)
    {
        if (!_displayClockPacingEnabled ||
            sink is not IPreviewDisplayClock displayClock ||
            !displayClock.TryGetDisplayClock(out var clock) ||
            clock.LastPresentTick <= 0)
        {
            return currentDueTick;
        }

        if (clock.LastPresentTick <= Interlocked.Read(ref _lastDisplayClockPacedPresentTick))
        {
            return currentDueTick;
        }

        var intervalTicks = clock.FrameIntervalTicks > 0 ? clock.FrameIntervalTicks : _frameIntervalTicks;
        var submitDelayTicks = MsToTicks(_displayClockSubmitDelayMs);
        var minLeadTicks = MsToTicks(_displayClockMinLeadMs);
        var nextPresentTick = clock.LastPresentTick + intervalTicks;
        while (nextPresentTick <= nowTick)
        {
            nextPresentTick += intervalTicks;
        }

        var preferredDueTick = clock.LastPresentTick + submitDelayTicks;
        while (preferredDueTick <= nowTick)
        {
            preferredDueTick += intervalTicks;
        }

        var latestSafeSubmitTick = nextPresentTick - minLeadTicks;
        if (nowTick <= latestSafeSubmitTick && preferredDueTick > latestSafeSubmitTick)
        {
            Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
            return nowTick;
        }

        if (preferredDueTick <= latestSafeSubmitTick)
        {
            Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
            return preferredDueTick;
        }

        Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
        return nextPresentTick + submitDelayTicks;
    }

    private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)
    {
        var submitTick = Stopwatch.GetTimestamp();
        var previewPresentId = Interlocked.Increment(ref _nextPreviewPresentId);
        try
        {
            if (frame.Lease != null)
            {
                var lease = frame.Lease;
                frame.Lease = null;
                try
                {
                    _previewFrameProbe?.Invoke(
                        lease.Memory.Span,
                        frame.Width,
                        frame.Height,
                        lease.PixelFormat,
                        frame.ArrivalTick,
                        frame.SequenceNumber);
                    sink.SubmitRawFrameLease(
                        lease,
                        isHdr: false,
                        previewPresentId: previewPresentId,
                        schedulerSubmitTick: submitTick);
                    lease = null;
                }
                finally
                {
                    lease?.Dispose();
                }
            }
            else
            {
                _previewFrameProbe?.Invoke(
                    frame.Buffer.AsSpan(0, frame.Length),
                    frame.Width,
                    frame.Height,
                    frame.PixelFormat,
                    frame.ArrivalTick,
                    frame.SequenceNumber);
                unsafe
                {
                    fixed (byte* pointer = frame.Buffer)
                    {
                        sink.SubmitRawFrame(
                            (IntPtr)pointer,
                            frame.Length,
                            frame.Width,
                            frame.Height,
                            false,
                            frame.ArrivalTick,
                            sourceSequenceNumber: frame.SequenceNumber,
                            previewPresentId: previewPresentId,
                            schedulerSubmitTick: submitTick);
                    }
                }
            }

            var now = Stopwatch.GetTimestamp();
            RecordSelectedFrame(frame, previewPresentId, submitTick);
            RecordOutputInterval(now);
            RecordQueueLatency(frame.EnqueueTick, now);
            Interlocked.Increment(ref _totalSubmitted);
        }
        catch (Exception ex)
        {
            RecordDroppedFrame(frame.SequenceNumber, "submit-failed");
            Interlocked.Increment(ref _totalDropped);
            Logger.Log($"MJPEG_PREVIEW_JITTER_SUBMIT_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private int GetDepth()
    {
        lock (_sync)
        {
            return _frames.Count;
        }
    }

    private BufferedFrame? TryDequeue()
        => TryDequeueCore(out _);

    private BufferedFrame? TryDequeueCore(out DequeueMissReason missReason)
    {
        missReason = DequeueMissReason.None;
        lock (_sync)
        {
            if (_frames.Count == 0)
            {
                missReason = DequeueMissReason.EmptyQueue;
                return null;
            }

            var index = GetNextPreviewFrameIndex(Stopwatch.GetTimestamp(), allowDeadlineSkip: true);
            if (index < 0)
            {
                missReason = DequeueMissReason.WaitingForSequence;
                return null;
            }

            var frame = _frames[index];
            _frames.RemoveAt(index);
            if (frame.SequenceNumber >= 0)
            {
                _nextPreviewSequence = frame.SequenceNumber + 1;
            }

            return frame;
        }
    }

    private bool AddFrameInOrder(BufferedFrame frame)
    {
        if (frame.SequenceNumber < 0)
        {
            _frames.Add(frame);
            return true;
        }

        if (frame.SequenceNumber < _nextPreviewSequence)
        {
            RecordDroppedFrame(frame.SequenceNumber, "late-sequence");
            frame.Dispose();
            Interlocked.Increment(ref _totalDropped);
            Interlocked.Increment(ref _deadlineDropCount);
            return false;
        }

        var index = _frames.FindIndex(candidate =>
            candidate.SequenceNumber >= 0 &&
            candidate.SequenceNumber > frame.SequenceNumber);
        if (index < 0)
        {
            _frames.Add(frame);
        }
        else
        {
            _frames.Insert(index, frame);
        }

        return true;
    }

    private BufferedFrame RemoveOldestFrame()
    {
        var oldestIndex = 0;
        for (var i = 1; i < _frames.Count; i++)
        {
            if (_frames[i].EnqueueTick < _frames[oldestIndex].EnqueueTick)
            {
                oldestIndex = i;
            }
        }

        var frame = _frames[oldestIndex];
        _frames.RemoveAt(oldestIndex);
        if (frame.SequenceNumber >= 0 && frame.SequenceNumber == _nextPreviewSequence)
        {
            _nextPreviewSequence++;
        }

        return frame;
    }

    private int GetNextPreviewFrameIndex(long nowTick, bool allowDeadlineSkip)
    {
        if (_frames.Count == 0)
        {
            return -1;
        }

        if (_nextPreviewSequence < 0)
        {
            var firstOrdered = _frames.FindIndex(frame => frame.SequenceNumber >= 0);
            return firstOrdered >= 0 ? firstOrdered : GetOldestFrameIndex();
        }

        var exact = _frames.FindIndex(frame => frame.SequenceNumber == _nextPreviewSequence);
        if (exact >= 0)
        {
            return exact;
        }

        if (!allowDeadlineSkip)
        {
            return -1;
        }

        var oldestIndex = GetOldestFrameIndex();
        var oldest = _frames[oldestIndex];
        if (!IsPastHardDeadline(oldest, nowTick))
        {
            return -1;
        }

        var nextOrdered = _frames.FindIndex(frame => frame.SequenceNumber >= 0);
        if (nextOrdered >= 0)
        {
            var skipped = Math.Max(1, _frames[nextOrdered].SequenceNumber - _nextPreviewSequence);
            RecordDroppedFrame(_nextPreviewSequence, "missing-sequence");
            _nextPreviewSequence = _frames[nextOrdered].SequenceNumber;
            Interlocked.Add(ref _deadlineDropCount, skipped);
            Interlocked.Add(ref _totalDropped, skipped);
            IncreaseTargetDepth(nowTick);
            return nextOrdered;
        }

        return oldestIndex;
    }

    private int GetOldestFrameIndex()
    {
        var oldestIndex = 0;
        for (var i = 1; i < _frames.Count; i++)
        {
            if (_frames[i].EnqueueTick < _frames[oldestIndex].EnqueueTick)
            {
                oldestIndex = i;
            }
        }

        return oldestIndex;
    }

    private void ClearQueue()
        => ClearQueue("cleared");

    private void ClearQueue(string reason)
    {
        lock (_sync)
        {
            foreach (var frame in _frames)
            {
                RecordDroppedFrame(frame.SequenceNumber, reason);
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _clearedDropCount);
            }

            _frames.Clear();
            _nextPreviewSequence = -1;
        }
    }

    private bool TryRecordResumeReprimeMiss(long nowTick)
    {
        while (true)
        {
            var budget = Volatile.Read(ref _resumeReprimeMissBudget);
            if (budget <= 0)
            {
                return false;
            }

            var startTick = Interlocked.Read(ref _resumeReprimeStartTick);
            var targetDepth = Math.Max(1, Volatile.Read(ref _targetDepth));
            var maxReprimeAgeTicks = _frameIntervalTicks * Math.Max(2, targetDepth + 2);
            if (startTick <= 0 ||
                (nowTick >= startTick && nowTick - startTick > maxReprimeAgeTicks))
            {
                Interlocked.Exchange(ref _resumeReprimeMissBudget, 0);
                return false;
            }

            if (Interlocked.CompareExchange(ref _resumeReprimeMissBudget, budget - 1, budget) == budget)
            {
                Interlocked.Increment(ref _resumeReprimeCount);
                Interlocked.Exchange(ref _lastUnderflowQpc, nowTick);
                Volatile.Write(ref _lastUnderflowQueueDepth, 0);
                Volatile.Write(ref _lastUnderflowReason, "resume-reprime");
                Interlocked.Exchange(ref _lastUnderflowInputAgeTicks, 0);
                Interlocked.Exchange(ref _lastUnderflowOutputAgeTicks, 0);
                return true;
            }
        }
    }

    private void DropDeadlineExpiredFrames(long nowTick)
    {
        var droppedAny = false;

        lock (_sync)
        {
            while (_frames.Count > 0)
            {
                var oldestIndex = GetOldestFrameIndex();
                var frame = _frames[oldestIndex];
                if (!IsPastHardDeadline(frame, nowTick))
                {
                    break;
                }

                _frames.RemoveAt(oldestIndex);
                if (frame.SequenceNumber >= 0 && frame.SequenceNumber >= _nextPreviewSequence)
                {
                    _nextPreviewSequence = frame.SequenceNumber + 1;
                }

                RecordDroppedFrame(frame.SequenceNumber, "hard-deadline");
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _deadlineDropCount);
                droppedAny = true;
            }
        }

        if (droppedAny)
        {
            IncreaseTargetDepth(nowTick);
        }
    }

    private void DropLatencyOverflowFrames(long nowTick)
    {
        lock (_sync)
        {
            var targetDepth = Volatile.Read(ref _targetDepth);
            while (_frames.Count > Math.Max(1, targetDepth))
            {
                var oldestIndex = GetOldestFrameIndex();
                var frame = _frames[oldestIndex];
                if (!IsPastSoftDeadline(frame, nowTick))
                {
                    break;
                }

                _frames.RemoveAt(oldestIndex);
                if (frame.SequenceNumber >= 0 && frame.SequenceNumber >= _nextPreviewSequence)
                {
                    _nextPreviewSequence = frame.SequenceNumber + 1;
                }

                RecordDroppedFrame(frame.SequenceNumber, "soft-deadline");
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
                Interlocked.Increment(ref _deadlineDropCount);
            }
        }
    }

    private bool IsPastSoftDeadline(BufferedFrame frame, long nowTick)
    {
        var targetDepth = Volatile.Read(ref _targetDepth);
        var softDeadlineTicks = Math.Max(
            _frameIntervalTicks,
            _frameIntervalTicks * (targetDepth + SoftDeadlineExtraFrames));
        return nowTick - frame.EnqueueTick > softDeadlineTicks;
    }

    private bool IsPastHardDeadline(BufferedFrame frame, long nowTick)
    {
        var targetDepth = Volatile.Read(ref _targetDepth);
        var hardDeadlineTicks = Math.Max(
            _frameIntervalTicks,
            _frameIntervalTicks * (targetDepth + HardDeadlineExtraFrames));
        return nowTick - frame.EnqueueTick > hardDeadlineTicks;
    }

    private long GetAdjustedOutputIntervalTicks()
    {
        var depth = GetDepth();
        var targetDepth = Volatile.Read(ref _targetDepth);
        var surplus = depth - targetDepth;
        var adjustment = surplus >= AggressiveCatchUpSurplusFrames ? 0.985 :
                         surplus >= FastCatchUpSurplusFrames ? 0.99 :
                         surplus > 0 ? 0.995 :
                         surplus < 0 ? 1.005 :
                         1.0;
        return Math.Max(1, (long)Math.Round(_frameIntervalTicks * adjustment));
    }

    private void IncreaseTargetDepth(long nowTick)
    {
        while (true)
        {
            var current = Volatile.Read(ref _targetDepth);
            if (current >= _maxAdaptiveTargetDepth)
            {
                Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
                return;
            }

            if (Interlocked.CompareExchange(ref _targetDepth, current + 1, current) == current)
            {
                Interlocked.Increment(ref _targetIncreaseCount);
                Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
                Logger.Log($"MJPEG_PREVIEW_JITTER_TARGET_INCREASE target={current + 1}");
                return;
            }
        }
    }

    private void MaybeDecreaseTargetDepth(long nowTick)
    {
        if (HasLatencyPressure(nowTick))
        {
            Interlocked.Exchange(ref _lastAdaptiveIssueTick, nowTick);
            return;
        }

        var lastIssue = Interlocked.Read(ref _lastAdaptiveIssueTick);
        var lastDecrease = Interlocked.Read(ref _lastTargetDecreaseTick);
        var stableTicks = nowTick - lastIssue;
        var sinceDecreaseTicks = nowTick - lastDecrease;
        if (stableTicks < Stopwatch.Frequency * 15L ||
            sinceDecreaseTicks < Stopwatch.Frequency * 15L)
        {
            return;
        }

        while (true)
        {
            var current = Volatile.Read(ref _targetDepth);
            if (current <= _minAdaptiveTargetDepth)
            {
                Interlocked.Exchange(ref _lastTargetDecreaseTick, nowTick);
                return;
            }

            if (Interlocked.CompareExchange(ref _targetDepth, current - 1, current) == current)
            {
                Interlocked.Increment(ref _targetDecreaseCount);
                Interlocked.Exchange(ref _lastTargetDecreaseTick, nowTick);
                Logger.Log($"MJPEG_PREVIEW_JITTER_TARGET_DECREASE target={current - 1}");
                return;
            }
        }
    }

    private bool HasLatencyPressure(long nowTick)
    {
        lock (_sync)
        {
            if (_frames.Count == 0)
            {
                return false;
            }

            var targetDepth = Volatile.Read(ref _targetDepth);
            if (_frames.Count > targetDepth + 1)
            {
                return true;
            }

            return IsPastSoftDeadline(_frames[GetOldestFrameIndex()], nowTick);
        }
    }

    private void WaitForTicks(long ticks)
    {
        var deadline = Stopwatch.GetTimestamp() + ticks;

        while (Volatile.Read(ref _disposed) == 0)
        {
            var remainingTicks = deadline - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
            {
                return;
            }

            var ms = remainingTicks * 1000.0 / Stopwatch.Frequency;
            if (ms >= 2.0)
            {
                Thread.Sleep(Math.Max(1, (int)Math.Floor(ms - 0.5)));
            }
            else if (ms > 0)
            {
                Thread.SpinWait(64);
            }
            else
            {
                return;
            }
        }
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
            window[index] = valueMs;
            index = (index + 1) % window.Length;
            if (count < window.Length)
            {
                count++;
            }
        }
    }

    private static double[] CopyRing(double[] window, int count, int index)
    {
        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var ringIndex = (index - count + i + window.Length) % window.Length;
            samples[i] = window[ringIndex];
        }

        return samples;
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

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
