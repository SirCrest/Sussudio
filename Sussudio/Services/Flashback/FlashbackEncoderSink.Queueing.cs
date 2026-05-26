using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Services.Capture;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    public void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
        => TryEnqueueRawVideoFrame(data, expectedSize);

    public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
    {
        var queue = _videoQueue;
        var rejectReason = GetVideoInputRejectReason(queue, expectedSize, data.IsEmpty);
        if (rejectReason != null)
        {
            TrackVideoQueueRejected(rejectReason);
            return false;
        }

        if (data.Length < expectedSize)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_SHORT actual={data.Length} expected={expectedSize}");
            return false;
        }

        var nv12FrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(_width, _height, isP010: false);
        var p010FrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(_width, _height, isP010: true);
        var maxFrameSize = Math.Max(nv12FrameSize, p010FrameSize);
        var matchesConfiguredFrameSize =
            expectedSize == nv12FrameSize ||
            (p010FrameSize > 0 && expectedSize == p010FrameSize);
        if (maxFrameSize <= 0 || !matchesConfiguredFrameSize)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} max={maxFrameSize} configured={_width}x{_height}");
            return false;
        }

        var buffer = GetBuffer(expectedSize);
        data[..expectedSize].CopyTo(buffer.AsSpan(0, expectedSize));
        var enqueueTick = Environment.TickCount64;
        var isP010 = p010FrameSize > 0 && expectedSize == p010FrameSize;
        var packet = VideoFramePacket.Frame(buffer, expectedSize, enqueueTick, isP010);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, enqueueTick);

        var enqueueResult = TryEnqueueVideoPacket(queue!, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _videoDropsQueueSaturated);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_VIDEO_OVERLOAD saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }

        return false;
    }

    public void EnqueueRawVideoFrame(PooledVideoFrameLease frame)
        => ((IRawVideoFrameLeaseTryEncoder)this).TryEnqueueRawVideoFrame(frame);

    bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var queue = _videoQueue;
        var rejectReason = GetVideoInputRejectReason(queue, expectedSize: 1, dataIsEmpty: false);
        if (rejectReason != null)
        {
            frame.Dispose();
            TrackVideoQueueRejected(rejectReason);
            return false;
        }

        var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(frame.Width, frame.Height, frame.PixelFormat == PooledVideoPixelFormat.P010);
        if (expectedSize <= 0)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} actual={frame.Width}x{frame.Height}");
            frame.Dispose();
            return false;
        }

        if (frame.Length < expectedSize)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_SHORT actual={frame.Length} expected={expectedSize}");
            frame.Dispose();
            return false;
        }

        if (frame.Width != _width || frame.Height != _height)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_SIZE_MISMATCH expected={_width}x{_height} actual={frame.Width}x{frame.Height}");
            frame.Dispose();
            return false;
        }

        var enqueueTick = Environment.TickCount64;
        var packet = VideoFramePacket.Frame(frame, enqueueTick);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, enqueueTick);

        var enqueueResult = TryEnqueueVideoPacket(queue!, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _videoDropsQueueSaturated);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_VIDEO_OVERLOAD saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }

        return false;
    }

    public void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
        => TryEnqueueGpuVideoFrame(d3d11Texture2D, subresourceIndex);

    public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
    {
        var queue = _gpuQueue;
        var rejectReason = GetGpuInputRejectReason(queue, d3d11Texture2D);
        if (rejectReason != null)
        {
            TrackGpuQueueRejected(rejectReason);
            return false;
        }

        if (subresourceIndex < 0)
        {
            TrackGpuQueueRejected("invalid_subresource");
            Logger.Log($"FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE subresource={subresourceIndex}");
            return false;
        }

        Marshal.AddRef(d3d11Texture2D);
        var packet = new GpuFramePacket(d3d11Texture2D, subresourceIndex);
        var enqueueResult = TryEnqueueGpuPacket(queue!, packet);
        if (enqueueResult != VideoEnqueueResult.Overloaded)
        {
            return enqueueResult == VideoEnqueueResult.Accepted;
        }

        var dropped = Interlocked.Increment(ref _gpuFramesDropped);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log($"FLASHBACK_SINK_GPU_OVERLOAD count={dropped} queue_depth={Volatile.Read(ref _gpuQueueDepth)}");
        }

        return false;
    }

    public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)
    {
        var queue = _audioQueue;
        if (_disposed || !_started || !_audioEnabled || queue == null || samples.IsEmpty || Volatile.Read(ref _forceRotateDraining))
        {
            return;
        }

        if (!TryValidateAudioPacketLength(samples.Length, "audio"))
        {
            return;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueAudioPacket(queue, packet, ref _audioQueueDepth, ref _audioDropsBacklogEviction))
        {
            Interlocked.Add(ref _audioSamplesReceived, GetSampleCount(samples.Length));
            return;
        }

        var dropped = Interlocked.Increment(ref _audioDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_AUDIO_DROP saturated={dropped} evicted={Interlocked.Read(ref _audioDropsBacklogEviction)} " +
                $"total_dropped_samples={Interlocked.Read(ref _droppedAudioSamplesCount)}");
        }
    }

    public void EnqueueMicrophoneSamples(ReadOnlyMemory<byte> samples)
    {
        var queue = _microphoneQueue;
        if (_disposed || !_started || !_microphoneEnabled || queue == null || samples.IsEmpty || Volatile.Read(ref _forceRotateDraining))
        {
            return;
        }

        if (!TryValidateAudioPacketLength(samples.Length, "microphone"))
        {
            return;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueAudioPacket(queue, packet, ref _microphoneQueueDepth, ref _microphoneDropsBacklogEviction))
        {
            return;
        }

        var dropped = Interlocked.Increment(ref _microphoneDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_MIC_DROP saturated={dropped} evicted={Interlocked.Read(ref _microphoneDropsBacklogEviction)}");
        }
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        EnqueueAudioSamples(samples);
        return Task.CompletedTask;
    }

    public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        EnqueueMicrophoneSamples(samples);
        return Task.CompletedTask;
    }

    private static long GetSampleCount(int byteLength)
    {
        return byteLength > 0 ? byteLength / AudioInputBlockAlignBytes : 0;
    }

    private static bool TryValidateAudioPacketLength(int byteLength, string source)
    {
        if (byteLength <= 0 || byteLength > MaxAudioPacketBytes)
        {
            Logger.Log($"FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=size bytes={byteLength}");
            return false;
        }

        if (byteLength % AudioInputBlockAlignBytes != 0)
        {
            Logger.Log($"FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=alignment bytes={byteLength} align={AudioInputBlockAlignBytes}");
            return false;
        }

        return true;
    }

    private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio);

    private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        lock (_videoQueueSync)
        {
            var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);
            if (rejectReason != null)
            {
                ReturnVideoPacket(packet);
                TrackVideoQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteVideoPacket(queue, packet))
            {
                _videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork("video_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            rejectReason = GetVideoEnqueueRejectReason(isGpu: false);
            if (rejectReason != null)
            {
                ReturnVideoPacket(packet);
                TrackVideoQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            Interlocked.Increment(ref _droppedVideoFrames);
            ReturnVideoPacket(packet);
            TrackVideoQueueRejected("queue_full");
            return VideoEnqueueResult.Overloaded;
        }
    }

    private bool TryEnqueueAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        ref long backlogEvictions)
    {
        lock (_videoQueueSync)
        {
            if (_disposed ||
                !_started ||
                _cts?.IsCancellationRequested == true ||
                (Volatile.Read(ref _forceRotateDraining) &&
                 IsForceRotateQueueGuarded(Volatile.Read(ref queueDepth), AudioQueueCapacity)) ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                ReturnBuffer(packet.Buffer);
                return false;
            }

            if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio"))
            {
                SignalWork("audio_enqueue");
                return true;
            }

            if (queue.Reader.TryRead(out var evictedPacket))
            {
                DecrementQueueDepth(ref queueDepth, "audio_evict");
                Interlocked.Increment(ref backlogEvictions);
                // Track dropped audio samples for A/V drift diagnostics (analogous to SkipVideoFrame for video)
                var evictedSamples = GetSampleCount(evictedPacket.Length);
                var totalDropped = Interlocked.Add(ref _droppedAudioSamplesCount, evictedSamples);
                if (totalDropped == evictedSamples || totalDropped % 48_000 < evictedSamples)
                {
                    Logger.Log(
                        $"FLASHBACK_SINK_AUDIO_EVICT_PTS samples={evictedSamples} total_dropped_samples={totalDropped} " +
                        $"drift_ms={totalDropped * 1000.0 / 48_000:F1}");
                }

                ReturnBuffer(evictedPacket.Buffer);
                if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio_after_evict"))
                {
                    SignalWork("audio_after_evict");
                    return true;
                }
            }

            // Total saturation: both eviction and re-enqueue failed.
            var saturatedSamples = GetSampleCount(packet.Length);
            Interlocked.Add(ref _droppedAudioSamplesCount, saturatedSamples);
            ReturnBuffer(packet.Buffer);
            return false;
        }
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        lock (_videoQueueSync)
        {
            var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);
            if (rejectReason != null)
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
                TrackGpuQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            if (TryWriteGpuPacket(queue, packet))
            {
                Interlocked.Increment(ref _gpuFramesEnqueued);
                SignalWork("gpu_enqueue");
                return VideoEnqueueResult.Accepted;
            }

            rejectReason = GetVideoEnqueueRejectReason(isGpu: true);
            if (rejectReason != null)
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
                TrackGpuQueueRejected(rejectReason);
                return VideoEnqueueResult.Rejected;
            }

            ReleaseGpuTextureBestEffort(packet.Texture);
            TrackGpuQueueRejected("queue_full");
            return VideoEnqueueResult.Overloaded;
        }
    }

    private string? GetVideoEnqueueRejectReason(bool isGpu)
    {
        if (_disposed)
        {
            return "disposed";
        }

        if (!_started)
        {
            return "not_started";
        }

        if (_cts?.IsCancellationRequested == true)
        {
            return "cancelled";
        }

        if (Volatile.Read(ref _forceRotateDraining))
        {
            return "force_rotate_draining";
        }

        var failure = Volatile.Read(ref _encodingFailure);
        return failure != null
            ? $"encoding_failed:{failure.GetType().Name}"
            : null;
    }

    private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: false);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        if (expectedSize <= 0)
        {
            return "invalid_expected_size";
        }

        return dataIsEmpty ? "data_empty" : null;
    }

    private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: true);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        return texture == IntPtr.Zero ? "null_texture" : null;
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

    private void TrackVideoQueueRejected(string reason)
    {
        Volatile.Write(ref _lastVideoQueueRejectReason, reason);
        var total = Interlocked.Increment(ref _videoQueueRejectedFrames);
        if (total == 1 || total % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_VIDEO_QUEUE_REJECT reason={reason} total={total} depth={Volatile.Read(ref _videoQueueDepth)} capacity={VideoQueueCapacityFrames}");
        }
    }

    private void TrackGpuQueueRejected(string reason)
    {
        Volatile.Write(ref _lastGpuQueueRejectReason, reason);
        var total = Interlocked.Increment(ref _gpuQueueRejectedFrames);
        if (total == 1 || total % 30 == 0)
        {
            Logger.Log(
                $"FLASHBACK_SINK_GPU_QUEUE_REJECT reason={reason} total={total} depth={Volatile.Read(ref _gpuQueueDepth)} capacity={GpuQueueCapacity}");
        }
    }

    private readonly record struct VideoFramePacket(byte[]? Buffer, PooledVideoFrameLease? Lease, int Length, long EnqueueTick, long? SequenceNumber, bool IsP010)
    {
        public static VideoFramePacket Frame(byte[] buffer, int length, long enqueueTick, bool isP010) => new(buffer, null, length, enqueueTick, null, isP010);
        public static VideoFramePacket Frame(PooledVideoFrameLease lease, long enqueueTick) => new(null, lease, lease.Length, enqueueTick, lease.SequenceNumber, lease.PixelFormat == PooledVideoPixelFormat.P010);
    }

    private enum VideoEnqueueResult
    {
        Accepted,
        Rejected,
        Overloaded
    }

    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);

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

    private static void ReturnVideoPacketBestEffort(VideoFramePacket packet)
    {
        try
        {
            ReturnVideoPacket(packet);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static void ReleaseGpuTextureBestEffort(IntPtr texture)
    {
        if (texture == IntPtr.Zero)
        {
            return;
        }

        try
        {
            Marshal.Release(texture);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork("complete_writer");
    }

    private void SignalWork(string operation)
    {
        try
        {
            _workAvailable.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"FLASHBACK_SINK_WORK_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private static void DecrementQueueDepth(ref int target, string queueName)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (current <= 0)
            {
                Logger.Log($"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW queue={queueName} depth={current - 1}");
                return;
            }

            if (Interlocked.CompareExchange(ref target, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private void ReturnAllRemainingQueuedBuffers()
    {
        ReturnRemainingBuffers(_videoQueue, ref _videoQueueDepth);
        ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
        ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
        ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
    }

    private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnVideoPacketBestEffort(packet);
        }

        lock (_videoQueueSync)
        {
            _videoLatencyTracker.ClearEnqueueTicksUnderLock();
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnBuffer(packet.Buffer);
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReleaseGpuTextureBestEffort(packet.Texture);
        }

        Interlocked.Exchange(ref queueDepth, 0);
    }

    private bool WaitForCancellation(TimeSpan timeout)
    {
        var cts = _cts;
        if (cts == null)
        {
            Thread.Sleep(timeout);
            return false;
        }

        try
        {
            return cts.Token.WaitHandle.WaitOne(timeout);
        }
        catch (ObjectDisposedException)
        {
            return true;
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

        Logger.Log($"FLASHBACK_SINK_FATAL type={ex.GetType().Name} msg={ex.Message}");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);

        try
        {
            _onFatalError?.Invoke(ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }
}