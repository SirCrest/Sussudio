using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
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

    private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetNullableLong(snapshot, propertyName) ?? 0 : 0;

    private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)
        => observed ? GetDouble(snapshot, propertyName) : 0;
}
