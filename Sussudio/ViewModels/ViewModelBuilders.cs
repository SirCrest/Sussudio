using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

internal static class OutputDriveSpacePresentationBuilder
{
    internal static string Build(string outputPath)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(outputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            return $"Free: {freeGb:F1} GB";
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}");
            return "";
        }
    }
}

internal static class LiveSignalTextPresentationBuilder
{
    internal static LiveSignalTextPresentation Build(
        CaptureRuntimeSnapshot runtime,
        string? encoderCodecName,
        string unavailableText)
    {
        var width = runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth;
        var height = runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight;
        var resolution = width.HasValue && height.HasValue
            ? $"{width.Value}x{height.Value}"
            : unavailableText;

        var frameRateValue = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate;
        var frameRate = frameRateValue.HasValue && frameRateValue.Value > 0
            ? frameRateValue.Value.ToString("0.00")
            : unavailableText;

        var pixelFormat =
            runtime.ReaderSourceSubtype ??
            runtime.VideoNegotiatedSubtype ??
            runtime.NegotiatedPixelFormat ??
            runtime.LatestObservedFramePixelFormat ??
            runtime.RequestedReaderSubtype ??
            runtime.RequestedPixelFormat;
        var codecSuffix = encoderCodecName switch
        {
            "hevc_nvenc" => " / HEVC",
            "h264_nvenc" => " / H264",
            "av1_nvenc" => " / AV1",
            _ => ""
        };
        var pixelFormatText = string.IsNullOrWhiteSpace(pixelFormat)
            ? unavailableText
            : pixelFormat + codecSuffix;

        return new LiveSignalTextPresentation(resolution, frameRate, pixelFormatText);
    }
}

internal readonly record struct LiveSignalTextPresentation(
    string Resolution,
    string FrameRate,
    string PixelFormat);

internal static class SourceTelemetryPresentationBuilder
{
    internal static string BuildSourceSummary(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)
    {
        if (!snapshot.HasSignalData &&
            snapshot.Availability is SourceTelemetryAvailability.Unavailable or SourceTelemetryAvailability.Unknown)
        {
            return "Source: waiting for signal telemetry";
        }

        var resolution = snapshot.HasDimensions
            ? $"{snapshot.Width}x{snapshot.Height}"
            : "?x?";
        var fps = snapshot.FrameRateArg ??
                  snapshot.FrameRateExact?.ToString("0.###") ??
                  "?";
        var hdr = snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? "HDR" : "SDR") : "HDR?";
        var ageText = BuildAgeText(snapshot.TimestampUtc, nowUtc);
        return $"Source: {resolution} @ {fps} | {hdr} | {snapshot.Availability}/{snapshot.Confidence} | {ageText}";
    }

    internal static string BuildAgeText(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        var ageSeconds = TelemetryAgeHelper.ComputeAgeSeconds(timestampUtc, nowUtc);
        if (!ageSeconds.HasValue)
        {
            return "updated ?";
        }

        return ageSeconds.Value <= 0
            ? "updated now"
            : $"updated {ageSeconds.Value}s ago";
    }

    internal static string BuildTargetSummary(
        string resolutionDisplayText,
        double selectedFrameRate,
        double? selectedFriendlyFrameRate,
        double? selectedExactFrameRate,
        string? selectedExactFrameRateArg,
        string? hdrRuntimeState)
    {
        var friendly = selectedFriendlyFrameRate ?? Math.Round(selectedFrameRate);
        var exact = selectedExactFrameRate ?? selectedFrameRate;
        var exactText = !string.IsNullOrWhiteSpace(selectedExactFrameRateArg)
            ? selectedExactFrameRateArg
            : exact > 0
                ? exact.ToString("0.###")
                : "?";
        var hdrStateText = string.IsNullOrWhiteSpace(hdrRuntimeState) ? "Unknown" : hdrRuntimeState;
        return $"Target: {resolutionDisplayText} @ {friendly:0} (exact {exactText}) | HDR={hdrStateText}";
    }
}

internal static class AutomationOptionsSnapshotBuilder
{
    internal static AutomationOptionsSnapshot Build(AutomationOptionsSnapshotInput input)
    {
        var clampedDecoderCount = Math.Clamp(input.MjpegDecoderCount, 1, 8);

        return new AutomationOptionsSnapshot
        {
            TimestampUtc = input.TimestampUtc,
            Devices = input.Devices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, input.SelectedDeviceId, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AudioInputDevices = input.AudioInputDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, input.SelectedAudioInputDeviceId, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            Resolutions = input.Resolutions
                .Select(option => new AutomationResolutionOption
                {
                    Value = option.Value,
                    Width = (int)option.Width,
                    Height = (int)option.Height,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = string.Equals(option.Value, input.SelectedResolution, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            FrameRates = input.FrameRates
                .Select(option => new AutomationFrameRateOption
                {
                    Value = option.Value,
                    FriendlyValue = option.FriendlyValue,
                    ExactValueArg = option.ExactValueArg ?? string.Empty,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = option.IsSelected
                })
                .ToArray(),
            RecordingFormats = BuildStringOptions(input.RecordingFormats, input.SelectedRecordingFormat),
            Qualities = BuildStringOptions(input.Qualities, input.SelectedQuality),
            Presets = BuildStringOptions(input.Presets, input.SelectedPreset),
            SplitEncodeModes = BuildStringOptions(input.SplitEncodeModes, input.SelectedSplitEncodeMode),
            VideoFormats = BuildStringOptions(input.VideoFormats, input.SelectedVideoFormat),
            MjpegDecoderCounts = Enumerable.Range(1, 8)
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == clampedDecoderCount
                })
                .ToArray(),
            SelectedDeviceId = input.SelectedDeviceId,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            SelectedResolution = input.SelectedResolution,
            SelectedFrameRate = input.SelectedFrameRate,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            SelectedVideoFormat = input.SelectedVideoFormat,
            MjpegDecoderCount = clampedDecoderCount,
            PreviewVolumePercent = input.PreviewVolume * 100.0,
            IsStatsVisible = input.IsStatsVisible
        };
    }

    private static AutomationStringOption[] BuildStringOptions(
        string[] values,
        string selectedValue)
    {
        return values
            .Select(value => new AutomationStringOption
            {
                Value = value,
                Label = value,
                IsEnabled = true,
                DisableReason = string.Empty,
                IsSelected = string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)
            })
            .ToArray();
    }
}

internal sealed class AutomationOptionsSnapshotInput
{
    public DateTimeOffset TimestampUtc { get; init; }
    public AutomationOptionsDeviceInput[] Devices { get; init; } = Array.Empty<AutomationOptionsDeviceInput>();
    public AutomationOptionsDeviceInput[] AudioInputDevices { get; init; } = Array.Empty<AutomationOptionsDeviceInput>();
    public AutomationOptionsResolutionInput[] Resolutions { get; init; } = Array.Empty<AutomationOptionsResolutionInput>();
    public AutomationOptionsFrameRateInput[] FrameRates { get; init; } = Array.Empty<AutomationOptionsFrameRateInput>();
    public string[] RecordingFormats { get; init; } = Array.Empty<string>();
    public string[] Qualities { get; init; } = Array.Empty<string>();
    public string[] Presets { get; init; } = Array.Empty<string>();
    public string[] SplitEncodeModes { get; init; } = Array.Empty<string>();
    public string[] VideoFormats { get; init; } = Array.Empty<string>();
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
    public double PreviewVolume { get; init; }
    public bool IsStatsVisible { get; init; }
}

internal sealed class AutomationOptionsDeviceInput
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

internal sealed class AutomationOptionsResolutionInput
{
    public string Value { get; init; } = string.Empty;
    public uint Width { get; init; }
    public uint Height { get; init; }
    public bool IsEnabled { get; init; }
    public string? DisableReason { get; init; }
}

internal sealed class AutomationOptionsFrameRateInput
{
    public double Value { get; init; }
    public double FriendlyValue { get; init; }
    public string? ExactValueArg { get; init; }
    public bool IsEnabled { get; init; }
    public string? DisableReason { get; init; }
    public bool IsSelected { get; init; }
}

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
