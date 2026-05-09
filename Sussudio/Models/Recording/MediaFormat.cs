using System;

namespace Sussudio.Models;

public class MediaFormat
{
    private static readonly string[] HdrSubtypeTokens =
    {
        "P010",
        "P016",
        "I010",
        "Y210",
        "Y410",
        "Y416",
        "R10G10B10",
        "XR10"
    };

    public uint Width { get; set; }
    public uint Height { get; set; }
    public double FrameRate { get; set; }
    public uint FrameRateNumerator { get; set; }
    public uint FrameRateDenominator { get; set; }
    public string PixelFormat { get; set; } = string.Empty;
    public bool IsHdr { get; set; }

    public double FrameRateExact
    {
        get
        {
            if (FrameRateNumerator > 0 && FrameRateDenominator > 0)
            {
                return (double)FrameRateNumerator / FrameRateDenominator;
            }

            return FrameRate;
        }
    }

    public string FrameRateRational =>
        FrameRateNumerator > 0 && FrameRateDenominator > 0
            ? $"{FrameRateNumerator}/{FrameRateDenominator}"
            : string.Empty;

    public string DisplayName
    {
        get
        {
            var fps = FrameRateExact;
            var rationalSuffix = string.IsNullOrWhiteSpace(FrameRateRational)
                ? string.Empty
                : $" ({FrameRateRational})";
            return $"{Width}x{Height} @ {fps:0.###}fps{rationalSuffix}{(IsHdr ? " (HDR)" : "")}";
        }
    }

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj)
    {
        if (obj is MediaFormat other)
        {
            var hasRational = FrameRateNumerator > 0 && FrameRateDenominator > 0;
            var otherHasRational = other.FrameRateNumerator > 0 && other.FrameRateDenominator > 0;
            var rationalMatches = hasRational && otherHasRational
                ? FrameRateNumerator == other.FrameRateNumerator &&
                  FrameRateDenominator == other.FrameRateDenominator
                : Math.Abs(FrameRateExact - other.FrameRateExact) < 0.01;

            return Width == other.Width &&
                   Height == other.Height &&
                   rationalMatches &&
                   PixelFormat == other.PixelFormat &&
                   IsHdr == other.IsHdr;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (FrameRateNumerator > 0 && FrameRateDenominator > 0)
        {
            return HashCode.Combine(
                Width,
                Height,
                FrameRateNumerator,
                FrameRateDenominator,
                PixelFormat,
                IsHdr);
        }

        return HashCode.Combine(Width, Height, Math.Round(FrameRateExact, 0), PixelFormat, IsHdr);
    }

    public static int GetPixelFormatPriority(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return 100;
        }

        if (pixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (pixelFormat.Equals("YUY2", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (pixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (pixelFormat.Equals("BGRA8", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Equals("RGB32", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (IsHdrPixelFormat(pixelFormat))
        {
            return 20;
        }

        return 10;
    }

    public static bool IsHdrPixelFormat(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return false;
        }

        foreach (var token in HdrSubtypeTokens)
        {
            if (pixelFormat.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return pixelFormat.Contains("BT2020", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("ST2084", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("HDR", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTrue10BitPixelFormat(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return false;
        }

        return pixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("P016", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("I010", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("Y210", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("Y410", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("Y416", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("R10G10B10", StringComparison.OrdinalIgnoreCase) ||
               pixelFormat.Contains("XR10", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps a <see cref="RecordingFormat"/> to the corresponding NVENC encoder codec name
    /// (e.g. "hevc_nvenc", "av1_nvenc", "h264_nvenc").
    /// </summary>
    public static string MapNvencCodecName(RecordingFormat format)
    {
        return format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => "av1_nvenc",
            _ => "h264_nvenc"
        };
    }
}
