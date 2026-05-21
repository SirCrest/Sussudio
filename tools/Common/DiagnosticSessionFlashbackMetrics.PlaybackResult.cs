using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

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

internal static partial class DiagnosticSessionFlashbackMetrics
{
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
}
