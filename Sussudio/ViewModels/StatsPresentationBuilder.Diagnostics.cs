using System;
using System.Collections.Generic;
using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Automation;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(
        IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails,
        string? diagnosticSummary)
    {
        var rows = new List<StatsDiagnosticRowPresentation>();
        if (telemetryDetails.Count > 0)
        {
            var currentGroup = string.Empty;
            var alt = true;
            foreach (var detail in telemetryDetails)
            {
                var showHeader = !string.Equals(currentGroup, detail.Group, StringComparison.Ordinal);
                if (showHeader)
                {
                    currentGroup = detail.Group;
                    alt = true;
                }

                rows.Add(new StatsDiagnosticRowPresentation(
                    GroupHeader: showHeader ? currentGroup : null,
                    Label: detail.Label,
                    Value: detail.DisplayValue,
                    IsAlternate: alt));
                alt = !alt;
            }

            return new StatsDiagnosticRowsPresentation(IsEmpty: false, rows);
        }

        if (string.IsNullOrWhiteSpace(diagnosticSummary))
        {
            return new StatsDiagnosticRowsPresentation(IsEmpty: true, Array.Empty<StatsDiagnosticRowPresentation>());
        }

        var fallbackAlt = true;
        foreach (var (label, value) in ParseDiagnosticSummary(diagnosticSummary))
        {
            rows.Add(new StatsDiagnosticRowPresentation(
                GroupHeader: null,
                Label: label,
                Value: value,
                IsAlternate: fallbackAlt));
            fallbackAlt = !fallbackAlt;
        }

        return new StatsDiagnosticRowsPresentation(IsEmpty: false, rows);
    }

    public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool isRecording,
        double sourceDropPercent,
        double previewSlowPercent,
        long rendererSubmitted,
        long rendererDrops,
        int presentSampleCount)
    {
        if (!isPreviewing && !isRecording)
        {
            return new StatsDiagnosticSummary(
                HealthStatus: "Idle",
                LikelyStage: "diagnostic_unavailable",
                Evidence: "Start preview or recording to collect live frame-lane diagnostics.");
        }

        var sourceEvidence =
            $"source target={FormatFrameBudgetMs(health.ExpectedFrameRate)} avg={Sanitize(health.CaptureCadenceAverageIntervalMs):0.##}ms p95={Sanitize(health.CaptureCadenceP95IntervalMs):0.##}ms p99={Sanitize(health.CaptureCadenceP99IntervalMs):0.##}ms max={Sanitize(health.CaptureCadenceMaxIntervalMs):0.##}ms rate={Sanitize(health.CaptureCadenceObservedFps):0.##}/{Sanitize(health.ExpectedFrameRate):0.##}fps 1pctLow={Sanitize(health.CaptureCadenceOnePercentLowFps):0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({sourceDropPercent:0.###}%)";

        if (health.CaptureCadenceSampleCount < 30 || (isPreviewing && presentSampleCount == 0))
        {
            return new StatsDiagnosticSummary("WarmingUp", "diagnostic_unavailable", sourceEvidence);
        }

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            sourceDropPercent > 0.1)
        {
            return new StatsDiagnosticSummary("Warning", "source_capture", sourceEvidence);
        }

        if (health.MjpegDecodeFailures > 0 ||
            health.MjpegEmitFailures > 0 ||
            health.MjpegCompressedDropsQueueFull > 0 ||
            health.MjpegTotalDropped > 0)
        {
            var decodeEvidence =
                $"decode p95={Sanitize(health.MjpegDecodeP95Ms):0.##}ms callbackP95={Sanitize(health.MjpegCallbackP95Ms):0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures}";
            return new StatsDiagnosticSummary("Warning", "mjpeg_decode", decodeEvidence);
        }

        var previewQueueBelowTarget =
            health.MjpegPreviewJitterQueueDepth < health.MjpegPreviewJitterTargetDepth;
        if (previewQueueBelowTarget &&
            (health.MjpegPreviewJitterDeadlineDropCount > 0 ||
            health.MjpegPreviewJitterUnderflowCount > 3))
        {
            var previewEvidence =
                $"scheduler target={health.MjpegPreviewJitterTargetDepth} depth={health.MjpegPreviewJitterQueueDepth}/{health.MjpegPreviewJitterMaxDepth} deadlineDrops={health.MjpegPreviewJitterDeadlineDropCount} underflows={health.MjpegPreviewJitterUnderflowCount} resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount}";
            return new StatsDiagnosticSummary("Warning", "preview_scheduler", previewEvidence);
        }

        var rendererDropPercent = DiagnosticThresholds.CalculatePercent(rendererDrops, rendererSubmitted);
        if ((rendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples && rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||
            previewSlowPercent > 1.0)
        {
            var renderEvidence =
                $"render drops={rendererDrops} ({rendererDropPercent:0.###}%) slow={previewSlowPercent:0.##}%";
            return new StatsDiagnosticSummary("Warning", "renderer", renderEvidence);
        }

        return new StatsDiagnosticSummary("Healthy", "none", "All monitored frame lanes are within current thresholds.");
    }

    private static List<(string Label, string Value)> ParseDiagnosticSummary(string summary)
    {
        if (!summary.StartsWith("nativexu:", StringComparison.OrdinalIgnoreCase))
        {
            return new List<(string Label, string Value)>
            {
                ("Summary", summary.Trim())
            };
        }

        var result = new List<(string Label, string Value)>();
        var parts = summary.Split(':');

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var eqIndex = part.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = part[..eqIndex].Trim();
                var val = part[(eqIndex + 1)..].Trim();
                var label = key switch
                {
                    "vic" => "VIC Code",
                    "vfreq" => "Vert Freq",
                    "quant" => "Quantization",
                    "hdr2sdr" => "HDR to SDR",
                    "eotf" => "EOTF",
                    "fw" => "Firmware",
                    "audiofmt" => "Audio Format",
                    "audiosrate" => "Audio Sample Rate",
                    "inputsrc" => "Input Source",
                    "usbproto" => "USB Protocol",
                    "usbcdc" => "USB CDC",
                    "usblinkst" => "USB Link State",
                    "usbspeed" => "USB Speed",
                    "txhpd" => "TX Hot Plug",
                    "txvrr" => "TX VRR",
                    "uvctiming" => "UVC Timing",
                    "uvcfmt" => "UVC Format",
                    "uvcerr" => "UVC Error",
                    "hdcpmode" => "HDCP Mode",
                    "hdcpver" => "HDCP Version",
                    "rxtxhdcp" => "RX/TX HDCP",
                    "hdr2sdrext" => "HDR2SDR Status",
                    "hdr2sdrcolor" => "HDR2SDR Color",
                    "colorrangesetting" => "Color Range",
                    "vtem" => "VTEM (VRR)",
                    "biterr" => "Bit Errors",
                    "rawtiming" => "Raw Timing",
                    _ => key
                };
                result.Add((label, val));
                continue;
            }

            var entry = part switch
            {
                "nativexu" => ("Origin", "NativeXu"),
                "hdr" => ("HDR", "Yes"),
                "sdr" => ("HDR", "No"),
                "unknown" => ("HDR", "Unknown"),
                _ when part.Contains('x') && part.Length > 3 && char.IsDigit(part[0]) => ("Resolution", part),
                _ when double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) && fps > 0 =>
                    ("Frame Rate", $"{fps:0.##} Hz"),
                _ => ("Info", part)
            };
            result.Add(entry);
        }

        return result;
    }
}
