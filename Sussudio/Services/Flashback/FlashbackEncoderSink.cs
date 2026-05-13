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

    private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)
        => !useHardwareFrames && IsHighResolutionFrame(context)
            ? HighResolutionCpuVideoQueueCapacity
            : DefaultVideoQueueCapacity;

    private static bool IsHighResolutionFrame(FlashbackSessionContext context)
        => context.Width >= 2560 || context.Height >= 1440;

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


}
