using System;
using System.Collections.Generic;

namespace Sussudio.Models;

// Point-in-time automation view of app, capture, preview, recording, and
// Flashback health. The object is intentionally broad because diagnostic
// sessions persist it as evidence, not just as an RPC convenience DTO.
public sealed partial class AutomationSnapshot
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
    public string PreviewPacingLikelySlowStage { get; init; } = "Unknown";
    public string PreviewPacingSlowStageConfidence { get; init; } = "None";
    public string PreviewPacingSlowStageEvidence { get; init; } = "Preview pacing classification has not run.";
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
    public double PerformanceThresholdCaptureDropPercent { get; init; }
    public double PerformanceThresholdCaptureP95Multiplier { get; init; }
    public double PerformanceThresholdPreviewSlowPercent { get; init; }
    public double PerformanceThresholdVerificationDropPercent { get; init; }

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
}
