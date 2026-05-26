using System.Text.Json;

namespace Sussudio.Tools;

public sealed class DiagnosticSessionOptions
{
    internal const string DefaultScenario = DiagnosticSessionScenarioCatalog.Observe;
    internal const int DefaultDurationSeconds = 10;
    internal const int DefaultSampleIntervalMs = 1000;
    internal const string CliUsage =
        "diagnostic-session [--scenario " + DiagnosticSessionScenarioCatalog.HelpList + "] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]";

    public string Scenario { get; init; } = DefaultScenario;
    public int DurationSeconds { get; init; } = DefaultDurationSeconds;
    public int SampleIntervalMs { get; init; } = DefaultSampleIntervalMs;
    public string? OutputDirectory { get; init; }
    public bool IncludePresentMon { get; init; }
    public string? PresentMonPath { get; init; }
    public bool VerifyRecording { get; init; }
    public bool LeaveRunning { get; init; }
}

public sealed class DiagnosticSessionSample
{
    public long OffsetMs { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public JsonElement Snapshot { get; init; }
}

internal sealed class DiagnosticSessionScenarioPhaseContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required string Scenario { get; init; }

    internal required DiagnosticSessionScenarioPlan ScenarioPlan { get; init; }

    internal required int DurationSeconds { get; init; }

    internal required int SampleIntervalMs { get; init; }

    internal required string OutputDirectory { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required bool InitialSnapshotKnown { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required List<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required CancellationTokenSource ScenarioCancellationSource { get; init; }

    internal required CancellationToken ScenarioCancellationToken { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Func<string> GetLastStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required Func<Task> WriteLiveStateBestEffortAsync { get; init; }

    internal required Func<Task> WriteSamplingLiveStateBestEffortAsync { get; init; }
}

internal sealed class DiagnosticSessionScenarioPhaseState
{
    internal bool StartedPreview { get; set; }

    internal bool StartedRecording { get; set; }

    internal bool EnabledFlashback { get; set; }

    internal bool DisabledFlashback { get; set; }

    internal bool StartedFlashbackPlayback { get; set; }

    internal PresentMonProbeResult? PresentMon { get; set; }

    internal FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState { get; set; }

    internal DiagnosticSessionScenarioPhaseResult ToResult()
        => new(
            StartedPreview,
            StartedRecording,
            EnabledFlashback,
            DisabledFlashback,
            StartedFlashbackPlayback,
            PresentMon,
            FlashbackRecordingSettingsDeferredPresetState);
}

internal sealed record DiagnosticSessionScenarioPhaseResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback,
    bool StartedFlashbackPlayback,
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState)
{
    internal static readonly DiagnosticSessionScenarioPhaseResult Empty = new(
        StartedPreview: false,
        StartedRecording: false,
        EnabledFlashback: false,
        DisabledFlashback: false,
        StartedFlashbackPlayback: false,
        PresentMon: null,
        FlashbackRecordingSettingsDeferredPresetState: default);
}

public sealed class DiagnosticSessionResult
{
    // Session summary and artifact paths.
    public string SessionId { get; init; } = string.Empty;
    public string Scenario { get; init; } = "observe";
    public bool Success { get; set; }
    public DateTimeOffset StartedUtc { get; init; }
    public DateTimeOffset CompletedUtc { get; set; }
    public string TerminalState { get; set; } = "unknown";
    public string LastStage { get; set; } = string.Empty;
    public string? UnhandledException { get; set; }
    public int RunnerProcessId { get; init; }
    public int DurationSeconds { get; init; }
    public int SampleIntervalMs { get; init; }
    public int SampleCount { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string LivePath { get; init; } = string.Empty;
    public string SummaryPath { get; init; } = string.Empty;
    public string SamplesPath { get; init; } = string.Empty;
    public string FrameLedgerPath { get; init; } = string.Empty;
    public string TimelinePath { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = "Unknown";
    public string LikelyStage { get; init; } = "diagnostic_unavailable";
    public string Summary { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();

    // Capture/source summary.
    public string SelectedResolutionAtEnd { get; init; } = string.Empty;
    public double SelectedFrameRateAtEnd { get; init; }
    public string SelectedFriendlyFrameRateAtEnd { get; init; } = string.Empty;
    public string SelectedExactFrameRateArgAtEnd { get; init; } = string.Empty;
    public string SelectedVideoFormatAtEnd { get; init; } = string.Empty;
    public string VideoRequestedSubtypeAtEnd { get; init; } = string.Empty;
    public string VideoNegotiatedSubtypeAtEnd { get; init; } = string.Empty;
    public int SourceWidthAtEnd { get; init; }
    public int SourceHeightAtEnd { get; init; }
    public double DetectedSourceFrameRateAtEnd { get; init; }
    public string DetectedSourceFrameRateArgAtEnd { get; init; } = string.Empty;
    public bool SourceIsHdrAtEnd { get; init; }
    public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;

    // End-of-run overview.
    public double ProcessCpuPercentAtEnd { get; init; }
    public double ProcessCpuMaxPercentObserved { get; init; }
    public bool RecordingVerificationRun { get; init; }
    public bool? RecordingVerificationSucceeded { get; init; }
    public string? RecordingVerificationMessage { get; init; }
    public PresentMonProbeResult? PresentMon { get; init; }

    // Flashback playback command queue summary.
    public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }
    public int FlashbackPlaybackMaxPendingCommandsObserved { get; init; }
    public int FlashbackPlaybackMaxCommandQueueLatencyMsObserved { get; init; }
    public string FlashbackPlaybackMaxCommandQueueLatencyCommandObserved { get; init; } = string.Empty;
    public long FlashbackPlaybackCommandsDroppedAtEnd { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReadyAtEnd { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalescedAtEnd { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalescedAtEnd { get; init; }
    public string FlashbackPlaybackLastCommandFailureAtEnd { get; init; } = string.Empty;
    public long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd { get; init; }

    // Flashback playback cadence and frame-delivery summary.
    public double FlashbackPlaybackObservedFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinObservedFpsObserved { get; init; }
    public double FlashbackPlaybackAvgFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackP99FrameMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxP99FrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxFrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxSlowFramePercentObserved { get; init; }
    public long FlashbackPlaybackFrameCountAtEnd { get; init; }
    public long FlashbackPlaybackLateFramesAtEnd { get; init; }
    public long FlashbackPlaybackSlowFramesAtEnd { get; init; }
    public double FlashbackPlaybackSlowFramePercentAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesDelta { get; init; }

    // Flashback playback 1% low sample-window summary.
    public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinOnePercentLowFpsObserved { get; init; }
    public bool FlashbackPlaybackOnePercentLowSampleWindowObserved { get; init; }
    public long FlashbackPlaybackOnePercentLowMinimumFrames { get; init; }
    public long FlashbackPlaybackMaxSessionFrameCountObserved { get; init; }
    public long FlashbackPlaybackMinOnePercentLowOffsetMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowFrameCount { get; init; }
    public double FlashbackPlaybackMinOnePercentLowP99FrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowMaxFrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeP99Ms { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeMaxMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowAvDriftMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks { get; init; }

    // Flashback playback audio-master summary.
    public long FlashbackPlaybackAudioMasterDelayDoublesAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayDoublesObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayShrinksObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterFallbacksObserved { get; init; }
    public double FlashbackPlaybackMaxAudioBufferedDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAudioQueueDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAbsAvDriftMsObserved { get; init; }

    // Flashback playback decode timing summary.
    public double FlashbackPlaybackDecodeAvgMsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP95MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeMaxMsAtEnd { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeP99MsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeMsObserved { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseObserved { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsObserved { get; init; }

    // Flashback playback stage and seek summary.
    public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }
    public long FlashbackPlaybackSubmitFailuresDelta { get; init; }
    public long FlashbackPlaybackSegmentSwitchesAtEnd { get; init; }
    public long FlashbackPlaybackFmp4ReopensAtEnd { get; init; }
    public long FlashbackPlaybackWriteHeadWaitsAtEnd { get; init; }
    public long FlashbackPlaybackNearLiveSnapsAtEnd { get; init; }
    public long FlashbackPlaybackDecodeErrorSnapsAtEnd { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsDelta { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }

    // Flashback recording summary.
    public bool FlashbackRecordingBackendObserved { get; init; }
    public bool FlashbackRecordingFileGrowthObserved { get; init; }
    public long FlashbackRecordingVideoFramesSubmittedDelta { get; init; }
    public long FlashbackRecordingVideoEncoderPacketsWrittenDelta { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsAtEnd { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsDelta { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesDelta { get; init; }

    // Flashback export summary.
    public bool FlashbackExportObserved { get; init; }
    public bool FlashbackExportActiveAtEnd { get; init; }
    public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;
    public string FlashbackExportMessageAtEnd { get; init; } = string.Empty;
    public string FlashbackExportFailureKindAtEnd { get; init; } = string.Empty;
    public string FlashbackExportOutputPathAtEnd { get; init; } = string.Empty;
    public long FlashbackExportForceRotateFallbacksAtEnd { get; init; }
    public long FlashbackExportForceRotateFallbacksDelta { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegmentsAtEnd { get; init; }
    public long LastExportIdAtEnd { get; init; }
    public string LastExportSuccessAtEnd { get; init; } = string.Empty;
    public string LastExportMessageAtEnd { get; init; } = string.Empty;
    public long FlashbackExportMaxElapsedMsObserved { get; init; }
    public long FlashbackExportMaxLastProgressAgeMsObserved { get; init; }
    public long FlashbackExportMaxOutputBytesObserved { get; init; }
    public double FlashbackExportMaxThroughputBytesPerSecObserved { get; init; }

    // Preview cadence summary.
    public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }
    public double PreviewCadenceMinOnePercentLowFpsObserved { get; init; }

    // Preview visual-cadence summary.
    public double VisualCadenceOutputFpsAtEnd { get; init; }
    public double VisualCadenceChangeFpsAtEnd { get; init; }
    public double VisualCadenceMinChangeFpsObserved { get; init; }
    public double VisualCadenceRepeatPercentAtEnd { get; init; }
    public double VisualCadenceMaxRepeatPercentObserved { get; init; }
    public long VisualCadenceRepeatFramesAtEnd { get; init; }
    public long VisualCadenceLongestRepeatRunAtEnd { get; init; }

    // Preview scheduler and jitter-buffer summary.
    public long PreviewSchedulerDroppedAtEnd { get; init; }
    public long PreviewSchedulerDeadlineDropsAtEnd { get; init; }
    public long PreviewSchedulerClearedDropsAtEnd { get; init; }
    public long PreviewSchedulerUnderflowsAtEnd { get; init; }
    public long PreviewSchedulerResumeReprimesAtEnd { get; init; }
    public long PreviewSchedulerDroppedDelta { get; init; }
    public long PreviewSchedulerDeadlineDropsDelta { get; init; }
    public long PreviewSchedulerClearedDropsDelta { get; init; }
    public long PreviewSchedulerUnderflowsDelta { get; init; }
    public long PreviewSchedulerResumeReprimesDelta { get; init; }
    public string PreviewSchedulerLastDropReasonAtEnd { get; init; } = string.Empty;
    public string PreviewSchedulerLastUnderflowReasonAtEnd { get; init; } = string.Empty;
    public double PreviewSchedulerLastUnderflowInputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerLastUnderflowOutputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerMaxScheduleLateMsObserved { get; init; }
    public long PreviewSchedulerScheduleLateDelta { get; init; }

    // Preview D3D frame-stat and CPU timing summary.
    public long PreviewD3DFrameStatsMissedRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsFailureDelta { get; init; }
    public int PreviewD3DMaxRecentSlowFramesObserved { get; init; }
    public string PreviewD3DLatestSlowFrameReason { get; init; } = string.Empty;
    public double PreviewD3DLatestSlowFrameOverBudgetMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentIntervalMs { get; init; }
    public double PreviewD3DLatestSlowFrameTotalFrameCpuMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentCallMs { get; init; }
    public int PreviewD3DLatestSlowFramePendingFrameCount { get; init; }
    public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }
    public double PreviewD3DInputUploadCpuMaxMsObserved { get; init; }
    public double PreviewD3DRenderSubmitCpuP99MsAtEnd { get; init; }
    public double PreviewD3DRenderSubmitCpuMaxMsObserved { get; init; }
    public double PreviewD3DPresentCallP99MsAtEnd { get; init; }
    public double PreviewD3DPresentCallMaxMsObserved { get; init; }
    public double PreviewD3DTotalFrameCpuP99MsAtEnd { get; init; }
    public double PreviewD3DTotalFrameCpuMaxMsObserved { get; init; }
}
