using System;
using System.Collections.Generic;
using System.Globalization;
using Sussudio.Models;

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
