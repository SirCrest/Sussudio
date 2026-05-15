using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var contextText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.Context.cs").Replace("\r\n", "\n");
        var deviceAudioText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.DeviceAudio.cs").Replace("\r\n", "\n");
        var selectionSyncText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.SelectionSync.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.AttachRecordingStringSelectionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(propertyChangedText, "EnsureResolutionSelection();");
        AssertContains(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertContains(controllerText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(contextText, "internal sealed class CaptureSelectionBindingControllerContext");
        AssertContains(selectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "public void AttachRecordingStringSelectionBindings()");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
        AssertContains(controllerText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(deviceAudioText, "public void ApplyDeviceAudioControlState()");
        AssertContains(deviceAudioText, "public void EnsureDeviceAudioModeSelection()");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(selectionSyncText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");

        return Task.CompletedTask;
    }

    private static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureDeviceActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureDeviceActionController.cs").Replace("\r\n", "\n");

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

        return Task.CompletedTask;
    }

    private static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureOptionPresentationController.cs").Replace("\r\n", "\n");
        var tooltipFormatterText = ReadRepoFile("Sussudio/Controllers/CaptureOptionTooltipFormatter.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");

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
        AssertContains(controllerText, "_selectedDecoderCount = Math.Clamp(_context.ViewModel.MjpegDecoderCount, 1, 8);");
        AssertContains(controllerText, "_context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;");
        AssertContains(controllerText, "public void UpdateDecoderCountVisibility()");
        AssertContains(controllerText, "private double GetSelectedFriendlyFrameRate()");
        AssertContains(controllerText, "public void HandleDecoderCountSelectionChanged()");
        AssertContains(controllerText, "_context.ViewModel.MjpegDecoderCount = count;");
        AssertContains(controllerText, "public void RefreshHdrHintText()");
        AssertContains(controllerText, "public void UpdateFpsTelemetryTooltip()");
        AssertContains(controllerText, "public void ApplyHdrToggleEnabledState()");
        AssertContains(controllerText, "public void ApplyBitrateVisibility()");
        AssertContains(controllerText, "public void ApplyAudioClipVisibility()");
        AssertContains(controllerText, "_context.ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildHdrHintText(");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip(");
        AssertContains(tooltipFormatterText, "internal static class CaptureOptionTooltipFormatter");
        AssertContains(tooltipFormatterText, "public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)");
        AssertContains(tooltipFormatterText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(tooltipFormatterText, "public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)");
        AssertDoesNotContain(controllerText, "var combinedHint =");
        AssertDoesNotContain(controllerText, "var parts = new List<string>();");
        AssertContains(controllerText, "_context.ViewModel.SourceTelemetrySummaryText");
        AssertContains(controllerText, "_context.ViewModel.SourceTargetSummaryText");
        AssertContains(mainWindowText, "InitializeCaptureOptionPresentationController();");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(propertyChangedText, "ApplyAudioClipVisibility();");
        AssertContains(propertyChangedText, "ApplyHdrToggleEnabledState();");
        AssertContains(propertyChangedText, "RefreshHdrHintText();");
        AssertContains(propertyChangedText, "UpdateFpsTelemetryTooltip();");
        AssertContains(propertyChangedText, "ApplyBitrateVisibility();");
        AssertDoesNotContain(bindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(bindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(bindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(bindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(bindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n"), "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertDoesNotContain(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertDoesNotContain(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionBindings.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.RecordingOptionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var selectionBindingControllerText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsWithoutVideoFormat = recordingOptionBindingsText.Replace("VideoFormatComboBox.SelectionChanged +=", string.Empty);

        AssertContains(captureOptionBindingsText, "private CaptureOptionBindingController _captureOptionBindingController = null!;");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionBindingController()");
        AssertContains(captureOptionBindingsText, "ResolutionComboBox = ResolutionComboBox,");
        AssertContains(captureOptionBindingsText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionBindingsText, "TrueHdrPreviewToggle = TrueHdrPreviewToggle,");
        AssertContains(captureOptionBindingsText, "ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,");
        AssertContains(captureOptionBindingsText, "EnsureSplitEncodeModeSelection = EnsureSplitEncodeModeSelection,");
        AssertContains(captureOptionBindingsText, "AttachRecordingStringSelectionBindings = AttachRecordingStringSelectionBindings");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionCollections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.InitializeCollections();");
        AssertContains(captureOptionBindingsText, "private void ApplyInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.ApplyInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void EnsureInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.EnsureInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void AttachCaptureModeSelectionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachCaptureModeSelectionBindings();");
        AssertContains(recordingOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(recordingOptionBindingsText, "=> _captureOptionBindingController.AttachRecordingOptionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureOptionBindingController();");

        AssertContains(controllerText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerText, "internal sealed class CaptureOptionBindingController");
        AssertContains(controllerText, "public void InitializeCollections()");
        AssertContains(controllerText, "_context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;");
        AssertContains(controllerText, "for (var i = 1; i <= 8; i++)");
        AssertContains(controllerText, "_context.DecoderCountComboBox.Items.Add(i);");
        AssertContains(controllerText, "public void ApplyInitialSelections()");
        AssertContains(controllerText, "_context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");
        AssertContains(controllerText, "_context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;");
        AssertContains(controllerText, "_context.ApplyInitialDecoderCountSelection();");
        AssertContains(controllerText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerText, "public void EnsureInitialSelections()");
        AssertContains(controllerText, "_context.EnsureSplitEncodeModeSelection();");
        AssertContains(controllerText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerText, "_context.ResolutionComboBox.SelectionChanged +=");
        AssertContains(controllerText, "_context.FrameRateComboBox.SelectionChanged +=");
        AssertContains(controllerText, "!string.Equals(resolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase)");
        AssertContains(controllerText, "if (IsAutoFrameRateOption(frameRate))");
        AssertContains(controllerText, "if (!_context.ViewModel.IsAutoFrameRateSelected)");
        AssertContains(controllerText, "else if (!IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))");
        AssertContains(controllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(controllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertContains(controllerText, "=> option.Value <= 0 || option.FriendlyValue <= 0;");
        AssertContains(controllerText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerText, "_context.AttachRecordingStringSelectionBindings();");
        AssertContains(controllerText, "_context.VideoFormatComboBox.SelectionChanged +=");
        AssertContains(controllerText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(controllerText, "if (!double.IsNaN(_context.CustomBitrateNumberBox.Value))");
        AssertContains(controllerText, "_context.HdrToggle.Click +=");
        AssertContains(controllerText, "_context.TrueHdrPreviewToggle.Click +=");

        AssertContains(bindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(bindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(bindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(bindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(bindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertContains(selectionBindingControllerText, "public void AttachRecordingStringSelectionBindings()");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(selectionBindingControllerText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(captureOptionBindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedFrameRate =");
        AssertDoesNotContain(recordingOptionBindingsWithoutVideoFormat, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "VideoFormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(recordingOptionBindingsText, "TrueHdrPreviewToggle.Click +=");
        AssertDoesNotContain(recordingOptionBindingsText, "ViewModel.SelectedRecordingFormat =");
        AssertDoesNotContain(recordingOptionBindingsText, "QualityComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "PresetComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "SplitEncodeComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy()
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
