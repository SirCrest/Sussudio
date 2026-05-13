using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
                        PreviewFrameTracking.Default with
                        {
                            PreviewPresentId = previewPresentId,
                            SchedulerSubmitTick = submitTick,
                        });
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
                            PreviewFrameTracking.Default with
                            {
                                ArrivalTick = frame.ArrivalTick,
                                SourceSequenceNumber = frame.SequenceNumber,
                                PreviewPresentId = previewPresentId,
                                SchedulerSubmitTick = submitTick,
                            });
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

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
