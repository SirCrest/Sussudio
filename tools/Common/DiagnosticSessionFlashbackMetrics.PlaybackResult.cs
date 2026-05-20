using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

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
