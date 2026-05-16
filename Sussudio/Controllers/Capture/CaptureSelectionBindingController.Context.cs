using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureSelectionBindingControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required ComboBox DeviceComboBox { get; init; }
    public required ComboBox AudioInputComboBox { get; init; }
    public required ComboBox MicrophoneComboBox { get; init; }
    public required ComboBox ResolutionComboBox { get; init; }
    public required ComboBox FrameRateComboBox { get; init; }
    public required ComboBox FormatComboBox { get; init; }
    public required ComboBox QualityComboBox { get; init; }
    public required ComboBox PresetComboBox { get; init; }
    public required ComboBox SplitEncodeComboBox { get; init; }
    public required Button ApplyDeviceButton { get; init; }
    public required StackPanel DeviceAudioControlPanel { get; init; }
    public required ToggleSwitch DeviceAudioModeToggle { get; init; }
    public required StackPanel AnalogAudioGainPanel { get; init; }
    public required Slider AnalogAudioGainSlider { get; init; }
    public required TextBlock AnalogAudioGainValueTextBlock { get; init; }
}
