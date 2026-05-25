using System;
using System.Collections.Generic;

namespace Sussudio.Models;

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
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
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

public enum PreviewStartupStrategy
{
    None,
    GpuMediaSourceNoFrameReader,
    GpuMediaSourceWithFrameReader,
    CpuSoftwareBitmap,
    DirectShow,
    D3D11VideoProcessor
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

    // Luma (Y plane) analysis from the preview adapter.
    public int? LumaMin { get; init; }
    public int? LumaMax { get; init; }
    public double? LumaMean { get; init; }
    public int? LumaBelow16Count { get; init; }
    public int? LumaAbove235Count { get; init; }
    public int? LumaSampleCount { get; init; }

    // Raw MF properties dump.
    public IReadOnlyDictionary<string, string> FormatProperties { get; init; } = new Dictionary<string, string>();

    // D3D11 Video Processor color spaces.
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
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
    public long CaptureCommandCommandsCoalesced { get; init; }
    public int CaptureCommandPendingCommands { get; init; }
    public int CaptureCommandMaxPendingCommands { get; init; }
    public long CaptureCommandOldestPendingCommandAgeMs { get; init; }
    public long CaptureCommandLastQueueLatencyMs { get; init; }
    public long CaptureCommandMaxQueueLatencyMs { get; init; }
    public string CaptureCommandLastCommand { get; init; } = "None";
    public string CaptureCommandLastOutcome { get; init; } = "None";
    public string CaptureCommandLastCorrelationId { get; init; } = string.Empty;
    public string CaptureCommandLastError { get; init; } = string.Empty;
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public double CustomBitrateMbps { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
    public bool IsHdrAvailable { get; init; }
    public bool IsHdrEnabled { get; init; }
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string LiveResolution { get; init; } = "—";
    public string LiveFrameRate { get; init; } = "—";
    public string LivePixelFormat { get; init; } = "—";
    public string OutputPath { get; init; } = string.Empty;
    public string RecordingTime { get; init; } = string.Empty;
    public string RecordingSizeInfo { get; init; } = string.Empty;
    public string RecordingBitrateInfo { get; init; } = string.Empty;
    public double AudioPeak { get; init; }
    public bool AudioClipping { get; init; }
}
