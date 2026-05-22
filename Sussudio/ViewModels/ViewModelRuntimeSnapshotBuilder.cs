using System;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

internal static class ViewModelRuntimeSnapshotBuilder
{
    internal static ViewModelRuntimeSnapshot Build(ViewModelRuntimeSnapshotInput input)
    {
        var sessionSnapshot = input.SessionSnapshot;

        return new ViewModelRuntimeSnapshot
        {
            TimestampUtc = input.TimestampUtc,
            IsInitialized = input.IsInitialized,
            IsPreviewing = input.IsPreviewing,
            IsRecording = input.IsRecording,
            IsAudioEnabled = input.IsAudioEnabled,
            IsAudioPreviewEnabled = input.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = input.IsCustomAudioInputEnabled,
            StatusText = input.StatusText,
            SelectedDeviceId = input.SelectedDeviceId,
            SelectedDeviceName = input.SelectedDeviceName,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = input.SelectedAudioInputDeviceName,
            SelectedResolution = input.SelectedResolution,
            SelectedFrameRate = input.SelectedFrameRate,
            SelectedFriendlyFrameRate = input.SelectedFriendlyFrameRate,
            SelectedExactFrameRate = input.SelectedExactFrameRate,
            SelectedExactFrameRateArg = input.SelectedExactFrameRateArg,
            DisabledResolutionReason = input.DisabledResolutionReason,
            DisabledFrameRateReason = input.DisabledFrameRateReason,
            HdrResolutionSupportHint = input.HdrResolutionSupportHint,
            DetectedSourceFrameRate = input.DetectedSourceFrameRate,
            DetectedSourceFrameRateArg = input.DetectedSourceFrameRateArg,
            SourceFrameRateOrigin = input.SourceFrameRateOrigin,
            SourceWidth = input.SourceWidth,
            SourceHeight = input.SourceHeight,
            SourceIsHdr = input.SourceIsHdr,
            SourceTelemetryAvailability = input.SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = input.SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = input.SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = input.SourceTelemetryDiagnosticSummary,
            SourceTelemetryTimestampUtc = input.SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(input.SourceTelemetryTimestampUtc, input.TimestampUtc),
            SourceTelemetrySummaryText = input.SourceTelemetrySummaryText,
            SourceTargetSummaryText = input.SourceTargetSummaryText,
            CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,
            CaptureCommandCommandsCompleted = sessionSnapshot.CommandsCompleted,
            CaptureCommandCommandsFailed = sessionSnapshot.CommandsFailed,
            CaptureCommandCommandsCanceled = sessionSnapshot.CommandsCanceled,
            CaptureCommandCommandsCoalesced = sessionSnapshot.CommandsCoalesced,
            CaptureCommandPendingCommands = sessionSnapshot.PendingCommands,
            CaptureCommandMaxPendingCommands = sessionSnapshot.MaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = sessionSnapshot.OldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = sessionSnapshot.LastCommandQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = sessionSnapshot.MaxCommandQueueLatencyMs,
            CaptureCommandLastCommand = sessionSnapshot.LastCommand?.ToString() ?? "None",
            CaptureCommandLastOutcome = sessionSnapshot.LastOutcome.ToString(),
            CaptureCommandLastCorrelationId = sessionSnapshot.LastCorrelationId ?? string.Empty,
            CaptureCommandLastError = sessionSnapshot.LastError ?? string.Empty,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            SelectedVideoFormat = input.SelectedVideoFormat,
            CustomBitrateMbps = input.CustomBitrateMbps,
            PreviewVolumePercent = input.PreviewVolume * 100.0,
            IsStatsVisible = input.IsStatsVisible,
            IsHdrAvailable = input.IsHdrAvailable,
            IsHdrEnabled = input.IsHdrEnabled,
            HdrRuntimeState = input.HdrRuntimeState,
            HdrReadinessReason = input.HdrReadinessReason,
            LiveResolution = input.LiveResolution,
            LiveFrameRate = input.LiveFrameRate,
            LivePixelFormat = input.LivePixelFormat,
            OutputPath = input.OutputPath,
            RecordingTime = input.RecordingTime,
            RecordingSizeInfo = input.RecordingSizeInfo,
            RecordingBitrateInfo = input.RecordingBitrateInfo,
            AudioPeak = input.AudioPeak,
            AudioClipping = input.AudioClipping
        };
    }
}

internal sealed class ViewModelRuntimeSnapshotInput
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public CaptureSessionSnapshot SessionSnapshot { get; init; } = new();
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
    public string SourceTelemetrySummaryText { get; init; } = string.Empty;
    public string SourceTargetSummaryText { get; init; } = string.Empty;
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public double CustomBitrateMbps { get; init; }
    public double PreviewVolume { get; init; }
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
