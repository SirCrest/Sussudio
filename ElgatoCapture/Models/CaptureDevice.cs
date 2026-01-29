using System.Collections.ObjectModel;

namespace ElgatoCapture.Models;

public class CaptureDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public bool IsHdrCapable { get; set; }
    public ObservableCollection<MediaFormat> SupportedFormats { get; set; } = new();

    public string DisplayName => string.IsNullOrEmpty(Name) ? "Unknown Device" : Name;

    public override string ToString() => DisplayName;
}
