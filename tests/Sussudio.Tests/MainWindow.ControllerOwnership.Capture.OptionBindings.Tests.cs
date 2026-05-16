using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionBindings.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.RecordingOptionBindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var controllerContextText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.Context.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.Initialization.cs").Replace("\r\n", "\n");
        var controllerSelectionHandlersText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.SelectionHandlers.cs").Replace("\r\n", "\n");
        var controllerRecordingOptionsText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.RecordingOptions.cs").Replace("\r\n", "\n");
        var controllerHdrText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.Hdr.cs").Replace("\r\n", "\n");
        var controllerShowAllText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.ShowAll.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedCaptureOptions.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var controllerText = string.Join(
            "\n",
            controllerRootText,
            controllerContextText,
            controllerInitializationText,
            controllerSelectionHandlersText,
            controllerRecordingOptionsText,
            controllerHdrText,
            controllerShowAllText);
        var selectionBindingControllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var selectionBindingFamilyText = string.Join(
            "\n",
            selectionBindingControllerText,
            ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.RecordingSelection.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.StringSelection.cs").Replace("\r\n", "\n"));
        var recordingOptionBindingsWithoutVideoFormat = recordingOptionBindingsText.Replace("VideoFormatComboBox.SelectionChanged +=", string.Empty);

        AssertContains(captureOptionBindingsText, "private CaptureOptionBindingController _captureOptionBindingController = null!;");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionBindingController()");
        AssertContains(captureOptionBindingsText, "ResolutionComboBox = ResolutionComboBox,");
        AssertContains(captureOptionBindingsText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionBindingsText, "TrueHdrPreviewToggle = TrueHdrPreviewToggle,");
        AssertContains(captureOptionBindingsText, "ShowAllCaptureOptionsToggle = ShowAllCaptureOptionsToggle,");
        AssertContains(captureOptionBindingsText, "ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,");
        AssertContains(captureOptionBindingsText, "SetHdrPassthroughEnabled = enabled => _previewRendererHostController.SetHdrPassthroughEnabled(enabled),");
        AssertContains(captureOptionBindingsText, "EnsureSplitEncodeModeSelection = EnsureSplitEncodeModeSelection");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionCollections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.InitializeCollections();");
        AssertContains(captureOptionBindingsText, "private void ApplyInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.ApplyInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void EnsureInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.EnsureInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void AttachCaptureModeSelectionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachCaptureModeSelectionBindings();");
        AssertContains(captureOptionBindingsText, "private void AttachShowAllCaptureOptionsBinding()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachShowAllCaptureOptionsBinding();");
        AssertContains(captureOptionBindingsText, "private void HandleCustomBitratePropertyChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleCustomBitratePropertyChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleHdrEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleHdrEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleShowAllCaptureOptionsChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleShowAllCaptureOptionsChanged();");
        AssertContains(recordingOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(recordingOptionBindingsText, "=> _captureOptionBindingController.AttachRecordingOptionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureOptionBindingController();");

        AssertContains(controllerRootText, "internal sealed partial class CaptureOptionBindingController");
        AssertContains(controllerRootText, "private readonly CaptureOptionBindingControllerContext _context;");
        AssertContains(controllerRootText, "public CaptureOptionBindingController(CaptureOptionBindingControllerContext context)");
        AssertContains(controllerContextText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerInitializationText, "public void InitializeCollections()");
        AssertContains(controllerInitializationText, "public void ApplyInitialSelections()");
        AssertContains(controllerInitializationText, "public void EnsureInitialSelections()");
        AssertContains(controllerSelectionHandlersText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerRecordingOptionsText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerRecordingOptionsText, "public void HandleCustomBitratePropertyChanged()");
        AssertContains(controllerRecordingOptionsText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");
        AssertContains(controllerHdrText, "private void AttachHdrToggleBindings()");
        AssertContains(controllerHdrText, "public void HandleHdrEnabledChanged()");
        AssertContains(controllerHdrText, "public void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(controllerShowAllText, "public void AttachShowAllCaptureOptionsBinding()");
        AssertContains(controllerShowAllText, "public void HandleShowAllCaptureOptionsChanged()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void InitializeCollections()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void ApplyInitialSelections()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void EnsureInitialSelections()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void AttachShowAllCaptureOptionsBinding()");
        AssertDoesNotContain(controllerSelectionHandlersText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(controllerSelectionHandlersText, "_context.HdrToggle.Click +=");
        AssertDoesNotContain(controllerSelectionHandlersText, "_context.TrueHdrPreviewToggle.Click +=");
        AssertDoesNotContain(controllerRecordingOptionsText, "public void AttachCaptureModeSelectionBindings()");
        AssertDoesNotContain(controllerInitializationText, "public void AttachCaptureModeSelectionBindings()");
        AssertDoesNotContain(controllerInitializationText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerInitializationText, "public void HandleHdrEnabledChanged()");
        AssertDoesNotContain(controllerInitializationText, "public void HandleShowAllCaptureOptionsChanged()");
        AssertDoesNotContain(controllerRootText, "public void InitializeCollections()");
        AssertDoesNotContain(controllerRootText, "public void ApplyInitialSelections()");
        AssertDoesNotContain(controllerRootText, "public void EnsureInitialSelections()");
        AssertDoesNotContain(controllerRootText, "public void AttachCaptureModeSelectionBindings()");
        AssertDoesNotContain(controllerRootText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerRootText, "public void AttachShowAllCaptureOptionsBinding()");
        AssertDoesNotContain(controllerRootText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerText, "public void InitializeCollections()");
        AssertContains(controllerText, "_context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;");
        AssertContains(controllerText, "for (var i = 1; i <= 8; i++)");
        AssertContains(controllerText, "_context.DecoderCountComboBox.Items.Add(i);");
        AssertContains(controllerText, "public void ApplyInitialSelections()");
        AssertContains(controllerText, "_context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");
        AssertContains(controllerText, "_context.ShowAllCaptureOptionsToggle.IsChecked = _context.ViewModel.ShowAllCaptureOptions;");
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
        AssertContains(controllerText, "if (CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(frameRate))");
        AssertContains(controllerText, "if (!_context.ViewModel.IsAutoFrameRateSelected)");
        AssertContains(controllerText, "else if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))");
        AssertDoesNotContain(controllerSelectionHandlersText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerSelectionHandlersText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(controllerSelectionHandlersText, "=> option.Value <= 0 || option.FriendlyValue <= 0;");
        AssertContains(controllerText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(controllerText, "_context.VideoFormatComboBox.SelectionChanged +=");
        AssertContains(controllerText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(controllerText, "if (!double.IsNaN(_context.CustomBitrateNumberBox.Value))");
        AssertContains(controllerText, "_context.HdrToggle.Click +=");
        AssertContains(controllerText, "_context.TrueHdrPreviewToggle.Click +=");
        AssertContains(controllerText, "if (_context.HdrToggle.IsChecked != _context.ViewModel.IsHdrEnabled)");
        AssertContains(controllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerText, "if (_context.TrueHdrPreviewToggle.IsChecked != _context.ViewModel.IsTrueHdrPreviewEnabled)");
        AssertContains(controllerText, "_context.SetHdrPassthroughEnabled(_context.ViewModel.IsTrueHdrPreviewEnabled);");
        AssertContains(controllerText, "_context.ShowAllCaptureOptionsToggle.Click +=");
        AssertContains(controllerText, "_context.ViewModel.ShowAllCaptureOptions = _context.ShowAllCaptureOptionsToggle.IsChecked == true;");
        AssertContains(controllerText, "public void HandleShowAllCaptureOptionsChanged()");
        AssertContains(controllerText, "(_context.ShowAllCaptureOptionsToggle.IsChecked == true) != _context.ViewModel.ShowAllCaptureOptions");
        AssertContains(controllerText, "_context.ShowAllCaptureOptionsToggle.IsChecked = _context.ViewModel.ShowAllCaptureOptions;");
        AssertContains(controllerText, "public void HandleCustomBitratePropertyChanged()");
        AssertContains(controllerText, "Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");

        AssertContains(bindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(bindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(bindingsText, "AttachRecordingOptionBindings();");
        AssertContains(bindingsText, "AttachShowAllCaptureOptionsBinding();");
        AssertOccursBefore(bindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(bindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(bindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "AttachAudioInputToggleBindings();", "AttachShowAllCaptureOptionsBinding();");
        AssertOccursBefore(bindingsText, "AttachShowAllCaptureOptionsBinding();", "AttachFlashbackSettingsBindings();");
        AssertDoesNotContain(selectionBindingFamilyText, "public void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionBindingFamilyText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertDoesNotContain(selectionBindingFamilyText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertContains(agentMapText, "`CaptureOptionBindingController.RecordingOptions.cs` owns recording option");
        AssertContains(agentMapText, "event bindings for format, quality, preset, split-encode, video format, and\n  custom bitrate plus custom-bitrate property-change value projection");
        AssertDoesNotContain(agentMapText, "Recording format, quality, preset, and split-encode string\n  selection handlers live with\n  `CaptureSelectionBindingController`");
        AssertContains(cleanupPlanText, "`CaptureOptionBindingController.RecordingOptions.cs` owns recording option event\nbindings for format, quality, preset, split-encode, video format, and custom\nbitrate plus custom-bitrate property-change value projection");
        AssertDoesNotContain(cleanupPlanText, "delegates recording\nformat/quality/preset/split-encode string selection to\n`CaptureSelectionBindingController`");

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
        AssertDoesNotContain(propertyChangedText, "CustomBitrateNumberBox.Value");
        AssertDoesNotContain(propertyChangedText, "Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(propertyChangedText, "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertContains(captureOptionPropertyChangedText, "HandleCustomBitratePropertyChanged();");
        AssertContains(captureOptionPropertyChangedText, "HandleHdrEnabledChanged();");
        AssertContains(captureOptionPropertyChangedText, "HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(captureOptionPropertyChangedText, "HandleShowAllCaptureOptionsChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedText, "HdrToggle.IsChecked = ViewModel.IsHdrEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedText, "TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedText, "_previewRendererHostController.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);");
        AssertDoesNotContain(captureOptionPropertyChangedText, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");
        AssertDoesNotContain(bindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");

        return Task.CompletedTask;
    }
}
