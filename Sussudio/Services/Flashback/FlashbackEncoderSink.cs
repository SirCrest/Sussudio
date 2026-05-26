using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

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

    public event EventHandler<long>? FrameEncoded;

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

    public long DroppedVideoFrames =>
        Interlocked.Read(ref _droppedVideoFrames) +
        Interlocked.Read(ref _gpuFramesDropped) +
        _encoder.DroppedFrameCount;

    public long EncodedVideoFrames => Interlocked.Read(ref _encodedVideoFrames);

    public long AudioSamplesReceived => Interlocked.Read(ref _audioSamplesReceived);

    public long OutputBytes => _bufferManager.TotalDiskBytes;
    public long TotalBytesWritten => _bufferManager.TotalBytesWritten;

    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoFramesSubmittedToEncoder => Interlocked.Read(ref _videoFramesSubmittedToEncoder);
    public long VideoEncoderPts => _encoder.NextVideoPts;
    public long VideoEncoderPacketsWritten => _encoder.VideoPacketsWritten;
    public long VideoEncoderDroppedFrames => _encoder.DroppedFrameCount;
    public long VideoSequenceGaps => _videoLatencyTracker.SequenceGaps;

    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);

    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);

    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);
    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);

    public long SegmentRotationFailures => Interlocked.Read(ref _segmentRotationFailures);

    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);
    public int VideoQueueCapacityFrames => Volatile.Read(ref _videoQueueCapacity);
    public int VideoQueueMaxDepth => Volatile.Read(ref _videoQueueMaxDepth);

    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public int AudioQueueCapacityPackets => AudioQueueCapacity;

    public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);
    public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);

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

    public TimeSpan LastRecordingStartPts { get; private set; }
    public TimeSpan LastRecordingEndPts { get; private set; }
    public bool IsRecordingActive => Volatile.Read(ref _recordingActive) != 0;

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

    Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return StartAsync(CreateSessionContext(context), cancellationToken);
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

    internal Task EncodingCompletionTask => _encodingTask ?? Task.CompletedTask;

    public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        return StopCoreAsync(cancellationToken);
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

    // REVIEWED 2026-04-07: IDisposable fallback only; all callers use DisposeAsync.
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
}
