using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ElgatoCapture.Models;

public enum AutomationCommandKind
{
    Authenticate,
    GetSnapshot,
    GetDiagnostics,
    RefreshDevices,
    SelectDevice,
    SelectAudioInputDevice,
    SetCustomAudioInput,
    SetResolution,
    SetFrameRate,
    SetRecordingFormat,
    SetQuality,
    SetCustomBitrate,
    SetHdrEnabled,
    SetAudioEnabled,
    SetAudioPreviewEnabled,
    SetOutputPath,
    SetPreviewEnabled,
    SetRecordingEnabled,
    ArmClose,
    WindowAction,
    WaitForCondition,
    VerifyLastRecording,
    AssertSnapshot,
    SetTrueHdrPreviewEnabled,
    ProbeVideoSource,
    ProbePreviewColor,
    CapturePreviewFrame,
    CaptureWindowScreenshot,
    SetVideoFormat,
    GetCaptureOptions,
    SetPreset,
    SetSplitEncodeMode,
    SetMjpegDecoderCount,
    SetShowAllCaptureOptions,
    SetPreviewVolume,
    SetStatsVisible,
    SetDeviceAudioMode,
    GetPerformanceTimeline,
    SetStatsSectionVisible,
    SetAnalogAudioGain,
    SetSettingsVisible,
    FlashbackAction
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

public sealed class AutomationCommandRequest
{
    public AutomationCommandKind Command { get; init; }
    public string? CorrelationId { get; init; }
    public string? AuthToken { get; init; }
    public JsonElement Payload { get; init; }
}

public sealed class AutomationCommandResponse
{
    public bool Success { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Status { get; init; } = "ok";
    public string CommandLifecycle { get; init; } = "completed";
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

    public string SessionState { get; init; } = "Unknown";
    public string StatusText { get; init; } = string.Empty;
    public double PerformanceScore { get; init; }
    public bool PerformancePerfectionMet { get; init; }
    public string PerformanceSummary { get; init; } = "NotEvaluated";
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
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }

    // WASAPI playback
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }

    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";

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
    public double PreviewCadenceMaxIntervalMs { get; init; }
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
    public long PreviewD3DFramesSubmitted { get; init; }
    public long PreviewD3DFramesRendered { get; init; }
    public long PreviewD3DFramesDropped { get; init; }
    public string PreviewD3DInputColorSpace { get; init; } = "None";
    public string PreviewD3DOutputColorSpace { get; init; } = "None";
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
    public double CaptureCadenceMaxIntervalMs { get; init; }
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
    public long MjpegReorderSkips { get; init; }
    public int MjpegReorderBufferDepth { get; init; }
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
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public long FlashbackPlaybackPositionMs { get; init; }
    public string FlashbackDecoderHwAccel { get; init; } = "N/A";
    public long FlashbackPlaybackFrameCount { get; init; }
    public long FlashbackPlaybackLateFrames { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackAvgFrameMs { get; init; }
    public double FlashbackAvDriftMs { get; init; }
}

public sealed class PerformanceTimelineEntry
{
    public DateTimeOffset TimestampUtc { get; init; }
    public double CaptureFps { get; init; }
    public double PreviewFps { get; init; }
    public int VideoQueueDepth { get; init; }
    public long VideoDrops { get; init; }
    public double CaptureCadenceP95Ms { get; init; }
    public long PipelineLatencyMs { get; init; }
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
    public double DisplayCadenceMaxIntervalMs { get; init; }
    public double DisplayCadenceJitterStdDevMs { get; init; }
    public long DisplayCadenceSlowFrameCount { get; init; }
    public double DisplayCadenceSlowFramePercent { get; init; }
    public bool BlankSuspected { get; init; }
    public bool StallSuspected { get; init; }
    public string RendererMode { get; init; } = "None";
    public string PreviewColorMetadata { get; init; } = "None";
    public long D3DFramesSubmitted { get; init; }
    public long D3DFramesRendered { get; init; }
    public long D3DFramesDropped { get; init; }
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
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
    public string PreviewColorMetadata { get; init; } = "None";
    public string SessionState { get; init; } = "Unknown";

    // Thread health probes
    public bool SourceReaderReadOutstanding { get; init; }
    public long SourceReaderReadOutstandingMs { get; init; }
    public long SourceReaderLastFrameTickMs { get; init; }
    public int SourceReaderFrameChannelDepth { get; init; }
    public long WasapiCaptureCallbackCount { get; init; }
    public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
    public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }

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

    public string? LastOutputPath { get; init; }
    public string LastFinalizeStatus { get; init; } = "None";
    public DateTimeOffset? LastFinalizeUtc { get; init; }
    public IReadOnlyList<string> LastPreservedArtifacts { get; init; } = Array.Empty<string>();
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
