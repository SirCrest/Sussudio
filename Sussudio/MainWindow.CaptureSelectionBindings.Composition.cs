using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private CaptureSelectionBindingController _captureSelectionBindingController = null!;

    private void InitializeCaptureSelectionBindingController()
    {
        _captureSelectionBindingController = new CaptureSelectionBindingController(
            new CaptureSelectionBindingControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                DeviceComboBox = DeviceComboBox,
                AudioInputComboBox = AudioInputComboBox,
                MicrophoneComboBox = MicrophoneComboBox,
                ResolutionComboBox = ResolutionComboBox,
                FrameRateComboBox = FrameRateComboBox,
                FormatComboBox = FormatComboBox,
                QualityComboBox = QualityComboBox,
                PresetComboBox = PresetComboBox,
                SplitEncodeComboBox = SplitEncodeComboBox,
                ApplyDeviceButton = ApplyDeviceButton,
                DeviceAudioControlPanel = DeviceAudioControlPanel,
                DeviceAudioModeToggle = DeviceAudioModeToggle,
                AnalogAudioGainPanel = AnalogAudioGainPanel,
                AnalogAudioGainSlider = AnalogAudioGainSlider,
                AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock
            });
    }

    private void AttachCaptureSelectionBindings()
        => _captureSelectionBindingController.AttachCollectionBindings();

    private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)
        => _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);

    private void AttachDeviceSelectionChangedBinding()
        => _captureSelectionBindingController.AttachDeviceSelectionChangedBinding();

    private void EnsureDeviceSelection()
        => _captureSelectionBindingController.EnsureDeviceSelection();

    private void HandleSelectedDevicePropertyChanged()
        => _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();

    private bool HasPendingDeviceSelection()
        => _captureSelectionBindingController.HasPendingDeviceSelection();

    private void UpdateDeviceApplyButtonState()
        => _captureSelectionBindingController.UpdateDeviceApplyButtonState();

    private void EnsureAudioInputSelection()
        => _captureSelectionBindingController.EnsureAudioInputSelection();

    private void EnsureMicrophoneSelection()
        => _captureSelectionBindingController.EnsureMicrophoneSelection();

    private void EnsureDeviceAudioModeSelection()
        => _captureSelectionBindingController.EnsureDeviceAudioModeSelection();

    private void ApplyDeviceAudioControlState()
        => _captureSelectionBindingController.ApplyDeviceAudioControlState();

    private void EnsureResolutionSelection()
        => _captureSelectionBindingController.EnsureResolutionSelection();

    private void HandleAvailableResolutionsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();

    private void EnsureFrameRateSelection()
        => _captureSelectionBindingController.EnsureFrameRateSelection();

    private void HandleAvailableFrameRatesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();

    private void EnsureFormatSelection()
        => _captureSelectionBindingController.EnsureFormatSelection();

    private void EnsureQualitySelection()
        => _captureSelectionBindingController.EnsureQualitySelection();

    private void EnsurePresetSelection()
        => _captureSelectionBindingController.EnsurePresetSelection();

    private void HandleAvailablePresetsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();

    private void EnsureSplitEncodeModeSelection()
        => _captureSelectionBindingController.EnsureSplitEncodeModeSelection();

    private void HandleAvailableSplitEncodeModesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();
}
