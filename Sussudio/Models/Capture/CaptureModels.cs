using System;
using System.Collections.ObjectModel;

namespace Sussudio.Models;

// Capture device option returned by Media Foundation enumeration.
public class CaptureDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeXuInterfacePath { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public bool IsHdrCapable { get; set; }
    public ObservableCollection<MediaFormat> SupportedFormats { get; set; } = new();

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Device" : Name;

    public override string ToString() => DisplayName;
}

// Resolution choice shown in the settings shelf.
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

// Frame-rate choice shown in the settings shelf. FriendlyValue is the rounded
// UI bucket, while Value/Rational carry the exact capture timing.
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

// Coarse capture lifecycle state surfaced to UI and automation snapshots.
public enum CaptureSessionState
{
    Uninitialized,
    Initializing,
    Ready,
    Previewing,
    Recording,
    CleaningUp,
    Faulted,
    Disposed
}
