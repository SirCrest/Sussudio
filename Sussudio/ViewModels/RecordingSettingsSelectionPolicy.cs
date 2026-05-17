using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

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
