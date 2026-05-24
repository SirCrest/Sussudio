using System;
using System.Diagnostics;
using System.IO;
using Sussudio.Models;
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
