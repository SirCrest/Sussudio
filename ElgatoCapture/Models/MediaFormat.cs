using System;

namespace ElgatoCapture.Models;

public class MediaFormat
{
    public uint Width { get; set; }
    public uint Height { get; set; }
    public double FrameRate { get; set; }
    public string PixelFormat { get; set; } = string.Empty;
    public bool IsHdr { get; set; }

    public string DisplayName => $"{Width}x{Height} @ {FrameRate:F0}fps{(IsHdr ? " (HDR)" : "")}";

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj)
    {
        if (obj is MediaFormat other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   Math.Abs(FrameRate - other.FrameRate) < 0.1 &&
                   PixelFormat == other.PixelFormat &&
                   IsHdr == other.IsHdr;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Width, Height, FrameRate, PixelFormat, IsHdr);

    public static int GetPixelFormatPriority(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return 100;
        }

        if (pixelFormat.Equals("YUY2", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (pixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
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

        if (pixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase) ||
            pixelFormat.Contains("HDR", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        return 10;
    }
}
