using System;

namespace ElgatoCapture.Models;

public sealed class PreviewFrame
{
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public required int Stride { get; init; }
    public required string PixelFormat { get; init; }
    public required byte[] Buffer { get; init; }
    public required long FrameIndex { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
}
