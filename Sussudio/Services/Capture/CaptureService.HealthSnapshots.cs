using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;
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

    private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(
        SourceSignalTelemetrySnapshot telemetry)
    {
        var suppressedReason = ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty;
        var suppressed = !string.IsNullOrWhiteSpace(suppressedReason);

        return new SourceTelemetryHealthSnapshotFields(
            telemetry.Availability,
            telemetry.Origin,
            telemetry.Confidence,
            telemetry.OriginDetail,
            telemetry.DiagnosticSummary,
            telemetry.TimestampUtc,
            telemetry.Width,
            telemetry.Height,
            telemetry.FrameRateExact,
            telemetry.FrameRateArg,
            telemetry.IsHdr,
            telemetry.VideoFormat,
            telemetry.Colorimetry,
            telemetry.Quantization,
            telemetry.HdrTransferFunction,
            telemetry.HdrTransferCode,
            telemetry.Firmware,
            telemetry.AudioFormat,
            telemetry.AudioSampleRate,
            telemetry.InputSource,
            telemetry.UsbHostProtocol,
            telemetry.HdcpMode,
            telemetry.HdcpVersion,
            telemetry.RxTxHdcpVersion,
            telemetry.RawTimingHex,
            telemetry.DetailEntries,
            ResolveSourceTelemetryBackend(telemetry),
            suppressedReason,
            suppressed,
            ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed));
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

    private readonly record struct SourceTelemetryHealthSnapshotFields(
        SourceTelemetryAvailability Availability,
        SourceTelemetryOrigin Origin,
        SourceTelemetryConfidence Confidence,
        string OriginDetail,
        string? DiagnosticSummary,
        DateTimeOffset TimestampUtc,
        int? Width,
        int? Height,
        double? FrameRateExact,
        string? FrameRateArg,
        bool? IsHdr,
        string? VideoFormat,
        string? Colorimetry,
        string? Quantization,
        string? HdrTransferFunction,
        int? HdrTransferCode,
        string? Firmware,
        string? AudioFormat,
        string? AudioSampleRate,
        string? InputSource,
        string? UsbHostProtocol,
        string? HdcpMode,
        string? HdcpVersion,
        string? RxTxHdcpVersion,
        string? RawTimingHex,
        IReadOnlyList<SourceTelemetryDetailEntry> Details,
        string Backend,
        string SuppressedReason,
        bool Suppressed,
        string CircuitState);

private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(
        FlashbackEncoderSink? fbSink,
        FlashbackBufferManager? bufMgr,
        CaptureSettings? flashbackBackendSettings,
        CaptureSettings? currentSettings)
    {
        var backendSettingsStaleReason = fbSink == null
            ? string.Empty
            : ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings);

        return new FlashbackBufferHealthSnapshotFields(
            fbSink != null,
            (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0),
            bufMgr?.SegmentCount ?? 0,
            bufMgr?.TotalDiskBytes ?? 0,
            bufMgr?.TotalBytesWritten ?? 0,
            bufMgr?.TempDriveAvailableFreeBytes ?? 0,
            bufMgr?.StartupCacheBudgetBytes ?? 0,
            bufMgr?.StartupCacheBytes ?? 0,
            bufMgr?.StartupCacheSessionCount ?? 0,
            bufMgr?.StartupCacheDeletedSessionCount ?? 0,
            bufMgr?.StartupCacheFreedBytes ?? 0,
            bufMgr?.StartupCacheOverBudget ?? false,
            fbSink?.OutputBytes ?? 0,
            bufMgr?.ActiveFilePath,
            fbSink?.EncodedVideoFrames ?? 0,
            fbSink?.DroppedVideoFrames ?? 0,
            fbSink?.GpuEncodingEnabled ?? false,
            !string.IsNullOrEmpty(backendSettingsStaleReason),
            backendSettingsStaleReason,
            flashbackBackendSettings?.Format.ToString() ?? string.Empty,
            currentSettings?.Format.ToString() ?? string.Empty,
            flashbackBackendSettings?.NvencPreset.ToString() ?? string.Empty,
            currentSettings?.NvencPreset.ToString() ?? string.Empty,
            fbSink?.CodecName,
            fbSink?.TargetBitRate ?? 0,
            fbSink?.EncoderWidth ?? 0,
            fbSink?.EncoderHeight ?? 0,
            fbSink?.EncoderFrameRate ?? 0,
            fbSink?.EncoderFrameRateNumerator,
            fbSink?.EncoderFrameRateDenominator);
    }

    private static string ResolveFlashbackBackendSettingsStaleReason(
        CaptureSettings? backendSettings,
        CaptureSettings? requestedSettings)
    {
        if (backendSettings == null || requestedSettings == null)
        {
            return string.Empty;
        }

        var reasons = new List<string>();
        if (backendSettings.Format != requestedSettings.Format)
        {
            reasons.Add($"format:{backendSettings.Format}->{requestedSettings.Format}");
        }

        if (backendSettings.Quality != requestedSettings.Quality)
        {
            reasons.Add($"quality:{backendSettings.Quality}->{requestedSettings.Quality}");
        }

        if (Math.Abs(backendSettings.CustomBitrateMbps - requestedSettings.CustomBitrateMbps) >= 0.01)
        {
            reasons.Add($"bitrate:{backendSettings.CustomBitrateMbps:0.##}->{requestedSettings.CustomBitrateMbps:0.##}");
        }

        if (backendSettings.NvencPreset != requestedSettings.NvencPreset)
        {
            reasons.Add($"preset:{backendSettings.NvencPreset}->{requestedSettings.NvencPreset}");
        }

        if (backendSettings.AudioEnabled != requestedSettings.AudioEnabled)
        {
            reasons.Add($"audio:{backendSettings.AudioEnabled}->{requestedSettings.AudioEnabled}");
        }

        if (backendSettings.MicrophoneEnabled != requestedSettings.MicrophoneEnabled)
        {
            reasons.Add($"microphone:{backendSettings.MicrophoneEnabled}->{requestedSettings.MicrophoneEnabled}");
        }

        if (backendSettings.FlashbackBufferMinutes != requestedSettings.FlashbackBufferMinutes)
        {
            reasons.Add($"bufferMinutes:{backendSettings.FlashbackBufferMinutes}->{requestedSettings.FlashbackBufferMinutes}");
        }

        if (backendSettings.FlashbackGpuDecode != requestedSettings.FlashbackGpuDecode)
        {
            reasons.Add($"gpuDecode:{backendSettings.FlashbackGpuDecode}->{requestedSettings.FlashbackGpuDecode}");
        }

        var backendHdr = HdrOutputPolicy.IsEnabled(backendSettings);
        var requestedHdr = HdrOutputPolicy.IsEnabled(requestedSettings);
        if (backendHdr != requestedHdr)
        {
            reasons.Add($"hdr:{backendHdr}->{requestedHdr}");
        }

        return reasons.Count == 0 ? string.Empty : string.Join(",", reasons);
    }

    private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(
        FlashbackEncoderSink? fbSink,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) videoQueueLatencyMetrics)
        => new(
            fbSink?.VideoQueueCount ?? 0,
            fbSink?.AudioQueueCount ?? 0,
            fbSink?.AudioQueueCapacityPackets ?? 0,
            fbSink?.IsForceRotateActive ?? false,
            fbSink?.IsForceRotateRequested ?? false,
            fbSink?.IsForceRotateDraining ?? false,
            fbSink?.VideoQueueCapacityFrames ?? 0,
            fbSink?.VideoQueueMaxDepth ?? 0,
            fbSink?.VideoFramesSubmittedToEncoder ?? 0,
            fbSink?.VideoEncoderPts ?? 0,
            fbSink?.VideoEncoderPacketsWritten ?? 0,
            fbSink?.VideoEncoderDroppedFrames ?? 0,
            fbSink?.VideoSequenceGaps ?? 0,
            fbSink?.VideoQueueRejectedFrames ?? 0,
            fbSink?.LastVideoQueueRejectReason ?? string.Empty,
            fbSink?.VideoQueueOldestFrameAgeMs ?? 0,
            fbSink?.LastVideoQueueLatencyMs ?? 0,
            videoQueueLatencyMetrics,
            fbSink?.VideoBackpressureWaitMs ?? 0,
            fbSink?.VideoBackpressureEvents ?? 0,
            fbSink?.LastVideoBackpressureWaitMs ?? 0,
            fbSink?.MaxVideoBackpressureWaitMs ?? 0,
            fbSink?.GpuQueueCount ?? 0,
            fbSink?.GpuQueueCapacityFrames ?? 0,
            fbSink?.GpuQueueMaxDepth ?? 0,
            fbSink?.GpuFramesEnqueued ?? 0,
            fbSink?.GpuFramesDropped ?? 0,
            fbSink?.GpuQueueRejectedFrames ?? 0,
            fbSink?.LastGpuQueueRejectReason ?? string.Empty);

    private readonly record struct FlashbackBufferHealthSnapshotFields(
        bool Active,
        long BufferedDurationMs,
        int SegmentCount,
        long DiskBytes,
        long TotalBytesWritten,
        long TempDriveFreeBytes,
        long StartupCacheBudgetBytes,
        long StartupCacheBytes,
        int StartupCacheSessionCount,
        int StartupCacheDeletedSessionCount,
        long StartupCacheFreedBytes,
        bool StartupCacheOverBudget,
        long OutputBytes,
        string? FilePath,
        long EncodedFrames,
        long DroppedFrames,
        bool GpuEncoding,
        bool BackendSettingsStale,
        string BackendSettingsStaleReason,
        string BackendActiveFormat,
        string BackendRequestedFormat,
        string BackendActivePreset,
        string BackendRequestedPreset,
        string? EncoderCodecName,
        uint EncoderTargetBitRate,
        int EncoderWidth,
        int EncoderHeight,
        double EncoderFrameRate,
        int? EncoderFrameRateNumerator,
        int? EncoderFrameRateDenominator);

    private readonly record struct FlashbackQueueHealthSnapshotFields(
        int VideoQueueDepth,
        int AudioQueueDepth,
        int AudioQueueCapacity,
        bool ForceRotateActive,
        bool ForceRotateRequested,
        bool ForceRotateDraining,
        int VideoQueueCapacity,
        int VideoQueueMaxDepth,
        long VideoFramesSubmittedToEncoder,
        long VideoEncoderPts,
        long VideoEncoderPacketsWritten,
        long VideoEncoderDroppedFrames,
        long VideoSequenceGaps,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long VideoQueueOldestFrameAgeMs,
        long VideoQueueLastLatencyMs,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics,
        long VideoBackpressureWaitMs,
        long VideoBackpressureEvents,
        long VideoBackpressureLastWaitMs,
        long VideoBackpressureMaxWaitMs,
        int GpuQueueDepth,
        int GpuQueueCapacity,
        int GpuQueueMaxDepth,
        long GpuFramesEnqueued,
        long GpuFramesDropped,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason);

private readonly record struct FlashbackPlaybackStateHealthSnapshotFields(
        string State,
        long PositionMs,
        string DecoderHwAccel,
        long FrameCount,
        long LateFrames,
        long DroppedFrames,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long LastSegmentSwitchUtcUnixMs,
        long LastFmp4ReopenUtcUnixMs,
        long LastWriteHeadWaitGapMs,
        double TargetFps,
        double ObservedFps,
        double AvgFrameMs,
        long PtsCadenceMismatchCount,
        long LastPtsCadenceMismatchUtcUnixMs,
        double LastPtsCadenceDeltaMs,
        double LastPtsCadenceExpectedMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap,
        double AvDriftMs,
        bool ThreadAlive);

    private readonly record struct FlashbackPlaybackHealthSnapshotFields(
        string State,
        long PositionMs,
        string DecoderHwAccel,
        long FrameCount,
        long LateFrames,
        long DroppedFrames,
        long AudioMasterDelayDoubles,
        long AudioMasterDelayShrinks,
        long AudioMasterFallbacks,
        long AudioMasterUnavailableFallbacks,
        long AudioMasterStaleFallbacks,
        long AudioMasterDriftOutlierFallbacks,
        string AudioMasterLastFallbackReason,
        double AudioMasterLastFallbackDriftMs,
        double AudioMasterLastFallbackClockAgeMs,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long LastSegmentSwitchUtcUnixMs,
        long LastFmp4ReopenUtcUnixMs,
        long LastWriteHeadWaitGapMs,
        double TargetFps,
        double ObservedFps,
        double AvgFrameMs,
        int CadenceSampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrames,
        double SlowFramePercent,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentFrameIntervalsMs,
        long PtsCadenceMismatchCount,
        long LastPtsCadenceMismatchUtcUnixMs,
        double LastPtsCadenceDeltaMs,
        double LastPtsCadenceExpectedMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap,
        int DecodeSampleCount,
        double DecodeAvgMs,
        double DecodeP95Ms,
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        double AvDriftMs,
        bool ThreadAlive,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        int CommandQueueCapacity,
        int PendingCommands,
        int MaxPendingCommands,
        long LastCommandQueueLatencyMs,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        string LastCommandQueued,
        string LastCommandProcessed,
        long LastCommandQueuedUtcUnixMs,
        long LastCommandProcessedUtcUnixMs,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);

    private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var state = CaptureFlashbackPlaybackStateHealthSnapshotFields(fbPlayback);
        var cadence = CaptureFlashbackPlaybackCadenceHealthSnapshotFields(fbPlayback);
        var decode = CaptureFlashbackPlaybackDecodeHealthSnapshotFields(fbPlayback);
        var audioMaster = CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(fbPlayback);
        var commands = CaptureFlashbackPlaybackCommandHealthSnapshotFields(fbPlayback);

        return new FlashbackPlaybackHealthSnapshotFields(
            state.State,
            state.PositionMs,
            state.DecoderHwAccel,
            state.FrameCount,
            state.LateFrames,
            state.DroppedFrames,
            audioMaster.DelayDoubles,
            audioMaster.DelayShrinks,
            audioMaster.Fallbacks,
            audioMaster.UnavailableFallbacks,
            audioMaster.StaleFallbacks,
            audioMaster.DriftOutlierFallbacks,
            audioMaster.LastFallbackReason,
            audioMaster.LastFallbackDriftMs,
            audioMaster.LastFallbackClockAgeMs,
            state.SegmentSwitches,
            state.Fmp4Reopens,
            state.WriteHeadWaits,
            state.NearLiveSnaps,
            state.DecodeErrorSnaps,
            state.SubmitFailures,
            state.LastDropUtcUnixMs,
            state.LastDropReason,
            state.LastSubmitFailureUtcUnixMs,
            state.LastSubmitFailure,
            state.LastSegmentSwitchUtcUnixMs,
            state.LastFmp4ReopenUtcUnixMs,
            state.LastWriteHeadWaitGapMs,
            state.TargetFps,
            state.ObservedFps,
            state.AvgFrameMs,
            cadence.SampleCount,
            cadence.P95FrameMs,
            cadence.P99FrameMs,
            cadence.MaxFrameMs,
            cadence.SlowFrames,
            cadence.SlowFramePercent,
            cadence.OnePercentLowFps,
            cadence.FivePercentLowFps,
            cadence.SampleDurationMs,
            cadence.RecentFrameIntervalsMs,
            state.PtsCadenceMismatchCount,
            state.LastPtsCadenceMismatchUtcUnixMs,
            state.LastPtsCadenceDeltaMs,
            state.LastPtsCadenceExpectedMs,
            state.SeekForwardDecodeCapHits,
            state.LastSeekHitForwardDecodeCap,
            decode.SampleCount,
            decode.AvgMs,
            decode.P95Ms,
            decode.P99Ms,
            decode.MaxMs,
            decode.MaxPhase,
            decode.MaxReceiveMs,
            decode.MaxFeedMs,
            decode.MaxReadMs,
            decode.MaxSendMs,
            decode.MaxAudioMs,
            decode.MaxConvertMs,
            decode.MaxUtcUnixMs,
            decode.MaxPositionMs,
            state.AvDriftMs,
            state.ThreadAlive,
            commands.CommandsEnqueued,
            commands.CommandsProcessed,
            commands.CommandsDropped,
            commands.CommandsSkippedNotReady,
            commands.ScrubUpdatesCoalesced,
            commands.SeekCommandsCoalesced,
            commands.CommandQueueCapacity,
            commands.PendingCommands,
            commands.MaxPendingCommands,
            commands.LastCommandQueueLatencyMs,
            commands.MaxCommandQueueLatencyMs,
            commands.MaxCommandQueueLatencyCommand,
            commands.LastCommandQueued,
            commands.LastCommandProcessed,
            commands.LastCommandQueuedUtcUnixMs,
            commands.LastCommandProcessedUtcUnixMs,
            commands.LastCommandFailureUtcUnixMs,
            commands.LastCommandFailure);
    }

    private static FlashbackPlaybackStateHealthSnapshotFields CaptureFlashbackPlaybackStateHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.State.ToString() ?? "N/A",
            (long)(fbPlayback?.PlaybackPosition.TotalMilliseconds ?? 0),
            fbPlayback?.DecoderHwAccel ?? "N/A",
            fbPlayback?.PlaybackFrameCount ?? 0,
            fbPlayback?.PlaybackLateFrames ?? 0,
            fbPlayback?.PlaybackDroppedFrames ?? 0,
            fbPlayback?.PlaybackSegmentSwitches ?? 0,
            fbPlayback?.PlaybackFmp4Reopens ?? 0,
            fbPlayback?.PlaybackWriteHeadWaits ?? 0,
            fbPlayback?.PlaybackNearLiveSnaps ?? 0,
            fbPlayback?.PlaybackDecodeErrorSnaps ?? 0,
            fbPlayback?.PlaybackSubmitFailures ?? 0,
            fbPlayback?.LastPlaybackDropUtcUnixMs ?? 0,
            fbPlayback?.LastPlaybackDropReason ?? string.Empty,
            fbPlayback?.LastSubmitFailureUtcUnixMs ?? 0,
            fbPlayback?.LastSubmitFailure ?? string.Empty,
            fbPlayback?.LastSegmentSwitchUtcUnixMs ?? 0,
            fbPlayback?.LastFmp4ReopenUtcUnixMs ?? 0,
            fbPlayback?.LastWriteHeadWaitGapMs ?? 0,
            fbPlayback?.PlaybackTargetFps ?? 0,
            fbPlayback?.PlaybackObservedFps ?? 0,
            fbPlayback?.PlaybackAvgFrameMs ?? 0,
            fbPlayback?.PlaybackPtsCadenceMismatchCount ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceMismatchUtcUnixMs ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceDeltaMs ?? 0,
            fbPlayback?.LastPlaybackPtsCadenceExpectedMs ?? 0,
            fbPlayback?.PlaybackSeekForwardDecodeCapHits ?? 0,
            fbPlayback?.LastPlaybackSeekHitForwardDecodeCap ?? false,
            fbPlayback?.AvDriftMs ?? 0,
            fbPlayback?.PlaybackThreadAlive ?? false);

    private readonly record struct FlashbackPlaybackCadenceHealthSnapshotFields(
        int SampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrames,
        double SlowFramePercent,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentFrameIntervalsMs);

    private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics() ?? default;
        return new FlashbackPlaybackCadenceHealthSnapshotFields(
            playbackCadence.SampleCount,
            playbackCadence.P95FrameMs,
            playbackCadence.P99FrameMs,
            playbackCadence.MaxFrameMs,
            playbackCadence.SlowFrameCount,
            playbackCadence.SlowFramePercent,
            playbackCadence.OnePercentLowFps,
            playbackCadence.FivePercentLowFps,
            playbackCadence.SampleDurationMs,
            playbackCadence.RecentFrameIntervalsMs);
    }

    private readonly record struct FlashbackPlaybackDecodeHealthSnapshotFields(
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        string MaxPhase,
        double MaxReceiveMs,
        double MaxFeedMs,
        double MaxReadMs,
        double MaxSendMs,
        double MaxAudioMs,
        double MaxConvertMs,
        long MaxUtcUnixMs,
        long MaxPositionMs);

    private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics() ?? default;
        return new FlashbackPlaybackDecodeHealthSnapshotFields(
            playbackDecode.SampleCount,
            playbackDecode.AvgMs,
            playbackDecode.P95Ms,
            playbackDecode.P99Ms,
            playbackDecode.MaxMs,
            fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty,
            fbPlayback?.PlaybackMaxDecodeReceiveMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeFeedMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeReadMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeSendMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeAudioMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeConvertMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeUtcUnixMs ?? 0,
            fbPlayback?.PlaybackMaxDecodePositionMs ?? 0);
    }

    private readonly record struct FlashbackPlaybackAudioMasterHealthSnapshotFields(
        long DelayDoubles,
        long DelayShrinks,
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks,
        string LastFallbackReason,
        double LastFallbackDriftMs,
        double LastFallbackClockAgeMs);

    private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.PlaybackAudioMasterDelayDoubles ?? 0,
            fbPlayback?.PlaybackAudioMasterDelayShrinks ?? 0,
            fbPlayback?.PlaybackAudioMasterFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterUnavailableFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterStaleFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterDriftOutlierFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackReason ?? string.Empty,
            fbPlayback?.PlaybackAudioMasterLastFallbackDriftMs ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackClockAgeMs ?? 0);

    private readonly record struct FlashbackPlaybackCommandHealthSnapshotFields(
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        int CommandQueueCapacity,
        int PendingCommands,
        int MaxPendingCommands,
        long LastCommandQueueLatencyMs,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        string LastCommandQueued,
        string LastCommandProcessed,
        long LastCommandQueuedUtcUnixMs,
        long LastCommandProcessedUtcUnixMs,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);

    private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.CommandsEnqueued ?? 0,
            fbPlayback?.CommandsProcessed ?? 0,
            fbPlayback?.CommandsDropped ?? 0,
            fbPlayback?.CommandsSkippedNotReady ?? 0,
            fbPlayback?.ScrubUpdatesCoalesced ?? 0,
            fbPlayback?.SeekCommandsCoalesced ?? 0,
            fbPlayback?.CommandQueueCapacityCommands ?? 0,
            fbPlayback?.PendingCommands ?? 0,
            fbPlayback?.MaxPendingCommands ?? 0,
            fbPlayback?.LastCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyMs ?? 0,
            fbPlayback?.MaxCommandQueueLatencyCommand ?? "None",
            fbPlayback?.LastCommandQueued ?? "None",
            fbPlayback?.LastCommandProcessed ?? "None",
            fbPlayback?.LastCommandQueuedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandProcessedUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailureUtcUnixMs ?? 0,
            fbPlayback?.LastCommandFailure ?? string.Empty);

private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink)
    {
        var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();
        var lastFailure = GetLastFailureTelemetry();
        var activeRecording = CaptureActiveRecordingBackendHealthSnapshotFields(
            sink,
            fbSink,
            flashbackIsRecordingBackend);

        return new RecordingHealthSnapshotFields(
            activeRecording.EncodingFailed || lastFailure.RecordingFailed,
            activeRecording.FailureType ?? lastFailure.RecordingFailureType,
            activeRecording.FailureMessage ?? lastFailure.RecordingFailureMessage,
            fbSink?.EncodingFailed == true || lastFailure.FlashbackFailed,
            fbSink?.EncodingFailureType ?? lastFailure.FlashbackFailureType,
            fbSink?.EncodingFailureMessage ?? lastFailure.FlashbackFailureMessage,
            activeRecording.VideoQueueDepth,
            activeRecording.VideoQueueCapacity,
            activeRecording.VideoQueueMaxDepth,
            activeRecording.VideoFramesEnqueued,
            activeRecording.VideoFramesSubmitted,
            activeRecording.VideoEncoderPts,
            activeRecording.VideoEncoderPacketsWritten,
            activeRecording.VideoEncoderDroppedFrames,
            activeRecording.VideoSequenceGaps,
            activeRecording.VideoQueueOldestFrameAgeMs,
            activeRecording.VideoQueueLastLatencyMs,
            activeRecording.VideoQueueLatencyMetrics,
            activeRecording.VideoBackpressureWaitMs,
            activeRecording.VideoBackpressureEvents,
            activeRecording.VideoBackpressureLastWaitMs,
            activeRecording.VideoBackpressureMaxWaitMs,
            activeRecording.DroppedFrames,
            activeRecording.VideoDropsQueueSaturated,
            activeRecording.VideoDropsBacklogEviction,
            activeRecording.AudioQueueDepth,
            activeRecording.AudioDropsQueueSaturated,
            activeRecording.AudioDropsBacklogEviction,
            activeRecording.LastVideoEnqueueTick,
            activeRecording.LastVideoWriteTick,
            activeRecording.EncodedVideoFrames,
            activeRecording.GpuQueueDepth,
            activeRecording.GpuQueueCapacity,
            activeRecording.GpuQueueMaxDepth,
            activeRecording.GpuFramesEnqueued,
            activeRecording.GpuFramesDropped,
            sink?.CudaQueueCount ?? 0,
            sink?.CudaQueueCapacityFrames ?? 0,
            sink?.CudaQueueMaxDepth ?? 0,
            sink?.CudaFramesEnqueued ?? 0,
            sink?.CudaFramesDropped ?? 0,
            activeRecording.FlashbackVideoQueueLatencyMetrics);
    }

    private readonly record struct RecordingHealthSnapshotFields(
        bool EncodingFailed,
        string? FailureType,
        string? FailureMessage,
        bool FlashbackEncodingFailed,
        string? FlashbackFailureType,
        string? FlashbackFailureMessage,
        int VideoQueueDepth,
        int VideoQueueCapacity,
        int VideoQueueMaxDepth,
        long VideoFramesEnqueued,
        long VideoFramesSubmitted,
        long VideoEncoderPts,
        long VideoEncoderPacketsWritten,
        long VideoEncoderDroppedFrames,
        long VideoSequenceGaps,
        long VideoQueueOldestFrameAgeMs,
        long VideoQueueLastLatencyMs,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics,
        long VideoBackpressureWaitMs,
        long VideoBackpressureEvents,
        long VideoBackpressureLastWaitMs,
        long VideoBackpressureMaxWaitMs,
        long DroppedFrames,
        long VideoDropsQueueSaturated,
        long VideoDropsBacklogEviction,
        int AudioQueueDepth,
        long AudioDropsQueueSaturated,
        long AudioDropsBacklogEviction,
        long LastVideoEnqueueTick,
        long LastVideoWriteTick,
        long EncodedVideoFrames,
        int GpuQueueDepth,
        int GpuQueueCapacity,
        int GpuQueueMaxDepth,
        long GpuFramesEnqueued,
        long GpuFramesDropped,
        int CudaQueueDepth,
        int CudaQueueCapacity,
        int CudaQueueMaxDepth,
        long CudaFramesEnqueued,
        long CudaFramesDropped,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) FlashbackVideoQueueLatencyMetrics);

    private ActiveRecordingBackendHealthSnapshotFields CaptureActiveRecordingBackendHealthSnapshotFields(
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink,
        bool flashbackIsRecordingBackend)
    {
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) emptyVideoQueueLatencyMetrics = default;
        var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics ?? emptyVideoQueueLatencyMetrics;
        var videoQueueLatencyMetrics = sink?.VideoQueueLatencyMetrics ??
            (flashbackIsRecordingBackend
                ? flashbackVideoQueueLatencyMetrics
                : emptyVideoQueueLatencyMetrics);

        return new ActiveRecordingBackendHealthSnapshotFields(
            sink?.EncodingFailed == true ||
                (flashbackIsRecordingBackend && fbSink?.EncodingFailed == true),
            sink?.EncodingFailureType ??
                (flashbackIsRecordingBackend ? fbSink?.EncodingFailureType : null),
            sink?.EncodingFailureMessage ??
                (flashbackIsRecordingBackend ? fbSink?.EncodingFailureMessage : null),
            sink?.VideoQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0),
            sink?.VideoQueueCapacityFrames ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueCapacityFrames ?? 0 : 0),
            sink?.VideoQueueMaxDepth ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueMaxDepth ?? 0 : 0),
            sink?.VideoFramesEnqueuedCount ??
                (flashbackIsRecordingBackend ? fbSink?.VideoFramesEnqueuedCount ?? 0 : 0),
            sink?.VideoFramesSubmittedToEncoder ??
                (flashbackIsRecordingBackend ? fbSink?.VideoFramesSubmittedToEncoder ?? 0 : 0),
            sink?.VideoEncoderPts ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPts ?? 0 : 0),
            sink?.VideoEncoderPacketsWritten ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPacketsWritten ?? 0 : 0),
            sink?.VideoEncoderDroppedFrames ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderDroppedFrames ?? 0 : 0),
            sink?.VideoSequenceGaps ??
                (flashbackIsRecordingBackend ? fbSink?.VideoSequenceGaps ?? 0 : 0),
            sink?.VideoQueueOldestFrameAgeMs ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueOldestFrameAgeMs ?? 0 : 0),
            sink?.LastVideoQueueLatencyMs ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoQueueLatencyMs ?? 0 : 0),
            videoQueueLatencyMetrics,
            sink?.VideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureWaitMs ?? 0 : 0),
            sink?.VideoBackpressureEvents ??
                (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureEvents ?? 0 : 0),
            sink?.LastVideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoBackpressureWaitMs ?? 0 : 0),
            sink?.MaxVideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.MaxVideoBackpressureWaitMs ?? 0 : 0),
            sink?.DroppedVideoFrames ??
                (flashbackIsRecordingBackend ? fbSink?.DroppedVideoFrames ?? 0 : Interlocked.Read(ref _videoFramesDropped)),
            sink?.VideoDropsQueueSaturated ??
                (flashbackIsRecordingBackend ? fbSink?.VideoDropsQueueSaturated ?? 0 : 0),
            sink?.VideoDropsBacklogEviction ??
                (flashbackIsRecordingBackend ? fbSink?.VideoDropsBacklogEviction ?? 0 : 0),
            sink?.AudioQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.AudioQueueCount ?? 0 : 0),
            sink?.AudioDropsQueueSaturated ??
                (flashbackIsRecordingBackend ? fbSink?.AudioDropsQueueSaturated ?? 0 : 0),
            sink?.AudioDropsBacklogEviction ??
                (flashbackIsRecordingBackend ? fbSink?.AudioDropsBacklogEviction ?? 0 : 0),
            sink?.LastVideoEnqueueTick ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoEnqueueTick ?? 0 : 0),
            sink?.LastVideoWriteTick ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoWriteTick ?? 0 : 0),
            sink?.EncodedVideoFrames ??
                (flashbackIsRecordingBackend ? fbSink?.EncodedVideoFrames ?? 0 : 0),
            sink?.GpuQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueCount ?? 0 : 0),
            sink?.GpuQueueCapacityFrames ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueCapacityFrames ?? 0 : 0),
            sink?.GpuQueueMaxDepth ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueMaxDepth ?? 0 : 0),
            sink?.GpuFramesEnqueued ??
                (flashbackIsRecordingBackend ? fbSink?.GpuFramesEnqueued ?? 0 : 0),
            sink?.GpuFramesDropped ??
                (flashbackIsRecordingBackend ? fbSink?.GpuFramesDropped ?? 0 : 0),
            flashbackVideoQueueLatencyMetrics);
    }

    private readonly record struct ActiveRecordingBackendHealthSnapshotFields(
        bool EncodingFailed,
        string? FailureType,
        string? FailureMessage,
        int VideoQueueDepth,
        int VideoQueueCapacity,
        int VideoQueueMaxDepth,
        long VideoFramesEnqueued,
        long VideoFramesSubmitted,
        long VideoEncoderPts,
        long VideoEncoderPacketsWritten,
        long VideoEncoderDroppedFrames,
        long VideoSequenceGaps,
        long VideoQueueOldestFrameAgeMs,
        long VideoQueueLastLatencyMs,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics,
        long VideoBackpressureWaitMs,
        long VideoBackpressureEvents,
        long VideoBackpressureLastWaitMs,
        long VideoBackpressureMaxWaitMs,
        long DroppedFrames,
        long VideoDropsQueueSaturated,
        long VideoDropsBacklogEviction,
        int AudioQueueDepth,
        long AudioDropsQueueSaturated,
        long AudioDropsBacklogEviction,
        long LastVideoEnqueueTick,
        long LastVideoWriteTick,
        long EncodedVideoFrames,
        int GpuQueueDepth,
        int GpuQueueCapacity,
        int GpuQueueMaxDepth,
        long GpuFramesEnqueued,
        long GpuFramesDropped,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) FlashbackVideoQueueLatencyMetrics);
}
