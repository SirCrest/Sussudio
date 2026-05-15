using Microsoft.UI.Xaml.Controls;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for capture option affordance presentation.
public sealed partial class MainWindow
{
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
