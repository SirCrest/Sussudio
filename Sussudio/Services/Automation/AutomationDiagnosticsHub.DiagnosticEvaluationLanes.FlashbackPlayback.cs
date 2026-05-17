using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildFlashbackPlaybackCommandLane(
        CaptureHealthSnapshot health,
        long playbackCommandQueueAgeMs,
        long playbackCommandFailureAgeMs,
        string playbackCommandFailure)
    {
        return $"playback commands pending={health.FlashbackPlaybackPendingCommands}/{health.FlashbackPlaybackCommandQueueCapacity} maxPending={health.FlashbackPlaybackMaxPendingCommands} lastLatency={health.FlashbackPlaybackLastCommandQueueLatencyMs}ms maxLatency={health.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={health.FlashbackPlaybackMaxCommandQueueLatencyCommand} lastQueued={health.FlashbackPlaybackLastCommandQueued} lastProcessed={health.FlashbackPlaybackLastCommandProcessed} queuedAge={playbackCommandQueueAgeMs}ms lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} threadAlive={health.FlashbackPlaybackThreadAlive}";
    }

    private static string BuildFlashbackPlaybackPerformanceLane(
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        double playbackTargetFps)
    {
        return
            $"playback perf state={health.FlashbackPlaybackState} fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##} target={health.FlashbackPlaybackTargetFps:0.##} encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##} " +
            $"1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps p99={health.FlashbackPlaybackP99FrameMs:0.##}ms max={health.FlashbackPlaybackMaxFrameMs:0.##}ms slow={health.FlashbackPlaybackSlowFramePercent:0.##}% ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={health.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={health.FlashbackPlaybackLastSeekHitForwardDecodeCap} decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={health.FlashbackPlaybackDecodeMaxMs:0.##}ms decodePhase={health.FlashbackPlaybackMaxDecodePhase} decodeReceive={health.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms decodeFeed={health.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={health.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={health.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={health.FlashbackPlaybackMaxDecodePositionMs}ms samples={health.FlashbackPlaybackCadenceSampleCount} frames={health.FlashbackPlaybackFrameCount} late={health.FlashbackPlaybackLateFrames} dropped={health.FlashbackPlaybackDroppedFrames} audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={health.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={health.FlashbackPlaybackAudioMasterFallbacks} submitFailures={health.FlashbackPlaybackSubmitFailures} switches={health.FlashbackPlaybackSegmentSwitches} fmp4Reopens={health.FlashbackPlaybackFmp4Reopens} writeHeadWaits={health.FlashbackPlaybackWriteHeadWaits} nearLiveSnaps={health.FlashbackPlaybackNearLiveSnaps} decodeErrorSnaps={health.FlashbackPlaybackDecodeErrorSnaps}";
    }
}
