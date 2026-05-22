using System.Text;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendFlashbackPlaybackCommands(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Commands: " +
            $"pendingEnd={result.FlashbackPlaybackPendingCommandsAtEnd} " +
            $"maxPending={result.FlashbackPlaybackMaxPendingCommandsObserved} " +
            $"maxLatencyMs={result.FlashbackPlaybackMaxCommandQueueLatencyMsObserved} " +
            $"maxLatencyCommand={FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)} " +
            $"droppedEnd={result.FlashbackPlaybackCommandsDroppedAtEnd} " +
            $"skippedEnd={result.FlashbackPlaybackCommandsSkippedNotReadyAtEnd} " +
            $"coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd} " +
            $"coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd} " +
            $"failureEnd={FormatOptional(result.FlashbackPlaybackLastCommandFailureAtEnd)} " +
            $"failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
    }

    private static void AppendFlashbackPlaybackPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Perf: " +
            BuildFlashbackPlaybackCadencePerformanceText(result) + " " +
            BuildFlashbackPlaybackAudioMasterPerformanceText(result) + " " +
            BuildFlashbackPlaybackSubmitPerformanceText(result));
    }

    private static string BuildFlashbackPlaybackSubmitPerformanceText(DiagnosticSessionResult result)
        => $"submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd} " +
           $"submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}";

    private static string BuildFlashbackPlaybackCadencePerformanceText(DiagnosticSessionResult result)
        =>
            $"fpsEnd={result.FlashbackPlaybackObservedFpsAtEnd:0.##} " +
            $"fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##} " +
            $"avgFrameMsEnd={result.FlashbackPlaybackAvgFrameMsAtEnd:0.##} " +
            $"p99FrameMsEnd={result.FlashbackPlaybackP99FrameMsAtEnd:0.##} " +
            $"maxFrameMsEnd={result.FlashbackPlaybackMaxFrameMsAtEnd:0.##} " +
            BuildFlashbackPlaybackOnePercentLowPerformanceText(result) + " " +
            $"p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##} " +
            $"maxFrameMsObserved={result.FlashbackPlaybackMaxFrameMsObserved:0.##} " +
            $"framesEnd={result.FlashbackPlaybackFrameCountAtEnd} " +
            $"lateEnd={result.FlashbackPlaybackLateFramesAtEnd} " +
            $"slowEnd={result.FlashbackPlaybackSlowFramesAtEnd} " +
            $"slowPctEnd={result.FlashbackPlaybackSlowFramePercentAtEnd:0.##} " +
            $"slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##} " +
            $"droppedFramesEnd={result.FlashbackPlaybackDroppedFramesAtEnd} " +
            $"droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}";

    private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(DiagnosticSessionResult result)
        =>
            $"onePercentLowFpsEnd={result.FlashbackPlaybackOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##} " +
            $"onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved} " +
            $"onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames} " +
            $"onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved} " +
            $"onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs} " +
            $"onePercentLowMinFrames={result.FlashbackPlaybackMinOnePercentLowFrameCount} " +
            $"onePercentLowMinP99FrameMs={result.FlashbackPlaybackMinOnePercentLowP99FrameMs:0.##} " +
            $"onePercentLowMinMaxFrameMs={result.FlashbackPlaybackMinOnePercentLowMaxFrameMs:0.##} " +
            $"onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##} " +
            $"onePercentLowMinDecodeMaxMs={result.FlashbackPlaybackMinOnePercentLowDecodeMaxMs:0.##} " +
            $"onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##} " +
            $"onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}";

    private static string BuildFlashbackPlaybackAudioMasterPerformanceText(DiagnosticSessionResult result)
        =>
            $"audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd} " +
            $"audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved} " +
            $"audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd} " +
            $"audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved} " +
            $"audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd} " +
            $"audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved} " +
            $"audioMasterUnavailableEnd={result.FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd} " +
            $"audioMasterStaleEnd={result.FlashbackPlaybackAudioMasterStaleFallbacksAtEnd} " +
            $"audioMasterDriftOutlierEnd={result.FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd} " +
            $"audioMasterLastFallback={FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)} " +
            $"audioMasterLastFallbackAgeMs={result.FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd:0.##} " +
            $"audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##} " +
            $"audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##} " +
            $"absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}";

    private static void AppendFlashbackPlaybackDecode(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Decode: " +
            $"avgMsEnd={result.FlashbackPlaybackDecodeAvgMsAtEnd:0.##} " +
            $"p95MsEnd={result.FlashbackPlaybackDecodeP95MsAtEnd:0.##} " +
            $"p99MsEnd={result.FlashbackPlaybackDecodeP99MsAtEnd:0.##} " +
            $"maxMsEnd={result.FlashbackPlaybackDecodeMaxMsAtEnd:0.##} " +
            $"phaseEnd={result.FlashbackPlaybackMaxDecodePhaseAtEnd} " +
            $"receiveMsEnd={result.FlashbackPlaybackMaxDecodeReceiveMsAtEnd:0.##} " +
            $"feedMsEnd={result.FlashbackPlaybackMaxDecodeFeedMsAtEnd:0.##} " +
            $"readMsEnd={result.FlashbackPlaybackMaxDecodeReadMsAtEnd:0.##} " +
            $"sendMsEnd={result.FlashbackPlaybackMaxDecodeSendMsAtEnd:0.##} " +
            $"audioMsEnd={result.FlashbackPlaybackMaxDecodeAudioMsAtEnd:0.##} " +
            $"convertMsEnd={result.FlashbackPlaybackMaxDecodeConvertMsAtEnd:0.##} " +
            $"maxPosEnd={result.FlashbackPlaybackMaxDecodePositionMsAtEnd}ms " +
            $"p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##} " +
            $"maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##} " +
            $"phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved} " +
            $"receiveMsObserved={result.FlashbackPlaybackMaxDecodeReceiveMsObserved:0.##} " +
            $"feedMsObserved={result.FlashbackPlaybackMaxDecodeFeedMsObserved:0.##} " +
            $"readMsObserved={result.FlashbackPlaybackMaxDecodeReadMsObserved:0.##} " +
            $"sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##} " +
            $"audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##} " +
            $"convertMsObserved={result.FlashbackPlaybackMaxDecodeConvertMsObserved:0.##} " +
            $"maxPosObserved={result.FlashbackPlaybackMaxDecodePositionMsObserved}ms");
    }

    private static void AppendFlashbackPlaybackStages(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Stages: " +
            $"switchesEnd={result.FlashbackPlaybackSegmentSwitchesAtEnd} " +
            $"fmp4ReopensEnd={result.FlashbackPlaybackFmp4ReopensAtEnd} " +
            $"writeHeadWaitsEnd={result.FlashbackPlaybackWriteHeadWaitsAtEnd} " +
            $"nearLiveSnapsEnd={result.FlashbackPlaybackNearLiveSnapsAtEnd} " +
            $"decodeErrorSnapsEnd={result.FlashbackPlaybackDecodeErrorSnapsAtEnd} " +
            $"seekCapHitsEnd={result.FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd} " +
            $"seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta} " +
            $"lastSeekCapEnd={result.FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd} " +
            $"lastWriteHeadGapMsEnd={result.FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd}");
    }
}
