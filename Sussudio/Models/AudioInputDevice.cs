namespace Sussudio.Models;

public class AudioInputDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Audio Device" : Name;

    public override string ToString() => DisplayName;
}
