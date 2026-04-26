using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using FFmpeg.AutoGen;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Runtime;

namespace ElgatoCapture.Services.Recording;

public sealed class LibAvRecordingSink : IRecordingSink, IRawVideoFrameEncoder, IRawVideoFrameTryEncoder, IRawVideoFrameLeaseEncoder, IRawVideoFrameLeaseTryEncoder, IGpuVideoFrameEncoder, IGpuVideoFrameTryEncoder, ICudaVideoFrameEncoder
{
    private const int VideoQueueCapacity = 360;
    private const int AudioQueueCapacity = 3600;
    private const int GpuQueueCapacity = 4;
    private const int CudaQueueCapacity = 12;
    private const int QueueBackpressureTimeoutMs = 250;
    private const int StopTimeoutMs = 30_000;
    private const int DisposeTimeoutMs = 1_000;
    private const int VideoQueueLatencyWindowSize = 256;

    private readonly object _sync = new();
    private readonly object _videoQueueSync = new();
    private readonly object _videoQueueLatencySync = new();
    private readonly object _videoSequenceSync = new();
    private readonly Queue<long> _videoQueueEnqueueTicks = new();
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
    private int _disposeFinalized;
    private int _deferredDisposeScheduled;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoFramesSubmittedToEncoder;
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
    private int _videoQueueMaxDepth;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private int _gpuQueueMaxDepth;
    private int _cudaQueueDepth;
    private int _cudaQueueMaxDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;
    private long _lastVideoQueueLatencyMs;
    private long _videoBackpressureWaitMs;
    private long _videoBackpressureEvents;
    private long _lastVideoBackpressureWaitMs;
    private long _maxVideoBackpressureWaitMs;
    private long _videoSequenceGaps;
    private long _lastVideoSequenceNumber = -1;
    private readonly double[] _videoQueueLatencySamples = new double[VideoQueueLatencyWindowSize];
    private int _videoQueueLatencySampleCount;
    private int _videoQueueLatencySampleIndex;
    private bool _gpuEncodingEnabled;
    private bool _cudaEncodingEnabled;

    public event EventHandler<long>? FrameEncoded;

    /// <summary>
    /// Invoked on the encoding thread when the encoding loop fails fatally.
    /// Allows CaptureService to immediately surface the failure to the UI
    /// instead of silently dropping all subsequent frames until Stop is called.
    /// </summary>
    public Action<Exception>? OnEncodingFailed { get; set; }

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
    public int VideoQueueCapacityFrames => VideoQueueCapacity;
    public int VideoQueueMaxDepth => Volatile.Read(ref _videoQueueMaxDepth);
    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public int MicrophoneQueueCount => Volatile.Read(ref _microphoneQueueDepth);
    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoFramesSubmittedToEncoder => Interlocked.Read(ref _videoFramesSubmittedToEncoder);
    public long VideoEncoderPts => _encoder.NextVideoPts;
    public long VideoEncoderPacketsWritten => _encoder.VideoPacketsWritten;
    public long VideoEncoderDroppedFrames => _encoder.DroppedFrameCount;
    public long VideoSequenceGaps => Interlocked.Read(ref _videoSequenceGaps);
    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);
    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);
    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);
    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);
    public long LastVideoQueueLatencyMs => Interlocked.Read(ref _lastVideoQueueLatencyMs);
    public long VideoQueueOldestFrameAgeMs => GetVideoQueueOldestFrameAgeMs();
    public int VideoQueueLatencySampleCount => GetVideoQueueLatencyMetrics().SampleCount;
    public double VideoQueueLatencyAvgMs => GetVideoQueueLatencyMetrics().AverageMs;
    public double VideoQueueLatencyP95Ms => GetVideoQueueLatencyMetrics().P95Ms;
    public double VideoQueueLatencyMaxMs => GetVideoQueueLatencyMetrics().MaxMs;
    public long VideoBackpressureWaitMs => Interlocked.Read(ref _videoBackpressureWaitMs);
    public long VideoBackpressureEvents => Interlocked.Read(ref _videoBackpressureEvents);
    public long LastVideoBackpressureWaitMs => Interlocked.Read(ref _lastVideoBackpressureWaitMs);
    public long MaxVideoBackpressureWaitMs => Interlocked.Read(ref _maxVideoBackpressureWaitMs);
    public bool GpuEncodingEnabled => Volatile.Read(ref _gpuEncodingEnabled);
    public bool CudaEncodingEnabled => Volatile.Read(ref _cudaEncodingEnabled);
    public int GpuQueueCount => Volatile.Read(ref _gpuQueueDepth);
    public int GpuQueueCapacityFrames => GpuQueueCapacity;
    public int GpuQueueMaxDepth => Volatile.Read(ref _gpuQueueMaxDepth);
    public long GpuFramesEnqueued => Interlocked.Read(ref _gpuFramesEnqueued);
    public long GpuFramesDropped => Interlocked.Read(ref _gpuFramesDropped);
    public int CudaQueueCount => Volatile.Read(ref _cudaQueueDepth);
    public int CudaQueueCapacityFrames => CudaQueueCapacity;
    public int CudaQueueMaxDepth => Volatile.Read(ref _cudaQueueMaxDepth);
    public long CudaFramesEnqueued => Interlocked.Read(ref _cudaFramesEnqueued);
    public long CudaFramesDropped => Interlocked.Read(ref _cudaFramesDropped);
    public bool EncodingFailed => Volatile.Read(ref _encodingFailure) != null;
    public string? EncodingFailureType => Volatile.Read(ref _encodingFailure)?.GetType().Name;
    public string? EncodingFailureMessage => Volatile.Read(ref _encodingFailure)?.Message;
    internal Task EncodingCompletionTask => _encodingTask ?? Task.CompletedTask;

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
            Interlocked.Exchange(ref _videoFramesSubmittedToEncoder, 0);
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
            Interlocked.Exchange(ref _videoQueueMaxDepth, 0);
            Interlocked.Exchange(ref _gpuQueueMaxDepth, 0);
            Interlocked.Exchange(ref _cudaQueueMaxDepth, 0);
            Interlocked.Exchange(ref _videoQueueDepth, 0);
            Interlocked.Exchange(ref _audioQueueDepth, 0);
            Interlocked.Exchange(ref _microphoneQueueDepth, 0);
            Interlocked.Exchange(ref _gpuQueueDepth, 0);
            Interlocked.Exchange(ref _cudaQueueDepth, 0);
            Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
            Interlocked.Exchange(ref _lastVideoWriteTick, 0);
            ResetVideoDiagnostics();
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

    // REVIEWED 2026-04-07: IDisposable fallback only — all callers use DisposeAsync.
    // CaptureService cleanup awaits StopAsync/DisposeAsync on background thread.
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

        if (_encodingTask == null)
        {
            FinalizeDisposeCore();
            return;
        }

        var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(DisposeTimeoutMs)).ConfigureAwait(false);
        if (ReferenceEquals(completedTask, _encodingTask))
        {
            ObserveEncodingTaskCompletion(_encodingTask);
            FinalizeDisposeCore();
            return;
        }

        Logger.Log($"LIBAV_SINK_DISPOSE_DEFERRED timeout_ms={DisposeTimeoutMs}");
        ScheduleDeferredDisposeCleanup(_encodingTask);
    }

    private void ScheduleDeferredDisposeCleanup(Task encodingTask)
    {
        if (Interlocked.CompareExchange(ref _deferredDisposeScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure ??= ex;
            }
            finally
            {
                FinalizeDisposeCore();
                Logger.Log("LIBAV_SINK_DISPOSE_DEFERRED_COMPLETE");
            }
        });
    }

    private void ObserveEncodingTaskCompletion(Task encodingTask)
    {
        try
        {
            encodingTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _encodingFailure ??= ex;
        }
    }

    private void FinalizeDisposeCore()
    {
        if (Interlocked.CompareExchange(ref _disposeFinalized, 1, 0) != 0)
        {
            return;
        }

        ReturnRemainingVideoBuffers(_videoQueue);
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
        => MediaFormat.MapNvencCodecName(format);

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
            _videoQueueEnqueueTicks.Clear();
        }

        Interlocked.Exchange(ref _videoQueueDepth, 0);
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
            OutputPath = context.VideoOutputPath,
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
            ReturnRemainingVideoBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue);
            ReturnRemainingCudaFrames(_cudaQueue);
        }
        catch (Exception ex)
        {
            _encodingFailure = ex;
            lock (_sync) { _started = false; }
            Logger.Log($"LIBAV_SINK_ENCODING_LOOP_FAIL type={ex.GetType().Name} msg={ex.Message}");
            ReturnRemainingVideoBuffers(_videoQueue);
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

            try
            {
                OnEncodingFailed?.Invoke(ex);
            }
            catch
            {
                // Best effort — callback must not mask the original failure.
            }
        }
    }

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader)
    {
        var drainedAny = false;
        while (true)
        {
            VideoFramePacket packet;
            lock (_videoQueueSync)
            {
                if (!reader.TryRead(out packet))
                {
                    break;
                }

                RemoveQueuedVideoTick(packet.EnqueueTick);
                Interlocked.Decrement(ref _videoQueueDepth);
            }

            RecordVideoPacketDequeued(packet);
            try
            {
                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, _width, _height);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
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
                ReturnVideoPacket(packet);
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
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
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
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
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

    private static void UpdateMaxDepth(ref int target, int depth)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (depth <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, depth, current) == current)
            {
                return;
            }
        }
    }

    private static void UpdateMaxValue(ref long target, long value)
    {
        while (true)
        {
            var current = Interlocked.Read(ref target);
            if (value <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

    private void ResetVideoDiagnostics()
    {
        Interlocked.Exchange(ref _lastVideoQueueLatencyMs, 0);
        Interlocked.Exchange(ref _videoBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _videoBackpressureEvents, 0);
        Interlocked.Exchange(ref _lastVideoBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _maxVideoBackpressureWaitMs, 0);
        Interlocked.Exchange(ref _videoSequenceGaps, 0);
        Interlocked.Exchange(ref _lastVideoSequenceNumber, -1);
        lock (_videoQueueSync)
        {
            _videoQueueEnqueueTicks.Clear();
        }

        lock (_videoQueueLatencySync)
        {
            Array.Clear(_videoQueueLatencySamples, 0, _videoQueueLatencySamples.Length);
            _videoQueueLatencySampleCount = 0;
            _videoQueueLatencySampleIndex = 0;
        }
    }

    private long GetVideoQueueOldestFrameAgeMs()
    {
        lock (_videoQueueSync)
        {
            while (_videoQueueEnqueueTicks.Count > Volatile.Read(ref _videoQueueDepth))
            {
                _videoQueueEnqueueTicks.Dequeue();
            }

            return _videoQueueEnqueueTicks.Count == 0
                ? 0
                : Math.Max(0, Environment.TickCount64 - _videoQueueEnqueueTicks.Peek());
        }
    }

    private void TrackQueuedVideoTick(long enqueueTick)
    {
        _videoQueueEnqueueTicks.Enqueue(enqueueTick);
    }

    private void RemoveQueuedVideoTick(long expectedEnqueueTick)
    {
        if (_videoQueueEnqueueTicks.Count == 0)
        {
            return;
        }

        var queuedTick = _videoQueueEnqueueTicks.Dequeue();
        if (queuedTick != expectedEnqueueTick)
        {
            Logger.Log($"LIBAV_SINK_QUEUE_TICK_MISMATCH expected={expectedEnqueueTick} actual={queuedTick}");
        }
    }

    private void RecordVideoBackpressure(long startTick, long endTick)
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

        Interlocked.Increment(ref _videoBackpressureEvents);
        Interlocked.Add(ref _videoBackpressureWaitMs, elapsedMs);
        Interlocked.Exchange(ref _lastVideoBackpressureWaitMs, elapsedMs);
        UpdateMaxValue(ref _maxVideoBackpressureWaitMs, elapsedMs);
    }

    private void RecordVideoPacketDequeued(VideoFramePacket packet)
    {
        var latencyMs = Math.Max(0, Environment.TickCount64 - packet.EnqueueTick);
        Interlocked.Exchange(ref _lastVideoQueueLatencyMs, latencyMs);
        lock (_videoQueueLatencySync)
        {
            _videoQueueLatencySamples[_videoQueueLatencySampleIndex] = latencyMs;
            _videoQueueLatencySampleIndex = (_videoQueueLatencySampleIndex + 1) % _videoQueueLatencySamples.Length;
            if (_videoQueueLatencySampleCount < _videoQueueLatencySamples.Length)
            {
                _videoQueueLatencySampleCount++;
            }
        }

        if (packet.SequenceNumber.HasValue)
        {
            lock (_videoSequenceSync)
            {
                var last = Interlocked.Read(ref _lastVideoSequenceNumber);
                var current = packet.SequenceNumber.Value;
                if (last >= 0 && current > last + 1)
                {
                    Interlocked.Add(ref _videoSequenceGaps, current - last - 1);
                }

                if (current > last)
                {
                    Interlocked.Exchange(ref _lastVideoSequenceNumber, current);
                }
            }
        }
    }

    private (int SampleCount, double AverageMs, double P95Ms, double MaxMs) GetVideoQueueLatencyMetrics()
    {
        double[] copy;
        int count;
        lock (_videoQueueLatencySync)
        {
            count = _videoQueueLatencySampleCount;
            if (count <= 0)
            {
                return (0, 0, 0, 0);
            }

            copy = new double[count];
            Array.Copy(_videoQueueLatencySamples, copy, count);
        }

        Array.Sort(copy);
        var total = 0.0;
        for (var i = 0; i < copy.Length; i++)
        {
            total += copy[i];
        }

        var p95Index = Math.Clamp((int)Math.Ceiling(copy.Length * 0.95) - 1, 0, copy.Length - 1);
        return (copy.Length, total / copy.Length, copy[p95Index], copy[^1]);
    }

    private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        var deadlineTick = Environment.TickCount64 + QueueBackpressureTimeoutMs;
        long backpressureStartTick = 0;
        while (true)
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

                if (queue.Writer.TryWrite(packet))
                {
                    RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                    TrackQueuedVideoTick(packet.EnqueueTick);
                    UpdateMaxDepth(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth));
                    Interlocked.Increment(ref _videoFramesEnqueued);
                    SignalWork();
                    return VideoEnqueueResult.Accepted;
                }

                if (!_started ||
                    _cts?.IsCancellationRequested == true ||
                    Volatile.Read(ref _encodingFailure) != null)
                {
                    ReturnVideoPacket(packet);
                    return VideoEnqueueResult.Rejected;
                }

                if (Environment.TickCount64 < deadlineTick)
                {
                    backpressureStartTick = backpressureStartTick == 0 ? Environment.TickCount64 : backpressureStartTick;
                    overloadFailure = null;
                }
                else
                {
                    RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                    Interlocked.Increment(ref _droppedVideoFrames);
                    overloadFailure = new InvalidOperationException(
                        $"LibAv recording video queue overloaded after {QueueBackpressureTimeoutMs}ms backpressure: capacity={VideoQueueCapacity} depth={Volatile.Read(ref _videoQueueDepth)}");
                    ReturnVideoPacket(packet);
                }
            }

            if (overloadFailure != null)
            {
                FailEncoding(overloadFailure);
                return VideoEnqueueResult.Overloaded;
            }

            SignalWork();
            Thread.Sleep(1);
        }
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        var deadlineTick = Environment.TickCount64 + QueueBackpressureTimeoutMs;
        long backpressureStartTick = 0;
        while (true)
        {
            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                Marshal.Release(packet.Texture);
                return VideoEnqueueResult.Rejected;
            }

            if (queue.Writer.TryWrite(packet))
            {
                RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                UpdateMaxDepth(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth));
                Interlocked.Increment(ref _gpuFramesEnqueued);
                SignalWork();
                return VideoEnqueueResult.Accepted;
            }

            if (!_started ||
                _cts?.IsCancellationRequested == true ||
                Volatile.Read(ref _encodingFailure) != null)
            {
                Marshal.Release(packet.Texture);
                return VideoEnqueueResult.Rejected;
            }

            if (Environment.TickCount64 >= deadlineTick)
            {
                RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                Marshal.Release(packet.Texture);
                FailEncoding(new InvalidOperationException(
                    $"LibAv GPU recording queue overloaded after {QueueBackpressureTimeoutMs}ms backpressure: capacity={GpuQueueCapacity} depth={Volatile.Read(ref _gpuQueueDepth)}"));
                return VideoEnqueueResult.Overloaded;
            }

            backpressureStartTick = backpressureStartTick == 0 ? Environment.TickCount64 : backpressureStartTick;
            SignalWork();
            Thread.Sleep(1);
        }
    }

    private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)
    {
        var deadlineTick = Environment.TickCount64 + QueueBackpressureTimeoutMs;
        long backpressureStartTick = 0;
        while (true)
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

            if (queue.Writer.TryWrite(packet))
            {
                RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                UpdateMaxDepth(ref _cudaQueueMaxDepth, Interlocked.Increment(ref _cudaQueueDepth));
                Interlocked.Increment(ref _cudaFramesEnqueued);
                SignalWork();
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

            if (Environment.TickCount64 >= deadlineTick)
            {
                RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64);
                var frame = (AVFrame*)packet.Frame;
                if (frame != null)
                {
                    ffmpeg.av_frame_free(&frame);
                }

                FailEncoding(new InvalidOperationException(
                    $"LibAv CUDA recording queue overloaded after {QueueBackpressureTimeoutMs}ms backpressure: capacity={CudaQueueCapacity} depth={Volatile.Read(ref _cudaQueueDepth)}"));
                return VideoEnqueueResult.Overloaded;
            }

            backpressureStartTick = backpressureStartTick == 0 ? Environment.TickCount64 : backpressureStartTick;
            SignalWork();
            Thread.Sleep(1);
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
            var evicted = Interlocked.Increment(ref _audioDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                // Log evicted audio bytes so A/V drift from dropped audio is traceable.
                Logger.Log(
                    $"LIBAV_SINK_AUDIO_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _audioQueueDepth)}");
            }

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
            var evicted = Interlocked.Increment(ref _microphoneDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                Logger.Log(
                    $"LIBAV_SINK_MIC_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _microphoneQueueDepth)}");
            }

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
    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
    private readonly record struct GpuFramePacket(IntPtr Texture, int Subresource);
    private readonly record struct CudaFramePacket(IntPtr Frame);
}
