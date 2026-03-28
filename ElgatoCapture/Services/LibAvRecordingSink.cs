using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

public sealed class LibAvRecordingSink : IRecordingSink, IRawVideoFrameEncoder, IGpuVideoFrameEncoder, ICudaVideoFrameEncoder
{
    private const int VideoQueueCapacity = 360;
    private const int AudioQueueCapacity = 3600;
    private const int GpuQueueCapacity = 4;
    private const int CudaQueueCapacity = 12;
    private const int StopTimeoutMs = 30_000;

    private readonly object _sync = new();
    private readonly LibAvEncoder _encoder = new();
    private readonly SemaphoreSlim _workAvailable = new(0, 1);
    private Channel<VideoFramePacket>? _videoQueue;
    private Channel<AudioSamplePacket>? _audioQueue;
    private Channel<AudioSamplePacket>? _microphoneQueue;
    private Channel<GpuFramePacket>? _gpuQueue;
    private Channel<CudaFramePacket>? _cudaQueue;
    private CancellationTokenSource? _cts;
    private Task? _encodingTask;
    private RecordingContext? _context;
    private Exception? _encodingFailure;
    private int _width;
    private int _height;
    private bool _audioEnabled;
    private bool _microphoneEnabled;
    private bool _started;
    private bool _disposed;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private long _microphoneDropsQueueSaturated;
    private long _microphoneDropsBacklogEviction;
    private long _gpuFramesEnqueued;
    private long _gpuFramesDropped;
    private long _cudaFramesEnqueued;
    private long _cudaFramesDropped;
    private int _videoQueueDepth;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private int _cudaQueueDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;
    private long _lastBurstEvictionTick;
    private bool _gpuEncodingEnabled;
    private bool _cudaEncodingEnabled;

    public event EventHandler<long>? FrameEncoded;

    public long DroppedVideoFrames =>
        Interlocked.Read(ref _droppedVideoFrames) +
        Interlocked.Read(ref _gpuFramesDropped) +
        Interlocked.Read(ref _cudaFramesDropped) +
        _encoder.DroppedFrameCount;
    public long EncodedVideoFrames => Interlocked.Read(ref _encodedVideoFrames);
    public long AudioSamplesReceived => _encoder.AudioSamplesReceived;
    public long MicrophoneSamplesReceived => _encoder.MicrophoneSamplesReceived;
    public long OutputBytes => _encoder.TotalBytesWritten;
    public string OutputPath => _context?.FinalOutputPath ?? _encoder.OutputPath;
    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);
    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public int MicrophoneQueueCount => Volatile.Read(ref _microphoneQueueDepth);
    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);
    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);
    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);
    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);
    public bool GpuEncodingEnabled => Volatile.Read(ref _gpuEncodingEnabled);
    public bool CudaEncodingEnabled => Volatile.Read(ref _cudaEncodingEnabled);
    public long CudaFramesEnqueued => Interlocked.Read(ref _cudaFramesEnqueued);
    public long CudaFramesDropped => Interlocked.Read(ref _cudaFramesDropped);

    public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)
        => _encoder.TryGetCurrentAvSyncDrift(out driftMs, out correctionSamples);

    public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_started)
            {
                throw new InvalidOperationException("LibAv recording sink has already started.");
            }
        }

        try
        {
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            _microphoneEnabled = !string.IsNullOrWhiteSpace(context.MicrophoneDeviceName);
            var options = CreateOptions(context);
            _encoder.Initialize(options);
            if (_encoder.UseCudaHardwareFrames)
            {
                _cudaQueue = Channel.CreateBounded<CudaFramePacket>(new BoundedChannelOptions(CudaQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
                _cudaEncodingEnabled = true;
                Logger.Log("LIBAV_SINK_CUDA_QUEUE_INIT capacity=" + CudaQueueCapacity);
            }
            else if (_encoder.UseHardwareFrames)
            {
                _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
                _gpuEncodingEnabled = true;
                Logger.Log("LIBAV_SINK_GPU_QUEUE_INIT capacity=" + GpuQueueCapacity);
            }

            _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(VideoQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            _audioQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            if (_microphoneEnabled)
            {
                _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            }
            _cts = new CancellationTokenSource();
            _context = context;
            _encodingFailure = null;
            _width = checked((int)context.EffectiveWidth);
            _height = checked((int)context.EffectiveHeight);
            _audioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName);
            Interlocked.Exchange(ref _droppedVideoFrames, 0);
            Interlocked.Exchange(ref _encodedVideoFrames, 0);
            Interlocked.Exchange(ref _videoFramesEnqueued, 0);
            Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
            Interlocked.Exchange(ref _gpuFramesDropped, 0);
            Interlocked.Exchange(ref _cudaFramesEnqueued, 0);
            Interlocked.Exchange(ref _cudaFramesDropped, 0);
            Interlocked.Exchange(ref _videoQueueDepth, 0);
            Interlocked.Exchange(ref _audioQueueDepth, 0);
            Interlocked.Exchange(ref _microphoneQueueDepth, 0);
            Interlocked.Exchange(ref _gpuQueueDepth, 0);
            Interlocked.Exchange(ref _cudaQueueDepth, 0);
            Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
            Interlocked.Exchange(ref _lastVideoWriteTick, 0);
            _encodingTask = Task.Factory.StartNew(
                () => EncodingLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            lock (_sync)
            {
                _started = true;
            }

            Logger.Log(
                $"LIBAV_SINK_START output='{context.FinalOutputPath}' codec='{options.CodecName}' " +
                $"width={_width} height={_height} fps={context.EffectiveFrameRate:0.###} p010={context.HdrPipelineActive} audio={_audioEnabled} microphone={_microphoneEnabled}");
            return Task.CompletedTask;
        }
        catch
        {
            /* Cleanup must not throw — tear down partially-initialized queues/state before re-throwing */
            CompleteWriter(_videoQueue);
            CompleteWriter(_audioQueue);
            CompleteWriter(_microphoneQueue);
            CompleteWriter(_gpuQueue);
            CompleteWriter(_cudaQueue);
            _videoQueue = null;
            _audioQueue = null;
            _microphoneQueue = null;
            _gpuQueue = null;
            _cudaQueue = null;
            _gpuEncodingEnabled = false;
            _cudaEncodingEnabled = false;
            _cts?.Dispose();
            _cts = null;
            _encodingTask = null;
            _context = null;
            _width = 0;
            _height = 0;
            _audioEnabled = false;
            _microphoneEnabled = false;
            lock (_sync)
            {
                _started = false;
            }

            _encoder.Dispose();
            throw;
        }
    }

    public void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
    {
        var queue = _gpuQueue;
        if (_disposed || !_started || queue == null || d3d11Texture2D == IntPtr.Zero)
        {
            return;
        }

        Marshal.AddRef(d3d11Texture2D);
        var packet = new GpuFramePacket(d3d11Texture2D, subresourceIndex);

        if (!queue.Writer.TryWrite(packet))
        {
            Marshal.Release(d3d11Texture2D);
            _encoder.SkipVideoFrame();  // Advance PTS even on dropped frames to prevent A/V drift
            var dropped = Interlocked.Increment(ref _gpuFramesDropped);
            if (dropped == 1 || dropped % 30 == 0)
            {
                Logger.Log($"LIBAV_SINK_GPU_DROP count={dropped} queue_depth={Volatile.Read(ref _gpuQueueDepth)}");
            }

            return;
        }

        Interlocked.Increment(ref _gpuQueueDepth);
        Interlocked.Increment(ref _gpuFramesEnqueued);
        SignalWork();
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
            _encoder.SkipVideoFrame();  // Advance PTS even on clone failure to prevent A/V drift
            Interlocked.Increment(ref _cudaFramesDropped);
            return;
        }

        var packet = new CudaFramePacket((IntPtr)cloned);
        if (!queue.Writer.TryWrite(packet))
        {
            ffmpeg.av_frame_free(&cloned);
            _encoder.SkipVideoFrame();  // Advance PTS even on dropped frames to prevent A/V drift
            var dropped = Interlocked.Increment(ref _cudaFramesDropped);
            if (dropped == 1 || dropped % 30 == 0)
            {
                Logger.Log($"LIBAV_SINK_CUDA_DROP count={dropped}");
            }

            return;
        }

        Interlocked.Increment(ref _cudaQueueDepth);
        Interlocked.Increment(ref _cudaFramesEnqueued);
        SignalWork();
    }

    public void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
    {
        var queue = _videoQueue;
        if (_disposed || !_started || queue == null || expectedSize <= 0 || data.IsEmpty)
        {
            return;
        }

        if (data.Length < expectedSize)
        {
            Logger.Log($"LIBAV_SINK_VIDEO_FRAME_SHORT actual={data.Length} expected={expectedSize}");
            return;
        }

        var buffer = GetBuffer(expectedSize);
        data[..expectedSize].CopyTo(buffer.AsSpan(0, expectedSize));
        var packet = new VideoFramePacket(buffer, expectedSize);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, Environment.TickCount64);

        if (TryEnqueueVideoPacket(queue, packet))
        {
            return;
        }

        var dropped = Interlocked.Increment(ref _videoDropsQueueSaturated);
        if (dropped == 1 || dropped % 30 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_VIDEO_DROP saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
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

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        var context = _context;
        var outputPath = context?.FinalOutputPath ?? OutputPath;

        if (_disposed)
        {
            return FinalizeResult.Success(outputPath, "Stopped");
        }

        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);

        if (_encodingTask != null)
        {
            var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(StopTimeoutMs, cancellationToken)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _encodingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _cts?.Cancel();
                return FinalizeResult.Failure(outputPath, "Stopped (libav encode drain timed out)");
            }

            try
            {
                await _encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure = ex;
            }
        }
        else
        {
            _encoder.FlushAndClose();
        }

        if (_encodingFailure != null)
        {
            Logger.Log($"LIBAV_SINK_STOP_FAIL type={_encodingFailure.GetType().Name} msg={_encodingFailure.Message}");
            return FinalizeResult.Failure(outputPath, $"Stopped (libav encode failed: {_encodingFailure.Message})");
        }

        if (context?.HdrPipelineActive == true)
        {
            var (validationSucceeded, validationDetail) = await HdrValidationRunner
                .RunAsync(context, outputPath, cancellationToken)
                .ConfigureAwait(false);

            if (!validationSucceeded)
            {
                if (validationDetail.Contains("validator-script-missing", StringComparison.Ordinal))
                {
                    Logger.Log($"HDR validation skipped (script not found): {validationDetail}");
                }
                else
                {
                    return FinalizeResult.Failure(
                        outputPath,
                        $"Stopped (hdr validation failed: {validationDetail})",
                        new[] { outputPath });
                }
            }
        }

        Logger.Log(
            $"LIBAV_SINK_STOP output='{outputPath}' frames={EncodedVideoFrames} dropped={DroppedVideoFrames} audio_samples={AudioSamplesReceived} mic_samples={MicrophoneSamplesReceived}");
        return FinalizeResult.Success(outputPath, "Stopped");
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);
        _cts?.Cancel();

        if (_encodingTask != null)
        {
            try
            {
                var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(1000)).ConfigureAwait(false);
                if (ReferenceEquals(completedTask, _encodingTask))
                {
                    await _encodingTask.ConfigureAwait(false);
                }
                else
                {
                    Logger.Log("LIBAV_SINK_DISPOSE_TIMEOUT task=encoding_loop");
                    _cts?.Cancel();
                    // Fall through to cleanup
                }
            }
            catch
            {
                // Best-effort cleanup path.
            }
        }

        ReturnRemainingBuffers(_videoQueue);
        ReturnRemainingBuffers(_audioQueue);
        ReturnRemainingBuffers(_microphoneQueue);
        ReturnRemainingGpuBuffers(_gpuQueue);
        ReturnRemainingCudaFrames(_cudaQueue);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _microphoneQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _cudaQueueDepth, 0);

        _cts?.Dispose();
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _cudaQueue = null;
        _gpuEncodingEnabled = false;
        _cudaEncodingEnabled = false;
        _microphoneEnabled = false;
        _encodingTask = null;
        _workAvailable.Dispose();

        try
        {
            _encoder.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"LIBAV_SINK_DISPOSE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static string MapCodecName(RecordingFormat format)
    {
        return format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => "av1_nvenc",
            _ => "h264_nvenc"
        };
    }

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(RecordingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FrameRateArg) || !context.FrameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = context.FrameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var numerator) ||
            !int.TryParse(parts[1], out var denominator) ||
            numerator <= 0 ||
            denominator <= 0)
        {
            return (null, null);
        }

        return (numerator, denominator);
    }

    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork();
    }

    private static void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue)
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

    private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            Marshal.Release(packet.Texture);
        }
    }

    private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue)
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
    }

    private LibAvEncoderOptions CreateOptions(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context);
        return new LibAvEncoderOptions
        {
            OutputPath = context.FinalOutputPath,
            CodecName = MapCodecName(context.Settings.Format),
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            NvencPreset = context.Settings.NvencPreset,
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            CudaHwDeviceCtxPtr = context.CudaHwDeviceCtxPtr,
            CudaHwFramesCtxPtr = context.CudaHwFramesCtxPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = _microphoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }

    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");
            var microphoneQueue = _microphoneQueue;
            var gpuQueue = _gpuQueue;
            var cudaQueue = _cudaQueue;

            while (true)
            {
                var madeProgress = false;
                if (cudaQueue != null)
                {
                    madeProgress = DrainCudaPackets(cudaQueue.Reader);
                }
                if (gpuQueue != null)
                {
                    madeProgress = DrainGpuPackets(gpuQueue.Reader) || madeProgress;
                }

                madeProgress = DrainVideoPackets(videoQueue.Reader) || madeProgress;
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    (microphoneQueue == null || microphoneQueue.Reader.Completion.IsCompleted) &&
                    (gpuQueue == null || gpuQueue.Reader.Completion.IsCompleted) &&
                    (cudaQueue == null || cudaQueue.Reader.Completion.IsCompleted) &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0 &&
                    Volatile.Read(ref _microphoneQueueDepth) == 0 &&
                    Volatile.Read(ref _gpuQueueDepth) == 0 &&
                    Volatile.Read(ref _cudaQueueDepth) == 0)
                {
                    break;
                }

                if (madeProgress)
                {
                    continue;
                }

                _workAvailable.Wait(cancellationToken);
            }

            _encoder.FlushAndClose();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReturnRemainingBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue);
            ReturnRemainingCudaFrames(_cudaQueue);
        }
        catch (Exception ex)
        {
            _encodingFailure = ex;
            lock (_sync) { _started = false; }
            ReturnRemainingBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue);
            ReturnRemainingCudaFrames(_cudaQueue);
            try
            {
                _encoder.Dispose();
            }
            catch
            {
                // Preserve the original failure.
            }
        }
    }

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            Interlocked.Decrement(ref _videoQueueDepth);
            try
            {
                _encoder.SendVideoFrame(packet.Buffer.AsSpan(0, packet.Length), _width, _height);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            Interlocked.Decrement(ref _gpuQueueDepth);
            try
            {
                _encoder.SendGpuVideoFrame(packet.Texture, packet.Subresource);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                Marshal.Release(packet.Texture);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            Interlocked.Decrement(ref _cudaQueueDepth);
            var frame = (AVFrame*)packet.Frame;
            try
            {
                _encoder.SendCudaVideoFrame(frame);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_SINK_CUDA_DRAIN_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                if (frame != null)
                {
                    ffmpeg.av_frame_free(&frame);
                }
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            Interlocked.Decrement(ref _audioQueueDepth);
            try
            {
                _encoder.SendAudioSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            Interlocked.Decrement(ref _microphoneQueueDepth);
            try
            {
                _encoder.SendMicrophoneSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private void SignalWork()
    {
        try { _workAvailable.Release(); }
        catch (SemaphoreFullException) { /* Best-effort: semaphore already signaled — work loop will pick it up */ }
    }

    private bool TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref _videoQueueDepth);
            Interlocked.Increment(ref _videoFramesEnqueued);
            SignalWork();
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref _videoQueueDepth);
            _encoder.SkipVideoFrame();  // Advance PTS to prevent A/V drift from evicted frames
            ReturnBuffer(evictedPacket.Buffer);
            var evictedCount = 1;

            // Burst eviction: under sustained overload, drain a burst to give the encoder headroom.
            var now = Environment.TickCount64;
            if (now - Interlocked.Read(ref _lastBurstEvictionTick) >= 1000)
            {
                Interlocked.Exchange(ref _lastBurstEvictionTick, now);
                var burstSize = Math.Clamp(Volatile.Read(ref _videoQueueDepth) / 10, 1, 30);
                for (var i = 1; i < burstSize && queue.Reader.TryRead(out var extra); i++)
                {
                    Interlocked.Decrement(ref _videoQueueDepth);
                    _encoder.SkipVideoFrame();  // Advance PTS for each evicted frame
                    ReturnBuffer(extra.Buffer);
                    evictedCount++;
                }

                if (evictedCount > 1)
                {
                    Logger.Log($"LIBAV_SINK_BURST_EVICT count={evictedCount} queue_depth={Volatile.Read(ref _videoQueueDepth)}");
                }
            }

            Interlocked.Add(ref _videoDropsBacklogEviction, evictedCount);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _videoQueueDepth);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork();
                return true;
            }
        }

        Interlocked.Increment(ref _droppedVideoFrames);
        _encoder.SkipVideoFrame();  // Advance PTS even on final drop to prevent A/V drift
        ReturnBuffer(packet.Buffer);
        return false;
    }

    private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref _audioQueueDepth);
            SignalWork();
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref _audioQueueDepth);
            Interlocked.Increment(ref _audioDropsBacklogEviction);
            ReturnBuffer(evictedPacket.Buffer);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _audioQueueDepth);
                SignalWork();
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

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref _microphoneQueueDepth);
            SignalWork();
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref _microphoneQueueDepth);
            Interlocked.Increment(ref _microphoneDropsBacklogEviction);
            ReturnBuffer(evictedPacket.Buffer);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _microphoneQueueDepth);
                SignalWork();
                return true;
            }
        }

        ReturnBuffer(packet.Buffer);
        return false;
    }

    private static byte[] GetBuffer(int size)
    {
        return ArrayPool<byte>.Shared.Rent(size);
    }

    private static void ReturnBuffer(byte[] buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private readonly record struct VideoFramePacket(byte[] Buffer, int Length);
    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);
    private readonly record struct CudaFramePacket(IntPtr Frame);
}
