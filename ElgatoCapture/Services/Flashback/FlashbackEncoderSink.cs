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
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;

namespace ElgatoCapture.Services.Flashback;

internal sealed class FlashbackEncoderSink : IRecordingSink, IRawVideoFrameEncoder, IRawVideoFrameTryEncoder, IRawVideoFrameLeaseEncoder, IRawVideoFrameLeaseTryEncoder, IGpuVideoFrameEncoder, IGpuVideoFrameTryEncoder
{
    private const int VideoQueueCapacity = 180;
    private const int AudioQueueCapacity = 1800;
    private const int GpuQueueCapacity = 8;
    private const int QueueBackpressureTimeoutMs = 250;
    private const int StopTimeoutMs = 30_000;
    private const int DisposeTimeoutMs = 1_000;
    private const int VideoQueueLatencyWindowSize = 256;
    private const int AudioInputBlockAlignBytes = 2 * sizeof(float);
    private const int MaxAudioPacketBytes = 4 * 1024 * 1024;
    private const double FallbackSessionFrameRate = 30.0;
    private const double MaxSessionFrameRate = 1000.0;

    private readonly object _sync = new();
    private readonly object _videoQueueSync = new();
    private readonly object _videoQueueLatencySync = new();
    private readonly object _videoSequenceSync = new();
    private readonly Queue<long> _videoQueueEnqueueTicks = new();
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
    private int _disposeFinalized;
    private int _deferredDisposeScheduled;

    private bool _forceRotateRequested;
    private volatile TaskCompletionSource<IReadOnlyList<string>>? _forceRotateTcs;
    private TimeSpan _forceRotateInPoint;
    private TimeSpan _forceRotateOutPoint;

    private long _segmentStartBytes;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoFramesSubmittedToEncoder;
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
    private int _videoQueueMaxDepth;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private int _gpuQueueMaxDepth;
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
    public int VideoQueueCapacityFrames => VideoQueueCapacity;
    public int VideoQueueMaxDepth => Volatile.Read(ref _videoQueueMaxDepth);

    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);

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
    public int GpuQueueCount => Volatile.Read(ref _gpuQueueDepth);
    public int GpuQueueCapacityFrames => GpuQueueCapacity;
    public int GpuQueueMaxDepth => Volatile.Read(ref _gpuQueueMaxDepth);
    public long GpuFramesEnqueued => Interlocked.Read(ref _gpuFramesEnqueued);
    public long GpuFramesDropped => Interlocked.Read(ref _gpuFramesDropped);
    public bool EncodingFailed => Volatile.Read(ref _encodingFailure) != null;
    public string? EncodingFailureType => Volatile.Read(ref _encodingFailure)?.GetType().Name;
    public string? EncodingFailureMessage => Volatile.Read(ref _encodingFailure)?.Message;

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
    internal Task EncodingCompletionTask => _encodingTask ?? Task.CompletedTask;

    public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        ValidateSessionContext(context);
        if (ptsBaseOffset < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ptsBaseOffset), "PTS base offset must not be negative.");
        }
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
            var sessionFrameRate = ResolveSessionFrameRate(context.FrameRate);
            var sessionContext = context with { FrameRate = sessionFrameRate };

            var sessionId = _bufferManager.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = CreateSessionId();
                _bufferManager.Initialize(sessionId);
            }
            _bufferManager.SetSegmentExtension(GetSegmentExtension(sessionContext.CodecName));

            var tsPath = _bufferManager.GetFilePath();
            _tsFilePath = tsPath;
            _recordingOutputPath = string.Empty;

            _encoder.Initialize(CreateOptions(sessionContext, tsPath));

            // FullMode = Wait only affects WriteAsync (which we never call).
            // TryWrite returns false immediately when full regardless of FullMode,
            // allowing our manual eviction paths to handle resource cleanup (COM Release,
            // ArrayPool Return) before dropping the packet.
            if (!_encoder.UseHardwareFrames && (sessionContext.Width >= 2560 || sessionContext.Height >= 1440))
            {
                Logger.Log($"FLASHBACK_SINK_WARN_CPU_ENCODING width={sessionContext.Width} height={sessionContext.Height} — GPU encoding unavailable, performance will be severely degraded");
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
            if (sessionContext.MicrophoneEnabled)
            {
                _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            }

            _cts = new CancellationTokenSource();
            _sessionContext = sessionContext;
            _encodingFailure = null;
            _width = sessionContext.Width;
            _height = sessionContext.Height;
            _audioEnabled = sessionContext.AudioEnabled;
            _microphoneEnabled = sessionContext.MicrophoneEnabled;
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

            if (ptsBaseOffset > TimeSpan.Zero)
            {
                var initialVideoPts = ToNonNegativeLongSaturated(ptsBaseOffset.TotalSeconds * sessionFrameRate);
                var initialAudioPts = ToNonNegativeLongSaturated(ptsBaseOffset.TotalSeconds * 48_000);
                _encoder.SetInitialPts(initialVideoPts, initialAudioPts);
                Logger.Log($"FLASHBACK_SINK_PTS_CONTINUE v_pts={initialVideoPts} a_pts={initialAudioPts} offset_s={ptsBaseOffset.TotalSeconds:F1}");
            }

            Logger.Log($"FLASHBACK_SINK_INIT_COMPLETE session='{sessionId}' gpu_encoding={_gpuEncodingEnabled} segment_duration_s={_segmentDuration.TotalSeconds:F0}");

            // Publish the encoder's frame rate as ground truth for playback pacing.
            _bufferManager.EncodeFrameRate = sessionFrameRate;

            _encodingTask = Task.Factory.StartNew(
                () => EncodingLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Logger.Log(
                $"FLASHBACK_SINK_START session='{sessionId}' output='{tsPath}' codec='{sessionContext.CodecName}' " +
                $"width={_width} height={_height} fps={sessionFrameRate:0.###} " +
                $"buffer_ms={(long)_bufferManager.Options.BufferDuration.TotalMilliseconds} " +
                $"audio={_audioEnabled} microphone={_microphoneEnabled} p010={sessionContext.IsP010}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            /* Cleanup must not throw — tear down partially-initialized queues/state before re-throwing */
            Logger.Log($"FLASHBACK_SINK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            CompleteWriter(_videoQueue);
            CompleteWriter(_audioQueue);
            CompleteWriter(_microphoneQueue);
            CompleteWriter(_gpuQueue);
            _videoQueue = null;
            _audioQueue = null;
            _microphoneQueue = null;
            _gpuQueue = null;
            _gpuEncodingEnabled = false;
            lock (_sync)
            {
                _started = false;
            }
            DisposeCtsBestEffort(_cts, "start_fail");
            _cts = null;
            _encodingTask = null;
            _sessionContext = null;
            _width = 0;
            _height = 0;
            _audioEnabled = false;
            _microphoneEnabled = false;

            if (_ownsBufferManager)
            {
                _bufferManager.PurgeAllSegments();
            }
            DisposeEncoderBestEffort("start_fail");
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
    public bool IsRecordingActive => Volatile.Read(ref _recordingActive) != 0;
    public bool IsForceRotateActive =>
        Volatile.Read(ref _forceRotateRequested) ||
        Volatile.Read(ref _forceRotateDraining);

    public bool CanBeginRecording
    {
        get
        {
            lock (_sync)
            {
                return !_disposed &&
                       _started &&
                       _encodingFailure == null &&
                       Volatile.Read(ref _recordingActive) == 0 &&
                       !IsForceRotateActive &&
                       _encodingTask?.IsCompleted != true;
            }
        }
    }

    public bool WaitForForceRotateIdle(TimeSpan timeout)
    {
        var timeoutMs = Math.Max(0, (long)timeout.TotalMilliseconds);
        var deadlineTick = Environment.TickCount64 + timeoutMs;
        while (IsForceRotateActive)
        {
            if (timeoutMs == 0 || Environment.TickCount64 >= deadlineTick)
            {
                return false;
            }

            _workAvailable.Set();
            if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))
            {
                return false;
            }
        }

        return true;
    }

    public void BeginRecording(string outputPath)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FlashbackEncoderSink));
            }

            if (_encodingFailure != null)
            {
                throw new InvalidOperationException("Cannot begin recording: encoding loop has failed", _encodingFailure);
            }

            if (!_started)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback encoder is not running.");
            }

            if (_encodingTask?.IsCompleted == true)
            {
                throw new InvalidOperationException("Cannot begin recording: encoding task has terminated.");
            }

            if (IsForceRotateActive)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback export rotation is still draining.");
            }

            if (Volatile.Read(ref _recordingActive) != 0)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback recording is already active.");
            }

            _recordingOutputPath = outputPath ?? string.Empty;
            _bufferManager.PauseEviction();
            Volatile.Write(ref _recordingActive, 1);
        }
        Logger.Log($"FLASHBACK_RECORDING_ACTIVE output='{_recordingOutputPath}'");
    }

    public void CancelRecordingStartRollback(string reason)
    {
        if (Interlocked.Exchange(ref _recordingActive, 0) != 0)
        {
            ResumeEvictionBestEffort(_bufferManager, "recording_start_rollback");
            Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK reason='{reason}'");
        }
    }

    public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)
    {
        var wasRecording = Interlocked.Exchange(ref _recordingActive, 0) != 0;
        if (!wasRecording)
        {
            const string message = "Flashback recording was not active.";
            Logger.Log($"FLASHBACK_RECORDING_END_REJECTED reason='{message}'");
            return new FinalizeResult
            {
                Succeeded = false,
                OutputPath = _recordingOutputPath ?? string.Empty,
                StatusMessage = message,
                PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Give encoding loop time to drain remaining queued frames.
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Check if the encoding loop crashed during the recording
            var failure = _encodingFailure;
            if (failure != null)
            {
                Logger.Log($"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'");
                return new FinalizeResult
                {
                    Succeeded = false,
                    OutputPath = _recordingOutputPath ?? string.Empty,
                    StatusMessage = $"Flashback recording failed: {failure.Message}",
                    PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
                };
            }

            // Capture end PTS BEFORE resuming eviction. When an outer pause is held
            // (FinalizeFlashbackRecordingAsync), ResumeEviction won't reach count=0 and
            // therefore won't snapshot the end PTS. Even if count does reach 0, the
            // stored _recordingEndPts may be stale from a previous recording. Always
            // use the live LatestPts as the authoritative recording end time.
            var endPts = _bufferManager.LatestPts;
            LastRecordingEndPts = endPts;

            return new FinalizeResult
            {
                Succeeded = true,
                OutputPath = _recordingOutputPath ?? string.Empty,
                StatusMessage = "Flashback recording ready (single .ts file)",
                PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
            };
        }
        finally
        {
            if (wasRecording)
            {
                var (startPts, _) = ResumeEvictionBestEffort(_bufferManager, "recording_end");
                LastRecordingStartPts = startPts;
                if (LastRecordingEndPts < LastRecordingStartPts)
                {
                    LastRecordingEndPts = _bufferManager.LatestPts;
                    if (LastRecordingEndPts < LastRecordingStartPts)
                    {
                        LastRecordingEndPts = LastRecordingStartPts;
                    }
                }

                Logger.Log(
                    $"FLASHBACK_RECORDING_READY output='{_recordingOutputPath}' " +
                    $"start_pts_ms={(long)LastRecordingStartPts.TotalMilliseconds} " +
                    $"end_pts_ms={(long)LastRecordingEndPts.TotalMilliseconds} " +
                    $"duration_s={NonNegativeDuration(LastRecordingEndPts, LastRecordingStartPts).TotalSeconds:F1}");
            }
        }
    }

    public void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
        => TryEnqueueRawVideoFrame(data, expectedSize);

    public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)
    {
        var queue = _videoQueue;
        if (_disposed || !_started || queue == null || expectedSize <= 0 || data.IsEmpty || Volatile.Read(ref _forceRotateDraining))
        {
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

        var enqueueResult = TryEnqueueVideoPacket(queue, packet);
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
        if (_disposed || !_started || queue == null || Volatile.Read(ref _forceRotateDraining))
        {
            frame.Dispose();
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

        var enqueueResult = TryEnqueueVideoPacket(queue, packet);
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
        if (_disposed || !_started || queue == null || d3d11Texture2D == IntPtr.Zero || Volatile.Read(ref _forceRotateDraining))
        {
            return false;
        }

        if (subresourceIndex < 0)
        {
            Logger.Log($"FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE subresource={subresourceIndex}");
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

    // REVIEWED 2026-04-07: IDisposable fallback only — all callers use DisposeAsync.
    // CaptureService.DisposeFlashbackPreviewBackendAsync awaits DisposeAsync directly.
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
        if (Interlocked.Exchange(ref _recordingActive, 0) != 0)
        {
            ResumeEvictionBestEffort(_bufferManager, "dispose");
        }

        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CancelEncodingCts("dispose");

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

        Logger.Log($"FLASHBACK_SINK_DISPOSE_DEFERRED timeout_ms={DisposeTimeoutMs}");
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
                Logger.Log("FLASHBACK_SINK_DISPOSE_DEFERRED_COMPLETE");
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

        ReturnAllRemainingQueuedBuffers();

        DisposeCtsBestEffort(_cts, "finalize_dispose");
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _gpuEncodingEnabled = false;
        _microphoneEnabled = false;
        _encodingTask = null;
        DisposeWorkAvailableBestEffort("finalize_dispose");
        CompletePendingForceRotateWithEmptyResult();
        DisposeEncoderBestEffort("finalize_dispose");

        if (_ownsBufferManager)
        {
            try
            {
                _bufferManager.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private void CancelEncodingCts(string operation)
    {
        try
        {
            _cts?.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_CANCEL_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeWorkAvailableBestEffort(string operation)
    {
        try
        {
            _workAvailable.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeEncoderBestEffort(string operation)
    {
        try
        {
            _encoder.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_ENCODER_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
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
                CancelEncodingCts("stop_timeout");
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
        Interlocked.Exchange(ref _videoFramesSubmittedToEncoder, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _droppedAudioSamplesCount, 0);
        Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
        Interlocked.Exchange(ref _gpuFramesDropped, 0);
        Interlocked.Exchange(ref _videoQueueMaxDepth, 0);
        Interlocked.Exchange(ref _gpuQueueMaxDepth, 0);
        Interlocked.Exchange(ref _audioSamplesReceived, 0);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _microphoneQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
        Interlocked.Exchange(ref _lastVideoWriteTick, 0);
        ResetVideoDiagnostics();
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
                    lock (_videoQueueSync)
                    {
                        Volatile.Write(ref _forceRotateDraining, true);
                    }

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
                        if (localTcs == null)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request");
                            madeProgress = true;
                            continue;
                        }

                        if (localTcs.Task.IsCompleted)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed");
                            madeProgress = true;
                            continue;
                        }

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

                        var currentPts = ResolveEncoderPts();

                        if (currentPts > _segmentStartPts)
                        {
                            if (!RotateSegment(currentPts))
                            {
                                localTcs?.TrySetResult(Array.Empty<string>());
                                madeProgress = true;
                                continue;
                            }
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
                        lock (_videoQueueSync)
                        {
                            Volatile.Write(ref _forceRotateDraining, false);
                        }
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
            var finalPts = ResolveEncoderPts();
            if (finalPts > TimeSpan.Zero)
            {
                if (_tsFilePath != null && finalPts > _segmentStartPts)
                {
                    var finalSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                    _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, finalPts, finalSegmentBytes);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED");
            CompletePendingForceRotateWithEmptyResult();
            ReturnAllRemainingQueuedBuffers();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_ENCODING_LOOP_FATAL type={ex.GetType().Name} msg={ex.Message}");
            _encodingFailure = ex;
            CompletePendingForceRotateWithEmptyResult();
            lock (_sync) { _started = false; }

            // Notify the owning service so it can surface the failure
            try { _onFatalError?.Invoke(ex); }
            catch (Exception callbackEx)
            {
                Logger.Log($"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
            }

            // Register the active segment so PurgeAllSegments can clean it up
            if (_tsFilePath != null)
            {
                try
                {
                    var crashPts = ResolveEncoderPts();
                    if (crashPts > _segmentStartPts)
                    {
                        var crashSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, crashPts, crashSegmentBytes);
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                    // Preserve the original fatal error.
                }
            }

            ReturnAllRemainingQueuedBuffers();
            DisposeEncoderBestEffort("encoding_loop_fatal");
        }
    }

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader)
    {
        var drainedAny = false;
        var w = _width;
        var h = _height;
        if (w <= 0 || h <= 0)
        {
            return false;
        }

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
                DecrementQueueDepth(ref _videoQueueDepth, "video");
            }

            RecordVideoPacketDequeued(packet);
            try
            {
                var expectedFrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010);
                // Defense-in-depth: if a stale frame from a previous resolution
                // leaks through during a reinit cycle, drop it rather than sending
                // mismatched dimensions to the encoder (which could crash in native code).
                if (expectedFrameSize > 0 && packet.Length != expectedFrameSize)
                {
                    Interlocked.Increment(ref _droppedVideoFrames);
                    Logger.Log($"FLASHBACK_SINK_FRAME_SIZE_MISMATCH expected={expectedFrameSize} actual={packet.Length} w={w} h={h} p010={packet.IsP010}");
                    continue;
                }

                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, w, h);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                OnVideoFrameEncoded();
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
            DecrementQueueDepth(ref _gpuQueueDepth, "gpu");
            try
            {
                _encoder.SendGpuVideoFrame(packet.Texture, packet.Subresource);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                OnVideoFrameEncoded();
            }
            finally
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private void OnVideoFrameEncoded()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
        var encoded = Interlocked.Increment(ref _encodedVideoFrames);

        // Notify buffer manager of PTS progress
        var pts = ResolveEncoderPts();
        if (pts > TimeSpan.Zero)
        {
            _bufferManager.UpdateLatestPts(pts);

            // Check if current segment duration exceeded — trigger rotation
            // All rotation now happens on the encoding thread, no lock needed
            if (_segmentDuration > TimeSpan.Zero && pts - _segmentStartPts >= _segmentDuration)
            {
                _ = RotateSegment(pts);
            }
        }

        // Periodically update disk bytes
        if (encoded % 300 == 0)
        {
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
        }

        // NOTE: This event fires on the encoding background thread, NOT the UI thread.
        // Handlers must marshal to DispatcherQueue if they need to update UI state.
        if (!_disposed && Volatile.Read(ref _recordingActive) == 1)
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

    private TimeSpan ResolveEncoderPts()
    {
        var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);
        var seconds = _encoder.NextVideoPts / frameRate;
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            return _ptsBaseOffset;
        }

        if (seconds >= TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.MaxValue;
        }

        var delta = TimeSpan.FromSeconds(seconds);
        return _ptsBaseOffset > TimeSpan.MaxValue - delta
            ? TimeSpan.MaxValue
            : _ptsBaseOffset + delta;
    }

    private static double ResolveSessionFrameRate(double frameRate)
    {
        if (!double.IsFinite(frameRate) || frameRate <= 0)
        {
            return FallbackSessionFrameRate;
        }

        return Math.Min(frameRate, MaxSessionFrameRate);
    }

    private static void ValidateSessionContext(FlashbackSessionContext context)
    {
        if (context.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Width, "Flashback session width must be positive.");
        }

        if (context.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Height, "Flashback session height must be positive.");
        }

        if (string.IsNullOrWhiteSpace(context.CodecName))
        {
            throw new ArgumentException("Flashback session codec name is required.", nameof(context));
        }
    }

    private static long ToNonNegativeLongSaturated(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 0;
        }

        return value >= long.MaxValue ? long.MaxValue : (long)value;
    }

    private static long NonNegativeByteDelta(long currentBytes, long startBytes)
    {
        currentBytes = Math.Max(0, currentBytes);
        startBytes = Math.Max(0, startBytes);
        if (currentBytes <= startBytes)
        {
            return 0;
        }

        return currentBytes - startBytes;
    }

    private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)
    {
        if (end <= start)
        {
            return TimeSpan.Zero;
        }

        var endTicks = end.Ticks;
        var startTicks = start.Ticks;
        if (startTicks < 0 && endTicks > long.MaxValue + startTicks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(endTicks - startTicks);
    }

    private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(
        FlashbackBufferManager bufferManager,
        string operation)
    {
        try
        {
            return bufferManager.ResumeEviction();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_EVICTION_RESUME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            return (bufferManager.RecordingStartPts, bufferManager.RecordingEndPts);
        }
    }

    private bool RotateSegment(TimeSpan currentPts)
    {
        string? completedPath = null;
        string? newPath = null;
        try
        {
            completedPath = _tsFilePath;
            newPath = _bufferManager.GenerateSegmentPath();

            // RotateOutput flushes encoder queues, writes trailer, then resets
            // TotalBytesWritten to 0 for the new segment. PreviousTotalBytes
            // in the result includes all drain/trailer bytes.
            var result = _encoder.RotateOutput(newPath);
            var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));

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
            return true;
        }
        catch (Exception ex)
        {
            if (newPath != null)
            {
                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);
            }

            // Advance _segmentStartPts to prevent infinite retry on every frame
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }

    public IReadOnlyList<string> ForceRotateForExport(TimeSpan inPoint, TimeSpan outPoint)
    {
        if (inPoint < TimeSpan.Zero || outPoint <= inPoint)
        {
            Logger.Log(
                $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)outPoint.TotalMilliseconds}");
            return Array.Empty<string>();
        }

        lock (_sync)
        {
            if (!_started || _disposed)
                return Array.Empty<string>();

            if (_encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED failed={_encodingFailure != null} " +
                    $"completed={_encodingTask?.IsCompleted == true} type={_encodingFailure?.GetType().Name ?? "None"}");
                return Array.Empty<string>();
            }
        }

        // Signal the encoding thread to perform the rotation (all encoder ops must be on that thread)
        var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<IReadOnlyList<string>>? supersededTcs;
        lock (_sync)
        {
            if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK started={_started} disposed={_disposed} " +
                    $"failed={_encodingFailure != null} completed={_encodingTask?.IsCompleted == true} " +
                    $"type={_encodingFailure?.GetType().Name ?? "None"}");
                return Array.Empty<string>();
            }

            supersededTcs = _forceRotateTcs;
            _forceRotateInPoint = inPoint;
            _forceRotateOutPoint = outPoint;
            _forceRotateTcs = tcs;
            Volatile.Write(ref _forceRotateRequested, true);
        }

        if (supersededTcs != null)
        {
            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
            supersededTcs.TrySetResult(Array.Empty<string>());
        }

        _workAvailable.Set();

        // AV1 encoding is significantly slower than H.264/HEVC — drain can take
        // much longer at 4K@120fps with a deep queue. Use a longer timeout for AV1.
        var codecName = _sessionContext?.CodecName ?? string.Empty;
        var isSlowCodec = codecName.Contains("av1", StringComparison.OrdinalIgnoreCase);
        var timeoutSeconds = isSlowCodec ? 10 : 3;
        if (!tcs.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            var clearedPending = TryCancelPendingForceRotate(tcs);
            tcs.TrySetResult(Array.Empty<string>());
            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT codec={codecName} timeout_s={timeoutSeconds} cleared_pending={clearedPending} vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
            return Array.Empty<string>();
        }

        return tcs.Task.Result;
    }

    private bool TryCancelPendingForceRotate(TaskCompletionSource<IReadOnlyList<string>> requestTcs)
    {
        var cleared = false;
        lock (_sync)
        {
            if (ReferenceEquals(_forceRotateTcs, requestTcs))
            {
                _forceRotateRequested = false;
                _forceRotateTcs = null;
                cleared = true;
            }
        }

        if (cleared)
        {
            requestTcs.TrySetResult(Array.Empty<string>());
        }

        return cleared;
    }

    private void CompletePendingForceRotateWithEmptyResult()
    {
        TaskCompletionSource<IReadOnlyList<string>>? pendingTcs;
        lock (_sync)
        {
            _forceRotateRequested = false;
            pendingTcs = _forceRotateTcs;
            _forceRotateTcs = null;
        }

        lock (_videoQueueSync)
        {
            Volatile.Write(ref _forceRotateDraining, false);
        }

        pendingTcs?.TrySetResult(Array.Empty<string>());
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio");
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
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone");
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
            Logger.Log($"FLASHBACK_SINK_QUEUE_TICK_MISMATCH expected={expectedEnqueueTick} actual={queuedTick}");
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
                if (_disposed ||
                    !_started ||
                    _cts?.IsCancellationRequested == true ||
                    Volatile.Read(ref _forceRotateDraining) ||
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

                if (_disposed ||
                    !_started ||
                    _cts?.IsCancellationRequested == true ||
                    Volatile.Read(ref _forceRotateDraining) ||
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
                        $"Flashback recording video queue overloaded after {QueueBackpressureTimeoutMs}ms backpressure: capacity={VideoQueueCapacity} depth={Volatile.Read(ref _videoQueueDepth)}");
                    ReturnVideoPacket(packet);
                }
            }

            if (overloadFailure != null)
            {
                FailEncoding(overloadFailure);
                return VideoEnqueueResult.Overloaded;
            }

            SignalWork();
            if (WaitForBackpressureRetryCancellation())
            {
                continue;
            }
        }
    }

    private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)
    {
        var deadlineTick = Environment.TickCount64 + QueueBackpressureTimeoutMs;
        long backpressureStartTick = 0;
        while (true)
        {
            Exception? overloadFailure = null;
            lock (_videoQueueSync)
            {
                if (_disposed ||
                    !_started ||
                    _cts?.IsCancellationRequested == true ||
                    Volatile.Read(ref _forceRotateDraining) ||
                    Volatile.Read(ref _encodingFailure) != null)
                {
                    ReleaseGpuTextureBestEffort(packet.Texture);
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

                if (_disposed ||
                    !_started ||
                    _cts?.IsCancellationRequested == true ||
                    Volatile.Read(ref _forceRotateDraining) ||
                    Volatile.Read(ref _encodingFailure) != null)
                {
                    ReleaseGpuTextureBestEffort(packet.Texture);
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
                    ReleaseGpuTextureBestEffort(packet.Texture);
                    overloadFailure = new InvalidOperationException(
                        $"Flashback GPU recording queue overloaded after {QueueBackpressureTimeoutMs}ms backpressure: capacity={GpuQueueCapacity} depth={Volatile.Read(ref _gpuQueueDepth)}");
                }
            }

            if (overloadFailure != null)
            {
                FailEncoding(overloadFailure);
                return VideoEnqueueResult.Overloaded;
            }

            SignalWork();
            if (WaitForBackpressureRetryCancellation())
            {
                continue;
            }
        }
    }

    private bool WaitForBackpressureRetryCancellation()
        => WaitForCancellation(TimeSpan.FromMilliseconds(1));

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
            Volatile.Read(ref _forceRotateDraining) ||
            Volatile.Read(ref _encodingFailure) != null)
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
            _videoQueueEnqueueTicks.Clear();
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
        var (frameRateNumerator, frameRateDenominator) = ResolveSessionFrameRateParts(
            context.FrameRateNumerator,
            context.FrameRateDenominator);

        return new LibAvEncoderOptions
        {
            OutputPath = outputPath,
            ContainerFormat = SupportsTransportStream(context.CodecName) ? "mpegts" : "mp4",
            FragmentedMp4 = !SupportsTransportStream(context.CodecName),
            CodecName = context.CodecName,
            Width = context.Width,
            Height = context.Height,
            FrameRate = context.FrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
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

    private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || numerator <= 0 || denominator <= 0)
        {
            return (null, null);
        }

        var fps = (double)numerator.Value / denominator.Value;
        if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)
        {
            return (null, null);
        }

        return (numerator, denominator);
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
            Logger.Log($"FLASHBACK_SINK_FILE_SIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return 0;
        }
    }

    private static string CreateSessionId()
    {
        return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}";
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
}
