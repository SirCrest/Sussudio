using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureOptionBindings.cs")), "MainWindow capture option adapter folded into MainWindow.ControlBindings.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.ControlBindings.cs");

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

        AssertContains(bindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(bindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(bindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(bindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(bindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertDoesNotContain(selectionBindingFamilyText, "public void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionBindingFamilyText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertDoesNotContain(selectionBindingFamilyText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertContains(agentMapText, "`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` owns the");
        AssertContains(agentMapText, "capture option binding adapter context, setup, UI event attachment");
        AssertContains(agentMapText, "capture-option/source-signal property-change routing");
        AssertContains(agentMapText, "`Sussudio/MainWindow.ControlBindings.cs` is the XAML-facing adapter");
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
        AssertContains(cleanupPlanText, "`Sussudio/MainWindow.ControlBindings.cs` now owns the XAML-facing");
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
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
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
            "capture-device button adapter folded into MainWindow.ControlBindings.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
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
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedMethod = ExtractMemberCode(captureOptionBindingsText, "TryHandleCaptureOptionPropertyChanged");
        var outputPathDisplayText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");

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
