using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sussudio.Models;

public enum SourceTelemetryAvailability
{
    Unknown,
    Available,
    Unavailable,
    Stale,
    Inconclusive
}

public enum SourceTelemetryOrigin
{
    Unknown,
    DeviceFormatFallback,
    NativeXu
}

public enum SourceTelemetryConfidence
{
    Unknown,
    Low,
    Medium,
    High
}

public enum SourceAudioInputAvailability
{
    Unknown,
    Unavailable,
    Inconclusive,
    Available
}

public enum SourceAudioInputMode
{
    Hdmi,
    Analog
}

public static class DeviceAudioMode
{
    public const string Hdmi = "HDMI";
    public const string Analog = "Analog";
}

public static class TelemetryLabels
{
    public const string GroupAudioInput = "Audio / Input";
    public const string AdcAnalog = "ADC (Analog)";
    public const string AnalogGain = "Analog Gain";
}

public sealed record SourceTelemetryDetailEntry(
    string Group,
    string Label,
    string DisplayValue,
    string? RawValue = null);

public sealed record SourceSignalTelemetrySnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public SourceTelemetryAvailability Availability { get; init; } = SourceTelemetryAvailability.Unknown;
    public SourceTelemetryOrigin Origin { get; init; } = SourceTelemetryOrigin.Unknown;
    public string OriginDetail { get; init; } = "Unknown";
    public SourceTelemetryConfidence Confidence { get; init; } = SourceTelemetryConfidence.Unknown;

    public int? Width { get; init; }
    public int? Height { get; init; }
    public double? FrameRateExact { get; init; }
    public string? FrameRateArg { get; init; }
    public bool? IsHdr { get; init; }
    public string? VideoFormat { get; init; }
    public string? Colorimetry { get; init; }
    public string? Quantization { get; init; }
    public string? HdrTransferFunction { get; init; }
    public int? HdrTransferCode { get; init; }
    public string? Firmware { get; init; }
    public string? AudioFormat { get; init; }
    public string? AudioSampleRate { get; init; }
    public string? InputSource { get; init; }
    public bool? AdcOnOff { get; init; }
    public int? AdcVolumeGain { get; init; }
    public int? AnalogGainByte { get; init; }
    public int? UacVolumeGain { get; init; }
    public bool? UacOut1Mute { get; init; }
    public bool? UacOut2Mute { get; init; }
    public int? UacOut2MixerSource { get; init; }
    public string? UsbHostProtocol { get; init; }
    public bool? TxEdidValid { get; init; }
    public string? HdcpMode { get; init; }
    public string? HdcpVersion { get; init; }
    public string? RxTxHdcpVersion { get; init; }
    public string? CustomerVersion { get; init; }
    public int? RescueVersion { get; init; }
    public string? RawTimingHex { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> DetailEntries { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();

    public string? DiagnosticSummary { get; init; }
    public string? EgavInitializeResultName { get; init; }
    public string? EgavOpenResultName { get; init; }
    public string? EgavSignalStatusResultName { get; init; }
    public string? EgavIsVideoHdrResultName { get; init; }

    public SourceAudioInputAvailability AudioInputAvailability { get; init; } = SourceAudioInputAvailability.Unavailable;
    public SourceAudioInputMode? AudioInputMode { get; init; }
    public string? AudioInputOrigin { get; init; } = "not-implemented";

    public bool HasDimensions => Width.HasValue && Height.HasValue && Width.Value > 0 && Height.Value > 0;
    public bool HasFrameRate => FrameRateExact.HasValue && FrameRateExact.Value > 0;
    public bool HasSignalData => HasDimensions && HasFrameRate;

    public string GetModeKey()
    {
        if (!HasSignalData)
        {
            return string.Empty;
        }

        var fpsToken = BuildCanonicalFrameRateToken(FrameRateExact, FrameRateArg);
        var hdrToken = IsHdr.HasValue ? (IsHdr.Value ? "hdr" : "sdr") : "unknown-hdr";
        return $"{Width}x{Height}@{fpsToken}:{hdrToken}";
    }

    private static string BuildCanonicalFrameRateToken(double? frameRateExact, string? frameRateArg)
    {
        if (frameRateExact.HasValue && frameRateExact.Value > 0)
        {
            return NormalizeFrameRateValue(frameRateExact.Value);
        }

        if (TryParseFrameRateRational(frameRateArg, out var numerator, out var denominator))
        {
            return $"{numerator}/{denominator}";
        }

        if (double.TryParse(frameRateArg, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            return NormalizeFrameRateValue(parsed);
        }

        return string.IsNullOrWhiteSpace(frameRateArg) ? "?" : frameRateArg.Trim();
    }

    private static string NormalizeFrameRateValue(double frameRate)
    {
        if (frameRate <= 0)
        {
            return "?";
        }

        var rounded = Math.Round(frameRate);
        if (rounded > 0 && Math.Abs(frameRate - rounded) <= 0.01)
        {
            return $"{(int)rounded}/1";
        }

        if (rounded > 0)
        {
            var ntscCandidate = rounded * 1000.0 / 1001.0;
            if (Math.Abs(frameRate - ntscCandidate) <= 0.03)
            {
                return $"{(int)rounded * 1000}/1001";
            }
        }

        return frameRate.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryParseFrameRateRational(string? raw, out int numerator, out int denominator)
    {
        numerator = 0;
        denominator = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out numerator) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out denominator))
        {
            return false;
        }

        if (numerator <= 0 || denominator <= 0)
        {
            return false;
        }

        var gcd = GreatestCommonDivisor(Math.Abs(numerator), Math.Abs(denominator));
        if (gcd > 1)
        {
            numerator /= gcd;
            denominator /= gcd;
        }

        return true;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return Math.Abs(a);
    }

    public static SourceSignalTelemetrySnapshot CreateUnavailable(string reason, string? detail = null)
        => new()
        {
            Availability = SourceTelemetryAvailability.Unavailable,
            Origin = SourceTelemetryOrigin.Unknown,
            OriginDetail = "Unavailable",
            Confidence = SourceTelemetryConfidence.Unknown,
            DiagnosticSummary = string.IsNullOrWhiteSpace(detail) ? reason : $"{reason}: {detail}"
        };
}
