using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs").Replace("\r\n", "\n");
        var policyText = controllerText;
        const string tooltipFormatterMarker = "internal static class CaptureOptionTooltipFormatter";
        var tooltipFormatterStart = controllerText.IndexOf(tooltipFormatterMarker, System.StringComparison.Ordinal);
        if (tooltipFormatterStart < 0)
        {
            throw new System.InvalidOperationException("CaptureOptionTooltipFormatter was not found in CaptureOptionPresentationController.cs.");
        }

        var tooltipFormatterText = controllerText[tooltipFormatterStart..];
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.ControllerInitialization.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionBindings.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");
        var outputPathDisplayText = ReadRepoFile("Sussudio/MainWindow.ButtonActions.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private CaptureOptionPresentationController _captureOptionPresentationController = null!;");
        AssertContains(captureOptionText, "private void InitializeCaptureOptionPresentationController()");
        AssertContains(captureOptionText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionText, "AudioClipText = AudioClipText");
        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateDecoderCountVisibility();");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.HandleDecoderCountSelectionChanged();");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.RefreshHdrHintText();");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateFpsTelemetryTooltip();");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyHdrToggleEnabledState();");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyBitrateVisibility();");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyAudioClipVisibility();");

        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationController");
        AssertContains(controllerText, "private int _selectedDecoderCount = 4;");
        AssertContains(controllerText, "public void ApplyInitialDecoderCountSelection()");
        AssertContains(controllerText, "_selectedDecoderCount = affordances.InitialDecoderCount;");
        AssertContains(controllerText, "_context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;");
        AssertContains(controllerText, "public void UpdateDecoderCountVisibility()");
        AssertContains(policyText, "InitialDecoderCount: Math.Clamp(input.MjpegDecoderCount, 1, 8)");
        AssertContains(controllerText, "public void HandleDecoderCountSelectionChanged()");
        AssertContains(controllerText, "_context.ViewModel.MjpegDecoderCount = count;");
        AssertContains(controllerText, "public void RefreshHdrHintText()");
        AssertContains(controllerText, "public void UpdateFpsTelemetryTooltip()");
        AssertContains(controllerText, "public void ApplyHdrToggleEnabledState()");
        AssertContains(controllerText, "public void ApplyBitrateVisibility()");
        AssertContains(controllerText, "public void ApplyAudioClipVisibility()");
        AssertContains(controllerText, "_context.ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(controllerText, "CaptureOptionPresentationPolicy.Build(BuildPolicyInput())");
        AssertContains(controllerText, "private CaptureOptionPresentationInput BuildPolicyInput()");
        AssertContains(controllerText, "private static Visibility ToVisibility(bool isVisible)");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildHdrHintText(");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip(");
        AssertContains(policyText, "internal static class CaptureOptionPresentationPolicy");
        AssertContains(policyText, "internal static CaptureOptionPresentationAffordances Build(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationInput(");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationAffordances(");
        AssertContains(policyText, "private static double ResolveSelectedFrameRate(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "private static bool ShouldShowDecoderCount(");
        AssertContains(policyText, "selectedFrameRate >= 90");
        AssertContains(tooltipFormatterText, "internal static class CaptureOptionTooltipFormatter");
        AssertContains(tooltipFormatterText, "public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)");
        AssertContains(tooltipFormatterText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(tooltipFormatterText, "public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)");
        AssertDoesNotContain(captureOptionText, "var combinedHint =");
        AssertDoesNotContain(controllerText, "var parts = new List<string>();");
        AssertContains(controllerText, "_context.ViewModel.SourceTelemetrySummaryText");
        AssertContains(controllerText, "_context.ViewModel.SourceTargetSummaryText");
        AssertContains(mainWindowText, "InitializeCaptureOptionPresentationController();");
        AssertContains(propertyChangedText, "TryHandleOutput = TryHandleOutputPropertyChanged,");
        AssertContains(propertyChangedText, "TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,");
        AssertContains(outputPathDisplayText, "=> _outputPathController.TryHandlePropertyChanged(propertyName);");
        AssertContains(captureOptionBindingsText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionPropertyChangedMethod, "=> _captureOptionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyAudioClipVisibility();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "RefreshHdrHintText();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "UpdateFpsTelemetryTooltip();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ApplyBitrateVisibility();");
        AssertDoesNotContain(bindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(bindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(bindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(bindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(bindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(ReadMainWindowCompositionSource(), "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertDoesNotContain(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertDoesNotContain(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertDoesNotContain(captureOptionText, "var isExplicitMjpg =");
        AssertDoesNotContain(captureOptionText, "var isAutoWithMjpgDevice =");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsHdrAvailable &&");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsCustomBitrateVisible ? Visibility.Visible");
        AssertDoesNotContain(controllerText, "_context.ViewModel.AudioClipping ? Visibility.Visible");

        return Task.CompletedTask;
    }
}
