using Microsoft.UI.Xaml.Controls;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for capture selection, option setup, affordance
// presentation, event binding, and property-change routing. Behavior lives in
// the capture controllers.
public sealed partial class MainWindow
{
    private CaptureSelectionBindingController _captureSelectionBindingController = null!;
    private CaptureOptionBindingController _captureOptionBindingController = null!;
    private CaptureOptionPresentationController _captureOptionPresentationController = null!;

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

    private void InitializeCaptureOptionPresentationController()
    {
        _captureOptionPresentationController = new CaptureOptionPresentationController(new CaptureOptionPresentationControllerContext
        {
            ViewModel = ViewModel,
            VideoFormatComboBox = VideoFormatComboBox,
            FrameRateComboBox = FrameRateComboBox,
            DecoderCountPanel = DecoderCountPanel,
            DecoderCountComboBox = DecoderCountComboBox,
            HdrToggle = HdrToggle,
            TrueHdrPreviewToggle = TrueHdrPreviewToggle,
            CustomBitratePanel = CustomBitratePanel,
            PresetPanel = PresetPanel,
            AudioClipText = AudioClipText
        });
    }

    private void InitializeCaptureOptionBindingController()
    {
        _captureOptionBindingController = new CaptureOptionBindingController(new CaptureOptionBindingControllerContext
        {
            ViewModel = ViewModel,
            ResolutionComboBox = ResolutionComboBox,
            FrameRateComboBox = FrameRateComboBox,
            FormatComboBox = FormatComboBox,
            QualityComboBox = QualityComboBox,
            PresetComboBox = PresetComboBox,
            SplitEncodeComboBox = SplitEncodeComboBox,
            VideoFormatComboBox = VideoFormatComboBox,
            DecoderCountComboBox = DecoderCountComboBox,
            CustomBitrateNumberBox = CustomBitrateNumberBox,
            HdrToggle = HdrToggle,
            TrueHdrPreviewToggle = TrueHdrPreviewToggle,
            ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,
            ApplyBitrateVisibility = ApplyBitrateVisibility,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            ApplyAudioClipVisibility = ApplyAudioClipVisibility,
            RefreshHdrHintText = RefreshHdrHintText,
            UpdateFpsTelemetryTooltip = UpdateFpsTelemetryTooltip,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            SetHdrPassthroughEnabled = enabled => _previewRendererHostController.SetHdrPassthroughEnabled(enabled),
            UpdateDecoderCountVisibility = UpdateDecoderCountVisibility,
            EnsureResolutionSelection = EnsureResolutionSelection,
            EnsureFrameRateSelection = EnsureFrameRateSelection,
            EnsureFormatSelection = EnsureFormatSelection,
            EnsureQualitySelection = EnsureQualitySelection,
            EnsurePresetSelection = EnsurePresetSelection,
            EnsureSplitEncodeModeSelection = EnsureSplitEncodeModeSelection
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

    private void InitializeCaptureOptionCollections()
        => _captureOptionBindingController.InitializeCollections();

    private void ApplyInitialCaptureOptionSelections()
        => _captureOptionBindingController.ApplyInitialSelections();

    private void EnsureInitialCaptureOptionSelections()
        => _captureOptionBindingController.EnsureInitialSelections();

    private void AttachCaptureModeSelectionBindings()
        => _captureOptionBindingController.AttachCaptureModeSelectionBindings();

    private void AttachRecordingOptionBindings()
        => _captureOptionBindingController.AttachRecordingOptionBindings();

    private void HandleCustomBitratePropertyChanged()
        => _captureOptionBindingController.HandleCustomBitratePropertyChanged();

    private void HandleHdrEnabledChanged()
        => _captureOptionBindingController.HandleHdrEnabledChanged();

    private void HandleTrueHdrPreviewEnabledChanged()
        => _captureOptionBindingController.HandleTrueHdrPreviewEnabledChanged();

    private bool TryHandleCaptureOptionPropertyChanged(string propertyName)
        => _captureOptionBindingController.TryHandlePropertyChanged(propertyName);

    private void ApplyInitialDecoderCountSelection()
        => _captureOptionPresentationController.ApplyInitialDecoderCountSelection();

    private void UpdateDecoderCountVisibility()
        => _captureOptionPresentationController.UpdateDecoderCountVisibility();

    private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _captureOptionPresentationController.HandleDecoderCountSelectionChanged();

    private void RefreshHdrHintText()
        => _captureOptionPresentationController.RefreshHdrHintText();

    private void UpdateFpsTelemetryTooltip()
        => _captureOptionPresentationController.UpdateFpsTelemetryTooltip();

    private void ApplyHdrToggleEnabledState()
        => _captureOptionPresentationController.ApplyHdrToggleEnabledState();

    private void ApplyBitrateVisibility()
        => _captureOptionPresentationController.ApplyBitrateVisibility();

    private void ApplyAudioClipVisibility()
        => _captureOptionPresentationController.ApplyAudioClipVisibility();
}
