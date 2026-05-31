using System.IO;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var controllerText = controllerRootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");
        var selectionBindingFamilyText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n"));
        var captureOptionBindingsWithoutVideoFormat = captureOptionBindingsText.Replace("VideoFormatComboBox.SelectionChanged +=", string.Empty);
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");

        AssertContains(captureOptionBindingsText, "private CaptureOptionBindingController _captureOptionBindingController = null!;");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionBindingController()");
        AssertContains(captureOptionBindingsText, "ResolutionComboBox = ResolutionComboBox,");
        AssertContains(captureOptionBindingsText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionBindingsText, "TrueHdrPreviewToggle = TrueHdrPreviewToggle,");
        AssertContains(captureOptionBindingsText, "ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,");
        AssertContains(captureOptionBindingsText, "ApplyAudioClipVisibility = ApplyAudioClipVisibility,");
        AssertContains(captureOptionBindingsText, "RefreshHdrHintText = RefreshHdrHintText,");
        AssertContains(captureOptionBindingsText, "UpdateFpsTelemetryTooltip = UpdateFpsTelemetryTooltip,");
        AssertContains(captureOptionBindingsText, "UpdateVideoContentOverlays = UpdateVideoContentOverlays,");
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
        AssertContains(captureOptionBindingsText, "private void HandleCustomBitratePropertyChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleCustomBitratePropertyChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleHdrEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleHdrEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(captureOptionBindingsText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionPropertyChangedMethod, "=> _captureOptionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertContains(captureOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachRecordingOptionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureOptionBindingController();");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureOptionBindings.cs")), "MainWindow capture option adapter folded into MainWindow.xaml.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.xaml.cs");

        AssertContains(controllerRootText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerRootText, "internal sealed class CaptureOptionBindingController");
        AssertContains(controllerRootText, "private readonly CaptureOptionBindingControllerContext _context;");
        AssertContains(controllerRootText, "public CaptureOptionBindingController(CaptureOptionBindingControllerContext context)");
        AssertContains(controllerRootText, "public void InitializeCollections()");
        AssertContains(controllerRootText, "_context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;");
        AssertContains(controllerRootText, "for (var i = 1; i <= 8; i++)");
        AssertContains(controllerRootText, "_context.DecoderCountComboBox.Items.Add(i);");
        AssertContains(controllerRootText, "public void ApplyInitialSelections()");
        AssertContains(controllerRootText, "_context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;");
        AssertContains(controllerRootText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");
        AssertContains(controllerRootText, "_context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;");
        AssertContains(controllerRootText, "_context.ApplyInitialDecoderCountSelection();");
        AssertContains(controllerRootText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerRootText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerRootText, "public void EnsureInitialSelections()");
        AssertContains(controllerRootText, "_context.EnsureSplitEncodeModeSelection();");
        AssertContains(controllerRootText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerRootText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerRootText, "_context.ResolutionComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "_context.FrameRateComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "!string.Equals(resolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase)");
        AssertContains(controllerRootText, "if (CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(frameRate))");
        AssertContains(controllerRootText, "if (!_context.ViewModel.IsAutoFrameRateSelected)");
        AssertContains(controllerRootText, "else if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))");
        AssertContains(controllerRootText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerRootText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(controllerRootText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(controllerRootText, "_context.VideoFormatComboBox.SelectionChanged +=");
        AssertContains(controllerRootText, "_context.CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(controllerRootText, "_context.HdrToggle.Click +=");
        AssertContains(controllerRootText, "_context.TrueHdrPreviewToggle.Click +=");
        AssertContains(controllerRootText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerRootText, "case nameof(MainViewModel.AudioClipping):");
        AssertContains(controllerRootText, "_context.ApplyAudioClipVisibility();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsHdrAvailable):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceIsHdr):");
        AssertContains(controllerRootText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsHdrEnabled):");
        AssertContains(controllerRootText, "HandleHdrEnabledChanged();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsTrueHdrPreviewEnabled):");
        AssertContains(controllerRootText, "HandleTrueHdrPreviewEnabledChanged();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrResolutionSupportHint):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrReadinessReason):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.HdrRuntimeState):");
        AssertContains(controllerRootText, "_context.RefreshHdrHintText();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceTelemetrySummaryText):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceTargetSummaryText):");
        AssertContains(controllerRootText, "_context.UpdateFpsTelemetryTooltip();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceWidth):");
        AssertContains(controllerRootText, "case nameof(MainViewModel.SourceHeight):");
        AssertContains(controllerRootText, "_context.UpdateVideoContentOverlays();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.IsCustomBitrateVisible):");
        AssertContains(controllerRootText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerRootText, "case nameof(MainViewModel.CustomBitrateMbps):");
        AssertContains(controllerRootText, "public void HandleCustomBitratePropertyChanged()");
        AssertContains(controllerRootText, "public void HandleHdrEnabledChanged()");
        AssertContains(controllerRootText, "public void HandleTrueHdrPreviewEnabledChanged()");
        AssertContains(controllerRootText, "private void AttachHdrToggleBindings()");
        AssertContains(controllerRootText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");
        AssertDoesNotContain(controllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertContains(setupBindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(setupBindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(setupBindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(setupBindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(setupBindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(setupBindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(setupBindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(setupBindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(setupBindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertDoesNotContain(selectionBindingFamilyText, "public void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionBindingFamilyText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertDoesNotContain(selectionBindingFamilyText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertContains(agentMapText, "`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` owns the");
        AssertContains(agentMapText, "capture option binding adapter context, setup, UI event attachment");
        AssertContains(agentMapText, "capture-option/source-signal property-change routing");
        AssertContains(agentMapText, "`Sussudio/MainWindow.xaml.cs` is the XAML-facing adapter");
        AssertContains(agentMapText, "option binding adapter context");
        AssertContains(agentMapText, "recording option event");
        AssertContains(agentMapText, "HDR/true-HDR click binding");
        AssertContains(agentMapText, "delegated presentation callbacks for option");
        AssertContains(agentMapText, "affordances, telemetry tooltips, and source overlay refreshes");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.Context.cs");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.Bindings.cs");
        AssertDoesNotContain(agentMapText, "CaptureOptionBindingController.PropertyChanges.cs");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`. It keeps the");
        AssertContains(cleanupPlanText, "capture-option binding adapter context, video-format and initial decoder");
        AssertContains(cleanupPlanText, "video-format and initial decoder");
        AssertContains(cleanupPlanText, "projection, initial selection projection");
        AssertContains(cleanupPlanText, "resolution/frame-rate selection");
        AssertContains(cleanupPlanText, "handlers, recording option event bindings for format, quality, preset");
        AssertContains(cleanupPlanText, "split-encode, video format, and custom bitrate");
        AssertContains(cleanupPlanText, "HDR/true-HDR click binding");
        AssertContains(cleanupPlanText, "`ShowAllCaptureOptionsToggle` click binding");
        AssertContains(cleanupPlanText, "capture-option/source-signal");
        AssertContains(cleanupPlanText, "property-change routing, custom-bitrate property-change value projection");
        AssertContains(cleanupPlanText, "preview HDR passthrough forwarding");
        AssertContains(cleanupPlanText, "presentation callback routing for option affordances, telemetry tooltips, and");
        AssertContains(cleanupPlanText, "source overlay refreshes");
        AssertContains(cleanupPlanText, "`Sussudio/MainWindow.xaml.cs` now owns the XAML-facing");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.Context.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.Bindings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureOptionBindingController.PropertyChanges.cs");

        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(captureOptionBindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedFrameRate =");
        AssertDoesNotContain(captureOptionBindingsWithoutVideoFormat, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(captureOptionBindingsText, "TrueHdrPreviewToggle.Click +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedRecordingFormat =");
        AssertDoesNotContain(captureOptionBindingsText, "QualityComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "PresetComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "SplitEncodeComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleCustomBitratePropertyChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleHdrEnabledChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleTrueHdrPreviewEnabledChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HandleShowAllCaptureOptionsChanged();");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "HdrToggle.IsChecked = ViewModel.IsHdrEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "_previewRendererHostController.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);");
        AssertDoesNotContain(captureOptionPropertyChangedMethod, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");
        AssertDoesNotContain(propertyChangedText, "CustomBitrateNumberBox.Value");
        AssertDoesNotContain(propertyChangedText, "Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(propertyChangedText, "TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,");
        AssertDoesNotContain(bindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.Click +=");
        AssertDoesNotContain(bindingsText, "ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;");

        return Task.CompletedTask;
    }

    internal static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureDeviceActionInit = ExtractMemberCode(adapterText, "InitializeCaptureDeviceActionController");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

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
        AssertDoesNotContain(captureDeviceActionInit, "UpdateDeviceApplyButtonState();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureDeviceActions.cs")),
            "capture-device button adapter folded into MainWindow.xaml.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var setupBindingsText = ExtractMemberCode(bindingsText, "SetupBindings");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var policyText = controllerText;
        const string tooltipFormatterMarker = "internal static class CaptureOptionTooltipFormatter";
        var tooltipFormatterStart = controllerText.IndexOf(tooltipFormatterMarker, System.StringComparison.Ordinal);
        if (tooltipFormatterStart < 0)
        {
            throw new System.InvalidOperationException("CaptureOptionTooltipFormatter was not found in CaptureOptionBindingController.cs.");
        }

        var tooltipFormatterText = controllerText[tooltipFormatterStart..];
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");
        var outputPathDisplayText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureOptionPresentationController.cs")),
            "capture option presentation policy and controller folded into CaptureOptionBindingController.cs");
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
        AssertDoesNotContain(setupBindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(setupBindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(setupBindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(setupBindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(setupBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
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
    internal static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void AttachDeviceSelectionChangedBinding()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.AttachDeviceSelectionChangedBinding();");
        AssertContains(adapterText, "private void HandleSelectedDevicePropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailablePresetsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(adapterText, "private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.xaml.cs");

        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(bindingsText, "AttachDeviceSelectionChangedBinding();");
        AssertContains(propertyChangedText, "TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "private readonly CaptureSelectionBindingControllerContext _context;");
        AssertContains(controllerText, "public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)");
        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingControllerContext");

        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(propertyChangedText, "DEVICE_SELECTION_SYNC");
        AssertDoesNotContain(propertyChangedText, "DeviceComboBox.SelectedItem");
        AssertDoesNotContain(propertyChangedText, "ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;");
        AssertDoesNotContain(propertyChangedText, "FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;");
        AssertDoesNotContain(propertyChangedText, "PresetComboBox.ItemsSource = ViewModel.AvailablePresets;");
        AssertDoesNotContain(propertyChangedText, "SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertContains(controllerText, "public void EnsureDeviceAudioModeSelection()");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingCollectionSync_LivesInControllerPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)");
        AssertContains(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertContains(controllerText, "public void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(controllerText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;");
        AssertContains(controllerText, "EnsureResolutionSelection();");
        AssertContains(controllerText, "public void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(controllerText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;");
        AssertContains(controllerText, "EnsureFrameRateSelection();");
        AssertContains(controllerText, "public void HandleAvailablePresetsPropertyChanged()");
        AssertContains(controllerText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;");
        AssertContains(controllerText, "EnsurePresetSelection();");
        AssertContains(controllerText, "public void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(controllerText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;");
        AssertContains(controllerText, "EnsureSplitEncodeModeSelection();");
        AssertOccursBefore(controllerText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;", "EnsureResolutionSelection();");
        AssertOccursBefore(controllerText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;", "EnsureFrameRateSelection();");
        AssertOccursBefore(controllerText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;", "EnsurePresetSelection();");
        AssertOccursBefore(controllerText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;", "EnsureSplitEncodeModeSelection();");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureSelectionBindingController.SelectionState.cs")),
            "empty selection-state marker partial should stay removed");
        AssertDoesNotContain(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(adapterText, "_captureSelectionBindingController.AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingPropertyRouter_LivesInController()
    {
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedRouteText = ExtractMemberCode(propertyChangedText, "RouteAsync");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(propertyChangesText, "public bool TryHandlePropertyChanged(string? propertyName)");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedFrameRate):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDeviceAudioMode):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AnalogAudioGainPercent):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):", "case nameof(MainViewModel.SelectedResolution):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):", "case nameof(MainViewModel.SelectedFrameRate):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):", "case nameof(MainViewModel.AvailableResolutions):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):", "case nameof(MainViewModel.AvailableFrameRates):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):", "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):", "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):", "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):", "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):", "case nameof(MainViewModel.SelectedQuality):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):", "case nameof(MainViewModel.AvailablePresets):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):", "case nameof(MainViewModel.SelectedPreset):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):", "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):", "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertContains(propertyChangesText, "HandleSelectedDevicePropertyChanged();");
        AssertContains(propertyChangesText, "EnsureResolutionSelection();");
        AssertContains(propertyChangesText, "EnsureFrameRateSelection();");
        AssertContains(propertyChangesText, "HandleAvailableResolutionsPropertyChanged();");
        AssertContains(propertyChangesText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(propertyChangesText, "ApplyDeviceAudioControlState();");
        AssertContains(propertyChangesText, "EnsureAudioInputSelection();");
        AssertContains(propertyChangesText, "EnsureMicrophoneSelection();");
        AssertContains(propertyChangesText, "EnsureFormatSelection();");
        AssertContains(propertyChangesText, "EnsureQualitySelection();");
        AssertContains(propertyChangesText, "HandleAvailablePresetsPropertyChanged();");
        AssertContains(propertyChangesText, "EnsurePresetSelection();");
        AssertContains(propertyChangesText, "HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(propertyChangesText, "EnsureSplitEncodeModeSelection();");

        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(propertyChangedRouteText, "HandleSelectedDevicePropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableResolutionsPropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertDoesNotContain(propertyChangedRouteText, "ApplyDeviceAudioControlState();");
        AssertDoesNotContain(propertyChangedRouteText, "EnsureFormatSelection();");
        AssertDoesNotContain(propertyChangedRouteText, "HandleAvailableSplitEncodeModesPropertyChanged();");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var selectionNormalizerText = controllerText.Substring(
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));
        var bindingControllerText = controllerText.Substring(
            0,
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void EnsureDeviceSelection()");
        AssertContains(controllerText, "public void AttachDeviceSelectionChangedBinding()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public void HandleSelectedDevicePropertyChanged()");
        AssertContains(controllerText, "DEVICE_SELECTION_SYNC");
        AssertContains(controllerText, "EnsureDeviceSelection();");
        AssertContains(controllerText, "UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(controllerText, "public void UpdateDeviceApplyButtonState()");
        var selectedDevicePropertyChangedText = controllerText.Substring(
            controllerText.IndexOf("public void HandleSelectedDevicePropertyChanged()", System.StringComparison.Ordinal));
        AssertOccursBefore(selectedDevicePropertyChangedText, "DEVICE_SELECTION_SYNC", "EnsureDeviceSelection();");
        AssertOccursBefore(selectedDevicePropertyChangedText, "EnsureDeviceSelection();", "UpdateDeviceApplyButtonState();");
        AssertOccursBefore(controllerText, "public void EnsureDeviceSelection()", "public void HandleSelectedDevicePropertyChanged()");

        AssertContains(controllerText, "public void EnsureAudioInputSelection()");
        AssertContains(controllerText, "public void EnsureMicrophoneSelection()");
        AssertOccursBefore(controllerText, "public void EnsureAudioInputSelection()", "public void EnsureMicrophoneSelection()");

        AssertContains(controllerText, "public void EnsureResolutionSelection()");
        AssertContains(controllerText, "public void EnsureFrameRateSelection()");
        AssertOccursBefore(controllerText, "public void EnsureResolutionSelection()", "public void EnsureFrameRateSelection()");

        AssertContains(controllerText, "public void EnsureFormatSelection()");
        AssertContains(controllerText, "public void EnsureQualitySelection()");
        AssertContains(controllerText, "public void EnsurePresetSelection()");
        AssertContains(controllerText, "public void EnsureSplitEncodeModeSelection()");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);");
        AssertOccursBefore(controllerText, "public void EnsureFormatSelection()", "public void EnsureQualitySelection()");
        AssertOccursBefore(controllerText, "public void EnsureQualitySelection()", "public void EnsurePresetSelection()");
        AssertOccursBefore(controllerText, "public void EnsurePresetSelection()", "public void EnsureSplitEncodeModeSelection()");

        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "internal static class CaptureComboBoxSelectionNormalizer");
        AssertContains(selectionNormalizerText, "public static CaptureDevice? ResolveCaptureDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static AudioInputDevice? ResolveAudioInputDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static ResolutionOption? ResolveResolutionSelection(");
        AssertContains(selectionNormalizerText, "public static FrameRateOption? ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "public static string? ResolveStringSelection(");
        AssertContains(selectionNormalizerText, "public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(selectionNormalizerText, "public static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertDoesNotContain(bindingsText, "DeviceComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingControllerText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(bindingControllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(bindingControllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(bindingControllerText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(bindingControllerText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(bindingControllerText, "AvailableFrameRates.FirstOrDefault(option =>");

        return Task.CompletedTask;
    }

    internal static Task CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks()
    {
        var normalizerType = RequireType("Sussudio.Controllers.CaptureComboBoxSelectionNormalizer");
        var captureDeviceType = RequireType("Sussudio.Models.CaptureDevice");
        var audioInputDeviceType = RequireType("Sussudio.Models.AudioInputDevice");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var resolveCaptureDevice = RequireNormalizerMethod(normalizerType, "ResolveCaptureDeviceSelection");
        var resolveAudioInputDevice = RequireNormalizerMethod(normalizerType, "ResolveAudioInputDeviceSelection");
        var resolveResolution = RequireNormalizerMethod(normalizerType, "ResolveResolutionSelection");
        var resolveFrameRate = RequireNormalizerMethod(normalizerType, "ResolveFrameRateSelection");
        var resolveString = RequireNormalizerMethod(normalizerType, "ResolveStringSelection");

        var staleCaptureDevice = CreateNormalizerDevice(captureDeviceType, "DEVICE-A", "old device");
        var firstCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-b", "first device");
        var liveCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-a", "live device");
        var captureDevices = CreateNormalizerList(captureDeviceType, firstCaptureDevice, liveCaptureDevice);
        AssertEqual(
            liveCaptureDevice,
            resolveCaptureDevice.Invoke(null, new[] { captureDevices, staleCaptureDevice }),
            "capture-device matching returns live collection instance by case-insensitive id");

        var staleAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "MIC-1", "old mic");
        var firstAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "line-1", "first input");
        var liveAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "mic-1", "live mic");
        var audioDevices = CreateNormalizerList(audioInputDeviceType, firstAudioDevice, liveAudioDevice);
        AssertEqual(
            liveAudioDevice,
            resolveAudioInputDevice.Invoke(null, new[] { audioDevices, staleAudioDevice }),
            "audio-device matching returns live collection instance by case-insensitive id");

        var disabledExactResolution = CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: false);
        var enabledFallbackResolution = CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true);
        var resolutionOptions = CreateResolutionOptionList(resolutionType, disabledExactResolution, enabledFallbackResolution);
        AssertEqual(
            disabledExactResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "3840X2160" }),
            "resolution exact selected value wins before enabled fallback");
        AssertEqual(
            enabledFallbackResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "1280x720" }),
            "resolution falls back to first enabled value");

        var disabledExactFrameRate = CreateFrameRateOption(frameRateType, 60d, 59.94d, "60000/1001", isEnabled: false);
        var autoFrameRate = CreateFrameRateOption(frameRateType, 0d, 0d, string.Empty, isEnabled: true);
        var enabledFrameRate = CreateFrameRateOption(frameRateType, 120d, 120d, "120/1", isEnabled: true);
        var frameRateOptions = CreateFrameRateOptionList(frameRateType, disabledExactFrameRate, autoFrameRate, enabledFrameRate);
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, true }),
            "auto frame-rate item wins when auto frame-rate is selected");
        AssertEqual(
            disabledExactFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, false }),
            "frame-rate exact selected value wins before enabled fallback");
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 30d, false }),
            "frame-rate fallback preserves first enabled item ordering");

        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "quality" }),
            "string fallback is case-insensitive");
        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "Missing" }),
            "string fallback uses the first item when no case-insensitive match exists");

        return Task.CompletedTask;
    }

    private static MethodInfo RequireNormalizerMethod(Type normalizerType, string methodName)
        => normalizerType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"CaptureComboBoxSelectionNormalizer.{methodName} was not found.");

    private static object CreateNormalizerDevice(Type deviceType, string id, string name)
    {
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException($"Failed to create {deviceType.Name}.");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", name);
        return device;
    }

    private static object CreateNormalizerList(Type elementType, params object[] items)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
                           ?? throw new InvalidOperationException($"Failed to create list for {elementType.Name}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }
}
