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
}
