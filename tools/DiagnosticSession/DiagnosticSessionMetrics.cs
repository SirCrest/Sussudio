using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal sealed class SourceCadenceSessionMetrics
{
    public long MaxSevereGapCountObserved { get; set; }
    public long MaxEstimatedDroppedFramesObserved { get; set; }
    public double MaxDropPercentObserved { get; set; }
}

internal sealed class PreviewCadenceSessionMetrics
{
    public double OnePercentLowFpsAtEnd { get; init; }
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
}

internal sealed class VisualCadenceSessionMetrics
{
    public double OutputFpsAtEnd { get; init; }
    public double ChangeFpsAtEnd { get; init; }
    public double MinChangeFpsObserved { get; set; } = double.PositiveInfinity;
    public double RepeatPercentAtEnd { get; init; }
    public double MaxRepeatPercentObserved { get; set; }
    public long RepeatFramesAtEnd { get; init; }
    public long LongestRepeatRunAtEnd { get; init; }
}

internal sealed class PreviewD3DMetrics
{
    public long MissedRefreshDelta { get; init; }
    public long StatsFailureDelta { get; init; }
    public int MaxRecentSlowFramesObserved { get; set; }
    public string LatestSlowFrameReason { get; set; } = string.Empty;
    public double LatestSlowFrameOverBudgetMs { get; set; }
    public double LatestSlowFramePresentIntervalMs { get; set; }
    public double LatestSlowFrameTotalFrameCpuMs { get; set; }
    public double LatestSlowFramePresentCallMs { get; set; }
    public int LatestSlowFramePendingFrameCount { get; set; }
    public double InputUploadCpuP99MsAtEnd { get; init; }
    public double InputUploadCpuMaxMsObserved { get; set; }
    public double RenderSubmitCpuP99MsAtEnd { get; init; }
    public double RenderSubmitCpuMaxMsObserved { get; set; }
    public double PresentCallP99MsAtEnd { get; init; }
    public double PresentCallMaxMsObserved { get; set; }
    public double TotalFrameCpuP99MsAtEnd { get; init; }
    public double TotalFrameCpuMaxMsObserved { get; set; }
}

internal readonly record struct PlaybackCommandHealth(
    long Dropped,
    long Skipped,
    long SubmitFailures,
    long CoalescedScrub,
    long CoalescedSeek,
    long NonCoalescedDropped);

internal static class DiagnosticSessionMetrics
{
    internal static long GetCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return Math.Max(0, current - baseline);
    }

    internal static long GetResetAwareCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return current >= baseline ? current - baseline : current;
    }

    internal static PlaybackCommandHealth BuildPlaybackCommandHealth(JsonElement snapshot, JsonElement baselineSnapshot)
    {
        var dropped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var submitFailures = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSubmitFailures");
        var coalescedScrub = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced");
        var coalescedSeek = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSeekCommandsCoalesced");
        return new PlaybackCommandHealth(
            dropped,
            skipped,
            submitFailures,
            coalescedScrub,
            coalescedSeek,
            Math.Max(0, dropped - coalescedScrub));
    }

    internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new SourceCadenceSessionMetrics();
        ObserveSourceCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObserveSourceCadenceSnapshot(metrics, sample.Snapshot);
        }

        return metrics;
    }

    private static void ObserveSourceCadenceSnapshot(SourceCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        metrics.MaxSevereGapCountObserved = Math.Max(
            metrics.MaxSevereGapCountObserved,
            GetNullableLong(snapshot, "CaptureCadenceSevereGapCount") ?? 0);
        metrics.MaxEstimatedDroppedFramesObserved = Math.Max(
            metrics.MaxEstimatedDroppedFramesObserved,
            GetNullableLong(snapshot, "CaptureCadenceEstimatedDroppedFrames") ?? 0);
        metrics.MaxDropPercentObserved = Math.Max(
            metrics.MaxDropPercentObserved,
            GetDouble(snapshot, "CaptureCadenceEstimatedDropPercent"));
    }

    internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new PreviewCadenceSessionMetrics
        {
            OnePercentLowFpsAtEnd = GetDouble(lastSnapshot, "PreviewCadenceOnePercentLowFps")
        };
        ObservePreviewCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObservePreviewCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        return metrics;
    }

    private static void ObservePreviewCadenceSnapshot(PreviewCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var onePercentLow = GetDouble(snapshot, "PreviewCadenceOnePercentLowFps");
        if (onePercentLow > 0)
        {
            metrics.MinOnePercentLowFpsObserved = Math.Min(metrics.MinOnePercentLowFpsObserved, onePercentLow);
        }
    }

    internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new VisualCadenceSessionMetrics
        {
            OutputFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceOutputObservedFps"),
            ChangeFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceChangeObservedFps"),
            RepeatPercentAtEnd = GetDouble(lastSnapshot, "VisualCadenceRepeatFramePercent"),
            RepeatFramesAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceRepeatFrameCount") ?? 0,
            LongestRepeatRunAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceLongestRepeatRun") ?? 0
        };
        ObserveVisualCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObserveVisualCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinChangeFpsObserved))
        {
            metrics.MinChangeFpsObserved = 0;
        }

        return metrics;
    }

    internal static bool IsVisualCadenceSessionHealthy(VisualCadenceSessionMetrics metrics, double targetFps)
        => targetFps > 0 &&
           metrics.MinChangeFpsObserved >= targetFps * 0.98 &&
           metrics.MaxRepeatPercentObserved <= 1.0 &&
           metrics.LongestRepeatRunAtEnd <= 1;

    private static void ObserveVisualCadenceSnapshot(VisualCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var changeFps = GetDouble(snapshot, "VisualCadenceChangeObservedFps");
        if (changeFps > 0)
        {
            metrics.MinChangeFpsObserved = Math.Min(metrics.MinChangeFpsObserved, changeFps);
        }

        metrics.MaxRepeatPercentObserved = Math.Max(
            metrics.MaxRepeatPercentObserved,
            GetDouble(snapshot, "VisualCadenceRepeatFramePercent"));
    }

    internal static PreviewD3DMetrics BuildPreviewD3DMetrics(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var missedRefreshStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var missedRefreshEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var failureStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var failureEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var metrics = new PreviewD3DMetrics
        {
            MissedRefreshDelta = Math.Max(0, missedRefreshEnd - missedRefreshStart),
            StatsFailureDelta = Math.Max(0, failureEnd - failureStart),
            InputUploadCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DInputUploadCpuP99Ms"),
            RenderSubmitCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DRenderSubmitCpuP99Ms"),
            PresentCallP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DPresentCallP99Ms"),
            TotalFrameCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DTotalFrameCpuP99Ms")
        };

        foreach (var sample in samples)
        {
            ObservePreviewD3DCpuTiming(metrics, sample.Snapshot);
            metrics.MaxRecentSlowFramesObserved = Math.Max(
                metrics.MaxRecentSlowFramesObserved,
                CountArrayItems(sample.Snapshot, "PreviewD3DRecentSlowFrames"));
            if (TryGetLatestSlowFrame(sample.Snapshot, out var slowFrame))
            {
                ApplySlowFrame(metrics, slowFrame);
            }
        }

        metrics.MaxRecentSlowFramesObserved = Math.Max(
            metrics.MaxRecentSlowFramesObserved,
            CountArrayItems(lastSnapshot, "PreviewD3DRecentSlowFrames"));
        ObservePreviewD3DCpuTiming(metrics, lastSnapshot);
        if (TryGetLatestSlowFrame(lastSnapshot, out var lastSlowFrame))
        {
            ApplySlowFrame(metrics, lastSlowFrame);
        }

        return metrics;
    }

    private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)
    {
        metrics.InputUploadCpuMaxMsObserved = Math.Max(
            metrics.InputUploadCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DInputUploadCpuMaxMs"));
        metrics.RenderSubmitCpuMaxMsObserved = Math.Max(
            metrics.RenderSubmitCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DRenderSubmitCpuMaxMs"));
        metrics.PresentCallMaxMsObserved = Math.Max(
            metrics.PresentCallMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DPresentCallMaxMs"));
        metrics.TotalFrameCpuMaxMsObserved = Math.Max(
            metrics.TotalFrameCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DTotalFrameCpuMaxMs"));
    }

    private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)
    {
        metrics.LatestSlowFrameReason = GetSlowFrameReason(slowFrame);
        metrics.LatestSlowFrameOverBudgetMs = GetDouble(slowFrame, "WorstOverBudgetMs");
        metrics.LatestSlowFramePresentIntervalMs = GetDouble(slowFrame, "PresentIntervalMs");
        metrics.LatestSlowFrameTotalFrameCpuMs = GetDouble(slowFrame, "TotalFrameCpuMs");
        metrics.LatestSlowFramePresentCallMs = GetDouble(slowFrame, "PresentCallMs");
        metrics.LatestSlowFramePendingFrameCount = GetInt(slowFrame, "PendingFrameCount");
    }

    private static string GetSlowFrameReason(JsonElement slowFrame)
        => GetString(slowFrame, "SlowReason") ?? GetString(slowFrame, "Reason") ?? string.Empty;

    private static int CountArrayItems(JsonElement snapshot, string propertyName)
    {
        return snapshot.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;
    }

    private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)
    {
        if (snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var frames) &&
            frames.ValueKind == JsonValueKind.Array &&
            frames.GetArrayLength() > 0)
        {
            slowFrame = frames.EnumerateArray().Last().Clone();
            return true;
        }

        slowFrame = default;
        return false;
    }
}

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

internal sealed class FlashbackExportSessionMetrics
{
    public bool Observed { get; set; }
    public bool ActiveAtEnd { get; set; }
    public string StatusAtEnd { get; set; } = "NotStarted";
    public string MessageAtEnd { get; set; } = string.Empty;
    public string FailureKindAtEnd { get; set; } = string.Empty;
    public string OutputPathAtEnd { get; set; } = string.Empty;
    public long ForceRotateFallbacksAtEnd { get; set; }
    public long ForceRotateFallbacksDelta { get; set; }
    public int LastForceRotateFallbackSegmentsAtEnd { get; set; }
    public long LastExportIdAtEnd { get; set; }
    public string LastSuccessAtEnd { get; set; } = string.Empty;
    public string LastMessageAtEnd { get; set; } = string.Empty;
    public long MaxElapsedMsObserved { get; set; }
    public long MaxLastProgressAgeMsObserved { get; set; }
    public long MaxOutputBytesObserved { get; set; }
    public double MaxThroughputBytesPerSecObserved { get; set; }
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
    public double MaxP99FrameMsObserved { get; set; }
    public double MaxFrameMsObserved { get; set; }
    public double MaxSlowFramePercentObserved { get; set; }
    public long DroppedFramesDelta { get; set; }

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
    public long FrameCountAtEnd { get; init; }
    public long LateFramesAtEnd { get; init; }
    public long SlowFramesAtEnd { get; init; }
    public double SlowFramePercentAtEnd { get; init; }
    public long DroppedFramesAtEnd { get; init; }

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
        metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, "FlashbackExportForceRotateFallbacks") ?? 0;
        metrics.ForceRotateFallbacksDelta = GetCounterDelta(
            lastSnapshot,
            initialSnapshot,
            "FlashbackExportForceRotateFallbacks");
        metrics.LastForceRotateFallbackSegmentsAtEnd =
            GetInt(lastSnapshot, "FlashbackExportLastForceRotateFallbackSegments");
        return metrics;
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

    private readonly record struct FlashbackPlaybackSnapshotRelevance(
        long FrameCount,
        long SessionFrameCount,
        bool IsRelevant);

    private static void ObservePlaybackSnapshot(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot,
        long offsetMs,
        long baselineFrameCount,
        long baselineCommandsEnqueued,
        long baselineCommandsProcessed,
        bool baselinePlaybackActive)
    {
        var relevance = BuildPlaybackSnapshotRelevance(
            snapshot,
            baselineFrameCount,
            baselineCommandsEnqueued,
            baselineCommandsProcessed,
            baselinePlaybackActive);
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
        metrics.MaxSessionFrameCountObserved = Math.Max(
            metrics.MaxSessionFrameCountObserved,
            relevance.SessionFrameCount);
        if (!relevance.IsRelevant)
        {
            return;
        }

        metrics.Observed = true;
        metrics.EndSnapshot = snapshot;
        metrics.EndSessionFrameCount = relevance.SessionFrameCount;
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

        ObservePlaybackOnePercentLow(
            metrics,
            snapshot,
            offsetMs,
            relevance.FrameCount,
            relevance.SessionFrameCount,
            minimumPlaybackFramesForLowPercentile);
        ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);
        ObservePlaybackAudioMasterMetrics(metrics, snapshot);
    }

    private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(
        JsonElement snapshot,
        long baselineFrameCount,
        long baselineCommandsEnqueued,
        long baselineCommandsProcessed,
        bool baselinePlaybackActive)
    {
        var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var sessionFrameCount = frameCount >= baselineFrameCount
            ? frameCount - baselineFrameCount
            : frameCount;
        var commandsEnqueued = GetNullableLong(snapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var commandsProcessed = GetNullableLong(snapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        var isRelevant =
            IsPlaybackSnapshotActive(snapshot) ||
            GetInt(snapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            frameCount > baselineFrameCount ||
            commandsEnqueued > baselineCommandsEnqueued ||
            commandsProcessed > baselineCommandsProcessed ||
            baselinePlaybackActive;

        return new FlashbackPlaybackSnapshotRelevance(
            FrameCount: frameCount,
            SessionFrameCount: sessionFrameCount,
            IsRelevant: isRelevant);
    }

    private static bool IsPlaybackSnapshotActive(JsonElement snapshot)
    {
        var state = GetString(snapshot, "FlashbackPlaybackState") ?? string.Empty;
        return GetBool(snapshot, "FlashbackPlaybackThreadAlive") ||
               string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Paused", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Seeking", StringComparison.OrdinalIgnoreCase);
    }

    private static void ObservePlaybackOnePercentLow(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot,
        long offsetMs,
        long frameCount,
        long sessionFrameCount,
        long minimumPlaybackFramesForLowPercentile)
    {
        var onePercentLow = GetDouble(snapshot, "FlashbackPlaybackOnePercentLowFps");
        if (onePercentLow <= 0 || sessionFrameCount < minimumPlaybackFramesForLowPercentile)
        {
            return;
        }

        metrics.OnePercentLowSampleWindowObserved = true;
        if (onePercentLow >= metrics.MinOnePercentLowFpsObserved)
        {
            return;
        }

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

    private static void ObservePlaybackFrameAndDecodeMetrics(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot)
    {
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
    }

    private static void ObservePlaybackAudioMasterMetrics(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot)
    {
        metrics.MaxAudioMasterDelayDoublesObserved = Math.Max(
            metrics.MaxAudioMasterDelayDoublesObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"));
        metrics.MaxAudioMasterDelayShrinksObserved = Math.Max(
            metrics.MaxAudioMasterDelayShrinksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"));
        metrics.MaxAudioMasterFallbacksObserved = Math.Max(
            metrics.MaxAudioMasterFallbacksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks"));
        metrics.MaxAudioBufferedDurationMsObserved = Math.Max(
            metrics.MaxAudioBufferedDurationMsObserved,
            GetDouble(snapshot, "WasapiPlaybackBufferedDurationMs"));
        metrics.MaxAudioQueueDurationMsObserved = Math.Max(
            metrics.MaxAudioQueueDurationMsObserved,
            GetDouble(snapshot, "WasapiPlaybackQueueDurationMs"));
        metrics.MaxAbsAvDriftMsObserved = Math.Max(
            metrics.MaxAbsAvDriftMsObserved,
            Math.Abs(GetDouble(snapshot, "FlashbackAvDriftMs")));
    }

    internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(
        FlashbackPlaybackSessionMetrics metrics)
    {
        var observed = metrics.Observed;
        var endSnapshot = metrics.EndSnapshot;
        var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);
        var cadence = BuildFlashbackPlaybackResultCadenceMetrics(observed, endSnapshot);
        var decode = BuildFlashbackPlaybackResultDecodeMetrics(observed, endSnapshot);
        var audioMaster = BuildFlashbackPlaybackResultAudioMasterMetrics(observed, endSnapshot);
        var stages = BuildFlashbackPlaybackResultStageMetrics(observed, endSnapshot, metrics);

        return new FlashbackPlaybackResultMetrics
        {
            EndSnapshot = endSnapshot,
            PendingCommandsAtEnd = commands.PendingCommandsAtEnd,
            MaxPendingCommandsObserved = commands.MaxPendingCommandsObserved,
            MaxCommandQueueLatencyMsObserved = commands.MaxCommandQueueLatencyMsObserved,
            MaxCommandQueueLatencyCommandObserved = commands.MaxCommandQueueLatencyCommandObserved,
            CommandsDroppedAtEnd = commands.CommandsDroppedAtEnd,
            CommandsSkippedNotReadyAtEnd = commands.CommandsSkippedNotReadyAtEnd,
            ScrubUpdatesCoalescedAtEnd = commands.ScrubUpdatesCoalescedAtEnd,
            SeekCommandsCoalescedAtEnd = commands.SeekCommandsCoalescedAtEnd,
            LastCommandFailureAtEnd = commands.LastCommandFailureAtEnd,
            LastCommandFailureUtcUnixMsAtEnd = commands.LastCommandFailureUtcUnixMsAtEnd,
            ObservedFpsAtEnd = cadence.ObservedFpsAtEnd,
            AvgFrameMsAtEnd = cadence.AvgFrameMsAtEnd,
            P99FrameMsAtEnd = cadence.P99FrameMsAtEnd,
            MaxFrameMsAtEnd = cadence.MaxFrameMsAtEnd,
            OnePercentLowFpsAtEnd = cadence.OnePercentLowFpsAtEnd,
            DecodeAvgMsAtEnd = decode.DecodeAvgMsAtEnd,
            DecodeP95MsAtEnd = decode.DecodeP95MsAtEnd,
            DecodeP99MsAtEnd = decode.DecodeP99MsAtEnd,
            DecodeMaxMsAtEnd = decode.DecodeMaxMsAtEnd,
            MaxDecodePhaseAtEnd = decode.MaxDecodePhaseAtEnd,
            MaxDecodeReceiveMsAtEnd = decode.MaxDecodeReceiveMsAtEnd,
            MaxDecodeFeedMsAtEnd = decode.MaxDecodeFeedMsAtEnd,
            MaxDecodeReadMsAtEnd = decode.MaxDecodeReadMsAtEnd,
            MaxDecodeSendMsAtEnd = decode.MaxDecodeSendMsAtEnd,
            MaxDecodeAudioMsAtEnd = decode.MaxDecodeAudioMsAtEnd,
            MaxDecodeConvertMsAtEnd = decode.MaxDecodeConvertMsAtEnd,
            MaxDecodeUtcUnixMsAtEnd = decode.MaxDecodeUtcUnixMsAtEnd,
            MaxDecodePositionMsAtEnd = decode.MaxDecodePositionMsAtEnd,
            FrameCountAtEnd = cadence.FrameCountAtEnd,
            LateFramesAtEnd = cadence.LateFramesAtEnd,
            SlowFramesAtEnd = cadence.SlowFramesAtEnd,
            SlowFramePercentAtEnd = cadence.SlowFramePercentAtEnd,
            DroppedFramesAtEnd = cadence.DroppedFramesAtEnd,
            AudioMasterDelayDoublesAtEnd = audioMaster.AudioMasterDelayDoublesAtEnd,
            AudioMasterDelayShrinksAtEnd = audioMaster.AudioMasterDelayShrinksAtEnd,
            AudioMasterFallbacksAtEnd = audioMaster.AudioMasterFallbacksAtEnd,
            AudioMasterUnavailableFallbacksAtEnd = audioMaster.AudioMasterUnavailableFallbacksAtEnd,
            AudioMasterStaleFallbacksAtEnd = audioMaster.AudioMasterStaleFallbacksAtEnd,
            AudioMasterDriftOutlierFallbacksAtEnd = audioMaster.AudioMasterDriftOutlierFallbacksAtEnd,
            AudioMasterLastFallbackReasonAtEnd = audioMaster.AudioMasterLastFallbackReasonAtEnd,
            AudioMasterLastFallbackClockAgeMsAtEnd = audioMaster.AudioMasterLastFallbackClockAgeMsAtEnd,
            SubmitFailuresAtEnd = stages.SubmitFailuresAtEnd,
            SegmentSwitchesAtEnd = stages.SegmentSwitchesAtEnd,
            Fmp4ReopensAtEnd = stages.Fmp4ReopensAtEnd,
            WriteHeadWaitsAtEnd = stages.WriteHeadWaitsAtEnd,
            NearLiveSnapsAtEnd = stages.NearLiveSnapsAtEnd,
            DecodeErrorSnapsAtEnd = stages.DecodeErrorSnapsAtEnd,
            LastWriteHeadWaitGapMsAtEnd = stages.LastWriteHeadWaitGapMsAtEnd,
            SeekForwardDecodeCapHitsAtEnd = stages.SeekForwardDecodeCapHitsAtEnd,
            SeekForwardDecodeCapHitsDelta = stages.SeekForwardDecodeCapHitsDelta,
            LastSeekHitForwardDecodeCapAtEnd = stages.LastSeekHitForwardDecodeCapAtEnd
        };
    }

    private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetNullableLong(snapshot, propertyName) ?? 0 : 0;

    private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetDouble(snapshot, propertyName) : 0;

    private readonly record struct FlashbackPlaybackResultCommandMetrics(
        int PendingCommandsAtEnd,
        int MaxPendingCommandsObserved,
        int MaxCommandQueueLatencyMsObserved,
        string MaxCommandQueueLatencyCommandObserved,
        long CommandsDroppedAtEnd,
        long CommandsSkippedNotReadyAtEnd,
        long ScrubUpdatesCoalescedAtEnd,
        long SeekCommandsCoalescedAtEnd,
        string LastCommandFailureAtEnd,
        long LastCommandFailureUtcUnixMsAtEnd);

    private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(
        bool observed,
        JsonElement endSnapshot,
        FlashbackPlaybackSessionMetrics metrics) =>
        new(
            PendingCommandsAtEnd: observed ? GetInt(endSnapshot, "FlashbackPlaybackPendingCommands") : 0,
            MaxPendingCommandsObserved: metrics.MaxPendingCommandsObserved,
            MaxCommandQueueLatencyMsObserved: metrics.MaxCommandQueueLatencyMsObserved,
            MaxCommandQueueLatencyCommandObserved: metrics.MaxCommandQueueLatencyCommandObserved,
            CommandsDroppedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsDropped"),
            CommandsSkippedNotReadyAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackCommandsSkippedNotReady"),
            ScrubUpdatesCoalescedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced"),
            SeekCommandsCoalescedAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekCommandsCoalesced"),
            LastCommandFailureAtEnd: observed ? GetString(endSnapshot, "FlashbackPlaybackLastCommandFailure") ?? string.Empty : string.Empty,
            LastCommandFailureUtcUnixMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs"));

    private readonly record struct FlashbackPlaybackResultCadenceMetrics(
        double ObservedFpsAtEnd,
        double AvgFrameMsAtEnd,
        double P99FrameMsAtEnd,
        double MaxFrameMsAtEnd,
        double OnePercentLowFpsAtEnd,
        long FrameCountAtEnd,
        long LateFramesAtEnd,
        long SlowFramesAtEnd,
        double SlowFramePercentAtEnd,
        long DroppedFramesAtEnd);

    private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(
        bool observed,
        JsonElement endSnapshot) =>
        new(
            ObservedFpsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackObservedFps"),
            AvgFrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackAvgFrameMs"),
            P99FrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackP99FrameMs"),
            MaxFrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxFrameMs"),
            OnePercentLowFpsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackOnePercentLowFps"),
            FrameCountAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFrameCount"),
            LateFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLateFrames"),
            SlowFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSlowFrames"),
            SlowFramePercentAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackSlowFramePercent"),
            DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDroppedFrames"));

    private readonly record struct FlashbackPlaybackResultDecodeMetrics(
        double DecodeAvgMsAtEnd,
        double DecodeP95MsAtEnd,
        double DecodeP99MsAtEnd,
        double DecodeMaxMsAtEnd,
        string MaxDecodePhaseAtEnd,
        double MaxDecodeReceiveMsAtEnd,
        double MaxDecodeFeedMsAtEnd,
        double MaxDecodeReadMsAtEnd,
        double MaxDecodeSendMsAtEnd,
        double MaxDecodeAudioMsAtEnd,
        double MaxDecodeConvertMsAtEnd,
        long MaxDecodeUtcUnixMsAtEnd,
        long MaxDecodePositionMsAtEnd);

    private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(
        bool observed,
        JsonElement endSnapshot) =>
        new(
            DecodeAvgMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeAvgMs"),
            DecodeP95MsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeP95Ms"),
            DecodeP99MsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeP99Ms"),
            DecodeMaxMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackDecodeMaxMs"),
            MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, "FlashbackPlaybackMaxDecodePhase") ?? string.Empty : string.Empty,
            MaxDecodeReceiveMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeReceiveMs"),
            MaxDecodeFeedMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeFeedMs"),
            MaxDecodeReadMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeReadMs"),
            MaxDecodeSendMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeSendMs"),
            MaxDecodeAudioMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeAudioMs"),
            MaxDecodeConvertMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxDecodeConvertMs"),
            MaxDecodeUtcUnixMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackMaxDecodeUtcUnixMs"),
            MaxDecodePositionMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackMaxDecodePositionMs"));

    private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(
        long AudioMasterDelayDoublesAtEnd,
        long AudioMasterDelayShrinksAtEnd,
        long AudioMasterFallbacksAtEnd,
        long AudioMasterUnavailableFallbacksAtEnd,
        long AudioMasterStaleFallbacksAtEnd,
        long AudioMasterDriftOutlierFallbacksAtEnd,
        string AudioMasterLastFallbackReasonAtEnd,
        double AudioMasterLastFallbackClockAgeMsAtEnd);

    private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(
        bool observed,
        JsonElement endSnapshot) =>
        new(
            AudioMasterDelayDoublesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"),
            AudioMasterDelayShrinksAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"),
            AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterFallbacks"),
            AudioMasterUnavailableFallbacksAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks"),
            AudioMasterStaleFallbacksAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks"),
            AudioMasterDriftOutlierFallbacksAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks"),
            AudioMasterLastFallbackReasonAtEnd: observed ? GetString(endSnapshot, "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty : string.Empty,
            AudioMasterLastFallbackClockAgeMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs"));

    private readonly record struct FlashbackPlaybackResultStageMetrics(
        long SubmitFailuresAtEnd,
        long SegmentSwitchesAtEnd,
        long Fmp4ReopensAtEnd,
        long WriteHeadWaitsAtEnd,
        long NearLiveSnapsAtEnd,
        long DecodeErrorSnapsAtEnd,
        long LastWriteHeadWaitGapMsAtEnd,
        long SeekForwardDecodeCapHitsAtEnd,
        long SeekForwardDecodeCapHitsDelta,
        bool LastSeekHitForwardDecodeCapAtEnd);

    private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(
        bool observed,
        JsonElement endSnapshot,
        FlashbackPlaybackSessionMetrics metrics) =>
        new(
            SubmitFailuresAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSubmitFailures"),
            SegmentSwitchesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSegmentSwitches"),
            Fmp4ReopensAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFmp4Reopens"),
            WriteHeadWaitsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackWriteHeadWaits"),
            NearLiveSnapsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackNearLiveSnaps"),
            DecodeErrorSnapsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDecodeErrorSnaps"),
            LastWriteHeadWaitGapMsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs"),
            SeekForwardDecodeCapHitsAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits"),
            SeekForwardDecodeCapHitsDelta: observed
                ? GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")
                : 0,
            LastSeekHitForwardDecodeCapAtEnd: observed &&
                                               GetBool(endSnapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap"));
}
