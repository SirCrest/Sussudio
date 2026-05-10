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
internal sealed class FlashbackEncoderSink : IRecordingSink, IRawVideoFrameEncoder, IRawVideoFrameTryEncoder, IRawVideoFrameLeaseEncoder, IRawVideoFrameLeaseTryEncoder, IGpuVideoFrameEncoder, IGpuVideoFrameTryEncoder
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
    public int VideoQueueCapacityFrames => Volatile.Read(ref _videoQueueCapacity);
    public int VideoQueueMaxDepth => Volatile.Read(ref _videoQueueMaxDepth);

    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public int AudioQueueCapacityPackets => AudioQueueCapacity;

    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoFramesSubmittedToEncoder => Interlocked.Read(ref _videoFramesSubmittedToEncoder);
    public long VideoEncoderPts => _encoder.NextVideoPts;
    public long VideoEncoderPacketsWritten => _encoder.VideoPacketsWritten;
    public long VideoEncoderDroppedFrames => _encoder.DroppedFrameCount;
    public long VideoSequenceGaps => _videoLatencyTracker.SequenceGaps;

    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);

    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);

    public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);

    public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);

    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);

    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);

    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);

    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);
    public long LastVideoQueueLatencyMs => _videoLatencyTracker.LastLatencyMs;
    public long VideoQueueOldestFrameAgeMs => _videoLatencyTracker.GetOldestFrameAgeMs(Volatile.Read(ref _videoQueueDepth));
    public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics => _videoLatencyTracker.GetMetrics();
    public int VideoQueueLatencySampleCount => _videoLatencyTracker.GetMetrics().SampleCount;
    public double VideoQueueLatencyAvgMs => _videoLatencyTracker.GetMetrics().AverageMs;
    public double VideoQueueLatencyP95Ms => _videoLatencyTracker.GetMetrics().P95Ms;
    public double VideoQueueLatencyP99Ms => _videoLatencyTracker.GetMetrics().P99Ms;
    public double VideoQueueLatencyMaxMs => _videoLatencyTracker.GetMetrics().MaxMs;
    public long VideoBackpressureWaitMs => _videoLatencyTracker.BackpressureWaitMs;
    public long VideoBackpressureEvents => _videoLatencyTracker.BackpressureEvents;
    public long LastVideoBackpressureWaitMs => _videoLatencyTracker.LastBackpressureWaitMs;
    public long MaxVideoBackpressureWaitMs => _videoLatencyTracker.MaxBackpressureWaitMs;

    public bool GpuEncodingEnabled => Volatile.Read(ref _gpuEncodingEnabled);
    public int GpuQueueCount => Volatile.Read(ref _gpuQueueDepth);
    public int GpuQueueCapacityFrames => GpuQueueCapacity;
    public int GpuQueueMaxDepth => Volatile.Read(ref _gpuQueueMaxDepth);
    public long GpuFramesEnqueued => Interlocked.Read(ref _gpuFramesEnqueued);
    public long GpuFramesDropped => Interlocked.Read(ref _gpuFramesDropped);
    public long GpuQueueRejectedFrames => Interlocked.Read(ref _gpuQueueRejectedFrames);
    public string? LastGpuQueueRejectReason => Volatile.Read(ref _lastGpuQueueRejectReason);
    public long SegmentRotationFailures => Interlocked.Read(ref _segmentRotationFailures);

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
    public int? EncoderFrameRateNumerator => _sessionContext?.FrameRateNumerator;
    public int? EncoderFrameRateDenominator => _sessionContext?.FrameRateDenominator;
    /// <summary>
    /// True when this sink was started with a P010 (HDR) session context.
    /// Used by CaptureService to detect pixel-format drift between UVC re-negotiations.
    /// </summary>
    public bool? IsP010 => _sessionContext?.IsP010;
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
    public bool IsForceRotateRequested => Volatile.Read(ref _forceRotateRequested);
    public bool IsForceRotateDraining => Volatile.Read(ref _forceRotateDraining);

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
                       !_bufferManager.IsSessionPreservedForRecovery &&
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

            SignalWork("force_rotate_idle");
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

            if (_bufferManager.IsSessionPreservedForRecovery)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback session is preserved for recovery.");
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

    public FlashbackForceRotateResult ForceRotateForExport(
        TimeSpan inPoint,
        TimeSpan outPoint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (inPoint < TimeSpan.Zero || outPoint <= inPoint)
        {
            Logger.Log(
                $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)outPoint.TotalMilliseconds}");
            return FlashbackForceRotateResult.Failed();
        }

        lock (_sync)
        {
            if (!_started || _disposed)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE started={_started} disposed={_disposed}");
                return FlashbackForceRotateResult.Failed();
            }

            if (_encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED failed={_encodingFailure != null} " +
                    $"completed={_encodingTask?.IsCompleted == true} type={_encodingFailure?.GetType().Name ?? "None"}");
                return FlashbackForceRotateResult.Failed();
            }
        }

        // Signal the encoding thread to perform the rotation (all encoder ops must be on that thread)
        var request = new ForceRotateRequest();
        ForceRotateRequest? supersededRequest;
        lock (_sync)
        {
            if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK started={_started} disposed={_disposed} " +
                    $"failed={_encodingFailure != null} completed={_encodingTask?.IsCompleted == true} " +
                    $"type={_encodingFailure?.GetType().Name ?? "None"}");
                return FlashbackForceRotateResult.Failed();
            }

            supersededRequest = _forceRotateRequest;
            _forceRotateInPoint = inPoint;
            _forceRotateOutPoint = outPoint;
            _forceRotateRequest = request;
            Volatile.Write(ref _forceRotateRequested, true);
        }

        if (supersededRequest != null)
        {
            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
            supersededRequest.TryCancel();
        }

        SignalWork("force_rotate_request");

        // AV1 encoding is significantly slower than H.264/HEVC — drain can take
        // much longer at 4K@120fps with a deep queue. Use a longer timeout for AV1.
        var codecName = _sessionContext?.CodecName ?? string.Empty;
        var isSlowCodec = codecName.Contains("av1", StringComparison.OrdinalIgnoreCase);
        var timeoutSeconds = isSlowCodec ? 10 : 3;
        try
        {
            if (!request.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken))
            {
                var cancelled = TryCancelForceRotate(request);
                Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT codec={codecName} timeout_s={timeoutSeconds} cancelled={cancelled} vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
                if (!cancelled)
                {
                    Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED");
                    if (request.Task.Wait(TimeSpan.FromMilliseconds(ForceRotateCommittedGraceMs)))
                    {
                        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());
                    }

                    Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING grace_ms={ForceRotateCommittedGraceMs}");
                    return FlashbackForceRotateResult.CommittedPending();
                }

                return FlashbackForceRotateResult.CanceledBeforeCommit();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var cancelled = TryCancelForceRotate(request);
            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_CANCELLED codec={codecName} cancelled={cancelled} vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
            if (!cancelled)
            {
                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED");
            }

            throw;
        }

        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());
    }

    private bool TryCancelForceRotate(ForceRotateRequest request)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_forceRotateRequest, request))
            {
                _forceRotateRequested = false;
                _forceRotateRequest = null;
            }
        }

        return request.TryCancel();
    }

    private void CompletePendingForceRotateWithEmptyResult()
    {
        ForceRotateRequest? pendingRequest;
        lock (_sync)
        {
            _forceRotateRequested = false;
            pendingRequest = _forceRotateRequest;
            _forceRotateRequest = null;
        }

        lock (_videoQueueSync)
        {
            Volatile.Write(ref _forceRotateDraining, false);
        }

        pendingRequest?.CompleteEmpty();
    }

    private static bool ShouldAbortForceRotateDrain(
        ForceRotateRequest request,
        string phase,
        int inFlightRounds)
    {
        if (!request.IsCompleted)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}");
        return true;
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

    // ── Queue Management ────────────────────────────────────────────────────

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

    private void ResetVideoDiagnostics()
    {
        _videoLatencyTracker.ResetAll();
    }

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

    private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio);

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

        // Total saturation — both eviction and re-enqueue failed
        var saturatedSamples = GetSampleCount(packet.Length);
        Interlocked.Add(ref _droppedAudioSamplesCount, saturatedSamples);
        ReturnBuffer(packet.Buffer);
        return false;
        }
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
            SplitEncodeMode = context.SplitEncodeMode,
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
            NvencPreset = context.Settings.NvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode),
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
