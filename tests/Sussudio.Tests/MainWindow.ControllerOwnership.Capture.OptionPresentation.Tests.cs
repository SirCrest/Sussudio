using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ButtonActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureDeviceActionController _captureDeviceActionController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureDeviceActionController()");
        AssertContains(adapterText, "RefreshButton = RefreshButton,");
        AssertContains(adapterText, "ApplyDeviceButton = ApplyDeviceButton,");
        AssertContains(adapterText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState");
        AssertContains(adapterText, "private Task RefreshDevicesFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.RefreshDevicesAsync();");
        AssertContains(adapterText, "private Task ApplySelectedDeviceFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.ApplySelectedDeviceAsync();");
        AssertContains(adapterText, "private void RefreshButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(adapterText, "private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(adapterText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(adapterText, "UpdateDeviceApplyButtonState();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureDeviceActions.cs")),
            "capture-device button adapter folded into MainWindow.ButtonActions.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs").Replace("\r\n", "\n");
        var policyText = controllerText;
        const string tooltipFormatterMarker = "internal static class CaptureOptionTooltipFormatter";
        var tooltipFormatterStart = controllerText.IndexOf(tooltipFormatterMarker, System.StringComparison.Ordinal);
        if (tooltipFormatterStart < 0)
        {
            throw new System.InvalidOperationException("CaptureOptionTooltipFormatter was not found in CaptureOptionPresentationController.cs.");
        }

        var tooltipFormatterText = controllerText[tooltipFormatterStart..];
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.CaptureBindings.cs").Replace("\r\n", "\n");
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

    internal static Task CaptureOptionPresentationPolicy_PreservesAffordanceRules()
    {
        var policyType = RequireType("Sussudio.Controllers.CaptureOptionPresentationPolicy");
        var inputType = RequireType("Sussudio.Controllers.CaptureOptionPresentationInput");
        var build = policyType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build was not found.");
        var constructor = inputType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 12);

        object Build(
            string? selectedVideoFormat,
            string? selectedFormatPixelFormat,
            double? selectedFrameRateOptionFriendlyValue,
            double? selectedFrameRateOptionValue,
            double selectedFrameRateFallback,
            int mjpegDecoderCount = 4,
            bool isHdrAvailable = true,
            bool isRecording = false,
            bool? sourceIsHdr = true,
            bool isHdrEnabled = true,
            bool isCustomBitrateVisible = false,
            bool audioClipping = false)
        {
            var input = constructor.Invoke(new object?[]
            {
                selectedVideoFormat,
                selectedFormatPixelFormat,
                selectedFrameRateOptionFriendlyValue,
                selectedFrameRateOptionValue,
                selectedFrameRateFallback,
                mjpegDecoderCount,
                isHdrAvailable,
                isRecording,
                sourceIsHdr,
                isHdrEnabled,
                isCustomBitrateVisible,
                audioClipping
            });

            return build.Invoke(null, new[] { input })
                ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build returned null.");
        }

        var explicitMjpgHighFps = Build("MJPG", null, 90d, null, 60d);
        AssertEqual(true, GetBoolProperty(explicitMjpgHighFps, "ShowDecoderCount"), "explicit MJPG at 90 FPS shows decoder count");

        var explicitMjpgLowFps = Build("MJPG", null, 89.99d, null, 120d);
        AssertEqual(false, GetBoolProperty(explicitMjpgLowFps, "ShowDecoderCount"), "explicit MJPG below 90 FPS hides decoder count");

        var autoMjpgValueFps = Build("Auto", "MJPG", 0d, 120d, 60d);
        AssertEqual(true, GetBoolProperty(autoMjpgValueFps, "ShowDecoderCount"), "Auto with MJPG device format uses frame-rate option value fallback");

        var autoNonMjpgHighFps = Build("Auto", "NV12", null, 120d, 60d);
        AssertEqual(false, GetBoolProperty(autoNonMjpgHighFps, "ShowDecoderCount"), "Auto with non-MJPG device format hides decoder count");

        var fallbackFrameRate = Build("MJPG", null, null, null, 120d);
        AssertEqual(true, GetBoolProperty(fallbackFrameRate, "ShowDecoderCount"), "missing frame-rate option falls back to selected frame rate");

        var sourceUnknown = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: null);
        AssertEqual(true, GetBoolProperty(sourceUnknown, "EnableHdrToggle"), "unknown source HDR state does not disable HDR toggle");

        var sdrSource = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: false);
        AssertEqual(false, GetBoolProperty(sdrSource, "EnableHdrToggle"), "SDR source disables HDR toggle");

        var recording = Build("Auto", "NV12", null, null, 60d, isRecording: true);
        AssertEqual(false, GetBoolProperty(recording, "EnableHdrToggle"), "recording disables HDR toggle");
        AssertEqual(false, GetBoolProperty(recording, "EnableTrueHdrPreviewToggle"), "recording disables true-HDR preview toggle");

        var unavailableHdr = Build("Auto", "NV12", null, null, 60d, isHdrAvailable: false);
        AssertEqual(false, GetBoolProperty(unavailableHdr, "EnableHdrToggle"), "HDR unavailable disables HDR toggle");

        var customBitrate = Build("Auto", "NV12", null, null, 60d, isCustomBitrateVisible: true, audioClipping: true);
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowCustomBitrate"), "custom bitrate shows custom panel");
        AssertEqual(false, GetBoolProperty(customBitrate, "ShowPreset"), "custom bitrate hides preset panel");
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowAudioClip"), "audio clipping shows warning text");

        var lowDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 0);
        var highDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 9);
        var normalDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 5);
        AssertEqual(1, GetIntProperty(lowDecoderCount, "InitialDecoderCount"), "decoder count clamps low");
        AssertEqual(8, GetIntProperty(highDecoderCount, "InitialDecoderCount"), "decoder count clamps high");
        AssertEqual(5, GetIntProperty(normalDecoderCount, "InitialDecoderCount"), "decoder count preserves valid values");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.CaptureOptionTooltipFormatter");
        var buildHdrHintText = formatterType.GetMethod("BuildHdrHintText", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildHdrHintText was not found.");
        var buildFpsTelemetryTooltip = formatterType.GetMethod("BuildFpsTelemetryTooltip", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip was not found.");

        string? Hdr(string? resolutionHint, string? readinessHint, bool isRecording)
            => buildHdrHintText.Invoke(null, new object?[] { resolutionHint, readinessHint, isRecording })?.ToString();

        string? Fps(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)
            => buildFpsTelemetryTooltip.Invoke(null, new object?[] { sourceTelemetrySummaryText, sourceTargetSummaryText })?.ToString();

        var stopRecordingText = "Stop recording before switching between HDR and SDR pipelines.";
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower",
            Hdr("  4K HDR requires 59.94 or lower ", " Source is SDR ", isRecording: false),
            "HDR hint trims and combines readiness before resolution support");
        AssertEqual(
            "4K HDR requires 59.94 or lower",
            Hdr("4K HDR requires 59.94 or lower", null, isRecording: false),
            "HDR hint uses resolution when readiness is empty");
        AssertEqual(
            stopRecordingText,
            Hdr(null, null, isRecording: true),
            "HDR hint uses recording guard when no other hint exists");
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower{System.Environment.NewLine}{stopRecordingText}",
            Hdr("4K HDR requires 59.94 or lower", "Source is SDR", isRecording: true),
            "HDR hint appends recording guard after existing hints");
        AssertEqual(
            null,
            Hdr(" ", null, isRecording: false),
            "HDR hint returns null when no hint text exists");

        AssertEqual(
            $"Telemetry: NativeXu{System.Environment.NewLine}Target: 3840 x 2160",
            Fps("Telemetry: NativeXu", "Target: 3840 x 2160"),
            "FPS tooltip combines telemetry and target summaries");
        AssertEqual(
            "  Telemetry: NativeXu  ",
            Fps("  Telemetry: NativeXu  ", null),
            "FPS tooltip preserves existing telemetry summary whitespace");
        AssertEqual(
            "Target: 3840 x 2160",
            Fps(null, "Target: 3840 x 2160"),
            "FPS tooltip uses target summary when telemetry is empty");
        AssertEqual(
            null,
            Fps(" ", null),
            "FPS tooltip returns null when both summaries are empty");

        return Task.CompletedTask;
    }
}
