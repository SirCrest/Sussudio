using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
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
