using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

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
