namespace Sussudio.Models;

public sealed class CaptureDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeXuInterfacePath { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Device" : Name;

    public override string ToString() => DisplayName;
}
