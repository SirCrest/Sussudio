namespace Sussudio.Models;

// Audio endpoint option displayed in the UI and persisted by settings.
public class AudioInputDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Audio Device" : Name;

    public override string ToString() => DisplayName;
}
