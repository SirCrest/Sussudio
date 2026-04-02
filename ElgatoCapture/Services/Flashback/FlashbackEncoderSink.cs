using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

internal sealed class FlashbackEncoderSink : IRecordingSink, IRawVideoFrameEncoder, IGpuVideoFrameEncoder
{
    private const int VideoQueueCapacity = 180;
    private const int AudioQueueCapacity = 1800;
    private const int GpuQueueCapacity = 8;
    private const int StopTimeoutMs = 30_000;

    private readonly object _sync = new();
    private readonly LibAvEncoder _encoder = new();
    private readonly FlashbackBufferManager _bufferManager;
    private readonly ManualResetEventSlim _workAvailable = new(false, 100);
    private readonly bool _ownsBufferManager;
    private Channel<VideoFramePacket>? _videoQueue;
    private Channel<AudioSamplePacket>? _audioQueue;
    private Channel<AudioSamplePacket>? _microphoneQueue;
    private Channel<GpuFramePacket>? _gpuQueue;
    private CancellationTokenSource? _cts;
    private Task? _encodingTask;
    private FlashbackSessionContext? _sessionContext;
    private Exception? _encodingFailure;
    private int _width;
    private int _height;
    private bool _audioEnabled;
    private bool _microphoneEnabled;
    private volatile bool _started;
    private volatile bool _disposed;
    private bool _gpuEncodingEnabled;

    private bool _forceRotateRequested;
    private volatile TaskCompletionSource<IReadOnlyList<string>>? _forceRotateTcs;
    private TimeSpan _forceRotateInPoint;
    private TimeSpan _forceRotateOutPoint;

    private long _segmentStartBytes;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _audioSamplesReceived;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private long _droppedAudioSamplesCount;
    private long _microphoneDropsQueueSaturated;
    private long _microphoneDropsBacklogEviction;
    private long _gpuFramesEnqueued;
    private long _gpuFramesDropped;
    private Action<Exception>? _onFatalError;
    private bool _forceRotateDraining;
    private int _videoQueueDepth;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;
    private long _lastBurstEvictionTick;
    private string? _tsFilePath;
    private string? _recordingOutputPath;
    private int _recordingActive;
    private TimeSpan _segmentStartPts;
    private TimeSpan _segmentDuration;
    private TimeSpan _ptsBaseOffset;

    public FlashbackEncoderSink(FlashbackBufferOptions? options = null)
    {
        var opts = options ?? new FlashbackBufferOptions();
        _bufferManager = new FlashbackBufferManager(opts);
        _ownsBufferManager = true;
    }

    public FlashbackEncoderSink(FlashbackBufferManager bufferManager)
    {
        ArgumentNullException.ThrowIfNull(bufferManager);
        _bufferManager = bufferManager;
        _ownsBufferManager = false;
    }

    public event EventHandler<long>? FrameEncoded;

    public long DroppedVideoFrames =>
        Interlocked.Read(ref _droppedVideoFrames) +
        Interlocked.Read(ref _gpuFramesDropped) +
        _encoder.DroppedFrameCount;

    public long EncodedVideoFrames => Interlocked.Read(ref _encodedVideoFrames);

    public long AudioSamplesReceived => Interlocked.Read(ref _audioSamplesReceived);

    public long OutputBytes => _bufferManager.TotalDiskBytes;
    public long TotalBytesWritten => _bufferManager.TotalBytesWritten;

    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);

    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);

    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);

    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);

    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);

    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);

    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);

    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);

    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);

    public bool GpuEncodingEnabled => Volatile.Read(ref _gpuEncodingEnabled);

    public bool AudioEnabled => Volatile.Read(ref _audioEnabled);

    public bool MicrophoneEnabled => Volatile.Read(ref _microphoneEnabled);

    /// <summary>
    /// Registers a callback invoked when the encoding loop encounters a fatal error.
    /// This lets the owning CaptureService surface the failure rather than going silently dead.
    /// </summary>
    public void SetFatalErrorCallback(Action<Exception>? callback) => _onFatalError = callback;

    public string? CodecName => _sessionContext?.CodecName;
    public uint TargetBitRate => _sessionContext?.BitRate ?? 0;
    public int EncoderWidth => _width;
    public int EncoderHeight => _height;
    public double EncoderFrameRate => _sessionContext?.FrameRate ?? 0;

    public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_started)
            {
                throw new InvalidOperationException("Flashback encoder sink has already started.");
            }
            _started = true;
        }

        try
        {
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            var sessionId = _bufferManager.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = CreateSessionId();
                _bufferManager.Initialize(sessionId);
            }
            _bufferManager.SetSegmentExtension(GetSegmentExtension(context.CodecName));

            var tsPath = _bufferManager.GetFilePath();
            _tsFilePath = tsPath;
            _recordingOutputPath = string.Empty;

            _encoder.Initialize(CreateOptions(context, tsPath));

            // FullMode = Wait only affects WriteAsync (which we never call).
            // TryWrite returns false immediately when full regardless of FullMode,
            // allowing our manual eviction paths to handle resource cleanup (COM Release,
            // ArrayPool Return) before dropping the packet.
            if (!_encoder.UseHardwareFrames && (_width >= 2560 || _height >= 1440))
            {
                Logger.Log($"FLASHBACK_SINK_WARN_CPU_ENCODING width={_width} height={_height} — GPU encoding unavailable, performance will be severely degraded");
            }

            if (_encoder.UseHardwareFrames)
            {
                _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
                _gpuEncodingEnabled = true;
                Logger.Log($"FLASHBACK_SINK_GPU_QUEUE_INIT capacity={GpuQueueCapacity}");
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
            if (context.MicrophoneEnabled)
            {
                _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            }

            _cts = new CancellationTokenSource();
            _sessionContext = context;
            _encodingFailure = null;
            _width = context.Width;
            _height = context.Height;
            _audioEnabled = context.AudioEnabled;
            _microphoneEnabled = context.MicrophoneEnabled;
            ResetEncodingCounters();
            Volatile.Write(ref _recordingActive, 0);
            // When continuing after a sink-only cycle (ptsBaseOffset > 0), we offset
            // the encoder's file-level PTS directly so segment timestamps continue from
            // the previous session. _ptsBaseOffset stays Zero because the buffer PTS
            // formula is: _ptsBaseOffset + encoder.NextVideoPts / frameRate — and the
            // encoder PTS already includes the offset.
            _ptsBaseOffset = TimeSpan.Zero;
            _segmentStartPts = ptsBaseOffset;
            _segmentDuration = _bufferManager.Options.SegmentDuration;

            if (ptsBaseOffset > TimeSpan.Zero && context.FrameRate > 0)
            {
                var initialVideoPts = (long)(ptsBaseOffset.TotalSeconds * context.FrameRate);
                var initialAudioPts = (long)(ptsBaseOffset.TotalSeconds * 48_000);
                _encoder.SetInitialPts(initialVideoPts, initialAudioPts);
                Logger.Log($"FLASHBACK_SINK_PTS_CONTINUE v_pts={initialVideoPts} a_pts={initialAudioPts} offset_s={ptsBaseOffset.TotalSeconds:F1}");
            }

            Logger.Log($"FLASHBACK_SINK_INIT_COMPLETE session='{sessionId}' gpu_encoding={_gpuEncodingEnabled} segment_duration_s={_segmentDuration.TotalSeconds:F0}");

            // Publish the encoder's frame rate as ground truth for playback pacing.
            _bufferManager.EncodeFrameRate = context.FrameRate;

            _encodingTask = Task.Factory.StartNew(
                () => EncodingLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Logger.Log(
                $"FLASHBACK_SINK_START session='{sessionId}' output='{tsPath}' codec='{context.CodecName}' " +
                $"width={_width} height={_height} fps={context.FrameRate:0.###} " +
                $"buffer_ms={(long)_bufferManager.Options.BufferDuration.TotalMilliseconds} " +
                $"audio={_audioEnabled} microphone={_microphoneEnabled} p010={context.IsP010}");
            return Task.CompletedTask;
        }
        catch
        {
            /* Cleanup must not throw — tear down partially-initialized queues/state before re-throwing */
            CompleteWriter(_videoQueue);
            CompleteWriter(_audioQueue);
            CompleteWriter(_microphoneQueue);
            CompleteWriter(_gpuQueue);
            _videoQueue = null;
            _audioQueue = null;
            _microphoneQueue = null;
            _gpuQueue = null;
            _gpuEncodingEnabled = false;
            _cts?.Dispose();
            _cts = null;
            _encodingTask = null;
            _sessionContext = null;
            _width = 0;
            _height = 0;
            _audioEnabled = false;
            _microphoneEnabled = false;
            lock (_sync)
            {
                _started = false;
            }

            _bufferManager.PurgeAllSegments();
            _encoder.Dispose();
            throw;
        }
    }

    Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return StartAsync(CreateSessionContext(context), cancellationToken);
    }

    public TimeSpan LastRecordingStartPts { get; private set; }
    public TimeSpan LastRecordingEndPts { get; private set; }

    public void BeginRecording(string outputPath)
    {
        lock (_sync)
        {
            if (_disposed || !_started) return;
        }

        if (_encodingFailure != null)
            throw new InvalidOperationException("Cannot begin recording: encoding loop has failed", _encodingFailure);
        if (_encodingTask?.IsCompleted == true && _encodingTask.IsFaulted)
            throw new InvalidOperationException("Cannot begin recording: encoding task has terminated");

        _recordingOutputPath = outputPath ?? string.Empty;
        Volatile.Write(ref _recordingActive, 1);
        _bufferManager.PauseEviction();
        Logger.Log($"FLASHBACK_RECORDING_BEGIN output='{_recordingOutputPath}'");
    }

    public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Volatile.Write(ref _recordingActive, 0);

        // Give encoding loop time to drain remaining queued frames
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

        // Check if the encoding loop crashed during the recording
        var failure = _encodingFailure;
        if (failure != null)
        {
            _bufferManager.ResumeEviction();
            Logger.Log($"FLASHBACK_RECORDING_END_FAIL error='{failure.Message}'");
            return new FinalizeResult
            {
                Succeeded = false,
                OutputPath = _recordingOutputPath ?? string.Empty,
                StatusMessage = $"Flashback recording failed: {failure.Message}",
                PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
            };
        }

        var (startPts, endPts) = _bufferManager.ResumeEviction();
        LastRecordingStartPts = startPts;
        LastRecordingEndPts = endPts;

        Logger.Log($"FLASHBACK_RECORDING_END output='{_recordingOutputPath}' start_pts_ms={(long)startPts.TotalMilliseconds} end_pts_ms={(long)endPts.TotalMilliseconds} duration_s={(endPts - startPts).TotalSeconds:F1}");

        return new FinalizeResult
        {
            Succeeded = true,
            OutputPath = _recordingOutputPath ?? string.Empty,
            StatusMessage = "Flashback recording ready (single .ts file)",
            PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
        };
    }

    public void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
    {
        var queue = _videoQueue;
        if (_disposed || !_started || queue == null || expectedSize <= 0 || data.IsEmpty || Volatile.Read(ref _forceRotateDraining))
        {
            return;
        }

        if (data.Length < expectedSize)
        {
            Logger.Log($"FLASHBACK_SINK_VIDEO_FRAME_SHORT actual={data.Length} expected={expectedSize}");
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
                $"FLASHBACK_SINK_VIDEO_DROP saturated={dropped} evicted={Interlocked.Read(ref _videoDropsBacklogEviction)} total_dropped={DroppedVideoFrames}");
        }
    }

    public void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)
    {
        var queue = _gpuQueue;
        if (_disposed || !_started || queue == null || d3d11Texture2D == IntPtr.Zero || Volatile.Read(ref _forceRotateDraining))
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
                Logger.Log($"FLASHBACK_SINK_GPU_DROP count={dropped} queue_depth={Volatile.Read(ref _gpuQueueDepth)}");
            }

            return;
        }

        Interlocked.Increment(ref _gpuQueueDepth);
        Interlocked.Increment(ref _gpuFramesEnqueued);
        SignalWork();
    }

    public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)
    {
        var queue = _audioQueue;
        if (_disposed || !_started || !_audioEnabled || queue == null || samples.IsEmpty || Volatile.Read(ref _forceRotateDraining))
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
        cancellationToken.ThrowIfCancellationRequested();
        EnqueueAudioSamples(samples);
        return Task.CompletedTask;
    }

    public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnqueueMicrophoneSamples(samples);
        return Task.CompletedTask;
    }

    public Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use EnqueueRawVideoFrame or EnqueueGpuVideoFrame instead.");
    }

    public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        return StopCoreAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
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
                    Logger.Log("FLASHBACK_SINK_DISPOSE_TIMEOUT task=encoding_loop");
                    _cts?.Cancel();
                    // Fall through to cleanup
                }
            }
            catch
            {
                // Best-effort cleanup path.
            }
        }

        ReturnRemainingBuffers(_videoQueue, ref _videoQueueDepth);
        ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
        ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
        ReturnRemainingGpuBuffers(_gpuQueue);

        _cts?.Dispose();
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _gpuEncodingEnabled = false;
        _microphoneEnabled = false;
        _encodingTask = null;
        _workAvailable.Dispose();
        _forceRotateTcs?.TrySetResult(Array.Empty<string>());
        _forceRotateTcs = null;

        try
        {
            _encoder.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_DISPOSE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }

        if (_ownsBufferManager)
        {
            _bufferManager.Dispose();
        }
    }

    private async Task<FinalizeResult> StopCoreAsync(CancellationToken cancellationToken)
    {
        var outputPath = _recordingOutputPath ?? _tsFilePath ?? string.Empty;
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

        if (_encodingTask != null)
        {
            var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(StopTimeoutMs, cancellationToken)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _encodingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _cts?.Cancel();
                Logger.Log("FLASHBACK_SINK_STOP_DRAIN_TIMEOUT");
                return FinalizeResult.Failure(outputPath, "Stopped (flashback encode drain timed out)");
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

        if (_encodingFailure != null)
        {
            Logger.Log($"FLASHBACK_SINK_STOP_FAIL type={_encodingFailure.GetType().Name} msg={_encodingFailure.Message}");
            return FinalizeResult.Failure(outputPath, $"Stopped (flashback encode failed: {_encodingFailure.Message})");
        }

        Logger.Log(
            $"FLASHBACK_SINK_STOP output='{outputPath}' frames={EncodedVideoFrames} dropped={DroppedVideoFrames} " +
            $"audio_samples={AudioSamplesReceived}");
        return FinalizeResult.Success(outputPath, "Stopped");
    }

    private void ResetEncodingCounters()
    {
        Interlocked.Exchange(ref _droppedVideoFrames, 0);
        Interlocked.Exchange(ref _encodedVideoFrames, 0);
        Interlocked.Exchange(ref _videoFramesEnqueued, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _droppedAudioSamplesCount, 0);
        Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
        Interlocked.Exchange(ref _gpuFramesDropped, 0);
        Interlocked.Exchange(ref _audioSamplesReceived, 0);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _microphoneQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
        Interlocked.Exchange(ref _lastVideoWriteTick, 0);
        Interlocked.Exchange(ref _lastBurstEvictionTick, 0);
        Interlocked.Exchange(ref _segmentStartBytes, 0);
    }

    // ── Encoding Loop ───────────────────────────────────────────────────────

    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_START");
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");
            var microphoneQueue = _microphoneQueue;
            var gpuQueue = _gpuQueue;

            while (true)
            {
                var madeProgress = false;

                // Audio FIRST — prevent starvation during slow video encoding
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                // Video (existing drain methods, unchanged behavior)
                if (gpuQueue != null)
                {
                    madeProgress = DrainGpuPackets(gpuQueue.Reader) || madeProgress;
                }
                madeProgress = DrainVideoPackets(videoQueue.Reader) || madeProgress;

                // Audio AGAIN — catch samples that arrived during video encoding
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                // Handle force-rotate requests from the export thread (must run on encoding thread)
                if (Volatile.Read(ref _forceRotateRequested))
                {
                    TaskCompletionSource<IReadOnlyList<string>>? localTcs;
                    TimeSpan localIn, localOut;

                    // Pause acceptance of new packets to ensure atomicity between drain and rotation.
                    // Producers calling Enqueue* will see this flag and drop packets rather than
                    // inserting them into the new segment that would be excluded from the export.
                    Volatile.Write(ref _forceRotateDraining, true);

                    lock (_sync)
                    {
                        _forceRotateRequested = false;
                        localTcs = _forceRotateTcs;
                        _forceRotateTcs = null;
                        localIn = _forceRotateInPoint;
                        localOut = _forceRotateOutPoint;
                    }
                    try
                    {
                        // Drain all remaining queued packets into the current segment before rotating.
                        // This ensures no data is lost at the live edge.
                        var inFlightCount = 0;
                        while (DrainAudioPackets(audioQueue.Reader)) inFlightCount++;
                        if (_microphoneEnabled && microphoneQueue != null)
                        {
                            while (DrainMicrophonePackets(microphoneQueue.Reader)) inFlightCount++;
                        }
                        if (gpuQueue != null)
                        {
                            while (DrainGpuPackets(gpuQueue.Reader)) inFlightCount++;
                        }
                        while (DrainVideoPackets(videoQueue.Reader)) inFlightCount++;

                        if (inFlightCount > 0)
                        {
                            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_DRAIN in_flight_rounds={inFlightCount}");
                        }

                        var frameRate = _sessionContext?.FrameRate ?? 30.0;
                        var currentPts = frameRate > 0
                            ? _ptsBaseOffset + TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate)
                            : TimeSpan.Zero;

                        if (currentPts > _segmentStartPts)
                        {
                            RotateSegment(currentPts);
                        }

                        localTcs?.TrySetResult(_bufferManager.GetValidSegmentPaths(localIn, localOut));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                        localTcs?.TrySetResult(Array.Empty<string>());
                    }
                    finally
                    {
                        Volatile.Write(ref _forceRotateDraining, false);
                    }
                    madeProgress = true;
                }

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    (microphoneQueue == null || microphoneQueue.Reader.Completion.IsCompleted) &&
                    (gpuQueue == null || gpuQueue.Reader.Completion.IsCompleted) &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0 &&
                    Volatile.Read(ref _microphoneQueueDepth) == 0 &&
                    Volatile.Read(ref _gpuQueueDepth) == 0)
                {
                    break;
                }

                if (madeProgress)
                {
                    continue;
                }

                // Reset THEN re-check queues before blocking. This closes the race where
                // a producer calls Set() between our drain loop exit and the Reset() call —
                // the re-check sees the item and loops back without entering Wait().
                _workAvailable.Reset();

                // Re-check all queues after reset to close the TOCTOU window
                if ((videoQueue.Reader.TryPeek(out _)) ||
                    (audioQueue.Reader.TryPeek(out _)) ||
                    (_microphoneEnabled && microphoneQueue != null && microphoneQueue.Reader.TryPeek(out _)) ||
                    (gpuQueue != null && gpuQueue.Reader.TryPeek(out _)) ||
                    Volatile.Read(ref _forceRotateRequested))
                {
                    continue;
                }

                while (!_workAvailable.Wait(50))
                    cancellationToken.ThrowIfCancellationRequested();
            }

            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_DRAIN_COMPLETE");
            _encoder.FlushAndClose();

            // Register the final active segment
            var finalFrameRate = _sessionContext?.FrameRate ?? 30.0;
            if (finalFrameRate > 0)
            {
                var finalPts = _ptsBaseOffset + TimeSpan.FromSeconds(_encoder.NextVideoPts / finalFrameRate);
                if (_tsFilePath != null && finalPts > _segmentStartPts)
                {
                    var finalSegmentBytes = _encoder.TotalBytesWritten - Interlocked.Read(ref _segmentStartBytes);
                    _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, finalPts, finalSegmentBytes);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED");
            _forceRotateTcs?.TrySetResult(Array.Empty<string>());
            ReturnRemainingBuffers(_videoQueue, ref _videoQueueDepth);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_ENCODING_LOOP_FATAL type={ex.GetType().Name} msg={ex.Message}");
            _encodingFailure = ex;
            _forceRotateTcs?.TrySetResult(Array.Empty<string>());
            lock (_sync) { _started = false; }

            // Notify the owning service so it can surface the failure
            try { _onFatalError?.Invoke(ex); }
            catch { /* Callback must not mask the original error */ }

            // Register the active segment so PurgeAllSegments can clean it up
            if (_tsFilePath != null)
            {
                try
                {
                    var frameRate = _sessionContext?.FrameRate ?? 30.0;
                    var crashPts = frameRate > 0
                        ? _ptsBaseOffset + TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate)
                        : TimeSpan.Zero;
                    if (crashPts > _segmentStartPts)
                    {
                        var crashSegmentBytes = _encoder.TotalBytesWritten - Interlocked.Read(ref _segmentStartBytes);
                        _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, crashPts, crashSegmentBytes);
                    }
                }
                catch { /* Best effort — don't mask the original fatal error */ }
            }

            ReturnRemainingBuffers(_videoQueue, ref _videoQueueDepth);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue);
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
                OnVideoFrameEncoded();
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
                OnVideoFrameEncoded();
            }
            finally
            {
                Marshal.Release(packet.Texture);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private void OnVideoFrameEncoded()
    {
        Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
        var encoded = Interlocked.Increment(ref _encodedVideoFrames);

        // Notify buffer manager of PTS progress
        var frameRate = _sessionContext?.FrameRate ?? 30.0;
        if (frameRate > 0)
        {
            var pts = _ptsBaseOffset + TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate);
            _bufferManager.UpdateLatestPts(pts);

            // Check if current segment duration exceeded — trigger rotation
            // All rotation now happens on the encoding thread, no lock needed
            if (_segmentDuration > TimeSpan.Zero && pts - _segmentStartPts >= _segmentDuration)
            {
                RotateSegment(pts);
            }
        }

        // Periodically update disk bytes
        if (encoded % 300 == 0)
        {
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
        }

        // NOTE: This event fires on the encoding background thread, NOT the UI thread.
        // Handlers must marshal to DispatcherQueue if they need to update UI state.
        if (Volatile.Read(ref _recordingActive) == 1)
        {
            try
            {
                FrameEncoded?.Invoke(this, encoded);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private void RotateSegment(TimeSpan currentPts)
    {
        try
        {
            var completedPath = _tsFilePath;
            var newPath = _bufferManager.GenerateSegmentPath();

            // RotateOutput flushes encoder queues, writes trailer, then resets
            // TotalBytesWritten to 0 for the new segment. PreviousTotalBytes
            // in the result includes all drain/trailer bytes.
            var result = _encoder.RotateOutput(newPath);
            var segmentBytes = result.PreviousTotalBytes - Interlocked.Read(ref _segmentStartBytes);

            _bufferManager.OnSegmentCompleted(completedPath!, _segmentStartPts, currentPts, segmentBytes);

            _segmentStartPts = currentPts;
            _tsFilePath = newPath;
            Interlocked.Exchange(ref _segmentStartBytes, _encoder.TotalBytesWritten);

            // Update disk bytes tracking
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);

            Logger.Log(
                $"FLASHBACK_SINK_ROTATE new_segment='{Path.GetFileName(newPath)}' " +
                $"prev_bytes={segmentBytes} " +
                $"segment_start_ms={(long)currentPts.TotalMilliseconds}");
        }
        catch (Exception ex)
        {
            // Advance _segmentStartPts to prevent infinite retry on every frame
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    public IReadOnlyList<string> ForceRotateForExport(TimeSpan inPoint, TimeSpan outPoint)
    {
        lock (_sync)
        {
            if (!_started || _disposed)
                return Array.Empty<string>();
        }

        // Signal the encoding thread to perform the rotation (all encoder ops must be on that thread)
        var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            _forceRotateInPoint = inPoint;
            _forceRotateOutPoint = outPoint;
            _forceRotateTcs = tcs;
            Volatile.Write(ref _forceRotateRequested, true);
        }
        _workAvailable.Set();

        // Keep timeout short — this blocks the caller (CaptureService) synchronously.
        // Changing to async would require updating all callers.
        if (!tcs.Task.Wait(TimeSpan.FromSeconds(3)))
        {
            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT");
            return Array.Empty<string>();
        }

        return tcs.Task.Result;
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

    // ── Queue Management ────────────────────────────────────────────────────

    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork();
    }

    private void SignalWork()
    {
        _workAvailable.Set();
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
            _encoder.SkipVideoFrame();
            ReturnBuffer(evictedPacket.Buffer);
            var evictedCount = 1;

            var now = Environment.TickCount64;
            if (now - Interlocked.Read(ref _lastBurstEvictionTick) >= 1000)
            {
                Interlocked.Exchange(ref _lastBurstEvictionTick, now);
                var burstSize = Math.Clamp(Volatile.Read(ref _videoQueueDepth) / 10, 1, 30);
                for (var index = 1; index < burstSize && queue.Reader.TryRead(out var extra); index++)
                {
                    Interlocked.Decrement(ref _videoQueueDepth);
                    _encoder.SkipVideoFrame();
                    ReturnBuffer(extra.Buffer);
                    evictedCount++;
                }

                if (evictedCount > 1)
                {
                    Logger.Log($"FLASHBACK_SINK_BURST_EVICT count={evictedCount} queue_depth={Volatile.Read(ref _videoQueueDepth)}");
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
        _encoder.SkipVideoFrame();
        ReturnBuffer(packet.Buffer);
        return false;
    }

    private bool TryEnqueueAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        ref long backlogEvictions)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref queueDepth);
            SignalWork();
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref queueDepth);
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
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref queueDepth);
                SignalWork();
                return true;
            }
        }

        // Total saturation — both eviction and re-enqueue failed
        var saturatedSamples = GetSampleCount(packet.Length);
        Interlocked.Add(ref _droppedAudioSamplesCount, saturatedSamples);
        ReturnBuffer(packet.Buffer);
        return false;
    }

    private static void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)
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

    // ── Options / Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// MPEG-TS supports H.264 and HEVC natively. AV1 in MPEG-TS requires newer ffmpeg builds
    /// (libavformat 61.7+) and is not widely supported. Use fMP4 for AV1.
    /// </summary>
    private static bool SupportsTransportStream(string codecName) =>
        codecName.Contains("264", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("265", StringComparison.OrdinalIgnoreCase);

    internal static string GetSegmentExtension(string codecName) =>
        SupportsTransportStream(codecName) ? ".ts" : ".mp4";

    private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)
    {
        return new LibAvEncoderOptions
        {
            OutputPath = outputPath,
            ContainerFormat = SupportsTransportStream(context.CodecName) ? "mpegts" : "mp4",
            FragmentedMp4 = !SupportsTransportStream(context.CodecName),
            CodecName = context.CodecName,
            Width = context.Width,
            Height = context.Height,
            FrameRate = context.FrameRate,
            FrameRateNumerator = context.FrameRateNumerator,
            FrameRateDenominator = context.FrameRateDenominator,
            BitRate = context.BitRate,
            IsP010 = context.IsP010,
            NvencPreset = context.NvencPreset,
            // 1-second GOP for fast interactive seeking. The default (2x frame rate)
            // means up to 2 seconds of decode-forward on every pause/scrub.
            // 1x frame rate halves worst-case seek latency with minimal bitrate impact.
            GopSize = (int)Math.Max(1, Math.Round(context.FrameRate)),
            HdrEnabled = context.HdrEnabled,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.HdrMasterDisplayMetadata,
            HdrMaxCll = context.HdrMaxCll,
            HdrMaxFall = context.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = context.AudioEnabled,
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = context.MicrophoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }

    private static FlashbackSessionContext CreateSessionContext(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context.FrameRateArg);
        return new FlashbackSessionContext
        {
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            CodecName = MapCodecName(context.Settings.Format),
            NvencPreset = context.Settings.NvencPreset,
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            MicrophoneEnabled = !string.IsNullOrWhiteSpace(context.MicrophoneDeviceName)
        };
    }

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)
    {
        if (string.IsNullOrWhiteSpace(frameRateArg) || !frameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = frameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
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

    private static string MapCodecName(RecordingFormat format)
        => MediaFormat.MapNvencCodecName(format);

    private static long GetFileSize(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in FlashbackEncoderSink.GetFileSize: {ex.Message}");
            return 0;
        }
    }

    private static string CreateSessionId()
    {
        return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}";
    }

    private static long GetSampleCount(int byteLength)
    {
        const int inputBlockAlign = 2 * sizeof(float);
        return byteLength > 0 ? byteLength / inputBlockAlign : 0;
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
}
