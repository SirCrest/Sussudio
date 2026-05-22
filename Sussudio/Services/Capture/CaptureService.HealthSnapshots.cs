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
        var sink = _recordingBackend.LibAvSink;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var fbSink = _flashbackBackend.Sink;
        var bufMgr = _flashbackBackend.BufferManager;
        var fbPlayback = _flashbackBackend.PlaybackController;
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
        var flashbackBackendSettings = _flashbackBackend.SettingsSnapshot;
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
            SessionState = CurrentSessionState,
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
        var timingSnapshot = _videoPipeline.GetMjpegTimingSnapshot(unifiedVideoCapture);
        var fullTiming = timingSnapshot.Details;

        return new MjpegHealthSnapshotFields(
            timingSnapshot.Summary,
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
