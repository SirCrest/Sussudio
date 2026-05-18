using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
