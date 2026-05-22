using System;
using System.Collections.Generic;

namespace Sussudio.Models;

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
