using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Continuous Flashback encoder sink. It receives the same frame/audio fan-out
/// as normal recording, writes rolling MPEG-TS segments, and exposes a recording
/// compatible sink contract when the user saves a retroactive clip.
/// </summary>
internal sealed partial class FlashbackEncoderSink : IRecordingSink, IRawVideoFrameEncoder, IRawVideoFrameTryEncoder, IRawVideoFrameLeaseEncoder, IRawVideoFrameLeaseTryEncoder, IGpuVideoFrameEncoder, IGpuVideoFrameTryEncoder
{
    private const int DefaultVideoQueueCapacity = 180;
    private const int HighResolutionCpuVideoQueueCapacity = 128;
    private const int AudioQueueCapacity = 1800;
    private const int GpuQueueCapacity = 8;
    private const int VideoDrainBatchLimit = 24;
    private const int AudioDrainBatchLimit = 128;
    private const int GpuDrainBatchLimit = 16;
    private const double ForceRotateQueueGuardRatio = 0.65;
    private const int StopTimeoutMs = 30_000;
    private const int DisposeTimeoutMs = 1_000;
    private const int ForceRotateCommittedGraceMs = 1_000;
    private const int VideoQueueLatencyWindowSize = 256;
    private const int AudioInputBlockAlignBytes = 2 * sizeof(float);
    private const int MaxAudioPacketBytes = 4 * 1024 * 1024;
    private const double FallbackSessionFrameRate = 30.0;
    private const double MaxSessionFrameRate = 1000.0;

    private readonly object _sync = new();
    private readonly object _videoQueueSync = new();
    private readonly LibAvEncoder _encoder = new();
    private readonly FlashbackBufferManager _bufferManager;
    private readonly ManualResetEventSlim _workAvailable = new(false, 100);
    private readonly bool _ownsBufferManager;
    private Channel<VideoFramePacket>? _videoQueue;
    private Channel<AudioSamplePacket>? _audioQueue;
    private Channel<AudioSamplePacket>? _microphoneQueue;
    private Channel<GpuFramePacket>? _gpuQueue;

    // One encoding task owns LibAvEncoder and segment rotation. Capture
    // callbacks only enqueue packets so file finalization never runs inline on
    // the source-reader or WASAPI callback threads.
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
    private volatile ForceRotateRequest? _forceRotateRequest;
    private TimeSpan _forceRotateInPoint;
    private TimeSpan _forceRotateOutPoint;

    private long _segmentStartBytes;
    private long _lastDiskBytesUpdateMs;
    private long _segmentRotationFailures;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoFramesSubmittedToEncoder;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _videoQueueRejectedFrames;
    private long _audioSamplesReceived;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private long _droppedAudioSamplesCount;
    private long _microphoneDropsQueueSaturated;
    private long _microphoneDropsBacklogEviction;
    private long _gpuFramesEnqueued;
    private long _gpuFramesDropped;
    private long _gpuQueueRejectedFrames;
    private Action<Exception>? _onFatalError;
    private bool _forceRotateDraining;
    private int _videoQueueDepth;
    private int _videoQueueMaxDepth;
    private int _videoQueueCapacity = DefaultVideoQueueCapacity;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private int _gpuQueueMaxDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;
    private string? _lastVideoQueueRejectReason;
    private string? _lastGpuQueueRejectReason;
    private readonly VideoQueueLatencyTracker _videoLatencyTracker;
    private string? _tsFilePath;
    private string? _recordingOutputPath;
    private int _recordingActive;
    private TimeSpan _segmentStartPts;
    private TimeSpan _segmentDuration;
    private TimeSpan _ptsBaseOffset;

    private sealed class ForceRotateRequest
    {
        private const int StatePending = 0;
        private const int StateCommitting = 1;
        private const int StateCompleted = 2;
        private const int StateCanceled = 3;

        private int _state = StatePending;

        private readonly TaskCompletionSource<IReadOnlyList<string>> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<string>> Task => _completion.Task;

        public bool IsCompleted
        {
            get
            {
                var state = Volatile.Read(ref _state);
                return state == StateCompleted ||
                       state == StateCanceled ||
                       _completion.Task.IsCompleted;
            }
        }

        public bool TryBeginCommit()
            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;

        public bool TryCancel()
        {
            if (Interlocked.CompareExchange(ref _state, StateCanceled, StatePending) != StatePending)
            {
                return false;
            }

            _completion.TrySetResult(Array.Empty<string>());
            return true;
        }

        public void Complete(IReadOnlyList<string> paths)
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                if (state == StateCompleted || state == StateCanceled)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _state, StateCompleted, state) == state)
                {
                    _completion.TrySetResult(paths);
                    return;
                }
            }
        }

        public void CompleteEmpty()
            => Complete(Array.Empty<string>());
    }

    public FlashbackEncoderSink(FlashbackBufferOptions? options = null)
    {
        var opts = options ?? new FlashbackBufferOptions();
        _bufferManager = new FlashbackBufferManager(opts);
        _ownsBufferManager = true;
        _videoLatencyTracker = new VideoQueueLatencyTracker(
            "FLASHBACK_SINK", _videoQueueSync, VideoQueueLatencyWindowSize);
    }

    public FlashbackEncoderSink(FlashbackBufferManager bufferManager)
    {
        ArgumentNullException.ThrowIfNull(bufferManager);
        _bufferManager = bufferManager;
        _ownsBufferManager = false;
        _videoLatencyTracker = new VideoQueueLatencyTracker(
            "FLASHBACK_SINK", _videoQueueSync, VideoQueueLatencyWindowSize);
    }

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
        string? startupGeneratedSegmentPath = null;

        lock (_sync)
        {
            if (_started || _encodingTask is { IsCompleted: false })
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

            var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);
            if (startupGeneratedSegment)
            {
                startupGeneratedSegmentPath = tsPath;
            }
            _tsFilePath = tsPath;
            _recordingOutputPath = string.Empty;

            _encoder.Initialize(CreateOptions(sessionContext, tsPath));

            // FullMode = Wait only affects WriteAsync (which we never call).
            // TryWrite returns false immediately when full regardless of FullMode,
            // allowing our manual eviction paths to handle resource cleanup (COM Release,
            // ArrayPool Return) before dropping the packet.
            var videoQueueCapacity = ResolveVideoQueueCapacity(sessionContext, _encoder.UseHardwareFrames);
            Volatile.Write(ref _videoQueueCapacity, videoQueueCapacity);
            if (!_encoder.UseHardwareFrames && IsHighResolutionFrame(sessionContext))
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

            _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(videoQueueCapacity)
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
            _bufferManager.MarkActiveSegmentStart(tsPath, _segmentStartPts);

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
            _tsFilePath = null;
            _recordingOutputPath = string.Empty;
            _segmentStartPts = TimeSpan.Zero;
            _segmentDuration = TimeSpan.Zero;
            _ptsBaseOffset = TimeSpan.Zero;
            Interlocked.Exchange(ref _segmentStartBytes, 0);

            DisposeEncoderBestEffort("start_fail");
            if (_ownsBufferManager)
            {
                _bufferManager.PurgeAllSegments();
            }
            else if (startupGeneratedSegmentPath != null)
            {
                _bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);
            }
            throw;
        }
    }

    public bool IsForceRotateActive =>
        Volatile.Read(ref _forceRotateRequested) ||
        Volatile.Read(ref _forceRotateDraining);
    public bool IsForceRotateRequested => Volatile.Read(ref _forceRotateRequested);
    public bool IsForceRotateDraining => Volatile.Read(ref _forceRotateDraining);

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

            SignalWork("force_rotate_idle");
            if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))
            {
                return false;
            }
        }

        return true;
    }

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

    private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)
        => !useHardwareFrames && IsHighResolutionFrame(context)
            ? HighResolutionCpuVideoQueueCapacity
            : DefaultVideoQueueCapacity;

    private static bool IsHighResolutionFrame(FlashbackSessionContext context)
        => context.Width >= 2560 || context.Height >= 1440;

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
        _audioEnabled = false;
        _microphoneEnabled = false;
        _sessionContext = null;
        _width = 0;
        _height = 0;
        _tsFilePath = null;
        _recordingOutputPath = string.Empty;
        _segmentStartPts = TimeSpan.Zero;
        _segmentDuration = TimeSpan.Zero;
        _ptsBaseOffset = TimeSpan.Zero;
        Interlocked.Exchange(ref _segmentStartBytes, 0);
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
                var timeoutFailure = new TimeoutException("Flashback encode drain timed out while stopping.");
                lock (_sync)
                {
                    _encodingFailure ??= timeoutFailure;
                }
                CancelEncodingCts("stop_timeout");
                CompletePendingForceRotateWithEmptyResult();
                Logger.Log("FLASHBACK_SINK_STOP_DRAIN_TIMEOUT");
                return FinalizeResult.Failure(outputPath, "Stopped (flashback encode drain timed out)");
            }

            try
            {
                await _encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure ??= ex;
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
        Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);
        Volatile.Write(ref _lastVideoQueueRejectReason, null);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _droppedAudioSamplesCount, 0);
        Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
        Interlocked.Exchange(ref _gpuFramesDropped, 0);
        Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);
        Volatile.Write(ref _lastGpuQueueRejectReason, null);
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
                    madeProgress = DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit) || madeProgress;
                }
                madeProgress = DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit) || madeProgress;

                // Audio AGAIN — catch samples that arrived during video encoding
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                // Handle force-rotate requests from the export thread (must run on encoding thread)
                if (Volatile.Read(ref _forceRotateRequested))
                {
                    ForceRotateRequest? localRequest;
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
                        localRequest = _forceRotateRequest;
                        _forceRotateRequest = null;
                        localIn = _forceRotateInPoint;
                        localOut = _forceRotateOutPoint;
                    }
                    try
                    {
                        if (localRequest == null)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request");
                            madeProgress = true;
                            continue;
                        }

                        if (localRequest.IsCompleted)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed");
                            madeProgress = true;
                            continue;
                        }

                        // Drain all remaining queued packets into the current segment before rotating.
                        // This ensures no data is lost at the live edge.
                        var inFlightCount = 0;
                        var forceRotateDrainAborted = ShouldAbortForceRotateDrain(localRequest, "before_drain", inFlightCount);
                        if (!forceRotateDrainAborted)
                        {
                            while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))
                            {
                                inFlightCount++;
                                if (ShouldAbortForceRotateDrain(localRequest, "audio", inFlightCount))
                                {
                                    forceRotateDrainAborted = true;
                                    break;
                                }
                            }

                            forceRotateDrainAborted = forceRotateDrainAborted ||
                                ShouldAbortForceRotateDrain(localRequest, "audio", inFlightCount);
                        }
                        if (!forceRotateDrainAborted && _microphoneEnabled && microphoneQueue != null)
                        {
                            while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))
                            {
                                inFlightCount++;
                                if (ShouldAbortForceRotateDrain(localRequest, "microphone", inFlightCount))
                                {
                                    forceRotateDrainAborted = true;
                                    break;
                                }
                            }

                            forceRotateDrainAborted = forceRotateDrainAborted ||
                                ShouldAbortForceRotateDrain(localRequest, "microphone", inFlightCount);
                        }
                        if (!forceRotateDrainAborted && gpuQueue != null)
                        {
                            while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))
                            {
                                inFlightCount++;
                                if (ShouldAbortForceRotateDrain(localRequest, "gpu", inFlightCount))
                                {
                                    forceRotateDrainAborted = true;
                                    break;
                                }
                            }

                            forceRotateDrainAborted = forceRotateDrainAborted ||
                                ShouldAbortForceRotateDrain(localRequest, "gpu", inFlightCount);
                        }
                        if (!forceRotateDrainAborted)
                        {
                            while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))
                            {
                                inFlightCount++;
                                if (ShouldAbortForceRotateDrain(localRequest, "video", inFlightCount))
                                {
                                    forceRotateDrainAborted = true;
                                    break;
                                }
                            }

                            forceRotateDrainAborted = forceRotateDrainAborted ||
                                ShouldAbortForceRotateDrain(localRequest, "video", inFlightCount);
                        }

                        if (inFlightCount > 0)
                        {
                            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_DRAIN in_flight_rounds={inFlightCount}");
                        }

                        if (forceRotateDrainAborted)
                        {
                            madeProgress = true;
                            continue;
                        }

                        if (localRequest.IsCompleted)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
                            madeProgress = true;
                            continue;
                        }

                        var currentPts = ResolveEncoderPts();

                        if (currentPts > _segmentStartPts)
                        {
                            if (!localRequest.TryBeginCommit())
                            {
                                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate");
                                madeProgress = true;
                                continue;
                            }

                            if (!RotateSegment(currentPts))
                            {
                                localRequest.CompleteEmpty();
                                madeProgress = true;
                                continue;
                            }
                        }

                        localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                        localRequest?.CompleteEmpty();
                        throw;
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

            // Register the in-progress segment so the buffer index sees the live edge.
            if (_tsFilePath != null)
            {
                try
                {
                    var cancelPts = ResolveEncoderPts();
                    if (cancelPts > _segmentStartPts)
                    {
                        var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);
                        Logger.Log(
                            $"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED " +
                            $"path='{_tsFilePath}' frames={_encoder.VideoPacketsWritten} " +
                            $"start_ms={(long)_segmentStartPts.TotalMilliseconds} end_ms={(long)cancelPts.TotalMilliseconds}");
                    }
                    else
                    {
                        Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_NO_SEGMENT no frames encoded in current segment");
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                }
            }
            else
            {
                Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_NO_SEGMENT tsFilePath is null");
            }

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

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var w = _width;
        var h = _height;
        if (w <= 0 || h <= 0)
        {
            return false;
        }

        var drainedCount = 0;
        while (drainedCount < maxPackets)
        {
            VideoFramePacket packet;
            lock (_videoQueueSync)
            {
                if (!reader.TryRead(out packet))
                {
                    break;
                }

                _videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick);
                DecrementQueueDepth(ref _videoQueueDepth, "video");
            }

            _videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber);
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
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
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
            drainedCount++;
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

        // Refresh disk bytes ~4 Hz so the monotonic counter stays current for UI
        // bitrate sampling; the prior frame-count gate plateaued for ~5 s at 60 fps.
        var nowMs = Environment.TickCount64;
        if (nowMs - _lastDiskBytesUpdateMs >= 250)
        {
            _lastDiskBytesUpdateMs = nowMs;
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
        var encoderRotated = false;
        try
        {
            completedPath = _tsFilePath;
            var completedStartPts = _segmentStartPts;
            newPath = _bufferManager.GenerateSegmentPath();

            // RotateOutput flushes encoder queues, writes trailer, then resets
            // TotalBytesWritten to 0 for the new segment. PreviousTotalBytes
            // in the result includes all drain/trailer bytes.
            var result = _encoder.RotateOutput(newPath);
            var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));
            encoderRotated = true;

            _segmentStartPts = currentPts;
            _tsFilePath = newPath;
            _bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);
            Interlocked.Exchange(ref _segmentStartBytes, _encoder.TotalBytesWritten);

            _bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);

            // Update disk bytes tracking
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
            _lastDiskBytesUpdateMs = Environment.TickCount64;

            Logger.Log(
                $"FLASHBACK_SINK_ROTATE new_segment='{Path.GetFileName(newPath)}' " +
                $"prev_bytes={segmentBytes} " +
                $"segment_start_ms={(long)currentPts.TotalMilliseconds}");
            return true;
        }
        catch (Exception ex)
        {
            if (newPath != null && !encoderRotated)
            {
                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);
            }

            Interlocked.Increment(ref _segmentRotationFailures);

            // Register the segment that was open before the rotation attempt so its
            // data remains visible in the buffer index even though rotation failed.
            if (completedPath != null)
            {
                try
                {
                    var failPts = ResolveEncoderPts();
                    if (failPts > _segmentStartPts)
                    {
                        var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);
                        Logger.Log(
                            $"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED " +
                            $"path='{completedPath}' frames={_encoder.VideoPacketsWritten} " +
                            $"start_ms={(long)_segmentStartPts.TotalMilliseconds} end_ms={(long)failPts.TotalMilliseconds}");
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                }
            }

            // Advance _segmentStartPts to prevent infinite retry on every frame
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
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
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
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
            drainedCount++;
        }

        return drainedAny;
    }
}
