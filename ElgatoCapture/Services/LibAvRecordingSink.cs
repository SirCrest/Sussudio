using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

public sealed class LibAvRecordingSink : IRecordingSink, IRawVideoFrameEncoder
{
    private const int VideoQueueCapacity = 360;
    private const int AudioQueueCapacity = 3600;
    private const int StopTimeoutMs = 30_000;

    private readonly object _sync = new();
    private readonly LibAvEncoder _encoder = new();
    private readonly SemaphoreSlim _workAvailable = new(0, 1);
    private Channel<VideoFramePacket>? _videoQueue;
    private Channel<AudioSamplePacket>? _audioQueue;
    private CancellationTokenSource? _cts;
    private Task? _encodingTask;
    private RecordingContext? _context;
    private Exception? _encodingFailure;
    private int _width;
    private int _height;
    private bool _audioEnabled;
    private bool _started;
    private bool _disposed;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private int _videoQueueDepth;
    private int _audioQueueDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;

    public event EventHandler<long>? FrameEncoded;

    public long DroppedVideoFrames => Interlocked.Read(ref _droppedVideoFrames) + _encoder.DroppedFrameCount;
    public long EncodedVideoFrames => Interlocked.Read(ref _encodedVideoFrames);
    public long AudioSamplesReceived => _encoder.AudioSamplesReceived;
    public long OutputBytes => _encoder.TotalBytesWritten;
    public string OutputPath => _context?.FinalOutputPath ?? _encoder.OutputPath;
    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);
    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);
    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);
    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);
    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);

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
            LibAvEncoder.InitializeFFmpeg();

            var options = CreateOptions(context);
            _encoder.Initialize(options);

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
            Interlocked.Exchange(ref _videoQueueDepth, 0);
            Interlocked.Exchange(ref _audioQueueDepth, 0);
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
                $"width={_width} height={_height} fps={context.EffectiveFrameRate:0.###} p010={context.HdrPipelineActive} audio={_audioEnabled}");
            return Task.CompletedTask;
        }
        catch
        {
            CompleteWriter(_videoQueue);
            CompleteWriter(_audioQueue);
            _videoQueue = null;
            _audioQueue = null;
            _cts?.Dispose();
            _cts = null;
            _encodingTask = null;
            _context = null;
            _width = 0;
            _height = 0;
            _audioEnabled = false;
            lock (_sync)
            {
                _started = false;
            }

            _encoder.Dispose();
            throw;
        }
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

    public Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
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
            $"LIBAV_SINK_STOP output='{outputPath}' frames={EncodedVideoFrames} dropped={DroppedVideoFrames} audio_samples={AudioSamplesReceived}");
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
                    return;
                }
            }
            catch
            {
                // Best-effort cleanup path.
            }
        }

        ReturnRemainingBuffers(_videoQueue);
        ReturnRemainingBuffers(_audioQueue);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);

        _cts?.Dispose();
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
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
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000
        };
    }

    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");

            while (true)
            {
                var madeProgress = DrainVideoPackets(videoQueue.Reader);
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0)
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
            ReturnRemainingBuffers(_audioQueue);
        }
        catch (Exception ex)
        {
            _encodingFailure = ex;
            ReturnRemainingBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue);
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

    private void SignalWork()
    {
        try { _workAvailable.Release(); }
        catch (SemaphoreFullException) { }
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
            Interlocked.Increment(ref _videoDropsBacklogEviction);
            ReturnBuffer(evictedPacket.Buffer);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _videoQueueDepth);
                Interlocked.Increment(ref _videoFramesEnqueued);
                SignalWork();
                return true;
            }
        }

        Interlocked.Increment(ref _droppedVideoFrames);
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
}
