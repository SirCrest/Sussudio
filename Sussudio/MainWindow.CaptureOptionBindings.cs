using Microsoft.UI.Xaml.Controls;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for capture option setup, affordance presentation, event binding,
// and property-change routing. Behavior lives in the capture option controllers.
public sealed partial class MainWindow
{
    private CaptureOptionBindingController _captureOptionBindingController = null!;
    private CaptureOptionPresentationController _captureOptionPresentationController = null!;

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
