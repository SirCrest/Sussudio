using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
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
}
