using System;

namespace Sussudio.Controllers;

internal static class CaptureOptionTooltipFormatter
{
    public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)
    {
        resolutionHint = resolutionHint?.Trim();
        readinessHint = readinessHint?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (isRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }

        return string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint;
    }

    public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)
    {
        if (string.IsNullOrWhiteSpace(sourceTelemetrySummaryText))
        {
            return string.IsNullOrWhiteSpace(sourceTargetSummaryText) ? null : sourceTargetSummaryText;
        }

        if (string.IsNullOrWhiteSpace(sourceTargetSummaryText))
        {
            return sourceTelemetrySummaryText;
        }

        return $"{sourceTelemetrySummaryText}{Environment.NewLine}{sourceTargetSummaryText}";
    }
}
