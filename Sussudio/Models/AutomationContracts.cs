using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sussudio.Models;

// Wire-format response status. The converter uses SnakeCaseLower so the
// on-the-wire spelling stays "ok"/"error"/"not_ready" — every consumer
// (ssctl, MCP tools, test fixtures) reads the JSON via JsonElement walking,
// so wire stability is the contract that matters.
[JsonConverter(typeof(SnakeCaseLowerStatusConverter))]
public enum AutomationResponseStatus
{
    Ok,
    Error,
    NotReady,
}

// Wire-format command lifecycle. SnakeCaseLower preserves "completed",
// "failed", "acknowledged" exactly — the values happen to also be
// snake_case-stable.
[JsonConverter(typeof(SnakeCaseLowerLifecycleConverter))]
public enum AutomationCommandLifecycle
{
    Completed,
    Failed,
    Acknowledged,
}

internal sealed class SnakeCaseLowerStatusConverter : JsonStringEnumConverter<AutomationResponseStatus>
{
    public SnakeCaseLowerStatusConverter()
        : base(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
    {
    }
}

internal sealed class SnakeCaseLowerLifecycleConverter : JsonStringEnumConverter<AutomationCommandLifecycle>
{
    public SnakeCaseLowerLifecycleConverter()
        : base(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
    {
    }
}

// Flashback actions exposed through automation. They describe timeline intent;
// the playback controller decides whether that intent is valid for the current
// Live/Scrub/Play/Pause state.
public enum AutomationFlashbackAction
{
    Play,
    Pause,
    GoLive,
    Seek,
    BeginScrub,
    UpdateScrub,
    EndScrub,
    SetInPoint,
    SetOutPoint,
    ClearInOutPoints
}

public enum AutomationWindowAction
{
    Minimize,
    Maximize,
    Restore,
    Close,
    SnapLeft,
    SnapRight,
    SnapTopLeft,
    SnapTopRight,
    SnapBottomLeft,
    SnapBottomRight,
    Center,
    Move,
    Resize
}

// Conditions that automation waits can poll against the latest snapshot. These
// names are part of the ssctl/MCP surface, so prefer adding a new condition over
// changing existing semantics.
public enum AutomationWaitCondition
{
    PreviewFramesActive,
    PreviewRendererHealthy,
    AudioSignalPresent,
    RecordingFileGrowing,
    RecordingStopped,
    VerificationReady,
    HdrModeApplied,
    PerformancePerfectionMet,
    HdrVerificationReady,
    AudioFramesFlowing,
    VideoFramesFlowing
}

[Flags]
public enum PreviewStartupSignalFlags
{
    None = 0,
    MediaOpened = 1 << 0,
    FirstCaptureFrame = 1 << 1,
    PlaybackAdvancing = 1 << 2,
    FirstVisual = 1 << 3
}

public enum PreviewStartupStrategy
{
    None,
    GpuMediaSourceNoFrameReader,
    GpuMediaSourceWithFrameReader,
    CpuSoftwareBitmap,
    DirectShow,
    D3D11VideoProcessor
}

// Wire-format request for the named-pipe automation server. Payload stays as a
// JsonElement so each command can validate only the fields it actually needs.
public sealed class AutomationCommandRequest
{
    public AutomationCommandKind Command { get; init; }
    public string? CorrelationId { get; init; }
    public string? AuthToken { get; init; }
    public JsonElement Payload { get; init; }
}

// Wire-format response shared by automation clients, ssctl, and MCP tools.
// Snapshot is optional so cheap acknowledgement commands do not have to force a
// full diagnostics refresh unless the caller needs it.
public sealed class AutomationCommandResponse
{
    public bool Success { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public AutomationResponseStatus Status { get; init; } = AutomationResponseStatus.Ok;
    public AutomationCommandLifecycle CommandLifecycle { get; init; } = AutomationCommandLifecycle.Completed;
    public int? RetryAfterMs { get; init; }
    public long? ElapsedMs { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public object? Data { get; init; }
    public AutomationSnapshot? Snapshot { get; init; }
}

public sealed record MjpegDecoderAutomationSnapshot(
    int WorkerIndex,
    int SampleCount,
    double AvgMs,
    double P95Ms,
    double MaxMs);

public sealed class PreviewSlowFrameDiagnostic
{
    public long PreviewPresentId { get; init; }
    public long SourceSequenceNumber { get; init; }
    public long QpcTimestamp { get; init; }
    public long UtcUnixMs { get; init; }
    public double PresentIntervalMs { get; init; }
    public double InputUploadCpuMs { get; init; }
    public double RenderSubmitCpuMs { get; init; }
    public double PresentCallMs { get; init; }
    public double TotalFrameCpuMs { get; init; }
    public double SchedulerToPresentMs { get; init; }
    public double PipelineLatencyMs { get; init; }
    public double ExpectedIntervalMs { get; init; }
    public double DiagnosticThresholdMs { get; init; }
    public double WorstOverBudgetMs { get; init; }
    public string SlowReason { get; init; } = string.Empty;
    public int PendingFrameCount { get; init; }
    public long DxgiPresentDelta { get; init; }
    public long DxgiPresentRefreshDelta { get; init; }
    public long DxgiSyncRefreshDelta { get; init; }
    public long DxgiMissedRefreshCount { get; init; }
}

public sealed class AutomationDeviceOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationStringOption
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationResolutionOption
{
    public string Value { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationFrameRateOption
{
    public double Value { get; init; }
    public double FriendlyValue { get; init; }
    public string ExactValueArg { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationIntOption
{
    public int Value { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsSelected { get; init; }
}

public sealed class AutomationOptionsSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public AutomationDeviceOption[] Devices { get; init; } = Array.Empty<AutomationDeviceOption>();
    public AutomationDeviceOption[] AudioInputDevices { get; init; } = Array.Empty<AutomationDeviceOption>();
    public AutomationResolutionOption[] Resolutions { get; init; } = Array.Empty<AutomationResolutionOption>();
    public AutomationFrameRateOption[] FrameRates { get; init; } = Array.Empty<AutomationFrameRateOption>();
    public AutomationStringOption[] RecordingFormats { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] Qualities { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] Presets { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] SplitEncodeModes { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] VideoFormats { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationIntOption[] MjpegDecoderCounts { get; init; } = Array.Empty<AutomationIntOption>();
    public string? SelectedDeviceId { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public int MjpegDecoderCount { get; init; }
    public bool ShowAllCaptureOptions { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
}

// Point-in-time automation view of app, capture, preview, recording, and
// Flashback health. The object is intentionally broad because diagnostic
// sessions persist it as evidence, not just as an RPC convenience DTO.
public sealed class AutomationSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsInitialized { get; init; }
    public bool IsPreviewing { get; init; }
    public bool IsRecording { get; init; }
    public bool VerificationInProgress { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsAudioPreviewEnabled { get; init; }
    public bool IsCustomAudioInputEnabled { get; init; }

    public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;
    public string StatusText { get; init; } = string.Empty;
    public double PerformanceScore { get; init; }
    public bool PerformancePerfectionMet { get; init; }
    public string PerformanceSummary { get; init; } = "NotEvaluated";
    public string DiagnosticHealthStatus { get; init; } = "Unknown";
    public string DiagnosticLikelyStage { get; init; } = "diagnostic_unavailable";
    public string DiagnosticSummary { get; init; } = "Diagnostics are not available yet.";
    public string DiagnosticEvidence { get; init; } = string.Empty;
    public string DiagnosticSourceLane { get; init; } = string.Empty;
    public string DiagnosticDecodeLane { get; init; } = string.Empty;
    public string DiagnosticPreviewLane { get; init; } = string.Empty;
    public string DiagnosticRenderLane { get; init; } = string.Empty;
    public string DiagnosticPresentLane { get; init; } = string.Empty;
    public string DiagnosticRecordingLane { get; init; } = string.Empty;
    public string DiagnosticAudioLane { get; init; } = string.Empty;
    public long CaptureCommandCommandsEnqueued { get; init; }
    public long CaptureCommandCommandsCompleted { get; init; }
    public long CaptureCommandCommandsFailed { get; init; }
    public long CaptureCommandCommandsCanceled { get; init; }
    public int CaptureCommandPendingCommands { get; init; }
    public int CaptureCommandMaxPendingCommands { get; init; }
    public long CaptureCommandOldestPendingCommandAgeMs { get; init; }
    public long CaptureCommandLastQueueLatencyMs { get; init; }
    public long CaptureCommandMaxQueueLatencyMs { get; init; }
    public string CaptureCommandLastCommand { get; init; } = "None";
    public string CaptureCommandLastError { get; init; } = string.Empty;
    public double PerformanceThresholdCaptureDropPercent { get; init; }
    public double PerformanceThresholdCaptureP95Multiplier { get; init; }
    public double PerformanceThresholdPreviewSlowPercent { get; init; }
    public double PerformanceThresholdVerificationDropPercent { get; init; }

    public string? SelectedDeviceId { get; init; }
    public string? SelectedDeviceName { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedAudioInputDeviceName { get; init; }

    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public double? SelectedFriendlyFrameRate { get; init; }
    public double? SelectedExactFrameRate { get; init; }
    public string? SelectedExactFrameRateArg { get; init; }
    public string? DisabledResolutionReason { get; init; }
    public string? DisabledFrameRateReason { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public double CustomBitrateMbps { get; init; }
    public bool ShowAllCaptureOptions { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
    public bool IsHdrAvailable { get; init; }
    public bool IsHdrEnabled { get; init; }
    public bool HdrOutputActive { get; init; }
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string HdrWarmupState { get; init; } = "NotStarted";
    public int HdrWarmupRequiredP010Frames { get; init; }
    public int HdrWarmupAllowedNonP010Frames { get; init; }
    public int HdrWarmupObservedP010Frames { get; init; }
    public int HdrWarmupObservedNonP010Frames { get; init; }
    public string HdrDowngradeCode { get; init; } = string.Empty;
    public string RequestedPipelineMode { get; init; } = "SDR";
    public string ActivePipelineMode { get; init; } = "SDR";
    public bool PipelineModeMatched { get; init; } = true;
    public string PipelineModeStatus { get; init; } = "Ready";
    public string PipelineModeReason { get; init; } = string.Empty;
    public string TelemetryAlignmentStatus { get; init; } = "Unknown";
    public string TelemetryAlignmentReason { get; init; } = string.Empty;

    public string OutputPath { get; init; } = string.Empty;
    public string RecordingTime { get; init; } = string.Empty;
    public string RecordingSizeInfo { get; init; } = string.Empty;
    public string RecordingBitrateInfo { get; init; } = string.Empty;

    public double AudioPeak { get; init; }
    public bool AudioClipping { get; init; }
    public bool AudioSignalPresent { get; init; }
    public bool AudioMutedSuspected { get; init; }
    public bool AudioReaderActive { get; init; }
    public long AudioFramesArrived { get; init; }
    public long AudioFramesWrittenToSink { get; init; }
    public bool VideoReaderActive { get; init; }
    public long IngestVideoFramesArrived { get; init; }
    public long IngestVideoFramesWrittenToSink { get; init; }
    public long IngestLastVideoFrameAgeMs { get; init; }
    public long VideoIngestErrorCount { get; init; }
    public long MfSourceReaderFramesDelivered { get; init; }
    public long MfSourceReaderFramesDropped { get; init; }
    public string? MfSourceReaderNegotiatedFormat { get; init; }
    public string MemoryPreference { get; init; } = "Cpu";
    public string VideoRequestedSubtype { get; init; } = "unknown";
    public string VideoNegotiatedSubtype { get; init; } = "unknown";
    public int FrameLedgerCapacity { get; init; }
    public long FrameLedgerEventCount { get; init; }
    public long FrameLedgerDroppedEventCount { get; init; }
    public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();
    public long EncoderVideoFramesEnqueued { get; init; }
    public long EncoderVideoFramesEncoded { get; init; }
    public long EncoderLastEnqueueAgeMs { get; init; }
    public long EncoderLastWriteAgeMs { get; init; }

    // === Thread Health Probes ===
    // Source reader
    public bool SourceReaderReadOutstanding { get; init; }
    public long SourceReaderReadOutstandingMs { get; init; }
    public long SourceReaderLastFrameTickMs { get; init; }
    public int SourceReaderFrameChannelDepth { get; init; }

    // WASAPI capture
    public long WasapiCaptureCallbackCount { get; init; }
    public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
    public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
    public long WasapiCaptureCallbackSevereGapCount { get; init; }
    public long WasapiCaptureAudioDiscontinuityCount { get; init; }
    public long WasapiCaptureAudioTimestampErrorCount { get; init; }
    public long WasapiCaptureAudioGlitchCount { get; init; }
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }

    // WASAPI playback
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public double WasapiPlaybackQueueDurationMs { get; init; }
    public double WasapiPlaybackActiveChunkDurationMs { get; init; }
    public double WasapiPlaybackEndpointQueuedDurationMs { get; init; }
    public double WasapiPlaybackBufferedDurationMs { get; init; }
    public double WasapiPlaybackStreamLatencyMs { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }

    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";
    public string RecordingIntegrityStatus { get; init; } = "NotStarted";
    public bool RecordingIntegrityComplete { get; init; }
    public string RecordingIntegrityBackend { get; init; } = "None";
    public DateTimeOffset? RecordingIntegrityCompletedUtc { get; init; }
    public long RecordingIntegritySourceFrames { get; init; }
    public long RecordingIntegrityAcceptedFrames { get; init; }
    public long RecordingIntegrityPipelineDroppedFrames { get; init; }
    public long RecordingIntegrityQueueDroppedFrames { get; init; }
    public long RecordingIntegritySubmittedFrames { get; init; }
    public long RecordingIntegrityEncodedFrames { get; init; }
    public long RecordingIntegrityPacketsWritten { get; init; }
    public long RecordingIntegrityEncoderDroppedFrames { get; init; }
    public long RecordingIntegritySequenceGaps { get; init; }
    public int RecordingIntegrityQueueMaxDepth { get; init; }
    public long RecordingIntegrityQueueOldestFrameAgeMs { get; init; }
    public long RecordingIntegrityBackpressureWaitMs { get; init; }
    public long RecordingIntegrityBackpressureEvents { get; init; }
    public long RecordingIntegrityBackpressureMaxWaitMs { get; init; }
    public string RecordingIntegrityAudioStatus { get; init; } = "Disabled";
    public bool RecordingIntegrityAudioEnabled { get; init; }
    public bool RecordingIntegrityAudioCaptureActive { get; init; }
    public long RecordingIntegrityAudioFramesArrived { get; init; }
    public long RecordingIntegrityAudioFramesWrittenToSink { get; init; }
    public long RecordingIntegrityAudioSamplesEncoded { get; init; }
    public long RecordingIntegrityAudioDropEvents { get; init; }
    public long RecordingIntegrityAudioDiscontinuities { get; init; }
    public long RecordingIntegrityAudioTimestampErrors { get; init; }
    public long RecordingIntegrityAudioCallbackGaps { get; init; }
    public double? RecordingIntegrityAvSyncDriftMs { get; init; }
    public double? RecordingIntegrityAvSyncDriftRateMsPerSec { get; init; }
    public double? RecordingIntegrityEncoderAvSyncDriftMs { get; init; }
    public long? RecordingIntegrityEncoderAvSyncCorrectionSamples { get; init; }
    public string RecordingIntegrityReason { get; init; } = "No recording has completed.";

    public uint? RequestedWidth { get; init; }
    public uint? RequestedHeight { get; init; }
    public double? RequestedFrameRate { get; init; }
    public string? RequestedFrameRateArg { get; init; }
    public uint? RequestedFrameRateNumerator { get; init; }
    public uint? RequestedFrameRateDenominator { get; init; }
    public string? RequestedPixelFormat { get; init; }
    public string? RequestedFormat { get; init; }
    public string? RequestedQuality { get; init; }
    public bool? RequestedHdrEnabled { get; init; }
    public bool? RequestedHdrMasteringMetadata { get; init; }
    public bool? RequestedAudioEnabled { get; init; }
    public string HdrActivationReason { get; init; } = "Unknown";
    public bool HdrAutoDowngraded { get; init; }
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
    public bool HdrRequestedButSourceNot10Bit { get; init; }

    public uint? ActualWidth { get; init; }
    public uint? ActualHeight { get; init; }
    public double? ActualFrameRate { get; init; }
    public string? ActualFrameRateArg { get; init; }
    public uint? NegotiatedWidth { get; init; }
    public uint? NegotiatedHeight { get; init; }
    public double? NegotiatedFrameRate { get; init; }
    public string? NegotiatedFrameRateArg { get; init; }
    public uint? NegotiatedFrameRateNumerator { get; init; }
    public uint? NegotiatedFrameRateDenominator { get; init; }
    public string? NegotiatedPixelFormat { get; init; }
    public string? RequestedReaderSubtype { get; init; }
    public string? ReaderSourceStreamType { get; init; }
    public string? ReaderSourceSubtype { get; init; }
    public string? FirstObservedFramePixelFormat { get; init; }
    public string? LatestObservedFramePixelFormat { get; init; }
    public string? LatestObservedSurfaceFormat { get; init; }
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public long ObservedP010BitDepthSampleCount { get; init; }
    public double ObservedP010Low2BitNonZeroPercent { get; init; }
    public bool? ObservedP010Likely8BitUpscaled { get; init; }
    public string? EncoderInputPixelFormat { get; init; }
    public string? EncoderOutputPixelFormat { get; init; }
    public string? EncoderVideoCodec { get; init; }
    public string? EncoderVideoProfile { get; init; }
    public bool? EncoderTenBitPipelineConfirmed { get; init; }
    public bool? MfReadwriteDisableConverters { get; init; }
    public string? NegotiatedMediaSubtypeToken { get; init; }
    public double? DetectedSourceFrameRate { get; init; }
    public string? DetectedSourceFrameRateArg { get; init; }
    public string SourceFrameRateOrigin { get; init; } = "Unknown";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public bool? SourceIsHdr { get; init; }
    public string? SourceVideoFormat { get; init; }
    public string? SourceColorimetry { get; init; }
    public string? SourceQuantization { get; init; }
    public string? SourceHdrTransferFunction { get; init; }
    public int? SourceHdrTransferCode { get; init; }
    public string? SourceFirmware { get; init; }
    public string? SourceAudioFormat { get; init; }
    public string? SourceAudioSampleRate { get; init; }
    public string? SourceInputSource { get; init; }
    public string? SourceUsbHostProtocol { get; init; }
    public string? SourceHdcpMode { get; init; }
    public string? SourceHdcpVersion { get; init; }
    public string? SourceRxTxHdcpVersion { get; init; }
    public string? SourceRawTimingHex { get; init; }
    public string SourceTelemetryAvailability { get; init; } = "Unknown";
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string SourceTelemetryConfidence { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public int? SourceTelemetryAgeSeconds { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";
    public string SourceTelemetrySummaryText { get; init; } = string.Empty;
    public string SourceTargetSummaryText { get; init; } = string.Empty;

    public long PreviewFramesArrived { get; init; }
    public long PreviewFramesDisplayed { get; init; }
    public long PreviewFramesDropped { get; init; }
    public int PreviewCadenceSampleCount { get; init; }
    public double PreviewCadenceObservedFps { get; init; }
    public double PreviewCadenceExpectedIntervalMs { get; init; }
    public double PreviewCadenceAverageIntervalMs { get; init; }
    public double PreviewCadenceP95IntervalMs { get; init; }
    public double PreviewCadenceP99IntervalMs { get; init; }
    public double PreviewCadenceMaxIntervalMs { get; init; }
    public double PreviewCadenceOnePercentLowFps { get; init; }
    public double PreviewCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceSampleDurationMs { get; init; }
    public double[] PreviewCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double PreviewCadenceJitterStdDevMs { get; init; }
    public long PreviewCadenceSlowFrameCount { get; init; }
    public double PreviewCadenceSlowFramePercent { get; init; }
    public bool PreviewGpuActive { get; init; }
    public bool PreviewPlaceholderVisible { get; init; }
    public bool PreviewGpuElementVisible { get; init; }
    public bool PreviewCpuElementVisible { get; init; }
    public bool PreviewRendererAttached { get; init; }
    public string PreviewStartupState { get; init; } = "Idle";
    public string? PreviewAttemptId { get; init; }
    public double? PreviewStartupElapsedMs { get; init; }
    public int PreviewStartupTimeoutMs { get; init; }
    public bool PreviewGpuSignalMediaOpened { get; init; }
    public bool PreviewGpuSignalFirstFrame { get; init; }
    public bool PreviewGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags PreviewStartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags PreviewStartupReceivedSignals { get; init; }
    public string PreviewStartupStrategy { get; init; } = "None";
    public string? PreviewStartupMissingSignals { get; init; }
    public int PreviewRecoveryAttemptCount { get; init; }
    public string? PreviewLastFailureReason { get; init; }
    public bool PreviewFirstVisualConfirmed { get; init; }
    public bool PreviewBlankSuspected { get; init; }
    public bool PreviewStalled { get; init; }
    public string PreviewRendererMode { get; init; } = "None";
    public int PreviewD3DPresentSyncInterval { get; init; }
    public int PreviewD3DMaxFrameLatency { get; init; }
    public int PreviewD3DSwapChainBufferCount { get; init; }
    public string PreviewD3DSwapChainAddress { get; init; } = string.Empty;
    public long PreviewD3DFramesSubmitted { get; init; }
    public long PreviewD3DFramesRendered { get; init; }
    public long PreviewD3DFramesDropped { get; init; }
    public long PreviewD3DRenderThreadFailureCount { get; init; }
    public string PreviewD3DLastRenderThreadFailureType { get; init; } = string.Empty;
    public string PreviewD3DLastRenderThreadFailureMessage { get; init; } = string.Empty;
    public int PreviewD3DLastRenderThreadFailureHResult { get; init; }
    public int PreviewD3DPendingFrameCount { get; init; }
    public string PreviewD3DInputColorSpace { get; init; } = "None";
    public string PreviewD3DOutputColorSpace { get; init; } = "None";
    public int PreviewD3DCpuTimingSampleCount { get; init; }
    public double PreviewD3DInputUploadCpuAvgMs { get; init; }
    public double PreviewD3DInputUploadCpuP95Ms { get; init; }
    public double PreviewD3DInputUploadCpuP99Ms { get; init; }
    public double PreviewD3DInputUploadCpuMaxMs { get; init; }
    public double PreviewD3DRenderSubmitCpuAvgMs { get; init; }
    public double PreviewD3DRenderSubmitCpuP95Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuP99Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuMaxMs { get; init; }
    public double PreviewD3DPresentCallAvgMs { get; init; }
    public double PreviewD3DPresentCallP95Ms { get; init; }
    public double PreviewD3DPresentCallP99Ms { get; init; }
    public double PreviewD3DPresentCallMaxMs { get; init; }
    public double PreviewD3DTotalFrameCpuAvgMs { get; init; }
    public double PreviewD3DTotalFrameCpuP95Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP99Ms { get; init; }
    public double PreviewD3DTotalFrameCpuMaxMs { get; init; }
    public int PreviewD3DPipelineLatencySampleCount { get; init; }
    public double PreviewD3DPipelineLatencyAvgMs { get; init; }
    public double PreviewD3DPipelineLatencyP95Ms { get; init; }
    public double PreviewD3DPipelineLatencyP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyMaxMs { get; init; }
    public bool PreviewD3DFrameLatencyWaitEnabled { get; init; }
    public bool PreviewD3DFrameLatencyWaitHandleActive { get; init; }
    public long PreviewD3DFrameLatencyWaitCallCount { get; init; }
    public long PreviewD3DFrameLatencyWaitSignaledCount { get; init; }
    public long PreviewD3DFrameLatencyWaitTimeoutCount { get; init; }
    public long PreviewD3DFrameLatencyWaitUnexpectedResultCount { get; init; }
    public uint PreviewD3DFrameLatencyWaitLastResult { get; init; }
    public double PreviewD3DFrameLatencyWaitLastMs { get; init; }
    public int PreviewD3DFrameLatencyWaitSampleCount { get; init; }
    public double PreviewD3DFrameLatencyWaitAvgMs { get; init; }
    public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitP99Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
    public long PreviewD3DFrameStatsSampleCount { get; init; }
    public long PreviewD3DFrameStatsSuccessCount { get; init; }
    public long PreviewD3DFrameStatsFailureCount { get; init; }
    public string PreviewD3DFrameStatsLastError { get; init; } = string.Empty;
    public long PreviewD3DFrameStatsPresentCount { get; init; }
    public long PreviewD3DFrameStatsPresentRefreshCount { get; init; }
    public long PreviewD3DFrameStatsSyncRefreshCount { get; init; }
    public long PreviewD3DFrameStatsSyncQpcTime { get; init; }
    public long PreviewD3DFrameStatsLastPresentDelta { get; init; }
    public long PreviewD3DFrameStatsLastPresentRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsLastSyncRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentFailureCount { get; init; }
    public long PreviewD3DLastSubmittedPreviewPresentId { get; init; }
    public long PreviewD3DLastSubmittedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastSubmittedSourcePtsTicks { get; init; }
    public long PreviewD3DLastSubmittedQpc { get; init; }
    public long PreviewD3DLastSubmittedUtcUnixMs { get; init; }
    public long PreviewD3DLastRenderedPreviewPresentId { get; init; }
    public long PreviewD3DLastRenderedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastRenderedSourcePtsTicks { get; init; }
    public long PreviewD3DLastRenderedQpc { get; init; }
    public long PreviewD3DLastRenderedUtcUnixMs { get; init; }
    public double PreviewD3DLastRenderedSchedulerToPresentMs { get; init; }
    public double PreviewD3DLastRenderedPipelineLatencyMs { get; init; }
    public long PreviewD3DLastDroppedPreviewPresentId { get; init; }
    public long PreviewD3DLastDroppedSourceSequenceNumber { get; init; }
    public long PreviewD3DLastDroppedSourcePtsTicks { get; init; }
    public long PreviewD3DLastDroppedQpc { get; init; }
    public long PreviewD3DLastDroppedUtcUnixMs { get; init; }
    public string PreviewD3DLastDropReason { get; init; } = string.Empty;
    public PreviewSlowFrameDiagnostic[] PreviewD3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string PreviewGpuPlaybackState { get; init; } = "None";
    public int PreviewGpuNaturalVideoWidth { get; init; }
    public int PreviewGpuNaturalVideoHeight { get; init; }
    public double PreviewGpuPositionMs { get; init; }
    public long PreviewGpuPositionEventCount { get; init; }
    public bool PreviewHdrInputDetected { get; init; }
    public string PreviewToneMapMode { get; init; } = "Unknown";
    public string? PreviewColorContext { get; init; }
    public string PreviewAdapterColorMetadata { get; init; } = "None";

    public int ConversionQueueDepth { get; init; }
    public int FfmpegVideoQueueDepth { get; init; }
    public int FfmpegAudioQueueDepth { get; init; }
    public long VideoFramesArrived { get; init; }
    public long VideoFramesQueued { get; init; }
    public long VideoFramesDropped { get; init; }
    public long VideoFramesDroppedBacklog { get; init; }
    public long VideoFramesConverted { get; init; }
    public long VideoFramesEnqueued { get; init; }
    public long VideoDropsQueueSaturated { get; init; }
    public long VideoDropsBacklogEviction { get; init; }
    public bool RecordingEncodingFailed { get; init; }
    public string? RecordingEncodingFailureType { get; init; }
    public string? RecordingEncodingFailureMessage { get; init; }
    public int RecordingVideoQueueCapacity { get; init; }
    public int RecordingVideoQueueMaxDepth { get; init; }
    public long RecordingVideoFramesSubmittedToEncoder { get; init; }
    public long RecordingVideoEncoderPts { get; init; }
    public long RecordingVideoEncoderPacketsWritten { get; init; }
    public long RecordingVideoEncoderDroppedFrames { get; init; }
    public long RecordingVideoSequenceGaps { get; init; }
    public long RecordingVideoQueueOldestFrameAgeMs { get; init; }
    public long RecordingVideoQueueLastLatencyMs { get; init; }
    public int RecordingVideoQueueLatencySampleCount { get; init; }
    public double RecordingVideoQueueLatencyAvgMs { get; init; }
    public double RecordingVideoQueueLatencyP95Ms { get; init; }
    public double RecordingVideoQueueLatencyP99Ms { get; init; }
    public double RecordingVideoQueueLatencyMaxMs { get; init; }
    public long RecordingVideoBackpressureWaitMs { get; init; }
    public long RecordingVideoBackpressureEvents { get; init; }
    public long RecordingVideoBackpressureLastWaitMs { get; init; }
    public long RecordingVideoBackpressureMaxWaitMs { get; init; }
    public int RecordingGpuQueueDepth { get; init; }
    public int RecordingGpuQueueCapacity { get; init; }
    public int RecordingGpuQueueMaxDepth { get; init; }
    public long RecordingGpuFramesEnqueued { get; init; }
    public long RecordingGpuFramesDropped { get; init; }
    public int RecordingCudaQueueDepth { get; init; }
    public int RecordingCudaQueueCapacity { get; init; }
    public int RecordingCudaQueueMaxDepth { get; init; }
    public long RecordingCudaFramesEnqueued { get; init; }
    public long RecordingCudaFramesDropped { get; init; }
    public bool FlashbackEncodingFailed { get; init; }
    public string? FlashbackEncodingFailureType { get; init; }
    public string? FlashbackEncodingFailureMessage { get; init; }
    public bool FatalCleanupInProgress { get; init; }
    public bool FlashbackCleanupInProgress { get; init; }
    public bool FlashbackForceRotateActive { get; init; }
    public bool FlashbackForceRotateRequested { get; init; }
    public bool FlashbackForceRotateDraining { get; init; }
    public int FlashbackVideoQueueCapacity { get; init; }
    public int FlashbackVideoQueueMaxDepth { get; init; }
    public long FlashbackVideoFramesSubmittedToEncoder { get; init; }
    public long FlashbackVideoEncoderPts { get; init; }
    public long FlashbackVideoEncoderPacketsWritten { get; init; }
    public long FlashbackVideoEncoderDroppedFrames { get; init; }
    public long FlashbackVideoSequenceGaps { get; init; }
    public long FlashbackVideoQueueRejectedFrames { get; init; }
    public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
    public long FlashbackVideoQueueOldestFrameAgeMs { get; init; }
    public long FlashbackVideoQueueLastLatencyMs { get; init; }
    public int FlashbackVideoQueueLatencySampleCount { get; init; }
    public double FlashbackVideoQueueLatencyAvgMs { get; init; }
    public double FlashbackVideoQueueLatencyP95Ms { get; init; }
    public double FlashbackVideoQueueLatencyP99Ms { get; init; }
    public double FlashbackVideoQueueLatencyMaxMs { get; init; }
    public long FlashbackVideoBackpressureWaitMs { get; init; }
    public long FlashbackVideoBackpressureEvents { get; init; }
    public long FlashbackVideoBackpressureLastWaitMs { get; init; }
    public long FlashbackVideoBackpressureMaxWaitMs { get; init; }
    public int FlashbackGpuQueueDepth { get; init; }
    public int FlashbackGpuQueueCapacity { get; init; }
    public int FlashbackGpuQueueMaxDepth { get; init; }
    public long FlashbackGpuFramesEnqueued { get; init; }
    public long FlashbackGpuFramesDropped { get; init; }
    public long FlashbackGpuQueueRejectedFrames { get; init; }
    public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;
    public long AudioDropsQueueSaturated { get; init; }
    public long AudioDropsBacklogEviction { get; init; }
    public long AudioChunksDropped { get; init; }
    public long AudioQueueDropsRealtime { get; init; }
    public long AudioQueueDropsFileWriter { get; init; }
    public long EstimatedPipelineLatencyMs { get; init; }
    public double ExpectedCaptureFrameRate { get; init; }
    public int CaptureCadenceSampleCount { get; init; }
    public double CaptureCadenceObservedFps { get; init; }
    public double CaptureCadenceExpectedIntervalMs { get; init; }
    public double CaptureCadenceAverageIntervalMs { get; init; }
    public double CaptureCadenceP95IntervalMs { get; init; }
    public double CaptureCadenceP99IntervalMs { get; init; }
    public double CaptureCadenceMaxIntervalMs { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceFivePercentLowFps { get; init; }
    public double CaptureCadenceSampleDurationMs { get; init; }
    public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double CaptureCadenceJitterStdDevMs { get; init; }
    public long CaptureCadenceSevereGapCount { get; init; }
    public long CaptureCadenceEstimatedDroppedFrames { get; init; }
    public double CaptureCadenceEstimatedDropPercent { get; init; }
    public int MjpegDecodeSampleCount { get; init; }
    public double MjpegDecodeAvgMs { get; init; }
    public double MjpegDecodeP95Ms { get; init; }
    public double MjpegDecodeMaxMs { get; init; }
    public int MjpegInteropCopySampleCount { get; init; }
    public double MjpegInteropCopyAvgMs { get; init; }
    public double MjpegInteropCopyP95Ms { get; init; }
    public double MjpegInteropCopyMaxMs { get; init; }
    public int MjpegCallbackSampleCount { get; init; }
    public double MjpegCallbackAvgMs { get; init; }
    public double MjpegCallbackP95Ms { get; init; }
    public double MjpegCallbackMaxMs { get; init; }
    public int MjpegDecoderCount { get; init; }
    public int MjpegReorderSampleCount { get; init; }
    public double MjpegReorderAvgMs { get; init; }
    public double MjpegReorderP95Ms { get; init; }
    public double MjpegReorderMaxMs { get; init; }
    public int MjpegPipelineSampleCount { get; init; }
    public double MjpegPipelineAvgMs { get; init; }
    public double MjpegPipelineP95Ms { get; init; }
    public double MjpegPipelineMaxMs { get; init; }
    public long MjpegTotalDecoded { get; init; }
    public long MjpegTotalEmitted { get; init; }
    public long MjpegTotalDropped { get; init; }
    public long MjpegCompressedFramesQueued { get; init; }
    public long MjpegCompressedFramesDequeued { get; init; }
    public long MjpegCompressedDropsQueueFull { get; init; }
    public long MjpegCompressedDropsByteBudget { get; init; }
    public long MjpegCompressedDropsDisposed { get; init; }
    public long MjpegDecodeFailures { get; init; }
    public long MjpegReorderCollisions { get; init; }
    public long MjpegEmitFailures { get; init; }
    public int MjpegCompressedQueueDepth { get; init; }
    public long MjpegCompressedQueueBytes { get; init; }
    public long MjpegCompressedQueueByteBudget { get; init; }
    public long MjpegReorderSkips { get; init; }
    public int MjpegReorderBufferDepth { get; init; }
    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalQueued { get; init; }
    public long MjpegPreviewJitterTotalSubmitted { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public int MjpegPreviewJitterInputSampleCount { get; init; }
    public double MjpegPreviewJitterInputAvgMs { get; init; }
    public double MjpegPreviewJitterInputP95Ms { get; init; }
    public double MjpegPreviewJitterInputMaxMs { get; init; }
    public int MjpegPreviewJitterOutputSampleCount { get; init; }
    public double MjpegPreviewJitterOutputAvgMs { get; init; }
    public double MjpegPreviewJitterOutputP95Ms { get; init; }
    public double MjpegPreviewJitterOutputMaxMs { get; init; }
    public int MjpegPreviewJitterLatencySampleCount { get; init; }
    public double MjpegPreviewJitterLatencyAvgMs { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterTargetIncreaseCount { get; init; }
    public long MjpegPreviewJitterTargetDecreaseCount { get; init; }
    public long MjpegPreviewJitterLastSelectedPreviewPresentId { get; init; }
    public long MjpegPreviewJitterLastSelectedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastSelectedQpc { get; init; }
    public double MjpegPreviewJitterLastSelectedSourceLatencyMs { get; init; }
    public long MjpegPreviewJitterLastDroppedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastDropQpc { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public long MjpegPreviewJitterLastUnderflowQpc { get; init; }
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public int MjpegPreviewJitterLastUnderflowQueueDepth { get; init; }
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterLastScheduleLateMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
    public int MjpegPacketHashSampleCount { get; init; }
    public long MjpegPacketHashUniqueFrameCount { get; init; }
    public long MjpegPacketHashDuplicateFrameCount { get; init; }
    public long MjpegPacketHashLongestDuplicateRun { get; init; }
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public string MjpegPacketHashLastHash { get; init; } = string.Empty;
    public bool MjpegPacketHashLastFrameDuplicate { get; init; }
    public string MjpegPacketHashPattern { get; init; } = "NoSamples";
    public double[] MjpegPacketHashRecentInputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] MjpegPacketHashRecentUniqueIntervalsMs { get; init; } = Array.Empty<double>();
    public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();
    public int VisualCadenceSampleCount { get; init; }
    public long VisualCadenceChangedFrameCount { get; init; }
    public long VisualCadenceRepeatFrameCount { get; init; }
    public long VisualCadenceLongestRepeatRun { get; init; }
    public double VisualCadenceOutputObservedFps { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public double VisualCadenceLastDelta { get; init; }
    public double VisualCadenceAverageDelta { get; init; }
    public double VisualCadenceP95Delta { get; init; }
    public double VisualCadenceMotionScore { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public int VisualCenterCadenceSampleCount { get; init; }
    public long VisualCenterCadenceChangedFrameCount { get; init; }
    public long VisualCenterCadenceRepeatFrameCount { get; init; }
    public long VisualCenterCadenceLongestRepeatRun { get; init; }
    public double VisualCenterCadenceOutputObservedFps { get; init; }
    public double VisualCenterCadenceChangeObservedFps { get; init; }
    public double VisualCenterCadenceRepeatFramePercent { get; init; }
    public double VisualCenterCadenceLastDelta { get; init; }
    public double VisualCenterCadenceAverageDelta { get; init; }
    public double VisualCenterCadenceP95Delta { get; init; }
    public double VisualCenterCadenceMotionScore { get; init; }
    public string VisualCenterCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCenterCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();

    public long RecordingVideoBytes { get; init; }
    public long RecordingAudioBytes { get; init; }
    public long RecordingTotalBytes { get; init; }
    public bool RecordingFileGrowing { get; init; }

    public string? LastOutputPath { get; init; }
    public string LastFinalizeStatus { get; init; } = "None";
    public DateTimeOffset? LastFinalizeUtc { get; init; }
    public bool LastOutputExists { get; init; }
    public long? LastOutputSizeBytes { get; init; }

    public RecordingVerificationResult? LastVerification { get; init; }
    public HdrTruthVerdict? HdrTruthVerdict { get; init; }

    // === Memory & GC ===
    public double MemoryWorkingSetMb { get; init; }
    public double MemoryPrivateBytesMb { get; init; }
    public double MemoryManagedHeapMb { get; init; }
    public double MemoryTotalAllocatedMb { get; init; }
    public double ProcessCpuPercent { get; init; }
    public double ProcessCpuTotalProcessorTimeMs { get; init; }
    public double MemoryGcHeapSizeMb { get; init; }
    public int MemoryGcGen0Collections { get; init; }
    public int MemoryGcGen1Collections { get; init; }
    public int MemoryGcGen2Collections { get; init; }
    public double MemoryGcPauseTimePercent { get; init; }
    public double MemoryGcFragmentationPercent { get; init; }
    public int ThreadPoolWorkerAvailable { get; init; }
    public int ThreadPoolWorkerMax { get; init; }
    public int ThreadPoolIoAvailable { get; init; }
    public int ThreadPoolIoMax { get; init; }

    // === AV Sync ===
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }

    // === Flashback ===
    public bool FlashbackActive { get; init; }
    public long FlashbackBufferedDurationMs { get; init; }
    public long FlashbackDiskBytes { get; init; }
    public long FlashbackTotalBytesWritten { get; init; }
    public long FlashbackTempDriveFreeBytes { get; init; }
    public long FlashbackStartupCacheBudgetBytes { get; init; }
    public long FlashbackStartupCacheBytes { get; init; }
    public int FlashbackStartupCacheSessionCount { get; init; }
    public int FlashbackStartupCacheDeletedSessionCount { get; init; }
    public long FlashbackStartupCacheFreedBytes { get; init; }
    public bool FlashbackStartupCacheOverBudget { get; init; }
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
    public string? FlashbackExportVerificationFormat { get; init; }
    public string? FlashbackCodecDowngradeReason { get; init; }
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
    public long LastExportId { get; init; }
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }
}

public sealed class FlashbackSegmentInfo
{
    public string Path { get; init; } = "";
    public int SequenceNumber { get; init; }
    public long StartPtsMs { get; init; }
    public long EndPtsMs { get; init; }
    public long SizeBytes { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PerformanceTimelineEntry
{
    public DateTimeOffset TimestampUtc { get; init; }
    public double CaptureFps { get; init; }
    public double PreviewFps { get; init; }
    public int VideoQueueDepth { get; init; }
    public long VideoDrops { get; init; }
    public double CaptureCadenceAverageMs { get; init; }
    public double CaptureCadenceP95Ms { get; init; }
    public double CaptureCadenceP99Ms { get; init; }
    public double CaptureCadenceMaxMs { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceAverageMs { get; init; }
    public double PreviewCadenceP95Ms { get; init; }
    public double PreviewCadenceP99Ms { get; init; }
    public double PreviewCadenceMaxMs { get; init; }
    public double PreviewCadenceOnePercentLowFps { get; init; }
    public double PreviewCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceSlowFramePercent { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = string.Empty;
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
    public int PreviewD3DPendingFrameCount { get; init; }
    public double PreviewD3DPresentCallP95Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP95Ms { get; init; }
    public double PreviewD3DInputUploadCpuP99Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuP99Ms { get; init; }
    public double PreviewD3DPresentCallP99Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyP95Ms { get; init; }
    public double PreviewD3DPipelineLatencyP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyMaxMs { get; init; }
    public long PreviewD3DFrameLatencyWaitTimeoutCount { get; init; }
    public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
    public long PreviewD3DFrameStatsRecentMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentFailureCount { get; init; }
    public double PreviewD3DLastRenderedSchedulerToPresentMs { get; init; }
    public double PreviewD3DLastRenderedPipelineLatencyMs { get; init; }
    public string PreviewD3DLastDropReason { get; init; } = string.Empty;
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public double FlashbackPlaybackTargetFps { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackP99FrameMs { get; init; }
    public double FlashbackPlaybackMaxFrameMs { get; init; }
    public double FlashbackPlaybackOnePercentLowFps { get; init; }
    public double FlashbackPlaybackFivePercentLowFps { get; init; }
    public double FlashbackPlaybackSlowFramePercent { get; init; }
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
    public long FlashbackPlaybackSeekForwardDecodeCapHits { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCap { get; init; }
    public int FlashbackPlaybackPendingCommands { get; init; }
    public int FlashbackPlaybackMaxPendingCommands { get; init; }
    public long FlashbackPlaybackCommandsEnqueued { get; init; }
    public long FlashbackPlaybackCommandsProcessed { get; init; }
    public long FlashbackPlaybackCommandsDropped { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReady { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalesced { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalesced { get; init; }
    public string FlashbackPlaybackLastCommandQueued { get; init; } = string.Empty;
    public string FlashbackPlaybackLastCommandProcessed { get; init; } = string.Empty;
    public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
    public string FlashbackPlaybackMaxCommandQueueLatencyCommand { get; init; } = string.Empty;
    public long FlashbackPlaybackSubmitFailures { get; init; }
    public long FlashbackPlaybackLastDropUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastDropReason { get; init; } = string.Empty;
    public long FlashbackPlaybackLastSubmitFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastSubmitFailure { get; init; } = string.Empty;
    public long FlashbackPlaybackDroppedFrames { get; init; }
    public long FlashbackPlaybackAudioMasterDelayDoubles { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinks { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacks { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReason { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMs { get; init; }
    public long FlashbackPlaybackSegmentSwitches { get; init; }
    public long FlashbackPlaybackFmp4Reopens { get; init; }
    public long FlashbackPlaybackWriteHeadWaits { get; init; }
    public long FlashbackPlaybackNearLiveSnaps { get; init; }
    public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
    public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;
    public bool FlashbackBackendSettingsStale { get; init; }
    public string FlashbackBackendSettingsStaleReason { get; init; } = string.Empty;
    public string FlashbackBackendActiveFormat { get; init; } = string.Empty;
    public string FlashbackBackendRequestedFormat { get; init; } = string.Empty;
    public string FlashbackBackendActivePreset { get; init; } = string.Empty;
    public string FlashbackBackendRequestedPreset { get; init; } = string.Empty;
    public long FlashbackVideoQueueRejectedFrames { get; init; }
    public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
    public long FlashbackGpuQueueRejectedFrames { get; init; }
    public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;
    public bool FatalCleanupInProgress { get; init; }
    public bool FlashbackCleanupInProgress { get; init; }
    public bool FlashbackForceRotateRequested { get; init; }
    public bool FlashbackForceRotateDraining { get; init; }
    public bool FlashbackExportActive { get; init; }
    public string FlashbackExportStatus { get; init; } = "NotStarted";
    public string FlashbackExportFailureKind { get; init; } = string.Empty;
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
    public long FlashbackExportForceRotateFallbacks { get; init; }
    public long FlashbackExportLastForceRotateFallbackUtcUnixMs { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegments { get; init; }
    public long FlashbackExportLastForceRotateFallbackInPointMs { get; init; }
    public long FlashbackExportLastForceRotateFallbackOutPointMs { get; init; }
    public long PipelineLatencyMs { get; init; }
    public double ProcessCpuPercent { get; init; }
    public double MemoryWorkingSetMb { get; init; }
    public double MemoryManagedHeapMb { get; init; }
    public int GcGen0Collections { get; init; }
    public int GcGen1Collections { get; init; }
    public int GcGen2Collections { get; init; }
    public double GcPauseTimePercent { get; init; }
    public int ThreadPoolWorkerAvailable { get; init; }
    public int ThreadPoolIoAvailable { get; init; }
}

public enum DiagnosticsSeverity
{
    Info,
    Warning,
    Error
}

public enum DiagnosticsCategory
{
    Control,
    Capture,
    Preview,
    Audio,
    Recording,
    Flashback,
    Verification,
    System
}

public sealed class DiagnosticsEvent
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public DiagnosticsSeverity Severity { get; init; } = DiagnosticsSeverity.Info;
    public DiagnosticsCategory Category { get; init; } = DiagnosticsCategory.System;
    public string Message { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class RecordingVerificationResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public bool FileExists { get; init; }
    public long FileSizeBytes { get; init; }
    public string VerificationMode { get; init; } = "None";
    public string? DetectedContainer { get; init; }
    public string? DetectedVideoCodec { get; init; }
    public string? DetectedPixelFormat { get; init; }
    public string? DetectedColorPrimaries { get; init; }
    public string? DetectedColorTransfer { get; init; }
    public string? DetectedColorSpace { get; init; }
    public IReadOnlyList<string> DetectedHdrSideDataTypes { get; init; } = Array.Empty<string>();
    public bool? HdrMetadataPresent { get; init; }
    public bool? HdrColorimetryValid { get; init; }
    public bool? HdrMasteringMetadataPresent { get; init; }
    public string HdrVerificationLevel { get; init; } = "NotHdr";
    public uint? DetectedWidth { get; init; }
    public uint? DetectedHeight { get; init; }
    public double? DetectedFrameRate { get; init; }
    public int? CadenceSampleCount { get; init; }
    public double? CadenceObservedFps { get; init; }
    public double? CadenceExpectedIntervalMs { get; init; }
    public double? CadenceAverageIntervalMs { get; init; }
    public double? CadenceP95IntervalMs { get; init; }
    public double? CadenceMaxIntervalMs { get; init; }
    public double? CadenceJitterStdDevMs { get; init; }
    public long? CadenceSevereGapCount { get; init; }
    public double? CadenceSevereGapPercent { get; init; }
    public long? CadenceEstimatedDroppedFrames { get; init; }
    public double? CadenceEstimatedDropPercent { get; init; }
    public string? PrimaryMismatchCode { get; init; }
    public string? PrimaryMismatchExpected { get; init; }
    public string? PrimaryMismatchActual { get; init; }
    public IReadOnlyList<string> Mismatches { get; init; } = Array.Empty<string>();
    public HdrParityResult? HdrParity { get; init; }
}

public sealed class HdrParityResult
{
    public bool Requested { get; init; }
    public bool Activated { get; init; }
    public bool Verified { get; init; }
    public bool Downgraded { get; init; }
    public string VerificationLevel { get; init; } = "NotHdr";
    public string Status { get; init; } = "NotRequested";
    public IReadOnlyList<MismatchTaxonomyEntry> MismatchTaxonomy { get; init; } = Array.Empty<MismatchTaxonomyEntry>();
}

public sealed class HdrTruthVerdict
{
    public string PipelineFormat { get; init; } = "unknown";
    public string EffectiveBitDepth { get; init; } = "unknown";
    public string HdrMetadataState { get; init; } = "unknown";
    public string SourceVsCaptureParity { get; init; } = "unknown";
    public string FinalClassification { get; init; } = "inconclusive";
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
}

public sealed class MismatchTaxonomyEntry
{
    public string Category { get; init; } = "General";
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = "Warning";
    public string? Expected { get; init; }
    public string? Actual { get; init; }
}

public sealed class PreviewRuntimeSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool IsPreviewing { get; init; }
    public bool GpuActive { get; init; }
    public bool PlaceholderVisible { get; init; }
    public bool GpuElementVisible { get; init; }
    public bool CpuElementVisible { get; init; }
    public bool RendererAttached { get; init; }
    public string StartupState { get; init; } = "Idle";
    public string? StartupAttemptId { get; init; }
    public double? StartupElapsedMs { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool StartupGpuSignalMediaOpened { get; init; }
    public bool StartupGpuSignalFirstFrame { get; init; }
    public bool StartupGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags StartupReceivedSignals { get; init; }
    public PreviewStartupStrategy StartupStrategy { get; init; }
    public string? StartupMissingSignals { get; init; }
    public int StartupRecoveryAttemptCount { get; init; }
    public string? StartupLastFailureReason { get; init; }
    public bool FirstVisualConfirmed { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public int DisplayCadenceSampleCount { get; init; }
    public double DisplayCadenceObservedFps { get; init; }
    public double DisplayCadenceExpectedIntervalMs { get; init; }
    public double DisplayCadenceAverageIntervalMs { get; init; }
    public double DisplayCadenceP95IntervalMs { get; init; }
    public double DisplayCadenceP99IntervalMs { get; init; }
    public double DisplayCadenceMaxIntervalMs { get; init; }
    public double DisplayCadenceOnePercentLowFps { get; init; }
    public double DisplayCadenceFivePercentLowFps { get; init; }
    public double DisplayCadenceSampleDurationMs { get; init; }
    public double[] DisplayCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double DisplayCadenceJitterStdDevMs { get; init; }
    public long DisplayCadenceSlowFrameCount { get; init; }
    public double DisplayCadenceSlowFramePercent { get; init; }
    public bool BlankSuspected { get; init; }
    public bool StallSuspected { get; init; }
    public string RendererMode { get; init; } = "None";
    public string PreviewColorMetadata { get; init; } = "None";
    public int D3DPresentSyncInterval { get; init; }
    public int D3DMaxFrameLatency { get; init; }
    public int D3DSwapChainBufferCount { get; init; }
    public string D3DSwapChainAddress { get; init; } = string.Empty;
    public long D3DFramesSubmitted { get; init; }
    public long D3DFramesRendered { get; init; }
    public long D3DFramesDropped { get; init; }
    public long D3DRenderThreadFailureCount { get; init; }
    public string D3DLastRenderThreadFailureType { get; init; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; init; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; init; }
    public int D3DPendingFrameCount { get; init; }
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
    public int D3DCpuTimingSampleCount { get; init; }
    public double D3DInputUploadCpuAvgMs { get; init; }
    public double D3DInputUploadCpuP95Ms { get; init; }
    public double D3DInputUploadCpuP99Ms { get; init; }
    public double D3DInputUploadCpuMaxMs { get; init; }
    public double D3DRenderSubmitCpuAvgMs { get; init; }
    public double D3DRenderSubmitCpuP95Ms { get; init; }
    public double D3DRenderSubmitCpuP99Ms { get; init; }
    public double D3DRenderSubmitCpuMaxMs { get; init; }
    public double D3DPresentCallAvgMs { get; init; }
    public double D3DPresentCallP95Ms { get; init; }
    public double D3DPresentCallP99Ms { get; init; }
    public double D3DPresentCallMaxMs { get; init; }
    public double D3DTotalFrameCpuAvgMs { get; init; }
    public double D3DTotalFrameCpuP95Ms { get; init; }
    public double D3DTotalFrameCpuP99Ms { get; init; }
    public double D3DTotalFrameCpuMaxMs { get; init; }
    public int D3DPipelineLatencySampleCount { get; init; }
    public double D3DPipelineLatencyAvgMs { get; init; }
    public double D3DPipelineLatencyP95Ms { get; init; }
    public double D3DPipelineLatencyP99Ms { get; init; }
    public double D3DPipelineLatencyMaxMs { get; init; }
    public bool D3DFrameLatencyWaitEnabled { get; init; }
    public bool D3DFrameLatencyWaitHandleActive { get; init; }
    public long D3DFrameLatencyWaitCallCount { get; init; }
    public long D3DFrameLatencyWaitSignaledCount { get; init; }
    public long D3DFrameLatencyWaitTimeoutCount { get; init; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; init; }
    public uint D3DFrameLatencyWaitLastResult { get; init; }
    public double D3DFrameLatencyWaitLastMs { get; init; }
    public int D3DFrameLatencyWaitSampleCount { get; init; }
    public double D3DFrameLatencyWaitAvgMs { get; init; }
    public double D3DFrameLatencyWaitP95Ms { get; init; }
    public double D3DFrameLatencyWaitP99Ms { get; init; }
    public double D3DFrameLatencyWaitMaxMs { get; init; }
    public long D3DFrameStatsSampleCount { get; init; }
    public long D3DFrameStatsSuccessCount { get; init; }
    public long D3DFrameStatsFailureCount { get; init; }
    public string D3DFrameStatsLastError { get; init; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; init; }
    public long D3DFrameStatsPresentRefreshCount { get; init; }
    public long D3DFrameStatsSyncRefreshCount { get; init; }
    public long D3DFrameStatsSyncQpcTime { get; init; }
    public long D3DFrameStatsLastPresentDelta { get; init; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; init; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; init; }
    public long D3DFrameStatsMissedRefreshCount { get; init; }
    public long D3DLastSubmittedPreviewPresentId { get; init; }
    public long D3DLastSubmittedSourceSequenceNumber { get; init; }
    public long D3DLastSubmittedSourcePtsTicks { get; init; }
    public long D3DLastSubmittedQpc { get; init; }
    public long D3DLastSubmittedUtcUnixMs { get; init; }
    public long D3DLastRenderedPreviewPresentId { get; init; }
    public long D3DLastRenderedSourceSequenceNumber { get; init; }
    public long D3DLastRenderedSourcePtsTicks { get; init; }
    public long D3DLastRenderedQpc { get; init; }
    public long D3DLastRenderedUtcUnixMs { get; init; }
    public double D3DLastRenderedSchedulerToPresentMs { get; init; }
    public double D3DLastRenderedPipelineLatencyMs { get; init; }
    public long D3DLastDroppedPreviewPresentId { get; init; }
    public long D3DLastDroppedSourceSequenceNumber { get; init; }
    public long D3DLastDroppedSourcePtsTicks { get; init; }
    public long D3DLastDroppedQpc { get; init; }
    public long D3DLastDroppedUtcUnixMs { get; init; }
    public string D3DLastDropReason { get; init; } = string.Empty;
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public double EstimatedPipelineLatencyMs { get; init; }

    // GPU MediaPlayer metrics (only meaningful when GpuActive == true)
    public string GpuPlaybackState { get; init; } = "None";
    public int GpuNaturalVideoWidth { get; init; }
    public int GpuNaturalVideoHeight { get; init; }
    public double GpuPositionMs { get; init; }
    public long GpuPositionEventCount { get; init; }
}

public sealed class CaptureRuntimeSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsInitialized { get; init; }
    public bool IsRecording { get; init; }
    public bool IsAudioPreviewActive { get; init; }
    public bool AudioReaderActive { get; init; }
    public long AudioFramesArrived { get; init; }
    public long AudioFramesWrittenToSink { get; init; }
    public bool VideoReaderActive { get; init; }
    public long IngestVideoFramesArrived { get; init; }
    public long IngestVideoFramesWrittenToSink { get; init; }
    public long IngestLastVideoFrameAgeMs { get; init; }
    public long VideoIngestErrorCount { get; init; }
    public long MfSourceReaderFramesDelivered { get; init; }
    public long MfSourceReaderFramesDropped { get; init; }
    public string? MfSourceReaderNegotiatedFormat { get; init; }
    public string MemoryPreference { get; init; } = "Cpu";
    public string VideoRequestedSubtype { get; init; } = "unknown";
    public string VideoNegotiatedSubtype { get; init; } = "unknown";
    public int FrameLedgerCapacity { get; init; }
    public long FrameLedgerEventCount { get; init; }
    public long FrameLedgerDroppedEventCount { get; init; }
    public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();
    public string PreviewColorMetadata { get; init; } = "None";
    public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;

    // Thread health probes
    public bool SourceReaderReadOutstanding { get; init; }
    public long SourceReaderReadOutstandingMs { get; init; }
    public long SourceReaderLastFrameTickMs { get; init; }
    public int SourceReaderFrameChannelDepth { get; init; }
    public long WasapiCaptureCallbackCount { get; init; }
    public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
    public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
    public long WasapiCaptureCallbackSevereGapCount { get; init; }
    public long WasapiCaptureAudioDiscontinuityCount { get; init; }
    public long WasapiCaptureAudioTimestampErrorCount { get; init; }
    public long WasapiCaptureAudioGlitchCount { get; init; }
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public double WasapiPlaybackQueueDurationMs { get; init; }
    public double WasapiPlaybackActiveChunkDurationMs { get; init; }
    public double WasapiPlaybackEndpointQueuedDurationMs { get; init; }
    public double WasapiPlaybackBufferedDurationMs { get; init; }
    public double WasapiPlaybackStreamLatencyMs { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }
    public double WasapiPlaybackTargetVolumePercent { get; init; }
    public double WasapiPlaybackCurrentVolumePercent { get; init; }
    public double WasapiPlaybackOutputPeak { get; init; }
    public double WasapiPlaybackOutputRms { get; init; }
    public long WasapiPlaybackOutputLevelLastTickMs { get; init; }

    public string? CurrentDeviceId { get; init; }
    public string? CurrentDeviceName { get; init; }
    public string? ActiveAudioDeviceId { get; init; }
    public string? ActiveAudioDeviceName { get; init; }

    public uint? RequestedWidth { get; init; }
    public uint? RequestedHeight { get; init; }
    public double? RequestedFrameRate { get; init; }
    public string? RequestedFrameRateArg { get; init; }
    public uint? RequestedFrameRateNumerator { get; init; }
    public uint? RequestedFrameRateDenominator { get; init; }
    public string? RequestedPixelFormat { get; init; }
    public string? RequestedFormat { get; init; }
    public string? RequestedQuality { get; init; }
    public bool? RequestedAudioEnabled { get; init; }
    public bool? RequestedHdrEnabled { get; init; }
    public bool? RequestedHdrMasteringMetadata { get; init; }
    public bool HdrOutputActive { get; init; }
    public string HdrActivationReason { get; init; } = "Unknown";
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string HdrWarmupState { get; init; } = "NotStarted";
    public int HdrWarmupRequiredP010Frames { get; init; }
    public int HdrWarmupAllowedNonP010Frames { get; init; }
    public int HdrWarmupObservedP010Frames { get; init; }
    public int HdrWarmupObservedNonP010Frames { get; init; }
    public bool HdrAutoDowngraded { get; init; }
    public string HdrDowngradeCode { get; init; } = string.Empty;
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
    public bool HdrRequestedButSourceNot10Bit { get; init; }
    public string RequestedPipelineMode { get; init; } = "SDR";
    public string ActivePipelineMode { get; init; } = "SDR";
    public bool PipelineModeMatched { get; init; } = true;
    public string PipelineModeStatus { get; init; } = "Ready";
    public string PipelineModeReason { get; init; } = string.Empty;
    public string? RequestedOutputPath { get; init; }

    public uint? ActualWidth { get; init; }
    public uint? ActualHeight { get; init; }
    public double? ActualFrameRate { get; init; }
    public string? ActualFrameRateArg { get; init; }
    public uint? NegotiatedWidth { get; init; }
    public uint? NegotiatedHeight { get; init; }
    public double? NegotiatedFrameRate { get; init; }
    public string? NegotiatedFrameRateArg { get; init; }
    public uint? NegotiatedFrameRateNumerator { get; init; }
    public uint? NegotiatedFrameRateDenominator { get; init; }
    public string? NegotiatedPixelFormat { get; init; }
    public string? RequestedReaderSubtype { get; init; }
    public string? ReaderSourceStreamType { get; init; }
    public string? ReaderSourceSubtype { get; init; }
    public string? FirstObservedFramePixelFormat { get; init; }
    public string? LatestObservedFramePixelFormat { get; init; }
    public string? LatestObservedSurfaceFormat { get; init; }
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public long ObservedP010BitDepthSampleCount { get; init; }
    public double ObservedP010Low2BitNonZeroPercent { get; init; }
    public bool? ObservedP010Likely8BitUpscaled { get; init; }
    public string? EncoderInputPixelFormat { get; init; }
    public string? EncoderOutputPixelFormat { get; init; }
    public string? EncoderVideoCodec { get; init; }
    public string? EncoderVideoProfile { get; init; }
    public bool? EncoderTenBitPipelineConfirmed { get; init; }
    public bool? MfReadwriteDisableConverters { get; init; }
    public string? NegotiatedMediaSubtypeToken { get; init; }
    public double? DetectedSourceFrameRate { get; init; }
    public string? DetectedSourceFrameRateArg { get; init; }
    public string SourceFrameRateOrigin { get; init; } = "Unknown";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public bool? SourceIsHdr { get; init; }
    public string? SourceVideoFormat { get; init; }
    public string? SourceColorimetry { get; init; }
    public string? SourceQuantization { get; init; }
    public string? SourceHdrTransferFunction { get; init; }
    public int? SourceHdrTransferCode { get; init; }
    public string? SourceFirmware { get; init; }
    public string? SourceAudioFormat { get; init; }
    public string? SourceAudioSampleRate { get; init; }
    public string? SourceInputSource { get; init; }
    public string? SourceUsbHostProtocol { get; init; }
    public string? SourceHdcpMode { get; init; }
    public string? SourceHdcpVersion { get; init; }
    public string? SourceRxTxHdcpVersion { get; init; }
    public string? SourceRawTimingHex { get; init; }
    public string SourceTelemetryAvailability { get; init; } = "Unknown";
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string SourceTelemetryConfidence { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public int? SourceTelemetryAgeSeconds { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";
    public string TelemetryAlignmentStatus { get; init; } = "Unknown";
    public string TelemetryAlignmentReason { get; init; } = string.Empty;

    // AV Sync diagnostics
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }

    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public bool MuxAttempted { get; init; }
    public bool? MuxSucceeded { get; init; }
    public string RecordingIntegrityStatus { get; init; } = "NotStarted";
    public bool RecordingIntegrityComplete { get; init; }
    public string RecordingIntegrityBackend { get; init; } = "None";
    public DateTimeOffset? RecordingIntegrityCompletedUtc { get; init; }
    public long RecordingIntegritySourceFrames { get; init; }
    public long RecordingIntegrityAcceptedFrames { get; init; }
    public long RecordingIntegrityPipelineDroppedFrames { get; init; }
    public long RecordingIntegrityQueueDroppedFrames { get; init; }
    public long RecordingIntegritySubmittedFrames { get; init; }
    public long RecordingIntegrityEncodedFrames { get; init; }
    public long RecordingIntegrityPacketsWritten { get; init; }
    public long RecordingIntegrityEncoderDroppedFrames { get; init; }
    public long RecordingIntegritySequenceGaps { get; init; }
    public int RecordingIntegrityQueueMaxDepth { get; init; }
    public long RecordingIntegrityQueueOldestFrameAgeMs { get; init; }
    public long RecordingIntegrityBackpressureWaitMs { get; init; }
    public long RecordingIntegrityBackpressureEvents { get; init; }
    public long RecordingIntegrityBackpressureMaxWaitMs { get; init; }
    public string RecordingIntegrityAudioStatus { get; init; } = "Disabled";
    public bool RecordingIntegrityAudioEnabled { get; init; }
    public bool RecordingIntegrityAudioCaptureActive { get; init; }
    public long RecordingIntegrityAudioFramesArrived { get; init; }
    public long RecordingIntegrityAudioFramesWrittenToSink { get; init; }
    public long RecordingIntegrityAudioSamplesEncoded { get; init; }
    public long RecordingIntegrityAudioDropEvents { get; init; }
    public long RecordingIntegrityAudioDiscontinuities { get; init; }
    public long RecordingIntegrityAudioTimestampErrors { get; init; }
    public long RecordingIntegrityAudioCallbackGaps { get; init; }
    public double? RecordingIntegrityAvSyncDriftMs { get; init; }
    public double? RecordingIntegrityAvSyncDriftRateMsPerSec { get; init; }
    public double? RecordingIntegrityEncoderAvSyncDriftMs { get; init; }
    public long? RecordingIntegrityEncoderAvSyncCorrectionSamples { get; init; }
    public string RecordingIntegrityReason { get; init; } = "No recording has completed.";

    public string? LastOutputPath { get; init; }
    public string LastFinalizeStatus { get; init; } = "None";
    public DateTimeOffset? LastFinalizeUtc { get; init; }
    public IReadOnlyList<string> LastPreservedArtifacts { get; init; } = Array.Empty<string>();
    public string? FlashbackExportOutputPath { get; init; }
    public string? FlashbackExportVerificationFormat { get; init; }
    /// <summary>
    /// Non-null when the user-requested flashback codec/encoder settings were silently
    /// substituted at session-start time (e.g. AV1 requested but software MJPEG at
    /// >=100fps cannot encode AV1 in real time, so the encoder falls back to HEVC, or
    /// NVENC preset coerced to "Fast" to keep up with high-frame-rate software decode).
    /// Encodes both what was changed and why so downstream automation, the verifier,
    /// and (eventually) the UI can surface it. Empty/null means user settings were
    /// honored as-is.
    /// </summary>
    public string? FlashbackCodecDowngradeReason { get; init; }
}

public sealed class ViewModelRuntimeSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool IsInitialized { get; init; }
    public bool IsPreviewing { get; init; }
    public bool IsRecording { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsAudioPreviewEnabled { get; init; }
    public bool IsCustomAudioInputEnabled { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string? SelectedDeviceId { get; init; }
    public string? SelectedDeviceName { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedAudioInputDeviceName { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public double? SelectedFriendlyFrameRate { get; init; }
    public double? SelectedExactFrameRate { get; init; }
    public string? SelectedExactFrameRateArg { get; init; }
    public string? DisabledResolutionReason { get; init; }
    public string? DisabledFrameRateReason { get; init; }
    public string HdrResolutionSupportHint { get; init; } = string.Empty;
    public double? DetectedSourceFrameRate { get; init; }
    public string? DetectedSourceFrameRateArg { get; init; }
    public string SourceFrameRateOrigin { get; init; } = "Unknown";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public bool? SourceIsHdr { get; init; }
    public string SourceTelemetryAvailability { get; init; } = "Unknown";
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string SourceTelemetryConfidence { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public int? SourceTelemetryAgeSeconds { get; init; }
    public string SourceTelemetrySummaryText { get; init; } = string.Empty;
    public string SourceTargetSummaryText { get; init; } = string.Empty;
    public long CaptureCommandCommandsEnqueued { get; init; }
    public long CaptureCommandCommandsCompleted { get; init; }
    public long CaptureCommandCommandsFailed { get; init; }
    public long CaptureCommandCommandsCanceled { get; init; }
    public int CaptureCommandPendingCommands { get; init; }
    public int CaptureCommandMaxPendingCommands { get; init; }
    public long CaptureCommandOldestPendingCommandAgeMs { get; init; }
    public long CaptureCommandLastQueueLatencyMs { get; init; }
    public long CaptureCommandMaxQueueLatencyMs { get; init; }
    public string CaptureCommandLastCommand { get; init; } = "None";
    public string CaptureCommandLastError { get; init; } = string.Empty;
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public double CustomBitrateMbps { get; init; }
    public bool ShowAllCaptureOptions { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
    public bool IsHdrAvailable { get; init; }
    public bool IsHdrEnabled { get; init; }
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string LiveResolution { get; init; } = "\u2014";
    public string LiveFrameRate { get; init; } = "\u2014";
    public string LivePixelFormat { get; init; } = "\u2014";
    public string OutputPath { get; init; } = string.Empty;
    public string RecordingTime { get; init; } = string.Empty;
    public string RecordingSizeInfo { get; init; } = string.Empty;
    public string RecordingBitrateInfo { get; init; } = string.Empty;
    public double AudioPeak { get; init; }
    public bool AudioClipping { get; init; }
}

public sealed class SnapshotAssertion
{
    public string Field { get; init; } = string.Empty;
    public string Op { get; init; } = "eq";
    public string? Value { get; init; }
}

public sealed class VideoSourceFormatEntry
{
    public string Subtype { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public double FrameRate { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed class VideoSourceProbeResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool SessionActive { get; init; }
    public string MemoryPreference { get; init; } = "Unknown";
    public string CurrentSubtype { get; init; } = "Unknown";
    public int CurrentWidth { get; init; }
    public int CurrentHeight { get; init; }
    public double CurrentFrameRate { get; init; }
    public bool P010Available { get; init; }
    public bool Nv12Available { get; init; }
    public IReadOnlyList<string> SupportedSubtypes { get; init; } = Array.Empty<string>();
    public int TotalFormatCount { get; init; }
    public IReadOnlyList<VideoSourceFormatEntry> Formats { get; init; } = Array.Empty<VideoSourceFormatEntry>();
}

public sealed class PreviewColorProbeResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool SessionActive { get; init; }
    public string RendererMode { get; init; } = "None";
    public string NegotiatedSubtype { get; init; } = "Unknown";
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public double SourceFrameRate { get; init; }

    // MF_MT_VIDEO_NOMINAL_RANGE: 0=Unknown, 1=Normal(0-255), 2=Wide(16-235)
    public int NominalRange { get; init; }
    public string NominalRangeLabel { get; init; } = "Unknown";

    // MF_MT_TRANSFER_FUNCTION: 1=Unknown, 6=BT709, 8=sRGB, 12=SMPTE2084(PQ), 16=HLG
    public int TransferFunction { get; init; }
    public string TransferFunctionLabel { get; init; } = "Unknown";

    // MF_MT_VIDEO_PRIMARIES: 1=Unknown, 2=BT709, 9=BT2020
    public int VideoPrimaries { get; init; }
    public string VideoPrimariesLabel { get; init; } = "Unknown";

    // MF_MT_YUV_MATRIX: 0=Unknown, 1=BT709, 2=BT601, 4=BT2020_non_const
    public int YuvMatrix { get; init; }
    public string YuvMatrixLabel { get; init; } = "Unknown";

    // Luma (Y plane) analysis from the preview adapter
    public int? LumaMin { get; init; }
    public int? LumaMax { get; init; }
    public double? LumaMean { get; init; }
    public int? LumaBelow16Count { get; init; }
    public int? LumaAbove235Count { get; init; }
    public int? LumaSampleCount { get; init; }

    // Raw MF properties dump (Guid → value)
    public IReadOnlyDictionary<string, string> FormatProperties { get; init; } = new Dictionary<string, string>();

    // D3D11 Video Processor color spaces (set when renderer is active)
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
}

public sealed class PreviewFrameCaptureResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int CapturedWidth { get; init; }
    public int CapturedHeight { get; init; }
    public string RendererMode { get; init; } = "Unknown";
    public double AverageR { get; init; }
    public double AverageG { get; init; }
    public double AverageB { get; init; }
    public double AverageLuminance { get; init; }
    public double MinLuminance { get; init; }
    public double MaxLuminance { get; init; }
    public double NearBlackPercent { get; init; }
    public double NearWhitePercent { get; init; }
    public double PureBlackPercent { get; init; }
    public int LetterboxTopRows { get; init; }
    public int LetterboxBottomRows { get; init; }
    public int PillarboxLeftCols { get; init; }
    public int PillarboxRightCols { get; init; }
    public int ContentWidth { get; init; }
    public int ContentHeight { get; init; }
    public double ContentAspectRatio { get; init; }
    public int[] LuminanceHistogram { get; init; } = Array.Empty<int>();
    public long TotalPixels { get; init; }
}

public sealed class WindowScreenshotResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int CapturedWidth { get; init; }
    public int CapturedHeight { get; init; }
    public long FileSizeBytes { get; init; }
}
