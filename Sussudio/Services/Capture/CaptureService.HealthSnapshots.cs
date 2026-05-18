using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.Services.Capture;

// Health snapshot projection for diagnostics and automation health checks.
// Keep this read-only; lifecycle mutations belong in coordinator/transition paths.
public partial class CaptureService
{
    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var fbSink = _flashbackSink;
        var bufMgr = _flashbackBufferManager;
        var fbPlayback = _flashbackPlaybackController;
        var fatalCleanupInProgress = Volatile.Read(ref _fatalCleanupInProgress) != 0;
        var flashbackCleanupInProgress = Volatile.Read(ref _flashbackCleanupInProgress) != 0;
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);
        var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);
        var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);
        var avSyncHealth = CaptureAvSyncHealthSnapshotFields();
        var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);
        var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(
            fbSink,
            recordingHealth.FlashbackVideoQueueLatencyMetrics);
        var snapshotUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var flashbackExport = CaptureFlashbackExportHealthSnapshotFields(snapshotUtcUnixMs);
        var flashbackBackendSettings = _flashbackBackendSettings;
        var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(
            fbSink,
            bufMgr,
            flashbackBackendSettings,
            _currentSettings);

        var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);
        var currentSettings = _currentSettings;
        var isRecording = _isRecording;

        return CaptureHealthSnapshotAssembler.Build(new CaptureHealthSnapshotAssemblyFields
        {
            SessionState = _sessionState,
            IsRecording = isRecording,
            RecordingBackend = ResolveRecordingBackendName(),
            RecordingElapsedMs = isRecording ? _recordingStopwatch.ElapsedMilliseconds : 0,
            ExpectedFrameRate = _actualFrameRate ?? currentSettings?.FrameRate ?? 0,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = currentSettings?.RequestedPixelFormat,
            ReaderSourceStreamType = (isRecording || _isVideoPreviewActive) && unifiedVideoCapture != null
                ? "MfSourceReader"
                : null,
            ReaderSourceSubtype = _actualPixelFormat,
            FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(currentSettings, unifiedVideoCapture),
            FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(currentSettings, unifiedVideoCapture),
            LastFrameArrivalMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            LastVideoEnqueueAgeMs = ComputeTickAge(recordingHealth.LastVideoEnqueueTick),
            LastVideoWriteAgeMs = ComputeTickAge(recordingHealth.LastVideoWriteTick),
            FatalCleanupInProgress = fatalCleanupInProgress,
            FlashbackCleanupInProgress = flashbackCleanupInProgress,
            ObservedTelemetry = observedTelemetry,
            SourceTelemetry = sourceTelemetry,
            CaptureCadence = captureCadence,
            MjpegHealth = mjpegHealth,
            AvSyncHealth = avSyncHealth,
            RecordingHealth = recordingHealth,
            FlashbackQueues = flashbackQueues,
            SnapshotUtcUnixMs = snapshotUtcUnixMs,
            FlashbackExport = flashbackExport,
            FlashbackBuffer = flashbackBuffer,
            FlashbackPlayback = flashbackPlayback
        });
    }

    private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        var sourceCadence = unifiedVideoCapture?.GetSourceCadenceMetrics()
            ?? default(MfSourceReaderVideoCapture.SourceCadenceMetrics);

        return new CaptureCadenceHealthSnapshotFields(
            sourceCadence.SampleCount,
            sourceCadence.ObservedFps,
            sourceCadence.ExpectedIntervalMs,
            sourceCadence.AverageIntervalMs,
            sourceCadence.P95IntervalMs,
            sourceCadence.P99IntervalMs,
            sourceCadence.MaxIntervalMs,
            sourceCadence.OnePercentLowFps,
            sourceCadence.FivePercentLowFps,
            sourceCadence.SampleDurationMs,
            sourceCadence.RecentIntervalsMs,
            sourceCadence.JitterStdDevMs,
            sourceCadence.SevereGapCount,
            sourceCadence.EstimatedDroppedFrames,
            sourceCadence.EstimatedDropPercent);
    }

    private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        var timingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();
        var fullTiming = timingSnapshot?.Details ?? _lastFullMjpegPipelineTimingMetrics;

        return new MjpegHealthSnapshotFields(
            timingSnapshot?.Summary ?? _lastMjpegPipelineTimingMetrics,
            fullTiming,
            unifiedVideoCapture?.GetMjpegPreviewJitterMetrics()
                ?? default(MjpegPreviewJitterBuffer.Metrics),
            unifiedVideoCapture?.GetPreviewVisualCadenceMetrics()
                ?? VisualCadenceTracker.Empty,
            unifiedVideoCapture?.GetPreviewVisualCenterCadenceMetrics()
                ?? VisualCadenceTracker.Empty,
            unifiedVideoCapture?.GetMjpegPacketHashMetrics()
                ?? FrameFingerprintCadenceTracker.Empty,
            BuildMjpegDecoderHealthSnapshots(fullTiming));
    }

    private static MjpegDecoderHealthSnapshot[] BuildMjpegDecoderHealthSnapshots(
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? fullTiming)
    {
        return fullTiming?.PerDecoder is { Length: > 0 } perDecoder
            ? Array.ConvertAll(
                perDecoder,
                worker => new MjpegDecoderHealthSnapshot(
                    worker.WorkerIndex,
                    worker.SampleCount,
                    worker.AvgMs,
                    worker.P95Ms,
                    worker.MaxMs))
            : Array.Empty<MjpegDecoderHealthSnapshot>();
    }

    private readonly record struct CaptureHealthSnapshotAssemblyFields
    {
        public CaptureSessionState SessionState { get; init; }

        public bool IsRecording { get; init; }

        public string RecordingBackend { get; init; }

        public long RecordingElapsedMs { get; init; }

        public double ExpectedFrameRate { get; init; }

        public uint? NegotiatedWidth { get; init; }

        public uint? NegotiatedHeight { get; init; }

        public double? NegotiatedFrameRate { get; init; }

        public string? NegotiatedFrameRateArg { get; init; }

        public uint? NegotiatedFrameRateNumerator { get; init; }

        public uint? NegotiatedFrameRateDenominator { get; init; }

        public string? NegotiatedPixelFormat { get; init; }

        public string? RequestedReaderSubtype { get; init; }

        public string? ReaderSourceStreamType { get; init; }

        public string? ReaderSourceSubtype { get; init; }

        public string? FlashbackExportVerificationFormat { get; init; }

        public string? FlashbackCodecDowngradeReason { get; init; }

        public long LastFrameArrivalMs { get; init; }

        public long VideoFramesArrived { get; init; }

        public long LastVideoEnqueueAgeMs { get; init; }

        public long LastVideoWriteAgeMs { get; init; }

        public bool FatalCleanupInProgress { get; init; }

        public bool FlashbackCleanupInProgress { get; init; }

        public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }

        public SourceTelemetryHealthSnapshotFields SourceTelemetry { get; init; }

        public CaptureCadenceHealthSnapshotFields CaptureCadence { get; init; }

        public MjpegHealthSnapshotFields MjpegHealth { get; init; }

        public AvSyncHealthSnapshotFields AvSyncHealth { get; init; }

        public RecordingHealthSnapshotFields RecordingHealth { get; init; }

        public FlashbackQueueHealthSnapshotFields FlashbackQueues { get; init; }

        public long SnapshotUtcUnixMs { get; init; }

        public FlashbackExportHealthSnapshotFields FlashbackExport { get; init; }

        public FlashbackBufferHealthSnapshotFields FlashbackBuffer { get; init; }

        public FlashbackPlaybackHealthSnapshotFields FlashbackPlayback { get; init; }
    }

    private readonly record struct CaptureCadenceHealthSnapshotFields(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentIntervalsMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    private readonly record struct MjpegHealthSnapshotFields(
        UnifiedVideoCapture.MjpegPipelineTimingMetrics Timing,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? FullTiming,
        MjpegPreviewJitterBuffer.Metrics PreviewJitter,
        VisualCadenceTracker.Metrics VisualCadence,
        VisualCadenceTracker.Metrics VisualCenterCadence,
        FrameFingerprintCadenceTracker.Metrics PacketHash,
        MjpegDecoderHealthSnapshot[] PerDecoder);
}
