using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Playback: {Get(snapshot, "FlashbackPlaybackState")} | Pos: {Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={Get(snapshot, "FlashbackPlaybackPendingCommands")}/{Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms maxLatencyCommand={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand")} enq={Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} coalescedSeek={Get(snapshot, "FlashbackPlaybackSeekCommandsCoalesced")} threadAlive={Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
    }

    private static void AppendFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)
    {
        var playbackFps = double.TryParse(Get(snapshot, "FlashbackPlaybackObservedFps", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var fps)
            ? fps
            : 0;
        var playbackAvgMs = double.TryParse(Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var avgMs)
            ? avgMs
            : 0;
        var avDrift = double.TryParse(Get(snapshot, "FlashbackAvDriftMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var drift)
            ? drift
            : 0;
        builder.AppendLine($"Playback Frame Time: avg={FormatNumber(playbackAvgMs, "F2")}ms P95={Get(snapshot, "FlashbackPlaybackP95FrameMs")}ms P99={Get(snapshot, "FlashbackPlaybackP99FrameMs")}ms max={Get(snapshot, "FlashbackPlaybackMaxFrameMs")}ms | Average Rate: {FormatNumber(playbackFps, "F1")} fps | Target: {Get(snapshot, "FlashbackPlaybackTargetFps")} fps | 5% Low: {Get(snapshot, "FlashbackPlaybackFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "FlashbackPlaybackOnePercentLowFps")} fps | Samples: {Get(snapshot, "FlashbackPlaybackCadenceSampleCount")} over {Get(snapshot, "FlashbackPlaybackSampleDurationMs")}ms");
        builder.AppendLine($"Playback Decode: avg={Get(snapshot, "FlashbackPlaybackDecodeAvgMs")}ms P95={Get(snapshot, "FlashbackPlaybackDecodeP95Ms")}ms P99={Get(snapshot, "FlashbackPlaybackDecodeP99Ms")}ms max={Get(snapshot, "FlashbackPlaybackDecodeMaxMs")}ms phase={Get(snapshot, "FlashbackPlaybackMaxDecodePhase", "")} receive={Get(snapshot, "FlashbackPlaybackMaxDecodeReceiveMs")}ms feed={Get(snapshot, "FlashbackPlaybackMaxDecodeFeedMs")}ms read={Get(snapshot, "FlashbackPlaybackMaxDecodeReadMs")}ms send={Get(snapshot, "FlashbackPlaybackMaxDecodeSendMs")}ms audio={Get(snapshot, "FlashbackPlaybackMaxDecodeAudioMs")}ms convert={Get(snapshot, "FlashbackPlaybackMaxDecodeConvertMs")}ms maxPos={Get(snapshot, "FlashbackPlaybackMaxDecodePositionMs")}ms samples={Get(snapshot, "FlashbackPlaybackDecodeSampleCount")} seekCapHits={Get(snapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")} lastSeekCap={Get(snapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap")}");
        builder.AppendLine($"Playback Frames: total={Get(snapshot, "FlashbackPlaybackFrameCount")} late={Get(snapshot, "FlashbackPlaybackLateFrames")} slow={Get(snapshot, "FlashbackPlaybackSlowFrames")} ({Get(snapshot, "FlashbackPlaybackSlowFramePercent")}%) dropped={Get(snapshot, "FlashbackPlaybackDroppedFrames")} audioMasterDouble={Get(snapshot, "FlashbackPlaybackAudioMasterDelayDoubles")} audioMasterShrink={Get(snapshot, "FlashbackPlaybackAudioMasterDelayShrinks")} audioMasterFallback={Get(snapshot, "FlashbackPlaybackAudioMasterFallbacks")} unavailable={Get(snapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks")} stale={Get(snapshot, "FlashbackPlaybackAudioMasterStaleFallbacks")} driftOutlier={Get(snapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks")} lastAudioFallback={Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackReason", "")}/{Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs")}ms lastDrop={Get(snapshot, "FlashbackPlaybackLastDropReason", "")} lastDropUtc={Get(snapshot, "FlashbackPlaybackLastDropUtcUnixMs")} submitFailures={Get(snapshot, "FlashbackPlaybackSubmitFailures")} lastSubmitFailure={Get(snapshot, "FlashbackPlaybackLastSubmitFailure", "")} lastSubmitFailureUtc={Get(snapshot, "FlashbackPlaybackLastSubmitFailureUtcUnixMs")}");
        builder.AppendLine($"Playback Stages: switches={Get(snapshot, "FlashbackPlaybackSegmentSwitches")} fmp4Reopens={Get(snapshot, "FlashbackPlaybackFmp4Reopens")} writeHeadWaits={Get(snapshot, "FlashbackPlaybackWriteHeadWaits")} nearLiveSnaps={Get(snapshot, "FlashbackPlaybackNearLiveSnaps")} decodeErrorSnaps={Get(snapshot, "FlashbackPlaybackDecodeErrorSnaps")} lastWriteHeadGap={Get(snapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs")}ms");
        builder.AppendLine($"A/V Drift: {FormatNumber(avDrift, "+0.0;-0.0;0.0")}ms (+ = audio ahead) | Audio buffered={Get(snapshot, "WasapiPlaybackBufferedDurationMs")}ms queue={Get(snapshot, "WasapiPlaybackQueueDurationMs")}ms active={Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")}ms endpoint={Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")}ms streamLatency={Get(snapshot, "WasapiPlaybackStreamLatencyMs")}ms | File: {Get(snapshot, "FlashbackFilePath")}");
    }
}
