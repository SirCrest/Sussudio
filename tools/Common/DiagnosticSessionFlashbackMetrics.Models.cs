using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal sealed class FlashbackRecordingSessionMetrics
{
    public int SampleCount { get; init; }
    public bool BackendObserved { get; init; }
    public bool FileGrowthObserved { get; init; }
    public long VideoFramesSubmittedDelta { get; init; }
    public long VideoEncoderPacketsWrittenDelta { get; init; }
    public long IntegritySequenceGapsAtEnd { get; init; }
    public long IntegrityQueueDroppedFramesAtEnd { get; init; }
    public long IntegritySequenceGapsDelta { get; init; }
    public long IntegrityQueueDroppedFramesDelta { get; init; }
}

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
    public double MaxP99FrameMsObserved { get; set; }
    public double MaxFrameMsObserved { get; set; }
    public double MaxSlowFramePercentObserved { get; set; }
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
    public long DroppedFramesDelta { get; set; }
    public long SubmitFailuresDelta { get; set; }
}

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

internal sealed class FlashbackExportSessionMetrics
{
    public bool Observed { get; set; }
    public bool ActiveAtEnd { get; set; }
    public string StatusAtEnd { get; set; } = "NotStarted";
    public string MessageAtEnd { get; set; } = string.Empty;
    public string FailureKindAtEnd { get; set; } = string.Empty;
    public string OutputPathAtEnd { get; set; } = string.Empty;
    public long LastExportIdAtEnd { get; set; }
    public string LastSuccessAtEnd { get; set; } = string.Empty;
    public string LastMessageAtEnd { get; set; } = string.Empty;
    public long MaxElapsedMsObserved { get; set; }
    public long MaxLastProgressAgeMsObserved { get; set; }
    public long MaxOutputBytesObserved { get; set; }
    public double MaxThroughputBytesPerSecObserved { get; set; }
}
