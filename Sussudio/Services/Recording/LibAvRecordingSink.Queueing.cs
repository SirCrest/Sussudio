using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        Exception? overloadFailure = null;
        lock (_videoQueueSync)
        {
            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnVideoPacket(packet);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteVideoPacket(queue, packet))
            {
                _videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork("video_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnVideoPacket(packet);
                return VideoEnqueueResult.Rejected;
            }

            Interlocked.Increment(ref _droppedVideoFrames);
            overloadFailure = new InvalidOperationException(
                $"LibAv recording video queue overloaded: capacity={VideoQueueCapacity} depth={Volatile.Read(ref _videoQueueDepth)}");
            ReturnVideoPacket(packet);
        }

        FailEncoding(overloadFailure);
        return VideoEnqueueResult.Overloaded;
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            Marshal.Release(packet.Texture);
            return VideoEnqueueResult.Rejected;
        }

        if (TryWriteGpuPacket(queue, packet))
        {
            Interlocked.Increment(ref _gpuFramesEnqueued);
            SignalWork("gpu_enqueue");
            return VideoEnqueueResult.Accepted;
        }

        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            Marshal.Release(packet.Texture);
            return VideoEnqueueResult.Rejected;
        }

        Marshal.Release(packet.Texture);
        FailEncoding(new InvalidOperationException(
            $"LibAv GPU recording queue overloaded: capacity={GpuQueueCapacity} depth={Volatile.Read(ref _gpuQueueDepth)}"));
        return VideoEnqueueResult.Overloaded;
    }

    private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)
    {
        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            var rejectedFrame = (AVFrame*)packet.Frame;
            if (rejectedFrame != null)
            {
                ffmpeg.av_frame_free(&rejectedFrame);
            }

            return VideoEnqueueResult.Rejected;
        }

        if (TryWriteCudaPacket(queue, packet))
        {
            Interlocked.Increment(ref _cudaFramesEnqueued);
            SignalWork("cuda_enqueue");
            return VideoEnqueueResult.Accepted;
        }

        if (!_started ||
            _cts?.IsCancellationRequested == true ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            var frame = (AVFrame*)packet.Frame;
            if (frame != null)
            {
                ffmpeg.av_frame_free(&frame);
            }

            return VideoEnqueueResult.Rejected;
        }

        var overloadedFrame = (AVFrame*)packet.Frame;
        if (overloadedFrame != null)
        {
            ffmpeg.av_frame_free(&overloadedFrame);
        }

        FailEncoding(new InvalidOperationException(
            $"LibAv CUDA recording queue overloaded: capacity={CudaQueueCapacity} depth={Volatile.Read(ref _cudaQueueDepth)}"));
        return VideoEnqueueResult.Overloaded;
    }

    private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _videoQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _videoQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _videoQueueDepth, "video_write_failed");
        return false;
    }

    private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _gpuQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _gpuQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _gpuQueueDepth, "gpu_write_failed");
        return false;
    }

    private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)
    {
        var depth = Interlocked.Increment(ref _cudaQueueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref _cudaQueueMaxDepth, depth);
            return true;
        }

        DecrementQueueDepth(ref _cudaQueueDepth, "cuda_write_failed");
        return false;
    }

    private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnVideoPacket(packet);
        }

        lock (_videoQueueSync)
        {
            _videoLatencyTracker.ClearEnqueueTicksUnderLock();
        }

        Interlocked.Exchange(ref _videoQueueDepth, 0);
    }

    private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            Marshal.Release(packet.Texture);
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            var frame = (AVFrame*)packet.Frame;
            if (frame != null)
            {
                ffmpeg.av_frame_free(&frame);
            }
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static byte[] GetBuffer(int size)
    {
        return ArrayPool<byte>.Shared.Rent(size);
    }

    private static void ReturnBuffer(byte[] buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private static void ReturnVideoPacket(VideoFramePacket packet)
    {
        if (packet.Buffer != null)
        {
            ReturnBuffer(packet.Buffer);
        }

        packet.Lease?.Dispose();
    }

    private readonly record struct VideoFramePacket(byte[]? Buffer, PooledVideoFrameLease? Lease, int Length, long EnqueueTick, long? SequenceNumber)
    {
        public static VideoFramePacket Frame(byte[] buffer, int length, long enqueueTick) => new(buffer, null, length, enqueueTick, null);
        public static VideoFramePacket Frame(PooledVideoFrameLease lease, long enqueueTick) => new(null, lease, lease.Length, enqueueTick, lease.SequenceNumber);
    }

    private enum VideoEnqueueResult
    {
        Accepted,
        Rejected,
        Overloaded
    }

    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);
    private readonly record struct CudaFramePacket(IntPtr Frame);

    public void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
        => TryEnqueueGpuVideoFrame(d3d11Texture2D, subresourceIndex);

    public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
    {
        var queue = _gpuQueue;
        if (_disposed || !_started || queue == null || d3d11Texture2D == IntPtr.Zero)
        {
            return false;
        }

        Marshal.AddRef(d3d11Texture2D);
        var packet = new GpuFramePacket(d3d11Texture2D, subresourceIndex);

        var enqueueResult = TryEnqueueGpuPacket(queue, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _gpuFramesDropped);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log($"LIBAV_SINK_GPU_OVERLOAD count={dropped} queue_depth={Volatile.Read(ref _gpuQueueDepth)}");
        }

        return false;
    }

    public unsafe void EnqueueCudaVideoFrame(AVFrame* cudaFrame)
    {
        var queue = _cudaQueue;
        if (_disposed || !_started || queue == null || cudaFrame == null)
        {
            return;
        }

        var cloned = ffmpeg.av_frame_clone(cudaFrame);
        if (cloned == null)
        {
            FailEncoding(new InvalidOperationException("LibAv CUDA frame clone failed."));
            Interlocked.Increment(ref _cudaFramesDropped);
            return;
        }

        var packet = new CudaFramePacket((IntPtr)cloned);
        var enqueueResult = TryEnqueueCudaPacket(queue, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return;
        }

        var dropped = Interlocked.Increment(ref _cudaFramesDropped);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log($"LIBAV_SINK_CUDA_OVERLOAD count={dropped}");
        }
    }

    public void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
        => TryEnqueueRawVideoFrame(data, expectedSize);

    public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
    {
        var queue = _videoQueue;
        if (_disposed || !_started || queue == null || expectedSize <= 0 || data.IsEmpty)
        {
            return false;
        }

        if (data.Length < expectedSize)
        {
            Logger.Log($"LIBAV_SINK_VIDEO_FRAME_SHORT actual={data.Length} expected={expectedSize}");
            return false;
        }

        var buffer = GetBuffer(expectedSize);
        data[..expectedSize].CopyTo(buffer.AsSpan(0, expectedSize));
        var enqueueTick = Environment.TickCount64;
        var packet = VideoFramePacket.Frame(buffer, expectedSize, enqueueTick);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, enqueueTick);

        var enqueueResult = TryEnqueueVideoPacket(queue, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _videoDropsQueueSaturated);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_VIDEO_OVERLOAD saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }

        return false;
    }

    void IRawVideoFrameLeaseEncoder.EnqueueRawVideoFrame(PooledVideoFrameLease frame)
        => ((IRawVideoFrameLeaseTryEncoder)this).TryEnqueueRawVideoFrame(frame);

    bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var queue = _videoQueue;
        if (_disposed || !_started || queue == null)
        {
            frame.Dispose();
            return false;
        }

        var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(frame.Width, frame.Height, frame.PixelFormat == PooledVideoPixelFormat.P010);
        if (frame.Length < expectedSize)
        {
            Logger.Log($"LIBAV_SINK_VIDEO_FRAME_SHORT actual={frame.Length} expected={expectedSize}");
            frame.Dispose();
            return false;
        }

        if (frame.Width != _width || frame.Height != _height)
        {
            Logger.Log($"LIBAV_SINK_VIDEO_FRAME_SIZE_MISMATCH expected={_width}x{_height} actual={frame.Width}x{frame.Height}");
            frame.Dispose();
            return false;
        }

        var enqueueTick = Environment.TickCount64;
        var packet = VideoFramePacket.Frame(frame, enqueueTick);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, enqueueTick);

        var enqueueResult = TryEnqueueVideoPacket(queue, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _videoDropsQueueSaturated);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_VIDEO_OVERLOAD saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }

        return false;
    }

    private void SignalWork(string operation)
    {
        try { _workAvailable.Release(); }
        catch (SemaphoreFullException) { /* Best-effort: semaphore already signaled — work loop will pick it up */ }
        catch (ObjectDisposedException)
        {
            Logger.Log($"LIBAV_SINK_WORK_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private void FailEncoding(Exception ex)
    {
        var shouldNotify = false;
        lock (_sync)
        {
            if (_encodingFailure == null)
            {
                _encodingFailure = ex;
                _started = false;
                shouldNotify = true;
            }
        }

        if (!shouldNotify)
        {
            return;
        }

        Logger.Log($"LIBAV_SINK_FATAL type={ex.GetType().Name} msg={ex.Message}");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);

        try
        {
            OnEncodingFailed?.Invoke(ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"LIBAV_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }

    private static void DecrementQueueDepth(ref int target, string queueName)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (current <= 0)
            {
                Logger.Log($"LIBAV_SINK_QUEUE_DEPTH_UNDERFLOW queue={queueName}");
                return;
            }

            if (Interlocked.CompareExchange(ref target, current - 1, current) == current)
            {
                return;
            }
        }
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        var queue = _audioQueue;
        if (_disposed || !_started || !_audioEnabled || queue == null || samples.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueAudioPacket(queue, packet))
        {
            return Task.CompletedTask;
        }

        var dropped = Interlocked.Increment(ref _audioDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_AUDIO_DROP saturated={dropped} evicted={Interlocked.Read(ref _audioDropsBacklogEviction)}");
        }

        return Task.CompletedTask;
    }

    public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        var queue = _microphoneQueue;
        if (_disposed || !_started || !_microphoneEnabled || queue == null || samples.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueMicrophonePacket(queue, packet))
        {
            return Task.CompletedTask;
        }

        var dropped = Interlocked.Increment(ref _microphoneDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_MIC_DROP saturated={dropped} evicted={Interlocked.Read(ref _microphoneDropsBacklogEviction)}");
        }

        return Task.CompletedTask;
    }

    private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnBuffer(packet.Buffer);
        }
    }

    private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)
    {
        ReturnRemainingBuffers(queue);
        Interlocked.Exchange(ref queueDepth, 0);
    }

    private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (TryWriteAudioPacket(queue, packet, ref _audioQueueDepth, "audio"))
        {
            SignalWork("audio_enqueue");
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio_evict");
            var evicted = Interlocked.Increment(ref _audioDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                // Log evicted audio bytes so A/V drift from dropped audio is traceable.
                Logger.Log(
                    $"LIBAV_SINK_AUDIO_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _audioQueueDepth)}");
            }

            ReturnBuffer(evictedPacket.Buffer);
            if (TryWriteAudioPacket(queue, packet, ref _audioQueueDepth, "audio_after_evict"))
            {
                SignalWork("audio_after_evict");
                return true;
            }
        }

        ReturnBuffer(packet.Buffer);
        return false;
    }

    private bool TryEnqueueMicrophonePacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (TryWriteAudioPacket(queue, packet, ref _microphoneQueueDepth, "microphone"))
        {
            SignalWork("microphone_enqueue");
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone_evict");
            var evicted = Interlocked.Increment(ref _microphoneDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                Logger.Log(
                    $"LIBAV_SINK_MIC_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _microphoneQueueDepth)}");
            }

            ReturnBuffer(evictedPacket.Buffer);
            if (TryWriteAudioPacket(queue, packet, ref _microphoneQueueDepth, "microphone_after_evict"))
            {
                SignalWork("microphone_after_evict");
                return true;
            }
        }

        ReturnBuffer(packet.Buffer);
        return false;
    }

    private static bool TryWriteAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        string queueName)
    {
        Interlocked.Increment(ref queueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            return true;
        }

        DecrementQueueDepth(ref queueDepth, $"{queueName}_write_failed");
        return false;
    }

    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
}

// Shared queue dwell-time/backpressure tracker for recording-style video sinks.
// It lives with queue admission/cleanup because callers must coordinate its
// enqueue ticks with queue-depth changes under the sink-owned queue lock.
internal sealed class VideoQueueLatencyTracker
{
    private readonly string _logTagPrefix;
    private readonly object _enqueueTickLock;
    private readonly Queue<long> _enqueueTicks = new();

    private readonly object _latencySync = new();
    private readonly double[] _latencySamples;
    private int _latencySampleIndex;
    private int _latencySampleCount;

    private readonly object _sequenceSync = new();
    private long _lastSequenceNumber = -1;
    private long _sequenceGaps;

    private long _lastLatencyMs;
    private long _backpressureWaitMs;
    private long _backpressureEvents;
    private long _lastBackpressureWaitMs;
    private long _maxBackpressureWaitMs;

    public VideoQueueLatencyTracker(string logTagPrefix, object enqueueTickLock, int latencyWindowSize)
    {
        _logTagPrefix = logTagPrefix;
        _enqueueTickLock = enqueueTickLock;
        _latencySamples = new double[Math.Max(1, latencyWindowSize)];
    }

    public long LastLatencyMs => Interlocked.Read(ref _lastLatencyMs);
    public long BackpressureWaitMs => Interlocked.Read(ref _backpressureWaitMs);
    public long BackpressureEvents => Interlocked.Read(ref _backpressureEvents);
    public long LastBackpressureWaitMs => Interlocked.Read(ref _lastBackpressureWaitMs);
    public long MaxBackpressureWaitMs => Interlocked.Read(ref _maxBackpressureWaitMs);
    public long LastSequenceNumber => Interlocked.Read(ref _lastSequenceNumber);
    public long SequenceGaps => Interlocked.Read(ref _sequenceGaps);

    // Caller must hold the supplied enqueueTickLock.
    public void TrackEnqueueUnderLock(long enqueueTick)
    {
        _enqueueTicks.Enqueue(enqueueTick);
    }

    // Caller must hold the supplied enqueueTickLock.
    public void ClearEnqueueTicksUnderLock()
    {
        _enqueueTicks.Clear();
    }

    // Caller must hold the supplied enqueueTickLock.
    public void TrackDequeueUnderLock(long expectedEnqueueTick)
    {
        if (_enqueueTicks.Count == 0)
        {
            return;
        }

        var queuedTick = _enqueueTicks.Dequeue();
        if (queuedTick != expectedEnqueueTick)
        {
            Logger.Log($"{_logTagPrefix}_QUEUE_TICK_MISMATCH expected={expectedEnqueueTick} actual={queuedTick}");
        }
    }

    public long GetOldestFrameAgeMs(int currentDepth)
    {
        lock (_enqueueTickLock)
        {
            while (_enqueueTicks.Count > currentDepth)
            {
                _enqueueTicks.Dequeue();
            }

            return _enqueueTicks.Count == 0
                ? 0
                : Math.Max(0, Environment.TickCount64 - _enqueueTicks.Peek());
        }
    }

    public void RecordBackpressure(long startTick, long endTick)
    {
        if (startTick <= 0)
        {
            return;
        }

        var elapsedMs = Math.Max(0, endTick - startTick);
        if (elapsedMs <= 0)
        {
            return;
        }

        Interlocked.Increment(ref _backpressureEvents);
        Interlocked.Add(ref _backpressureWaitMs, elapsedMs);
        Interlocked.Exchange(ref _lastBackpressureWaitMs, elapsedMs);
        AtomicMax.Update(ref _maxBackpressureWaitMs, elapsedMs);
    }

    public void RecordPacketDequeued(long enqueueTick, long? sequenceNumber)
    {
        var latencyMs = Math.Max(0, Environment.TickCount64 - enqueueTick);
        Interlocked.Exchange(ref _lastLatencyMs, latencyMs);
        lock (_latencySync)
        {
            _latencySamples[_latencySampleIndex] = latencyMs;
            _latencySampleIndex = (_latencySampleIndex + 1) % _latencySamples.Length;
            if (_latencySampleCount < _latencySamples.Length)
            {
                _latencySampleCount++;
            }
        }

        if (sequenceNumber.HasValue)
        {
            lock (_sequenceSync)
            {
                var last = Interlocked.Read(ref _lastSequenceNumber);
                var current = sequenceNumber.Value;
                if (last >= 0 && current > last + 1)
                {
                    Interlocked.Add(ref _sequenceGaps, current - last - 1);
                }

                if (current > last)
                {
                    Interlocked.Exchange(ref _lastSequenceNumber, current);
                }
            }
        }
    }

    public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) GetMetrics()
    {
        double[] copy;
        int count;
        lock (_latencySync)
        {
            count = _latencySampleCount;
            if (count <= 0)
            {
                return (0, 0, 0, 0, 0);
            }

            copy = new double[count];
            Array.Copy(_latencySamples, copy, count);
        }

        Array.Sort(copy);
        var total = 0.0;
        for (var i = 0; i < copy.Length; i++)
        {
            total += copy[i];
        }

        var p95Index = Math.Clamp((int)Math.Ceiling(copy.Length * 0.95) - 1, 0, copy.Length - 1);
        var p99Index = Math.Clamp((int)Math.Ceiling(copy.Length * 0.99) - 1, 0, copy.Length - 1);
        return (copy.Length, total / copy.Length, copy[p95Index], copy[p99Index], copy[^1]);
    }

    public void ResetAll()
    {
        Interlocked.Exchange(ref _lastLatencyMs, 0);
        Interlocked.Exchange(ref _backpressureWaitMs, 0);
        Interlocked.Exchange(ref _backpressureEvents, 0);
        Interlocked.Exchange(ref _lastBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _maxBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _sequenceGaps, 0);
        Interlocked.Exchange(ref _lastSequenceNumber, -1);

        lock (_enqueueTickLock)
        {
            _enqueueTicks.Clear();
        }

        lock (_latencySync)
        {
            Array.Clear(_latencySamples, 0, _latencySamples.Length);
            _latencySampleCount = 0;
            _latencySampleIndex = 0;
        }
    }
}
