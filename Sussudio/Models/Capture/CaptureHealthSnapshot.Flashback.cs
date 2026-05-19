using System;

namespace Sussudio.Models;

public sealed partial class CaptureHealthSnapshot
{
    // ── Flashback detail (beyond base counts) ──────────────────────────
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public bool FlashbackBackendSettingsStale { get; init; }
    public string FlashbackBackendSettingsStaleReason { get; init; } = string.Empty;
    public string FlashbackBackendActiveFormat { get; init; } = string.Empty;
    public string FlashbackBackendRequestedFormat { get; init; } = string.Empty;
    public string FlashbackBackendActivePreset { get; init; } = string.Empty;
    public string FlashbackBackendRequestedPreset { get; init; } = string.Empty;
    public string? EncoderCodecName { get; init; }
    public uint EncoderTargetBitRate { get; init; }
    public int EncoderWidth { get; init; }
    public int EncoderHeight { get; init; }
    public double EncoderFrameRate { get; init; }
    public int? EncoderFrameRateNumerator { get; init; }
    public int? EncoderFrameRateDenominator { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public int FlashbackAudioQueueCapacity { get; init; }
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public long FlashbackPlaybackPositionMs { get; init; }
    public string FlashbackDecoderHwAccel { get; init; } = "N/A";
    public long FlashbackPlaybackFrameCount { get; init; }
    public long FlashbackPlaybackLateFrames { get; init; }
    public long FlashbackPlaybackDroppedFrames { get; init; }
    public long FlashbackPlaybackAudioMasterDelayDoubles { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinks { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacks { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReason { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackDriftMs { get; init; }
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMs { get; init; }
    public long FlashbackPlaybackSegmentSwitches { get; init; }
    public long FlashbackPlaybackFmp4Reopens { get; init; }
    public long FlashbackPlaybackWriteHeadWaits { get; init; }
    public long FlashbackPlaybackNearLiveSnaps { get; init; }
    public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
    public long FlashbackPlaybackSubmitFailures { get; init; }
    public long FlashbackPlaybackLastDropUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastDropReason { get; init; } = string.Empty;
    public long FlashbackPlaybackLastSubmitFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastSubmitFailure { get; init; } = string.Empty;
    public long FlashbackPlaybackLastSegmentSwitchUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastFmp4ReopenUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
    public double FlashbackPlaybackTargetFps { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackAvgFrameMs { get; init; }
    public int FlashbackPlaybackCadenceSampleCount { get; init; }
    public double FlashbackPlaybackP95FrameMs { get; init; }
    public double FlashbackPlaybackP99FrameMs { get; init; }
    public double FlashbackPlaybackMaxFrameMs { get; init; }
    public long FlashbackPlaybackSlowFrames { get; init; }
    public double FlashbackPlaybackSlowFramePercent { get; init; }
    public double FlashbackPlaybackOnePercentLowFps { get; init; }
    public double FlashbackPlaybackFivePercentLowFps { get; init; }
    public double FlashbackPlaybackSampleDurationMs { get; init; }
    public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();
    public long FlashbackPlaybackPtsCadenceMismatchCount { get; init; }
    public long FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs { get; init; }
    public double FlashbackPlaybackLastPtsCadenceDeltaMs { get; init; }
    public double FlashbackPlaybackLastPtsCadenceExpectedMs { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHits { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCap { get; init; }
    public int FlashbackPlaybackDecodeSampleCount { get; init; }
    public double FlashbackPlaybackDecodeAvgMs { get; init; }
    public double FlashbackPlaybackDecodeP95Ms { get; init; }
    public double FlashbackPlaybackDecodeP99Ms { get; init; }
    public double FlashbackPlaybackDecodeMaxMs { get; init; }
    public string FlashbackPlaybackMaxDecodePhase { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMs { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMs { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMs { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMs { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMs { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMs { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMs { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMs { get; init; }
    public double FlashbackAvDriftMs { get; init; }
    public bool FlashbackPlaybackThreadAlive { get; init; }
    public long FlashbackPlaybackCommandsEnqueued { get; init; }
    public long FlashbackPlaybackCommandsProcessed { get; init; }
    public long FlashbackPlaybackCommandsDropped { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReady { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalesced { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalesced { get; init; }
    public int FlashbackPlaybackCommandQueueCapacity { get; init; }
    public int FlashbackPlaybackPendingCommands { get; init; }
    public int FlashbackPlaybackMaxPendingCommands { get; init; }
    public long FlashbackPlaybackLastCommandQueueLatencyMs { get; init; }
    public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
    public string FlashbackPlaybackMaxCommandQueueLatencyCommand { get; init; } = "None";
    public string FlashbackPlaybackLastCommandQueued { get; init; } = "None";
    public string FlashbackPlaybackLastCommandProcessed { get; init; } = "None";
    public long FlashbackPlaybackLastCommandQueuedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandProcessedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;

    // ── Export ──────────────────────────────────────────────────────────
    public bool FlashbackExportActive { get; init; }
    public long FlashbackExportId { get; init; }
    public string FlashbackExportStatus { get; init; } = "NotStarted";
    public string FlashbackExportOutputPath { get; init; } = string.Empty;
    public long FlashbackExportStartedUtcUnixMs { get; init; }
    public long FlashbackExportLastProgressUtcUnixMs { get; init; }
    public long FlashbackExportCompletedUtcUnixMs { get; init; }
    public long FlashbackExportElapsedMs { get; init; }
    public long FlashbackExportLastProgressAgeMs { get; init; }
    public long FlashbackExportOutputBytes { get; init; }
    public double FlashbackExportThroughputBytesPerSec { get; init; }
    public int FlashbackExportSegmentsProcessed { get; init; }
    public int FlashbackExportTotalSegments { get; init; }
    public double FlashbackExportPercent { get; init; }
    public long FlashbackExportInPointMs { get; init; }
    public long FlashbackExportOutPointMs { get; init; }
    public string FlashbackExportMessage { get; init; } = string.Empty;
    public string FlashbackExportFailureKind { get; init; } = string.Empty;
    public long FlashbackExportForceRotateFallbacks { get; init; }
    public long FlashbackExportLastForceRotateFallbackUtcUnixMs { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegments { get; init; }
    public long FlashbackExportLastForceRotateFallbackInPointMs { get; init; }
    public long FlashbackExportLastForceRotateFallbackOutPointMs { get; init; }
    /// <summary>
    /// The actual codec/container the next flashback export will produce. This
    /// should match the user-requested <c>SelectedRecordingFormat</c>; mismatches
    /// are reserved for future explicit, user-visible substitutions.
    /// </summary>
    public string? FlashbackExportVerificationFormat { get; init; }
    /// <summary>
    /// Legacy compatibility field for recording settings substitutions. It should
    /// remain null while Flashback honors the selected codec and preset directly.
    /// </summary>
    public string? FlashbackCodecDowngradeReason { get; init; }
    public long LastExportId { get; init; }
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }
}
