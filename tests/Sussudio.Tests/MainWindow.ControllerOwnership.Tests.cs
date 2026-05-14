using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordingActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/RecordingButtonActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingButtonActionController _recordingButtonActionController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingButtonActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(");
        AssertContains(adapterText, "_d3dRenderer != null && PreviewSwapChainPanel.Visibility == Visibility.Visible");
        AssertContains(adapterText, "_previewSource != null && PreviewImage.Visibility == Visibility.Visible");
        AssertContains(adapterText, "NoDevicePlaceholder.Visibility == Visibility.Visible");
        AssertContains(adapterText, "private Task ToggleRecordingFromButtonAsync()");
        AssertContains(adapterText, "=> _recordingButtonActionController.ToggleRecordingAsync();");
        AssertContains(mainWindowText, "InitializeRecordingButtonActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));");
        AssertContains(controllerText, "internal readonly record struct RecordingPreviewActivitySnapshot");
        AssertContains(controllerText, "public bool RendererActive => GpuActive || CpuActive;");
        AssertContains(controllerText, "public async Task ToggleRecordingAsync()");
        AssertContains(controllerText, "await _context.ViewModel.ToggleRecordingAsync();");
        AssertContains(controllerText, "if (!_context.ViewModel.IsRecording)");
        AssertContains(controllerText, "PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}");
        AssertContains(controllerText, "WARNING: preview renderer appears inactive while recording.");
        AssertDoesNotContain(eventHandlersText, "ViewModel.ToggleRecordingAsync();");
        AssertDoesNotContain(eventHandlersText, "PreviewStateDuringRecording");
        AssertDoesNotContain(eventHandlersText, "WARNING: preview renderer appears inactive while recording.");

        return Task.CompletedTask;
    }

    private static Task LiveSignalInfoPresentation_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var liveSignalAdapterText = ReadRepoFile("Sussudio/MainWindow.LiveSignalInfo.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/LiveSignalInfoController.cs").Replace("\r\n", "\n");

        AssertContains(liveSignalAdapterText, "private LiveSignalInfoController _liveSignalInfoController = null!;");
        AssertContains(liveSignalAdapterText, "private void InitializeLiveSignalInfoController()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.Update(");
        AssertContains(liveSignalAdapterText, "ViewModel.LiveResolution,");
        AssertContains(liveSignalAdapterText, "private void StopLiveSignalInfoTimers()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.StopTimers();");
        AssertContains(mainWindowText, "InitializeLiveSignalInfoController();");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(controllerText, "internal sealed class LiveSignalInfoController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _showDebounceTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _hideDebounceTimer;");
        AssertContains(controllerText, "public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "private bool HasCompleteLiveSignal()");
        AssertContains(controllerText, "private void AnimateIn()");
        AssertContains(controllerText, "private void AnimateOut()");
        AssertDoesNotContain(mainWindowText, "private bool _liveSignalInfoVisible;");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _liveSignalDebounceTimer;");
        AssertDoesNotContain(animationsText, "private void UpdateLiveSignalInfoVisibility()");
        AssertDoesNotContain(animationsText, "private void AnimateLiveSignalInfoIn()");
        AssertDoesNotContain(animationsText, "private void AnimateLiveSignalInfoOut()");

        return Task.CompletedTask;
    }

    private static Task PreviewAudioFadeState_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewAudioFade.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewAudioFadeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewAudioFadeController _previewAudioFadeController = null!;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;");
        AssertContains(adapterText, "private void InitializePreviewAudioFadeController()");
        AssertContains(adapterText, "=> _previewAudioFadeController.PrimeFadeIn();");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeIn(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeOutAsync(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.CancelFadeInForUser();");
        AssertContains(mainWindowText, "InitializePreviewAudioFadeController();");
        AssertContains(bindingsText, "IsPreviewAudioFadeInActive || IsPreviewAudioFadeAnimationActive");
        AssertContains(propertyChangedText, "await HandlePreviewingChangedAsync();");
        AssertContains(propertyChangedText, "HandlePreviewVolumeChanged();");
        AssertContains(audioPropertyChangedText, "if (IsPreviewAudioFadeInActive)");
        AssertContains(previewPropertyChangedText, "PrimePreviewAudioFadeIn();");
        AssertContains(controllerText, "internal sealed class PreviewAudioFadeController");
        AssertContains(controllerText, "private double _savedPreviewVolume;");
        AssertContains(controllerText, "private Storyboard? _volumeFadeStoryboard;");
        AssertContains(controllerText, "public void PrimeFadeIn()");
        AssertContains(controllerText, "public async Task StartFadeOutAsync(int durationMs = 450)");
        AssertContains(controllerText, "Sussudio.Logger.Log(\"PREVIEW_AUDIO_FADE_OUT_COMPLETED\");");
        AssertDoesNotContain(mainWindowText, "private double _savedPreviewVolume;");
        AssertDoesNotContain(mainWindowText, "private bool _isVolumeFadingIn;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _previewVolumeFadeStoryboard;");
        AssertDoesNotContain(animationsText, "private void PrimePreviewAudioFadeIn()");
        AssertDoesNotContain(animationsText, "private void CompletePreviewAudioFadeIn(");
        AssertDoesNotContain(animationsText, "private async Task StartPreviewAudioFadeOutAsync(");

        return Task.CompletedTask;
    }

    private static Task MicrophoneControls_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.MicrophoneControls.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/MicrophoneControlsController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private MicrophoneControlsController _microphoneControlsController = null!;");
        AssertContains(adapterText, "private void InitializeMicrophoneControlsController()");
        AssertContains(adapterText, "=> _microphoneControlsController.AttachVolumeBindings();");
        AssertContains(adapterText, "=> _microphoneControlsController.SyncVolumeControls(volumePercent);");
        AssertContains(adapterText, "=> _microphoneControlsController.ApplyInitialVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.UpdateVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.StopRowAnimation();");
        AssertContains(mainWindowText, "InitializeMicrophoneControlsController();");
        AssertContains(bindingsText, "SetupMicrophoneVolumeBindings();");
        AssertContains(bindingsText, "ApplyInitialMicrophoneControlsVisibility();");
        AssertContains(propertyChangedText, "HandleMicrophoneEnabledChanged();");
        AssertContains(propertyChangedText, "HandleMicrophoneVolumeChanged();");
        AssertContains(audioPropertyChangedText, "UpdateMicrophoneControlsVisibility();");
        AssertContains(audioPropertyChangedText, "SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(controllerText, "internal sealed class MicrophoneControlsController");
        AssertContains(controllerText, "private bool _syncingVolumeControls;");
        AssertContains(controllerText, "private Storyboard? _activeRowStoryboard;");
        AssertContains(controllerText, "public void AttachVolumeBindings()");
        AssertContains(controllerText, "public void SyncVolumeControls(double volumePercent)");
        AssertContains(controllerText, "public void ApplyInitialVisibility()");
        AssertContains(controllerText, "public void UpdateVisibility()");
        AssertContains(controllerText, "public void StopRowAnimation()");
        AssertContains(controllerText, "private Storyboard CreateRowStoryboard(bool showing)");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _micMeterRowStoryboard;");
        AssertDoesNotContain(mainWindowText, "private bool _syncingMicrophoneVolumeControls;");
        AssertDoesNotContain(mainWindowText, "private const double MicMeterRowHeight = 14;");
        AssertDoesNotContain(bindingsText, "MicVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "private void SyncMicrophoneVolumeControls(double volumePercent)");
        AssertDoesNotContain(bindingsText, "private Storyboard CreateMicMeterRowStoryboard(bool showing)");

        return Task.CompletedTask;
    }

    private static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ResponsiveShellLayout.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ResponsiveShellLayoutController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "=> _responsiveShellLayoutController.Attach();");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertContains(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertContains(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");

        return Task.CompletedTask;
    }

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
        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(propertyChangedText, "EnsureResolutionSelection();");
        AssertContains(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertContains(controllerText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(contextText, "internal sealed class CaptureSelectionBindingControllerContext");
        AssertContains(selectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
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
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
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
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(eventHandlersText, "UpdateDeviceApplyButtonState();");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionPresentation_LivesInFocusedPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionPresentation.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertContains(captureOptionText, "private double GetSelectedFriendlyFrameRate()");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(captureOptionText, "ViewModel.SourceTelemetrySummaryText");
        AssertContains(bindingsText, "ApplyHdrToggleEnabledState();");
        AssertContains(bindingsText, "RefreshHdrHintText();");
        AssertContains(bindingsText, "UpdateFpsTelemetryTooltip();");
        AssertContains(bindingsText, "ApplyBitrateVisibility();");
        AssertContains(bindingsText, "ApplyAudioClipVisibility();");
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
        AssertDoesNotContain(ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n"), "private int _selectedDecoderCount = 4;");

        return Task.CompletedTask;
    }

    private static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathDisplay.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathDisplayController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathDisplayController _outputPathDisplayController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathDisplayController()");
        AssertContains(adapterText, "OutputPathTextBox = OutputPathTextBox,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "private void AttachOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Attach();");
        AssertContains(adapterText, "private void UpdateOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Update();");
        AssertContains(mainWindowText, "InitializeOutputPathDisplayController();");
        AssertContains(bindingsText, "AttachOutputPathDisplay();");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(controllerText, "internal sealed class OutputPathDisplayController");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "public void Update()");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.OutputPathTextBox, path);");
        AssertContains(controllerText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertContains(controllerText, "var parts = path.Split('\\\\', '/');");
        AssertContains(controllerText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(bindingsText, "OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();");
        AssertDoesNotContain(bindingsText, "private void UpdateOutputPathDisplay()");
        AssertDoesNotContain(bindingsText, "ToolTipService.SetToolTip(OutputPathTextBox, path);");

        return Task.CompletedTask;
    }

    private static Task OutputPathButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathActionController _outputPathActionController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()");
        AssertContains(adapterText, "private Task BrowseOutputPathFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.BrowseAsync();");
        AssertContains(adapterText, "private Task OpenRecordingsFolderFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.OpenRecordingsFolderIfAvailableAsync();");
        AssertContains(mainWindowText, "InitializeOutputPathActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));");
        AssertContains(controllerText, "internal sealed class OutputPathActionController");
        AssertContains(controllerText, "public Task BrowseAsync()");
        AssertContains(controllerText, "=> _context.ViewModel.BrowseOutputPathAsync();");
        AssertContains(controllerText, "public Task OpenRecordingsFolderIfAvailableAsync()");
        AssertContains(controllerText, "string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)");
        AssertContains(controllerText, "return _context.OpenRecordingsFolderAsync();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.BrowseOutputPathAsync()");
        AssertDoesNotContain(eventHandlersText, "System.IO.Directory.Exists(path)");

        return Task.CompletedTask;
    }

    private static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewScreenshot.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewScreenshotController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewScreenshotController _previewScreenshotController = null!;");
        AssertContains(adapterText, "private void InitializePreviewScreenshotController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "ScreenshotButton = ScreenshotButton,");
        AssertContains(adapterText, "private Task CapturePreviewScreenshotAsync()");
        AssertContains(adapterText, "=> _previewScreenshotController.CaptureAsync();");
        AssertContains(mainWindowText, "InitializePreviewScreenshotController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));");
        AssertContains(controllerText, "internal sealed class PreviewScreenshotController");
        AssertContains(controllerText, "public async Task CaptureAsync()");
        AssertContains(controllerText, "Start preview before capturing a screenshot");
        AssertContains(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertContains(controllerText, "Directory.CreateDirectory(outputDir);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(filePath)");
        AssertContains(controllerText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertContains(controllerText, "SCREENSHOT_SAVED");
        AssertContains(controllerText, "SCREENSHOT_FAILED");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertDoesNotContain(eventHandlersText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(eventHandlersText, "CapturePreviewFrameAsync(filePath)");

        return Task.CompletedTask;
    }
}
