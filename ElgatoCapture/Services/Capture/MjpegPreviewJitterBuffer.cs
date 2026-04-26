using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ElgatoCapture.Services.Preview;

namespace ElgatoCapture.Services.Capture;

internal sealed class MjpegPreviewJitterBuffer : IDisposable
{
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
        long TargetIncreaseCount,
        long TargetDecreaseCount);

    private readonly object _sync = new();
    private readonly List<BufferedFrame> _frames = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Func<IPreviewFrameSink?> _getPreviewSink;
    private readonly Func<bool> _isPreviewSuppressed;
    private readonly PreviewFrameProbe? _previewFrameProbe;
    private readonly double _fps;
    private readonly long _frameIntervalTicks;
    private const int MinAdaptiveTargetDepth = 2;
    private const int MaxAdaptiveTargetDepth = 8;
    private const int SoftDeadlineExtraFrames = 2;
    private const int HardDeadlineExtraFrames = 4;
    private const int FastCatchUpSurplusFrames = 2;
    private const int AggressiveCatchUpSurplusFrames = 4;
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
    private long _underflowCount;
    private long _targetIncreaseCount;
    private long _targetDecreaseCount;
    private long _lastAdaptiveIssueTick;
    private long _lastTargetDecreaseTick;
    private int _disposed;

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
        _targetDepth = Math.Clamp(targetDepth, MinAdaptiveTargetDepth, MaxAdaptiveTargetDepth);
        _maxDepth = MaxAdaptiveTargetDepth + 4;
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
            $"MJPEG_PREVIEW_JITTER_INIT fps={fps:0.###} target={_targetDepth} max={_maxDepth} " +
            $"timerResolutionRaised={_timerResolutionRaised}");
    }

    public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)
    {
        if (Volatile.Read(ref _disposed) != 0 || nv12Data.IsEmpty || width <= 0 || height <= 0)
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

        if (Volatile.Read(ref _disposed) != 0 || frame.Length <= 0 || frame.Width <= 0 || frame.Height <= 0)
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
                dropped.Dispose();
                Interlocked.Increment(ref _totalDropped);
            }

            AddFrameInOrder(frame);
            Interlocked.Increment(ref _totalQueued);
            shouldSignal = true;
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
            TargetIncreaseCount: Interlocked.Read(ref _targetIncreaseCount),
            TargetDecreaseCount: Interlocked.Read(ref _targetDecreaseCount));
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
            $"dropped={_totalDropped} underflows={_underflowCount}");
    }

    public void Clear()
    {
        ClearQueue();
    }

    private void EmitLoop()
    {
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
            var remainingTicks = nextDueTick - now;
            if (remainingTicks > 0)
            {
                WaitForTicks(remainingTicks);
                continue;
            }

            var sink = _getPreviewSink();
            if (sink == null || _isPreviewSuppressed())
            {
                ClearQueue();
                primed = false;
                continue;
            }

            DropDeadlineExpiredFrames(now);
            DropLatencyOverflowFrames(now);
            MaybeDecreaseTargetDepth(now);

            var frame = TryDequeue();
            if (frame == null)
            {
                Interlocked.Increment(ref _underflowCount);
                IncreaseTargetDepth(now);
                primed = false;
                continue;
            }

            using (frame)
            {
                SubmitFrame(sink, frame);
            }

            nextDueTick += GetAdjustedOutputIntervalTicks();
            var lateTicks = Stopwatch.GetTimestamp() - nextDueTick;
            if (lateTicks > _frameIntervalTicks * 2)
            {
                nextDueTick = Stopwatch.GetTimestamp() + _frameIntervalTicks;
            }
        }
    }

    private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)
    {
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
                    sink.SubmitRawFrameLease(lease, isHdr: false);
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
                        sink.SubmitRawFrame((IntPtr)pointer, frame.Length, frame.Width, frame.Height, false, frame.ArrivalTick);
                    }
                }
            }

            var now = Stopwatch.GetTimestamp();
            RecordOutputInterval(now);
            RecordQueueLatency(frame.EnqueueTick, now);
            Interlocked.Increment(ref _totalSubmitted);
        }
        catch (Exception ex)
        {
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
    {
        lock (_sync)
        {
            if (_frames.Count == 0)
            {
                return null;
            }

            var index = GetNextPreviewFrameIndex(Stopwatch.GetTimestamp(), allowDeadlineSkip: true);
            if (index < 0)
            {
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

    private void AddFrameInOrder(BufferedFrame frame)
    {
        if (frame.SequenceNumber < 0)
        {
            _frames.Add(frame);
            return;
        }

        if (frame.SequenceNumber < _nextPreviewSequence)
        {
            frame.Dispose();
            Interlocked.Increment(ref _totalDropped);
            Interlocked.Increment(ref _deadlineDropCount);
            return;
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
    {
        lock (_sync)
        {
            foreach (var frame in _frames)
            {
                frame.Dispose();
                Interlocked.Increment(ref _totalDropped);
            }

            _frames.Clear();
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
            if (current >= MaxAdaptiveTargetDepth)
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
            if (current <= MinAdaptiveTargetDepth)
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

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
