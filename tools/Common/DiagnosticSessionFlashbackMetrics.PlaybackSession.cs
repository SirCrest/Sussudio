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
}
