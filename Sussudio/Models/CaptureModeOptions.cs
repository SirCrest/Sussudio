using System;

namespace ElgatoCapture.Models;

public sealed class ResolutionOption
{
    public required string Value { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public bool IsEnabled { get; init; }
    public string DisableReason { get; init; } = string.Empty;
    public string? DisplayTextOverride { get; init; }
    public string DisplayText => string.IsNullOrWhiteSpace(DisplayTextOverride) ? Value : DisplayTextOverride;
}

public sealed class FrameRateOption
{
    public required double FriendlyValue { get; init; }
    public required double Value { get; init; }
    public string Rational { get; init; } = string.Empty;
    public uint? Numerator { get; init; }
    public uint? Denominator { get; init; }
    public bool IsEnabled { get; init; }
    public string DisableReason { get; init; } = string.Empty;
    public string? DisplayTextOverride { get; init; }
    public string DisplayText => string.IsNullOrWhiteSpace(DisplayTextOverride)
        ? $"{Math.Round(FriendlyValue):0}"
        : DisplayTextOverride;
}
