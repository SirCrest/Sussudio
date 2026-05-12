using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal sealed class FlashbackRecordingSessionMetrics
{
    public int SampleCount { get; init; }
    public bool BackendObserved { get; init; }
    public bool FileGrowthObserved { get; init; }
    public long VideoFramesSubmittedDelta { get; init; }
    public long VideoEncoderPacketsWrittenDelta { get; init; }
    public long IntegritySequenceGapsAtEnd { get; init; }
    public long IntegrityQueueDroppedFramesAtEnd { get; init; }
    public long IntegritySequenceGapsDelta { get; init; }
    public long IntegrityQueueDroppedFramesDelta { get; init; }
}

internal sealed class FlashbackPlaybackSessionMetrics
{
    public bool Observed { get; set; }
    public JsonElement BaselineSnapshot { get; init; }
    public JsonElement EndSnapshot { get; set; }
    public long EndSessionFrameCount { get; set; }
    public int MaxPendingCommandsObserved { get; set; }
    public int MaxCommandQueueLatencyMsObserved { get; set; }
    public string MaxCommandQueueLatencyCommandObserved { get; set; } = string.Empty;
    public double MinObservedFpsObserved { get; set; } = double.PositiveInfinity;
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
    public bool OnePercentLowSampleWindowObserved { get; set; }
    public long MinimumOnePercentLowFrameCount { get; set; }
    public long MaxSessionFrameCountObserved { get; set; }
    public long MinOnePercentLowOffsetMs { get; set; }
    public long MinOnePercentLowFrameCount { get; set; }
    public double MinOnePercentLowP99FrameMs { get; set; }
    public double MinOnePercentLowMaxFrameMs { get; set; }
    public double MinOnePercentLowDecodeP99Ms { get; set; }
    public double MinOnePercentLowDecodeMaxMs { get; set; }
    public double MinOnePercentLowAvDriftMs { get; set; }
    public long MinOnePercentLowAudioMasterFallbacks { get; set; }
    public double MaxP99FrameMsObserved { get; set; }
    public double MaxFrameMsObserved { get; set; }
    public double MaxSlowFramePercentObserved { get; set; }
    public double MaxDecodeP99MsObserved { get; set; }
    public double MaxDecodeMsObserved { get; set; }
    public string MaxDecodePhaseObserved { get; set; } = string.Empty;
    public double MaxDecodeReceiveMsObserved { get; set; }
    public double MaxDecodeFeedMsObserved { get; set; }
    public double MaxDecodeReadMsObserved { get; set; }
    public double MaxDecodeSendMsObserved { get; set; }
    public double MaxDecodeAudioMsObserved { get; set; }
    public double MaxDecodeConvertMsObserved { get; set; }
    public long MaxDecodeUtcUnixMsObserved { get; set; }
    public long MaxDecodePositionMsObserved { get; set; }
    public long MaxAudioMasterDelayDoublesObserved { get; set; }
    public long MaxAudioMasterDelayShrinksObserved { get; set; }
    public long MaxAudioMasterFallbacksObserved { get; set; }
    public double MaxAudioBufferedDurationMsObserved { get; set; }
    public double MaxAudioQueueDurationMsObserved { get; set; }
    public double MaxAbsAvDriftMsObserved { get; set; }
    public long DroppedFramesDelta { get; set; }
    public long SubmitFailuresDelta { get; set; }
}

internal sealed class FlashbackPlaybackResultMetrics
{
    public JsonElement EndSnapshot { get; init; }
    public int PendingCommandsAtEnd { get; init; }
    public int MaxPendingCommandsObserved { get; init; }
    public int MaxCommandQueueLatencyMsObserved { get; init; }
    public string MaxCommandQueueLatencyCommandObserved { get; init; } = string.Empty;
    public long CommandsDroppedAtEnd { get; init; }
    public long CommandsSkippedNotReadyAtEnd { get; init; }
    public long ScrubUpdatesCoalescedAtEnd { get; init; }
    public long SeekCommandsCoalescedAtEnd { get; init; }
    public string LastCommandFailureAtEnd { get; init; } = string.Empty;
    public long LastCommandFailureUtcUnixMsAtEnd { get; init; }
    public double ObservedFpsAtEnd { get; init; }
    public double AvgFrameMsAtEnd { get; init; }
    public double P99FrameMsAtEnd { get; init; }
    public double MaxFrameMsAtEnd { get; init; }
    public double OnePercentLowFpsAtEnd { get; init; }
    public double DecodeAvgMsAtEnd { get; init; }
    public double DecodeP95MsAtEnd { get; init; }
    public double DecodeP99MsAtEnd { get; init; }
    public double DecodeMaxMsAtEnd { get; init; }
    public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;
    public double MaxDecodeReceiveMsAtEnd { get; init; }
    public double MaxDecodeFeedMsAtEnd { get; init; }
    public double MaxDecodeReadMsAtEnd { get; init; }
    public double MaxDecodeSendMsAtEnd { get; init; }
    public double MaxDecodeAudioMsAtEnd { get; init; }
    public double MaxDecodeConvertMsAtEnd { get; init; }
    public long MaxDecodeUtcUnixMsAtEnd { get; init; }
    public long MaxDecodePositionMsAtEnd { get; init; }
    public long FrameCountAtEnd { get; init; }
    public long LateFramesAtEnd { get; init; }
    public long SlowFramesAtEnd { get; init; }
    public double SlowFramePercentAtEnd { get; init; }
    public long DroppedFramesAtEnd { get; init; }
    public long AudioMasterDelayDoublesAtEnd { get; init; }
    public long AudioMasterDelayShrinksAtEnd { get; init; }
    public long AudioMasterFallbacksAtEnd { get; init; }
    public long AudioMasterUnavailableFallbacksAtEnd { get; init; }
    public long AudioMasterStaleFallbacksAtEnd { get; init; }
    public long AudioMasterDriftOutlierFallbacksAtEnd { get; init; }
    public string AudioMasterLastFallbackReasonAtEnd { get; init; } = string.Empty;
    public double AudioMasterLastFallbackClockAgeMsAtEnd { get; init; }
    public long SubmitFailuresAtEnd { get; init; }
    public long SegmentSwitchesAtEnd { get; init; }
    public long Fmp4ReopensAtEnd { get; init; }
    public long WriteHeadWaitsAtEnd { get; init; }
    public long NearLiveSnapsAtEnd { get; init; }
    public long DecodeErrorSnapsAtEnd { get; init; }
    public long LastWriteHeadWaitGapMsAtEnd { get; init; }
    public long SeekForwardDecodeCapHitsAtEnd { get; init; }
    public long SeekForwardDecodeCapHitsDelta { get; init; }
    public bool LastSeekHitForwardDecodeCapAtEnd { get; init; }
}

internal sealed class FlashbackExportSessionMetrics
{
    public bool Observed { get; set; }
    public bool ActiveAtEnd { get; set; }
    public string StatusAtEnd { get; set; } = "NotStarted";
    public string MessageAtEnd { get; set; } = string.Empty;
    public string FailureKindAtEnd { get; set; } = string.Empty;
    public string OutputPathAtEnd { get; set; } = string.Empty;
    public long LastExportIdAtEnd { get; set; }
    public string LastSuccessAtEnd { get; set; } = string.Empty;
    public string LastMessageAtEnd { get; set; } = string.Empty;
    public long MaxElapsedMsObserved { get; set; }
    public long MaxLastProgressAgeMsObserved { get; set; }
    public long MaxOutputBytesObserved { get; set; }
    public double MaxThroughputBytesPerSecObserved { get; set; }
}

internal static class DiagnosticSessionFlashbackMetrics
{
    internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var recordingSamples = samples
            .Select(sample => sample.Snapshot)
            .Where(snapshot => GetBool(snapshot, "IsRecording"))
            .ToArray();
        if (recordingSamples.Length == 0)
        {
            return new FlashbackRecordingSessionMetrics();
        }

        var firstRecordingSample = recordingSamples[0];
        var finalRecordingSample = recordingSamples[^1];
        return new FlashbackRecordingSessionMetrics
        {
            SampleCount = recordingSamples.Length,
            BackendObserved = recordingSamples.Any(snapshot =>
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase)),
            FileGrowthObserved = recordingSamples.Any(snapshot => GetBool(snapshot, "RecordingFileGrowing")),
            VideoFramesSubmittedDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0),
            VideoEncoderPacketsWrittenDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0),
            IntegritySequenceGapsAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegritySequenceGaps") ?? 0,
            IntegrityQueueDroppedFramesAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegrityQueueDroppedFrames") ?? 0,
            IntegritySequenceGapsDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegritySequenceGaps"),
            IntegrityQueueDroppedFramesDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegrityQueueDroppedFrames")
        };
    }

    internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackPlaybackSessionMetrics { BaselineSnapshot = initialSnapshot };
        var baselinePlaybackActive = IsPlaybackSnapshotActive(initialSnapshot);
        var baselineFrameCount = GetNullableLong(initialSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineCommandsEnqueued = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var baselineCommandsProcessed = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        foreach (var sample in samples)
        {
            ObservePlaybackSnapshot(
                metrics,
                sample.Snapshot,
                sample.OffsetMs,
                baselineFrameCount,
                baselineCommandsEnqueued,
                baselineCommandsProcessed,
                baselinePlaybackActive);
        }

        ObservePlaybackSnapshot(
            metrics,
            lastSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0,
            baselineFrameCount,
            baselineCommandsEnqueued,
            baselineCommandsProcessed,
            baselinePlaybackActive);

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        if (double.IsPositiveInfinity(metrics.MinObservedFpsObserved))
        {
            metrics.MinObservedFpsObserved = 0;
        }

        if (metrics.Observed)
        {
            metrics.DroppedFramesDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackDroppedFrames");
            metrics.SubmitFailuresDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackSubmitFailures");
        }

        return metrics;
    }

    internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(
        FlashbackPlaybackSessionMetrics metrics)
    {
        var observed = metrics.Observed;
        var endSnapshot = metrics.EndSnapshot;
        return new FlashbackPlaybackResultMetrics
        {
            EndSnapshot = endSnapshot,
            PendingCommandsAtEnd = observed ? GetInt(endSnapshot, "FlashbackPlaybackPendingCommands") : 0,
            MaxPendingCommandsObserved = metrics.MaxPendingCommandsObserved,
            MaxCommandQueueLatencyMsObserved = metrics.MaxCommandQueueLatencyMsObserved,
            MaxCommandQueueLatencyCommandObserved = metrics.MaxCommandQueueLatencyCommandObserved,
            CommandsDroppedAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsDropped"),
            CommandsSkippedNotReadyAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsSkippedNotReady"),
            ScrubUpdatesCoalescedAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced"),
            SeekCommandsCoalescedAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekCommandsCoalesced"),
            LastCommandFailureAtEnd = observed ? GetString(endSnapshot, "FlashbackPlaybackLastCommandFailure") ?? string.Empty : string.Empty,
            LastCommandFailureUtcUnixMsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs"),
            ObservedFpsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackObservedFps"),
            AvgFrameMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackAvgFrameMs"),
            P99FrameMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackP99FrameMs"),
            MaxFrameMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxFrameMs"),
            OnePercentLowFpsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackOnePercentLowFps"),
            DecodeAvgMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeAvgMs"),
            DecodeP95MsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeP95Ms"),
            DecodeP99MsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeP99Ms"),
            DecodeMaxMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeMaxMs"),
            MaxDecodePhaseAtEnd = observed ? GetString(endSnapshot, "FlashbackPlaybackMaxDecodePhase") ?? string.Empty : string.Empty,
            MaxDecodeReceiveMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeReceiveMs"),
            MaxDecodeFeedMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeFeedMs"),
            MaxDecodeReadMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeReadMs"),
            MaxDecodeSendMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeSendMs"),
            MaxDecodeAudioMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeAudioMs"),
            MaxDecodeConvertMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeConvertMs"),
            MaxDecodeUtcUnixMsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackMaxDecodeUtcUnixMs"),
            MaxDecodePositionMsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackMaxDecodePositionMs"),
            FrameCountAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFrameCount"),
            LateFramesAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLateFrames"),
            SlowFramesAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSlowFrames"),
            SlowFramePercentAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackSlowFramePercent"),
            DroppedFramesAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDroppedFrames"),
            AudioMasterDelayDoublesAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"),
            AudioMasterDelayShrinksAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"),
            AudioMasterFallbacksAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterFallbacks"),
            AudioMasterUnavailableFallbacksAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks"),
            AudioMasterStaleFallbacksAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks"),
            AudioMasterDriftOutlierFallbacksAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks"),
            AudioMasterLastFallbackReasonAtEnd = observed ? GetString(endSnapshot, "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty : string.Empty,
            AudioMasterLastFallbackClockAgeMsAtEnd = GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs"),
            SubmitFailuresAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSubmitFailures"),
            SegmentSwitchesAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSegmentSwitches"),
            Fmp4ReopensAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFmp4Reopens"),
            WriteHeadWaitsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackWriteHeadWaits"),
            NearLiveSnapsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackNearLiveSnaps"),
            DecodeErrorSnapsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDecodeErrorSnaps"),
            LastWriteHeadWaitGapMsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs"),
            SeekForwardDecodeCapHitsAtEnd = GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits"),
            SeekForwardDecodeCapHitsDelta = observed
                ? GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")
                : 0,
            LastSeekHitForwardDecodeCapAtEnd = observed &&
                                               GetBool(endSnapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap")
        };
    }

    internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackExportSessionMetrics();
        var baselineExportId = GetNullableLong(initialSnapshot, "FlashbackExportId") ?? 0;
        var baselineExportActive = GetBool(initialSnapshot, "FlashbackExportActive");
        foreach (var sample in samples)
        {
            ObserveExportSnapshot(metrics, sample.Snapshot, baselineExportId, baselineExportActive);
        }

        ObserveExportSnapshot(metrics, lastSnapshot, baselineExportId, baselineExportActive);
        return metrics;
    }

    private static void ObservePlaybackSnapshot(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot,
        long offsetMs,
        long baselineFrameCount,
        long baselineCommandsEnqueued,
        long baselineCommandsProcessed,
        bool baselinePlaybackActive)
    {
        var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var sessionFrameCount = frameCount >= baselineFrameCount
            ? frameCount - baselineFrameCount
            : frameCount;
        var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
        }

        var minimumPlaybackFramesForLowPercentile = Math.Max(
            240,
            targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240);
        metrics.MinimumOnePercentLowFrameCount = Math.Max(
            metrics.MinimumOnePercentLowFrameCount,
            minimumPlaybackFramesForLowPercentile);
        metrics.MaxSessionFrameCountObserved = Math.Max(metrics.MaxSessionFrameCountObserved, sessionFrameCount);
        var commandsEnqueued = GetNullableLong(snapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var commandsProcessed = GetNullableLong(snapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        var relevantToSession =
            IsPlaybackSnapshotActive(snapshot) ||
            GetInt(snapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            frameCount > baselineFrameCount ||
            commandsEnqueued > baselineCommandsEnqueued ||
            commandsProcessed > baselineCommandsProcessed ||
            baselinePlaybackActive;
        if (!relevantToSession)
        {
            return;
        }

        metrics.Observed = true;
        metrics.EndSnapshot = snapshot;
        metrics.EndSessionFrameCount = sessionFrameCount;
        metrics.MaxPendingCommandsObserved = Math.Max(
            metrics.MaxPendingCommandsObserved,
            GetInt(snapshot, "FlashbackPlaybackMaxPendingCommands"));
        var maxCommandQueueLatencyMs = GetInt(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)
        {
            metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;
            metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand") ?? string.Empty;
        }

        var observedFps = GetDouble(snapshot, "FlashbackPlaybackObservedFps");
        if (observedFps > 0)
        {
            metrics.MinObservedFpsObserved = Math.Min(metrics.MinObservedFpsObserved, observedFps);
        }

        var onePercentLow = GetDouble(snapshot, "FlashbackPlaybackOnePercentLowFps");
        if (onePercentLow > 0 && sessionFrameCount >= minimumPlaybackFramesForLowPercentile)
        {
            metrics.OnePercentLowSampleWindowObserved = true;
            if (onePercentLow < metrics.MinOnePercentLowFpsObserved)
            {
                metrics.MinOnePercentLowFpsObserved = onePercentLow;
                metrics.MinOnePercentLowOffsetMs = offsetMs;
                metrics.MinOnePercentLowFrameCount = frameCount;
                metrics.MinOnePercentLowP99FrameMs = GetDouble(snapshot, "FlashbackPlaybackP99FrameMs");
                metrics.MinOnePercentLowMaxFrameMs = GetDouble(snapshot, "FlashbackPlaybackMaxFrameMs");
                metrics.MinOnePercentLowDecodeP99Ms = GetDouble(snapshot, "FlashbackPlaybackDecodeP99Ms");
                metrics.MinOnePercentLowDecodeMaxMs = GetDouble(snapshot, "FlashbackPlaybackDecodeMaxMs");
                metrics.MinOnePercentLowAvDriftMs = GetDouble(snapshot, "FlashbackAvDriftMs");
                metrics.MinOnePercentLowAudioMasterFallbacks =
                    GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
            }
        }

        metrics.MaxP99FrameMsObserved = Math.Max(metrics.MaxP99FrameMsObserved, GetDouble(snapshot, "FlashbackPlaybackP99FrameMs"));
        metrics.MaxFrameMsObserved = Math.Max(metrics.MaxFrameMsObserved, GetDouble(snapshot, "FlashbackPlaybackMaxFrameMs"));
        metrics.MaxSlowFramePercentObserved = Math.Max(metrics.MaxSlowFramePercentObserved, GetDouble(snapshot, "FlashbackPlaybackSlowFramePercent"));
        metrics.MaxDecodeP99MsObserved = Math.Max(metrics.MaxDecodeP99MsObserved, GetDouble(snapshot, "FlashbackPlaybackDecodeP99Ms"));
        var decodeMaxMs = GetDouble(snapshot, "FlashbackPlaybackDecodeMaxMs");
        if (decodeMaxMs >= metrics.MaxDecodeMsObserved)
        {
            metrics.MaxDecodeMsObserved = decodeMaxMs;
            metrics.MaxDecodePhaseObserved = GetString(snapshot, "FlashbackPlaybackMaxDecodePhase") ?? string.Empty;
            metrics.MaxDecodeReceiveMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeReceiveMs");
            metrics.MaxDecodeFeedMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeFeedMs");
            metrics.MaxDecodeReadMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeReadMs");
            metrics.MaxDecodeSendMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeSendMs");
            metrics.MaxDecodeAudioMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeAudioMs");
            metrics.MaxDecodeConvertMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeConvertMs");
            metrics.MaxDecodeUtcUnixMsObserved = GetNullableLong(snapshot, "FlashbackPlaybackMaxDecodeUtcUnixMs") ?? 0;
            metrics.MaxDecodePositionMsObserved = GetNullableLong(snapshot, "FlashbackPlaybackMaxDecodePositionMs") ?? 0;
        }
        metrics.MaxAudioMasterDelayDoublesObserved = Math.Max(
            metrics.MaxAudioMasterDelayDoublesObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"));
        metrics.MaxAudioMasterDelayShrinksObserved = Math.Max(
            metrics.MaxAudioMasterDelayShrinksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"));
        metrics.MaxAudioMasterFallbacksObserved = Math.Max(
            metrics.MaxAudioMasterFallbacksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks"));
        metrics.MaxAudioBufferedDurationMsObserved = Math.Max(metrics.MaxAudioBufferedDurationMsObserved, GetDouble(snapshot, "WasapiPlaybackBufferedDurationMs"));
        metrics.MaxAudioQueueDurationMsObserved = Math.Max(metrics.MaxAudioQueueDurationMsObserved, GetDouble(snapshot, "WasapiPlaybackQueueDurationMs"));
        metrics.MaxAbsAvDriftMsObserved = Math.Max(metrics.MaxAbsAvDriftMsObserved, Math.Abs(GetDouble(snapshot, "FlashbackAvDriftMs")));
    }

    private static void ObserveExportSnapshot(
        FlashbackExportSessionMetrics metrics,
        JsonElement snapshot,
        long baselineExportId,
        bool baselineExportActive)
    {
        var exportId = GetNullableLong(snapshot, "FlashbackExportId") ?? 0;
        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var active = GetBool(snapshot, "FlashbackExportActive");
        var relevantToSession =
            active ||
            exportId > baselineExportId ||
            baselineExportActive && exportId == baselineExportId ||
            baselineExportId <= 0 &&
            !string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "NotStarted", StringComparison.OrdinalIgnoreCase);
        if (!relevantToSession)
        {
            return;
        }

        metrics.Observed = true;
        metrics.ActiveAtEnd = active;
        metrics.StatusAtEnd = status;
        metrics.MessageAtEnd = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        metrics.FailureKindAtEnd = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        metrics.OutputPathAtEnd = GetString(snapshot, "FlashbackExportOutputPath") ?? string.Empty;
        var lastExportId = GetNullableLong(snapshot, "LastExportId") ?? 0;
        metrics.LastExportIdAtEnd = lastExportId;
        if (!active && exportId > 0 && lastExportId == exportId)
        {
            metrics.LastSuccessAtEnd = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
            metrics.LastMessageAtEnd = GetString(snapshot, "LastExportMessage") ?? string.Empty;
        }
        else
        {
            metrics.LastSuccessAtEnd = string.Empty;
            metrics.LastMessageAtEnd = string.Empty;
        }
        metrics.MaxElapsedMsObserved = Math.Max(
            metrics.MaxElapsedMsObserved,
            GetNullableLong(snapshot, "FlashbackExportElapsedMs") ?? 0);
        metrics.MaxLastProgressAgeMsObserved = Math.Max(
            metrics.MaxLastProgressAgeMsObserved,
            GetNullableLong(snapshot, "FlashbackExportLastProgressAgeMs") ?? 0);
        metrics.MaxOutputBytesObserved = Math.Max(
            metrics.MaxOutputBytesObserved,
            GetNullableLong(snapshot, "FlashbackExportOutputBytes") ?? 0);
        metrics.MaxThroughputBytesPerSecObserved = Math.Max(
            metrics.MaxThroughputBytesPerSecObserved,
            GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"));
    }

    private static bool IsPlaybackSnapshotActive(JsonElement snapshot)
    {
        var state = GetString(snapshot, "FlashbackPlaybackState") ?? string.Empty;
        return GetBool(snapshot, "FlashbackPlaybackThreadAlive") ||
               string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Paused", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Seeking", StringComparison.OrdinalIgnoreCase);
    }

    private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetNullableLong(snapshot, propertyName) ?? 0 : 0;

    private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetDouble(snapshot, propertyName) : 0;
}
