using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for capture option setup and event binding.
public sealed partial class MainWindow
{
    private CaptureOptionBindingController _captureOptionBindingController = null!;

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
            ShowAllCaptureOptionsToggle = ShowAllCaptureOptionsToggle,
            ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,
            ApplyBitrateVisibility = ApplyBitrateVisibility,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
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

    private void AttachShowAllCaptureOptionsBinding()
        => _captureOptionBindingController.AttachShowAllCaptureOptionsBinding();

    private void HandleCustomBitratePropertyChanged()
        => _captureOptionBindingController.HandleCustomBitratePropertyChanged();

    private void HandleHdrEnabledChanged()
        => _captureOptionBindingController.HandleHdrEnabledChanged();

    private void HandleTrueHdrPreviewEnabledChanged()
        => _captureOptionBindingController.HandleTrueHdrPreviewEnabledChanged();

    private void HandleShowAllCaptureOptionsChanged()
        => _captureOptionBindingController.HandleShowAllCaptureOptionsChanged();
}
