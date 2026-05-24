using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

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

internal static partial class DiagnosticSessionFlashbackMetrics
{
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
}
