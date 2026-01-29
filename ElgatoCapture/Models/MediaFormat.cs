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
}
