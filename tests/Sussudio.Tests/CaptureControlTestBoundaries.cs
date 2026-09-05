using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Additional passive shapes for the complete linked CaptureBindingControllers.cs.
// ObservableCollection delivers real collection events. Model setters only store
// values/count writes; operation delegates let tests control completion and faults.
// No capture selection, queue coalescing, or control enablement policy lives here.
namespace Microsoft.UI.Xaml
{
    public enum Visibility { Visible, Collapsed }
}

namespace Microsoft.UI.Xaml.Controls
{
    public sealed class Button : Control
    {
        public object? Content { get; set; }
    }

    public sealed class StackPanel : FrameworkElement { }

    public sealed class ProgressRing : Control
    {
        public bool IsActive { get; set; }
    }

    public sealed class FontIcon : FrameworkElement
    {
        public string Glyph { get; set; } = string.Empty;
        public double FontSize { get; set; }
    }

    public sealed class NumberBox : Control
    {
        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                var previous = _value;
                _value = value;
                ValueChanged?.Invoke(this, new ValueChangedEventArgs(previous, value));
            }
        }
        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
    }

    public static class ToolTipService
    {
        private sealed class StoredToolTip { internal object? Value; }
        private static readonly ConditionalWeakTable<UIElement, StoredToolTip> Values = new();
        public static void SetToolTip(UIElement element, object? value)
            => Values.GetValue(element, _ => new StoredToolTip()).Value = value;
        public static object? GetToolTip(UIElement element)
            => Values.TryGetValue(element, out var stored) ? stored.Value : null;
    }
}

namespace Sussudio.Models
{
    public sealed class CaptureDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public sealed class ResolutionOption
    {
        public string Value { get; set; } = string.Empty;
        public uint Width { get; set; }
        public uint Height { get; set; }
        public bool IsEnabled { get; set; }
    }

    public sealed class FrameRateOption
    {
        public double Value { get; set; }
        public double FriendlyValue { get; set; }
        public string Rational { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public sealed class MediaFormat
    {
        public string PixelFormat { get; set; } = string.Empty;
    }
}

namespace Sussudio.ViewModels
{
    public sealed partial class MainViewModel
    {
        public ObservableCollection<Models.CaptureDevice> Devices { get; set; } = new();
        public ObservableCollection<Models.AudioInputDevice> AudioInputDevices { get; set; } = new();
        public ObservableCollection<Models.AudioInputDevice> MicrophoneDevices { get; set; } = new();
        public ObservableCollection<Models.ResolutionOption> AvailableResolutions { get; set; } = new();
        public ObservableCollection<Models.FrameRateOption> AvailableFrameRates { get; set; } = new();
        public ObservableCollection<string> AvailableRecordingFormats { get; set; } = new();
        public ObservableCollection<string> AvailableQualities { get; set; } = new();
        public ObservableCollection<string> AvailablePresets { get; set; } = new();
        public ObservableCollection<string> AvailableSplitEncodeModes { get; set; } = new();
        public ObservableCollection<string> AvailableVideoFormats { get; set; } = new();
        public ObservableCollection<string> AvailableDeviceAudioModes { get; set; } = new();
        public Models.CaptureDevice? SelectedDevice { get; set; }
        public Models.MediaFormat? SelectedFormat { get; set; }
        public string SelectedRecordingFormat { get; set; } = string.Empty;
        public string SelectedQuality { get; set; } = string.Empty;
        public string SelectedPreset { get; set; } = string.Empty;
        public string SelectedSplitEncodeMode { get; set; } = string.Empty;
        public string SelectedVideoFormat { get; set; } = string.Empty;
        private string _selectedResolution = string.Empty;
        private double _selectedFrameRate;
        private double _customBitrateMbps;
        public int SelectedResolutionSetCount { get; private set; }
        public int SelectedFrameRateSetCount { get; private set; }
        public int CustomBitrateMbpsSetCount { get; private set; }
        public string SelectedResolution { get => _selectedResolution; set { SelectedResolutionSetCount++; _selectedResolution = value; } }
        public double SelectedFrameRate { get => _selectedFrameRate; set { SelectedFrameRateSetCount++; _selectedFrameRate = value; } }
        public double CustomBitrateMbps { get => _customBitrateMbps; set { CustomBitrateMbpsSetCount++; _customBitrateMbps = value; } }
        public bool IsAutoFrameRateSelected { get; set; }
        public bool IsPreviewing { get; set; }
        public bool IsPreviewReinitializing { get; set; }
        public bool IsDeviceAudioControlSupported { get; set; }
        public bool IsHdrAvailable { get; set; }
        public bool IsHdrEnabled { get; set; }
        public bool IsTrueHdrPreviewEnabled { get; set; }
        public bool IsCustomBitrateVisible { get; set; }
        public bool AudioClipping { get; set; }
        public bool? SourceIsHdr { get; set; }
        public int? SourceWidth { get; set; }
        public int? SourceHeight { get; set; }
        public int MjpegDecoderCount { get; set; }
        public string HdrResolutionSupportHint { get; set; } = string.Empty;
        public string HdrReadinessReason { get; set; } = string.Empty;
        public string HdrRuntimeState { get; set; } = string.Empty;
        public string SourceTelemetrySummaryText { get; set; } = string.Empty;
        public string SourceTargetSummaryText { get; set; } = string.Empty;
        public Func<Task>? OnRefreshDevicesAsync { get; set; }
        public Func<Models.CaptureDevice, Task>? OnApplySelectedDeviceAsync { get; set; }
        public int RefreshDevicesAsyncCount { get; private set; }
        public int ApplySelectedDeviceAsyncCount { get; private set; }
        public Models.CaptureDevice? AppliedDevice { get; private set; }
        public Task RefreshDevicesAsync()
        {
            RefreshDevicesAsyncCount++;
            return OnRefreshDevicesAsync?.Invoke() ?? Task.CompletedTask;
        }
        public Task ApplySelectedDeviceAsync(Models.CaptureDevice device)
        {
            ApplySelectedDeviceAsyncCount++;
            AppliedDevice = device;
            return OnApplySelectedDeviceAsync?.Invoke(device) ?? Task.CompletedTask;
        }
    }
}
