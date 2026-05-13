using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
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
}
