using System;

namespace Sussudio.Models;

public sealed class PreviewFrameCaptureResult
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int CapturedWidth { get; init; }
    public int CapturedHeight { get; init; }
    public string RendererMode { get; init; } = "Unknown";
    public double AverageR { get; init; }
    public double AverageG { get; init; }
    public double AverageB { get; init; }
    public double AverageLuminance { get; init; }
    public double MinLuminance { get; init; }
    public double MaxLuminance { get; init; }
    public double NearBlackPercent { get; init; }
    public double NearWhitePercent { get; init; }
    public double PureBlackPercent { get; init; }
    public int LetterboxTopRows { get; init; }
    public int LetterboxBottomRows { get; init; }
    public int PillarboxLeftCols { get; init; }
    public int PillarboxRightCols { get; init; }
    public int ContentWidth { get; init; }
    public int ContentHeight { get; init; }
    public double ContentAspectRatio { get; init; }
    public int[] LuminanceHistogram { get; init; } = Array.Empty<int>();
    public long TotalPixels { get; init; }
}

public sealed class WindowScreenshotResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int CapturedWidth { get; init; }
    public int CapturedHeight { get; init; }
    public long FileSizeBytes { get; init; }
}
