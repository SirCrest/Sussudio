using System;
using System.Collections.Generic;
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
            MicrophoneDevices = input.MicrophoneDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, input.SelectedMicrophoneDeviceId, StringComparison.OrdinalIgnoreCase)
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
            FlashbackBufferMinuteOptions = input.FlashbackBufferMinuteOptions
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == input.FlashbackBufferMinutes
                })
                .ToArray(),
            SelectedDeviceId = input.SelectedDeviceId,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            SelectedMicrophoneDeviceId = input.SelectedMicrophoneDeviceId,
            SelectedResolution = input.SelectedResolution,
            SelectedFrameRate = input.SelectedFrameRate,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            SelectedVideoFormat = input.SelectedVideoFormat,
            MjpegDecoderCount = clampedDecoderCount,
            PreviewVolumePercent = input.PreviewVolume * 100.0,
            IsMicrophoneEnabled = input.IsMicrophoneEnabled,
            MicrophoneVolumePercent = input.MicrophoneVolume,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            IsFlashbackEnabled = input.IsFlashbackEnabled,
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
    public AutomationOptionsDeviceInput[] MicrophoneDevices { get; init; } = Array.Empty<AutomationOptionsDeviceInput>();
    public AutomationOptionsResolutionInput[] Resolutions { get; init; } = Array.Empty<AutomationOptionsResolutionInput>();
    public AutomationOptionsFrameRateInput[] FrameRates { get; init; } = Array.Empty<AutomationOptionsFrameRateInput>();
    public string[] RecordingFormats { get; init; } = Array.Empty<string>();
    public string[] Qualities { get; init; } = Array.Empty<string>();
    public string[] Presets { get; init; } = Array.Empty<string>();
    public string[] SplitEncodeModes { get; init; } = Array.Empty<string>();
    public string[] VideoFormats { get; init; } = Array.Empty<string>();
    public int[] FlashbackBufferMinuteOptions { get; init; } = Array.Empty<int>();
    public string? SelectedDeviceId { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedMicrophoneDeviceId { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public int MjpegDecoderCount { get; init; }
    public double PreviewVolume { get; init; }
    public bool IsMicrophoneEnabled { get; init; }
    public double MicrophoneVolume { get; init; }
    public int FlashbackBufferMinutes { get; init; }
    public bool FlashbackGpuDecode { get; init; }
    public bool IsFlashbackEnabled { get; init; }
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
            CaptureSessionEpoch = sessionSnapshot.SessionGeneration,
            SourceTelemetryEpoch = input.SourceTelemetryEpoch,
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
    public long SourceTelemetryEpoch { get; init; }
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

internal static class RecordingSettingsSelectionPolicy
{
    /// <summary>
    /// H.264 is intentionally excluded from HDR recording: the nvenc H.264
    /// encoder has no 10-bit profile, so it cannot carry bt2020/PQ metadata.
    /// Only HEVC (Main 10) and AV1 (main profile, 10-bit) support HDR output.
    /// </summary>
    internal static bool IsHdrCompatible(string? format)
        => !string.IsNullOrWhiteSpace(format) &&
           (format.Contains("HEVC", StringComparison.OrdinalIgnoreCase) ||
            format.Contains("AV1", StringComparison.OrdinalIgnoreCase));

    internal static RecordingFormat ParseRecordingFormat(string? format)
    {
        return format switch
        {
            "HEVC" => RecordingFormat.HevcMp4,
            "AV1" => RecordingFormat.Av1Mp4,
            _ => RecordingFormat.H264Mp4
        };
    }

    internal static VideoQuality ParseVideoQuality(string? value)
    {
        return value switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Super High" => VideoQuality.SuperHigh,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };
    }

    internal static double ClampCustomBitrateMbps(double bitrateMbps)
        => Math.Clamp(bitrateMbps, 1, 300);

    internal static RecordingFormatSelection Select(
        IReadOnlyCollection<string> detectedFormats,
        IReadOnlyCollection<string> currentAvailableFormats,
        string? selectedFormat,
        bool isHdrEnabled,
        string defaultFormat,
        string hevcFormat,
        string av1Format)
    {
        var sourceFormats = detectedFormats.Count > 0
            ? detectedFormats.ToList()
            : currentAvailableFormats.ToList();
        if (sourceFormats.Count == 0)
        {
            sourceFormats.Add(defaultFormat);
        }

        var formats = isHdrEnabled
            ? sourceFormats.Where(IsHdrCompatible).ToList()
            : sourceFormats.ToList();
        if (formats.Count == 0 && currentAvailableFormats.Count > 0)
        {
            // Keep the last known real formats visible if capability refresh temporarily produced none.
            formats = currentAvailableFormats.ToList();
        }

        var targetFormat = isHdrEnabled
            ? SelectHdrFormat(formats, selectedFormat, hevcFormat, av1Format)
            : SelectSdrFormat(formats, selectedFormat);

        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            targetFormat = defaultFormat;
        }

        return new RecordingFormatSelection(formats, targetFormat);
    }

    private static string? SelectHdrFormat(IReadOnlyCollection<string> formats, string? selectedFormat, string hevcFormat, string av1Format)
    {
        if (!string.IsNullOrWhiteSpace(selectedFormat) &&
            formats.Any(format => string.Equals(format, selectedFormat, StringComparison.OrdinalIgnoreCase)) &&
            IsHdrCompatible(selectedFormat))
        {
            return selectedFormat;
        }

        return formats.FirstOrDefault(format =>
            string.Equals(format, hevcFormat, StringComparison.OrdinalIgnoreCase))
            ?? formats.FirstOrDefault(format =>
                string.Equals(format, av1Format, StringComparison.OrdinalIgnoreCase))
            ?? formats.FirstOrDefault();
    }

    private static string? SelectSdrFormat(IReadOnlyCollection<string> formats, string? selectedFormat)
    {
        if (!string.IsNullOrWhiteSpace(selectedFormat) &&
            formats.Any(format => string.Equals(format, selectedFormat, StringComparison.OrdinalIgnoreCase)))
        {
            return selectedFormat;
        }

        return formats.FirstOrDefault(format =>
            format.Contains("H.264", StringComparison.OrdinalIgnoreCase) ||
            format.Contains("H264", StringComparison.OrdinalIgnoreCase))
            ?? formats.FirstOrDefault();
    }
}

internal sealed record RecordingFormatSelection(IReadOnlyList<string> AvailableFormats, string SelectedFormat);

internal static class CaptureSettingsProjectionBuilder
{
    public static CaptureSettings Build(CaptureSettingsProjectionInput input)
    {
        var frameRateProjection = ProjectFrameRate(input);
        var settings = new CaptureSettings
        {
            Width = input.EffectiveResolutionKnown ? input.EffectiveWidth : (input.SelectedFormat?.Width ?? 1920),
            Height = input.EffectiveResolutionKnown ? input.EffectiveHeight : (input.SelectedFormat?.Height ?? 1080),
            FrameRate = frameRateProjection.EffectiveFrameRate,
            RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,
            RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,
            RequestedPixelFormat = ResolveRequestedPixelFormat(input),
            ForceMjpegDecode = ShouldForceMjpegDecode(input),
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
            Format = RecordingSettingsSelectionPolicy.ParseRecordingFormat(input.SelectedRecordingFormat),
            Quality = RecordingSettingsSelectionPolicy.ParseVideoQuality(input.SelectedQuality),
            NvencPreset = NvencPresetParser.Parse(input.SelectedPreset),
            SplitEncodeMode = SplitEncodeModeParser.Parse(input.SelectedSplitEncodeMode),
            CustomBitrateMbps = input.CustomBitrateMbps,
            HdrEnabled = input.IsHdrEnabled,
            HdrOutputMode = input.IsHdrEnabled ? HdrOutputMode.Hdr10Pq : HdrOutputMode.Off,
            PreviewMode = input.IsTrueHdrPreviewEnabled ? PreviewMode.TrueHdr : PreviewMode.GpuFast,
            OutputPath = input.OutputPath,
            AudioEnabled = input.IsAudioEnabled,
            MjpegDecoderCount = Math.Clamp(input.MjpegDecoderCount, 1, 8)
        };

        settings.UseCustomAudioInput = input.IsCustomAudioInputEnabled;
        if (input.IsCustomAudioInputEnabled && input.SelectedAudioInputDeviceId != null)
        {
            settings.AudioDeviceId = input.SelectedAudioInputDeviceId;
            settings.AudioDeviceName = input.SelectedAudioInputDeviceName;
        }

        settings.MicrophoneEnabled = input.IsMicrophoneEnabled;
        if (input.IsMicrophoneEnabled && input.SelectedMicrophoneDeviceId != null)
        {
            settings.MicrophoneDeviceId = input.SelectedMicrophoneDeviceId;
            settings.MicrophoneDeviceName = input.SelectedMicrophoneDeviceName;
        }

        return settings;
    }

    private static CaptureSettingsFrameRateProjection ProjectFrameRate(CaptureSettingsProjectionInput input)
    {
        var selectedFrameRateOption = input.AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, input.SelectedFrameRate))
            ?? input.AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, input.SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = input.IsAutoResolutionSelected && input.AutoResolvedFrameRate.HasValue && input.AutoResolvedFrameRate.Value > 0
            ? input.AutoResolvedFrameRate.Value
            : input.SelectedFrameRate > 0
            ? input.SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? input.SelectedFormat?.FrameRateExact
                ?? 60;
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = input.Runtime.ActualFrameRate ?? input.Runtime.NegotiatedFrameRate;
        var runtimeRateArg = input.Runtime.ActualFrameRateArg ?? input.Runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (input.EffectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (input.Runtime.ActualWidth == input.EffectiveWidth && input.Runtime.ActualHeight == input.EffectiveHeight) ||
                (input.Runtime.NegotiatedWidth == input.EffectiveWidth && input.Runtime.NegotiatedHeight == input.EffectiveHeight);
        }

        if (runtimeMatchesResolution &&
            runtimeRate.HasValue &&
            runtimeRate.Value > 0 &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(selectedFriendlyRate, runtimeRate.Value))
        {
            if (!string.IsNullOrWhiteSpace(runtimeRateArg))
            {
                requestedFrameRateArg = runtimeRateArg;
            }

            if (input.Runtime.NegotiatedFrameRateNumerator.HasValue &&
                input.Runtime.NegotiatedFrameRateDenominator.HasValue &&
                input.Runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = input.Runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = input.Runtime.NegotiatedFrameRateDenominator;
            }
            else if (FrameRateTimingPolicy.TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (input.SourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(selectedFriendlyRate, input.SourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(input.SourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = input.SourceTelemetry.FrameRateArg;
            }

            if (FrameRateTimingPolicy.TryParseFrameRateRational(input.SourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
            {
                requestedFrameRateNumerator = sourceNumerator;
                requestedFrameRateDenominator = sourceDenominator;
            }
        }

        if ((requestedFrameRateNumerator == null || requestedFrameRateDenominator == null) &&
            FrameRateTimingPolicy.TryParseFrameRateRational(requestedFrameRateArg, out var parsedNumerator, out var parsedDenominator))
        {
            requestedFrameRateNumerator = parsedNumerator;
            requestedFrameRateDenominator = parsedDenominator;
        }

        if (requestedFrameRateNumerator == null || requestedFrameRateDenominator == null)
        {
            if (input.SelectedFormat?.FrameRateNumerator > 0 && input.SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = input.SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = input.SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = input.SelectedFormat.FrameRateRational;
            }
            else
            {
                requestedFrameRateArg = effectiveFrameRate.ToString("0.###");
            }
        }

        return new CaptureSettingsFrameRateProjection(
            effectiveFrameRate,
            requestedFrameRateArg,
            requestedFrameRateNumerator,
            requestedFrameRateDenominator);
    }

    private static string? ResolveRequestedPixelFormat(CaptureSettingsProjectionInput input)
    {
        if (!string.Equals(input.SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return input.SelectedVideoFormat;
        }

        var format = input.SelectedFormat;
        if (format != null &&
            !input.IsHdrEnabled &&
            format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100)
        {
            return "MJPG";
        }

        return format?.PixelFormat;
    }

    private static bool ShouldForceMjpegDecode(CaptureSettingsProjectionInput input)
    {
        if (string.Equals(input.SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(input.SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            var format = input.SelectedFormat;
            return format != null &&
                   !input.IsHdrEnabled &&
                   format.Width >= 3840 &&
                   format.Height >= 2160 &&
                   format.FrameRateExact >= 100;
        }

        return false;
    }
}

internal sealed class CaptureSettingsProjectionInput
{
    public bool EffectiveResolutionKnown { get; init; }
    public uint EffectiveWidth { get; init; }
    public uint EffectiveHeight { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public double? AutoResolvedFrameRate { get; init; }
    public bool IsAutoResolutionSelected { get; init; }
    public MediaFormat? SelectedFormat { get; init; }
    public IReadOnlyList<FrameRateOption> AvailableFrameRates { get; init; } = Array.Empty<FrameRateOption>();
    public CaptureRuntimeSnapshot Runtime { get; init; } = new();
    public SourceSignalTelemetrySnapshot SourceTelemetry { get; init; } = new();
    public string? SelectedVideoFormat { get; init; }
    public bool IsHdrEnabled { get; init; }
    public bool IsTrueHdrPreviewEnabled { get; init; }
    public int MjpegDecoderCount { get; init; }
    public string? SelectedRecordingFormat { get; init; }
    public string? SelectedQuality { get; init; }
    public string? SelectedPreset { get; init; }
    public string? SelectedSplitEncodeMode { get; init; }
    public double CustomBitrateMbps { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public bool FlashbackGpuDecode { get; init; }
    public int FlashbackBufferMinutes { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsCustomAudioInputEnabled { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedAudioInputDeviceName { get; init; }
    public bool IsMicrophoneEnabled { get; init; }
    public string? SelectedMicrophoneDeviceId { get; init; }
    public string? SelectedMicrophoneDeviceName { get; init; }
}

internal readonly record struct CaptureSettingsFrameRateProjection(
    double EffectiveFrameRate,
    string? RequestedFrameRateArg,
    uint? RequestedFrameRateNumerator,
    uint? RequestedFrameRateDenominator);
