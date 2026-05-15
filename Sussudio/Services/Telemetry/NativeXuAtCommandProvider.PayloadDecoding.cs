using System;
using System.Globalization;
using System.Text;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static int? ExtractInt32AsVicCode(byte[] buffer)
    {
        if (buffer.Length < 4 || !HasNonZeroData(buffer))
        {
            return null;
        }

        var value = BitConverter.ToInt32(buffer, 0);
        return value > 0 ? value : null;
    }

    private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)
    {
        if (buffer.Length < 8 || !HasNonZeroData(buffer) || buffer[0] != 0x82)
        {
            return AviInfoFrameInfo.Empty;
        }

        var db1 = buffer[4];
        var db2 = buffer[5];
        var db3 = buffer[6];

        var colorSpace = ((db1 >> 5) & 0x03) switch
        {
            0 => "RGB",
            1 => "YCbCr422",
            2 => "YCbCr444",
            3 => "YCbCr420",
            _ => null
        };

        var colorimetry = ((db2 >> 6) & 0x03) switch
        {
            0 => null,
            1 => "BT.601",
            2 => "BT.709",
            3 => ((db3 >> 4) & 0x07) switch
            {
                0 => "xvYCC601",
                1 => "xvYCC709",
                2 => "sYCC601",
                3 => "AdobeYCC601",
                4 => "AdobeRGB",
                5 => "BT.2020cYCC",
                6 => "BT.2020",
                7 => "Reserved",
                _ => null
            },
            _ => null
        };

        var quantization = ((db3 >> 2) & 0x03) switch
        {
            0 => "Default",
            1 => "Limited",
            2 => "Full",
            _ => "Reserved"
        };

        return new AviInfoFrameInfo(true, colorSpace, colorimetry, quantization);
    }

    private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)
    {
        if (buffer.Length < 4 || !HasNonZeroData(buffer) || buffer[0] != 0x87)
        {
            return new HdrMetadataInfo(false, null, null);
        }

        var eotf = buffer[3];
        var isHdr = eotf switch
        {
            2 or 3 => true,
            0 or 1 => false,
            _ => (bool?)null
        };
        return new HdrMetadataInfo(true, eotf, isHdr);
    }

    private static double SnapToCanonicalFrameRate(double measured)
    {
        const double tolerance = 0.05;
        foreach (var canonical in CanonicalFrameRates)
        {
            if (Math.Abs(measured - canonical) <= tolerance)
            {
                return canonical;
            }
        }

        return measured;
    }

    private static string? InferFrameRateRational(double? frameRate)
    {
        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return null;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01 && rounded > 0)
        {
            return $"{(int)rounded}/1";
        }

        if (rounded > 0)
        {
            var ntscCandidate = rounded * 1000.0 / 1001.0;
            if (Math.Abs(value - ntscCandidate) <= 0.03)
            {
                return $"{(int)rounded * 1000}/1001";
            }
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static SourceTelemetryConfidence ResolveConfidence(
        bool hasVicCode,
        HdrMetadataInfo hdrInfo,
        AviInfoFrameInfo aviInfoFrame,
        double? frameRateExact)
    {
        if (hasVicCode && hdrInfo.HasMetadata)
        {
            return SourceTelemetryConfidence.High;
        }

        if (hasVicCode)
        {
            return SourceTelemetryConfidence.Medium;
        }

        if (aviInfoFrame.HasData || hdrInfo.HasMetadata || frameRateExact.HasValue)
        {
            return SourceTelemetryConfidence.Low;
        }

        return SourceTelemetryConfidence.Unknown;
    }

    private static bool TryReadInt32(byte[] buffer, out int value)
    {
        value = 0;
        if (buffer.Length < 4)
        {
            return false;
        }

        value = BitConverter.ToInt32(buffer, 0);
        return true;
    }

    private static int? TryReadInt16(byte[] buffer)
        => buffer.Length >= 2 ? BitConverter.ToInt16(buffer, 0) : null;

    private static bool? TryReadBoolean(byte[] buffer)
        => buffer.Length >= 1 ? buffer[0] != 0 : null;

    private static string? TryDecodePrintableAscii(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, (byte)0);
        if (terminatorIndex < 0)
        {
            terminatorIndex = buffer.Length;
        }

        if (terminatorIndex == 0)
        {
            return null;
        }

        for (var i = 0; i < terminatorIndex; i++)
        {
            var value = buffer[i];
            if (value < 0x20 || value > 0x7E)
            {
                return null;
            }
        }

        var decoded = Encoding.ASCII.GetString(buffer, 0, terminatorIndex).Trim();
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string? DecodeCString(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, (byte)0);
        if (terminatorIndex < 0)
        {
            terminatorIndex = buffer.Length;
        }

        var decoded = Encoding.ASCII.GetString(buffer, 0, terminatorIndex).Trim();
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string BoolToToken(bool? value)
        => value switch
        {
            true => "true",
            false => "false",
            _ => "unknown"
        };

    private static bool HasNonZeroData(byte[] buffer)
        => buffer.AsSpan().IndexOfAnyExcept((byte)0) >= 0;
}
