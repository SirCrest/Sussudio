using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordingActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingButtonActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingButtonActionController _recordingButtonActionController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingButtonActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(");
        AssertContains(adapterText, "_previewRendererHostController.HasD3DRenderer && PreviewSwapChainPanel.Visibility == Visibility.Visible");
        AssertContains(adapterText, "_previewRendererHostController.IsCpuPreviewSourceAttached && PreviewImage.Visibility == Visibility.Visible");
        AssertContains(adapterText, "NoDevicePlaceholder.Visibility == Visibility.Visible");
        AssertContains(adapterText, "private Task ToggleRecordingFromButtonAsync()");
        AssertContains(adapterText, "=> _recordingButtonActionController.ToggleRecordingAsync();");
        AssertContains(adapterText, "private void RecordButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));");
        AssertContains(mainWindowText, "InitializeRecordingButtonActionController();");
        AssertContains(controllerText, "internal readonly record struct RecordingPreviewActivitySnapshot");
        AssertContains(controllerText, "public bool RendererActive => GpuActive || CpuActive;");
        AssertContains(controllerText, "public async Task ToggleRecordingAsync()");
        AssertContains(controllerText, "await _context.ViewModel.ToggleRecordingAsync();");
        AssertContains(controllerText, "if (!_context.ViewModel.IsRecording)");
        AssertContains(controllerText, "PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}");
        AssertContains(controllerText, "WARNING: preview renderer appears inactive while recording.");
        AssertDoesNotContain(adapterText, "ViewModel.ToggleRecordingAsync();");
        AssertDoesNotContain(adapterText, "PreviewStateDuringRecording");
        AssertDoesNotContain(adapterText, "WARNING: preview renderer appears inactive while recording.");

        return Task.CompletedTask;
    }

    private static Task LiveSignalInfoPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var liveSignalAdapterText = ReadRepoFile("Sussudio/MainWindow.LiveSignalInfo.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/LiveSignalInfoController.cs").Replace("\r\n", "\n");

        AssertContains(liveSignalAdapterText, "private LiveSignalInfoController _liveSignalInfoController = null!;");
        AssertContains(liveSignalAdapterText, "private void InitializeLiveSignalInfoController()");
        AssertContains(liveSignalAdapterText, "LiveResolutionTextBlock = LiveResolutionTextBlock,");
        AssertContains(liveSignalAdapterText, "LiveFrameRateTextBlock = LiveFrameRateTextBlock,");
        AssertContains(liveSignalAdapterText, "LivePixelFormatTextBlock = LivePixelFormatTextBlock,");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.Update(");
        AssertContains(liveSignalAdapterText, "ViewModel.LiveResolution,");
        AssertContains(liveSignalAdapterText, "private void StopLiveSignalInfoTimers()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.StopTimers();");
        AssertContains(mainWindowText, "InitializeLiveSignalInfoController();");
        AssertContains(bindingsText, "UpdateLiveSignalInfoVisibility();");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(controllerText, "internal sealed class LiveSignalInfoController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _showDebounceTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _hideDebounceTimer;");
        AssertContains(controllerText, "public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "_context.LiveResolutionTextBlock.Text = liveResolution;");
        AssertContains(controllerText, "_context.LiveFrameRateTextBlock.Text = liveFrameRate;");
        AssertContains(controllerText, "_context.LivePixelFormatTextBlock.Text = livePixelFormat;");
        AssertContains(controllerText, "private bool HasCompleteLiveSignal()");
        AssertContains(controllerText, "private void AnimateIn()");
        AssertContains(controllerText, "private void AnimateOut()");
        AssertDoesNotContain(propertyChangedText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(propertyChangedText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(propertyChangedText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(mainWindowText, "private bool _liveSignalInfoVisible;");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _liveSignalDebounceTimer;");

        return Task.CompletedTask;
    }

    private static Task StatusStripPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.StatusStripPresentation.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/StatusStripPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private StatusStripPresentationController _statusStripPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeStatusStripPresentationController()");
        AssertContains(adapterText, "DiskWarningInfoBar = DiskWarningInfoBar,");
        AssertContains(adapterText, "StatusTextBlock = StatusTextBlock,");
        AssertContains(adapterText, "RecordingTimeTextBlock = RecordingTimeTextBlock,");
        AssertContains(adapterText, "DiskSpaceTextBlock = DiskSpaceTextBlock,");
        AssertContains(adapterText, "RecordingSizeTextBlock = RecordingSizeTextBlock,");
        AssertContains(adapterText, "RecordingBitrateTextBlock = RecordingBitrateTextBlock,");
        AssertContains(adapterText, "private void ApplyInitialStatusStripPresentation()");
        AssertContains(adapterText, "private void UpdateStatusTextPresentation()");
        AssertContains(adapterText, "private void UpdateRecordingTimePresentation()");
        AssertContains(adapterText, "private void UpdateDiskSpacePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingSizePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingBitratePresentation()");
        AssertContains(adapterText, "private void UpdateFlashbackBitratePresentation()");
        AssertContains(adapterText, "private void UpdateDiskWarningPresentation()");
        AssertContains(adapterText, "private bool TryHandleStatusStripPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "case nameof(MainViewModel.StatusText):");
        AssertContains(adapterText, "case nameof(MainViewModel.RecordingTime):");
        AssertContains(adapterText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertContains(adapterText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertContains(adapterText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertContains(adapterText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertContains(adapterText, "if (ViewModel.IsRecording)");
        AssertContains(adapterText, "ApplyWindowTitle();");
        AssertContains(mainWindowText, "InitializeStatusStripPresentationController();");
        AssertContains(bindingsText, "ApplyInitialStatusStripPresentation();");
        AssertContains(propertyChangedText, "if (TryHandleStatusStripPropertyChanged(propertyName))");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackBitratePresentation();");
        AssertContains(controllerText, "internal readonly record struct StatusStripPresentationSnapshot");
        AssertContains(controllerText, "internal sealed class StatusStripPresentationController");
        AssertContains(controllerText, "public void ApplyInitial(StatusStripPresentationSnapshot snapshot)");
        AssertContains(controllerText, "_context.StatusTextBlock.Text = statusText;");
        AssertContains(controllerText, "_context.RecordingTimeTextBlock.Text = recordingTime;");
        AssertContains(controllerText, "_context.DiskSpaceTextBlock.Text = diskSpaceInfo;");
        AssertContains(controllerText, "_context.RecordingSizeTextBlock.Text = recordingSizeInfo;");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = recordingBitrateInfo;");
        AssertContains(controllerText, "if (!isRecording && isFlashbackEnabled)");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = flashbackBitrateInfo;");
        AssertContains(controllerText, "_context.DiskWarningInfoBar.IsOpen = isDiskWarningActive;");
        AssertDoesNotContain(bindingsText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(bindingsText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(bindingsText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(bindingsText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(bindingsText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(bindingsText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(propertyChangedText, "StatusTextBlock.Text = ViewModel.StatusText;");
        AssertDoesNotContain(propertyChangedText, "RecordingTimeTextBlock.Text = ViewModel.RecordingTime;");
        AssertDoesNotContain(propertyChangedText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(propertyChangedText, "DiskWarningInfoBar.IsOpen = ViewModel.IsDiskWarningActive;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.StatusText):");
        AssertDoesNotContain(propertyChangedText, "UpdateStatusTextPresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingTime):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingTimePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskSpacePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingSizePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingBitratePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskWarningPresentation();");
        AssertDoesNotContain(flashbackPropertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.FlashbackBitrateInfo;");

        return Task.CompletedTask;
    }

    private static Task PreviewAudioFadeState_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewAudioFade.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerRootText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerContextText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Context.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerInitialStateText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.InitialState.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerMetersText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Meters.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerSelectionsText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Selections.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerTogglesText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Toggles.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(audioBindingsText, "private AudioControlBindingController _audioControlBindingController = null!;");
        AssertContains(audioBindingsText, "private void InitializeAudioControlBindingController()");
        AssertContains(audioBindingsText, "PreviewVolumeSlider = PreviewVolumeSlider,");
        AssertContains(audioBindingsText, "IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,");
        AssertContains(audioBindingsText, "CancelPreviewAudioFadeInForUser = CancelPreviewAudioFadeInForUser,");
        AssertContains(adapterText, "private PreviewAudioFadeController _previewAudioFadeController = null!;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;");
        AssertContains(adapterText, "private void InitializePreviewAudioFadeController()");
        AssertContains(adapterText, "=> _previewAudioFadeController.PrimeFadeIn();");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeIn(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeOutAsync(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.CancelFadeInForUser();");
        AssertContains(mainWindowText, "InitializePreviewAudioFadeController();");
        AssertContains(mainWindowText, "InitializeAudioControlBindingController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioControlBindingControllerRootText, "internal sealed partial class AudioControlBindingController");
        AssertContains(audioControlBindingControllerContextText, "internal sealed class AudioControlBindingControllerContext");
        AssertContains(audioControlBindingControllerInitialStateText, "public void ApplyInitialAudioControlBindings()");
        AssertContains(audioControlBindingControllerInitialStateText, "_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive()");
        AssertContains(audioControlBindingControllerInitialStateText, "_context.PreviewVolumeSlider.ValueChanged +=");
        AssertContains(audioControlBindingControllerInitialStateText, "_context.CancelPreviewAudioFadeInForUser();");
        AssertContains(audioControlBindingControllerMetersText, "public void AttachAudioMeterActivationBindings()");
        AssertContains(audioControlBindingControllerMetersText, "public void ApplyInitialAudioMeterPresentation()");
        AssertContains(audioControlBindingControllerMetersText, "public void AttachDeviceAudioGainAndMeterBindings()");
        AssertContains(audioControlBindingControllerSelectionsText, "public void EnsureAudioControlSelections()");
        AssertContains(audioControlBindingControllerSelectionsText, "public void AttachAudioSelectionBindings()");
        AssertContains(audioControlBindingControllerTogglesText, "public void AttachAudioRecordPreviewToggleBindings()");
        AssertContains(audioControlBindingControllerTogglesText, "public void AttachAudioInputToggleBindings()");
        AssertDoesNotContain(audioControlBindingControllerRootText, "public void ApplyInitialAudioControlBindings()");
        AssertDoesNotContain(audioControlBindingControllerRootText, "public void AttachAudioSelectionBindings()");
        AssertDoesNotContain(audioControlBindingControllerRootText, "public void AttachDeviceAudioGainAndMeterBindings()");
        AssertContains(propertyChangedText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(propertyChangedText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(audioPropertyChangedText, "HandlePreviewVolumeChanged();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.HandlePreviewVolumeChanged();");
        AssertContains(audioControlPresentationControllerText, "if (_context.IsPreviewAudioFadeInActive())");
        AssertContains(previewLifecycleControllerText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(controllerText, "internal sealed class PreviewAudioFadeController");
        AssertContains(controllerText, "private double _savedPreviewVolume;");
        AssertContains(controllerText, "private Storyboard? _volumeFadeStoryboard;");
        AssertContains(controllerText, "public void PrimeFadeIn()");
        AssertContains(controllerText, "public async Task StartFadeOutAsync(int durationMs = 450)");
        AssertContains(controllerText, "Sussudio.Logger.Log(\"PREVIEW_AUDIO_FADE_OUT_COMPLETED\");");
        AssertDoesNotContain(mainWindowText, "private double _savedPreviewVolume;");
        AssertDoesNotContain(mainWindowText, "private bool _isVolumeFadingIn;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _previewVolumeFadeStoryboard;");
        AssertDoesNotContain(bindingsText, "PreviewVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(audioBindingsText, "PreviewVolumeSlider.ValueChanged +=");

        return Task.CompletedTask;
    }

    private static Task AudioControlPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(audioPropertyChangedText, "private AudioControlPresentationController _audioControlPresentationController = null!;");
        AssertContains(audioPropertyChangedText, "private void InitializeAudioControlPresentationController()");
        AssertContains(audioPropertyChangedText, "CustomAudioToggle = CustomAudioToggle,");
        AssertContains(audioPropertyChangedText, "AudioInputComboBox = AudioInputComboBox,");
        AssertContains(audioPropertyChangedText, "MicrophoneToggle = MicrophoneToggle,");
        AssertContains(audioPropertyChangedText, "MicrophoneComboBox = MicrophoneComboBox,");
        AssertContains(audioPropertyChangedText, "AudioRecordToggle = AudioRecordToggle,");
        AssertContains(audioPropertyChangedText, "AudioPreviewToggle = AudioPreviewToggle,");
        AssertContains(audioPropertyChangedText, "PreviewVolumeSlider = PreviewVolumeSlider,");
        AssertContains(audioPropertyChangedText, "PreviewVolumeLabel = PreviewVolumeLabel,");
        AssertContains(audioPropertyChangedText, "IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,");
        AssertContains(audioPropertyChangedText, "SetAudioMeterMonitoringState = SetAudioMeterMonitoringState,");
        AssertContains(audioPropertyChangedText, "AnimateAudioMeterDisabled = AnimateAudioMeterDisabled,");
        AssertContains(audioPropertyChangedText, "UpdateMicrophoneControlsVisibility = UpdateMicrophoneControlsVisibility,");
        AssertContains(audioPropertyChangedText, "SyncMicrophoneVolumeControls = SyncMicrophoneVolumeControls");
        AssertContains(mainWindowText, "InitializeAudioControlPresentationController();");

        AssertContains(controllerText, "internal sealed class AudioControlPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class AudioControlPresentationController");
        AssertContains(controllerText, "public void HandleCustomAudioInputEnabledChanged()");
        AssertContains(controllerText, "_context.AudioInputComboBox.IsEnabled = _context.ViewModel.IsCustomAudioInputEnabled && !_context.ViewModel.IsRecording;");
        AssertContains(controllerText, "public void HandleMicrophoneEnabledChanged()");
        AssertContains(controllerText, "_context.MicrophoneComboBox.IsEnabled = _context.ViewModel.IsMicrophoneEnabled && !_context.ViewModel.IsRecording;");
        AssertContains(controllerText, "_context.UpdateMicrophoneControlsVisibility();");
        AssertContains(controllerText, "public void HandleAudioEnabledChanged()");
        AssertContains(controllerText, "_context.AudioPreviewToggle.IsEnabled = _context.ViewModel.IsAudioEnabled;");
        AssertContains(controllerText, "_context.AudioPreviewToggle.IsChecked = false;");
        AssertContains(controllerText, "_context.AnimateAudioMeterDisabled(!_context.ViewModel.IsAudioEnabled);");
        AssertContains(controllerText, "public void HandleAudioPreviewActiveChanged()");
        AssertContains(controllerText, "_context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);");
        AssertContains(controllerText, "public void HandlePreviewVolumeChanged()");
        AssertContains(controllerText, "if (_context.IsPreviewAudioFadeInActive())");
        AssertContains(controllerText, "_context.PreviewVolumeLabel.Text = $\"{(int)volumePct}%\";");
        AssertContains(controllerText, "public void HandleMicrophoneVolumeChanged()");
        AssertContains(controllerText, "_context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);");

        AssertDoesNotContain(audioPropertyChangedText, "AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled");
        AssertDoesNotContain(audioPropertyChangedText, "AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled");
        AssertDoesNotContain(audioPropertyChangedText, "PreviewVolumeLabel.Text = $\"{(int)volumePct}%\";");

        return Task.CompletedTask;
    }

    private static Task PreviewButtonPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewActions.cs").Replace("\r\n", "\n");
        var propertyChangedPreviewText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewReinit.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewButtonPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonPresentationController.cs").Replace("\r\n", "\n");
        var actionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs").Replace("\r\n", "\n");
        var reinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewButtonPresentationController _previewButtonPresentationController = null!;");
        AssertContains(adapterText, "private void InitializePreviewButtonPresentationController()");
        AssertContains(adapterText, "PreviewButton = PreviewButton,");
        AssertContains(adapterText, "PreviewButtonIcon = PreviewButtonIcon,");
        AssertContains(adapterText, "private void ShowStopPreviewButtonPresentation()");
        AssertContains(adapterText, "=> _previewButtonPresentationController.ShowStopPreview();");
        AssertContains(adapterText, "private void ShowStartPreviewButtonPresentation()");
        AssertContains(adapterText, "=> _previewButtonPresentationController.ShowStartPreview();");
        AssertContains(mainWindowText, "InitializePreviewButtonPresentationController();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation();");
        AssertContains(controllerText, "internal sealed class PreviewButtonPresentationController");
        AssertContains(controllerText, "private const string StopPreviewGlyph = \"\\uE71A\";");
        AssertContains(controllerText, "private const string StartPreviewGlyph = \"\\uE768\";");
        AssertContains(controllerText, "_context.PreviewButtonIcon.Glyph = StopPreviewGlyph;");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.PreviewButton, \"Stop Preview\");");
        AssertContains(controllerText, "_context.PreviewButtonIcon.Glyph = StartPreviewGlyph;");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.PreviewButton, \"Start Preview\");");
        AssertContains(previewActionsText, "private PreviewButtonActionController _previewButtonActionController = null!;");
        AssertContains(previewActionsText, "private void InitializePreviewButtonActionController()");
        AssertContains(previewActionsText, "private Task TogglePreviewFromButtonAsync()");
        AssertContains(previewActionsText, "=> _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));");
        AssertContains(previewActionsText, "_ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));");
        AssertContains(mainWindowText, "InitializePreviewButtonActionController();");
        AssertContains(actionControllerText, "internal sealed class PreviewButtonActionController");
        AssertContains(actionControllerText, "public async Task TogglePreviewAsync(string operationName)");
        AssertContains(actionControllerText, "viewModel.CancelPendingPreviewRestart();");
        AssertContains(actionControllerText, "Logger.Log($\"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_context.GetPreviewStartupAttemptId() ?? \"none\"}\", operationName);");
        AssertContains(previewActionsText, "_previewReinitTransitionController.Clear(operationName, operationName: operationName);");
        AssertContains(reinitTransitionControllerText, "Logger.Log(message, operationName);");
        AssertContains(actionControllerText, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(actionControllerText, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(actionControllerText, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertContains(actionControllerText, "await viewModel.StopPreviewAsync(userInitiated: true);");
        AssertContains(actionControllerText, "_context.ClearPreviewReinitAnimation(operationName);");
        AssertContains(actionControllerText, "await viewModel.StartPreviewAsync(userInitiated: true);");
        AssertDoesNotContain(previewActionsText, "var audioFadeOutTask = StartPreviewAudioFadeOutAsync();");
        AssertDoesNotContain(previewActionsText, "await ViewModel.StopPreviewAsync(userInitiated: true);");
        AssertDoesNotContain(propertyChangedPreviewText, "PreviewButtonIcon.Glyph = \"\\uE71A\";");
        AssertDoesNotContain(propertyChangedPreviewText, "PreviewButtonIcon.Glyph = \"\\uE768\";");
        AssertDoesNotContain(propertyChangedPreviewText, "ToolTipService.SetToolTip(PreviewButton, \"Stop Preview\");");
        AssertDoesNotContain(propertyChangedPreviewText, "ToolTipService.SetToolTip(PreviewButton, \"Start Preview\");");
        AssertDoesNotContain(previewReinitText, "PreviewButtonIcon.Glyph = \"\\uE71A\";");
        AssertDoesNotContain(previewReinitText, "PreviewButtonIcon.Glyph = \"\\uE768\";");
        AssertDoesNotContain(previewReinitText, "ToolTipService.SetToolTip(PreviewButton, \"Stop Preview\");");
        AssertDoesNotContain(previewReinitText, "ToolTipService.SetToolTip(PreviewButton, \"Start Preview\");");

        return Task.CompletedTask;
    }

    private static Task MicrophoneControls_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.MicrophoneControls.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Audio/MicrophoneControlsController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerInitialStateText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.InitialState.cs").Replace("\r\n", "\n");
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private MicrophoneControlsController _microphoneControlsController = null!;");
        AssertContains(adapterText, "private void InitializeMicrophoneControlsController()");
        AssertContains(adapterText, "=> _microphoneControlsController.AttachVolumeBindings();");
        AssertContains(adapterText, "=> _microphoneControlsController.SyncVolumeControls(volumePercent);");
        AssertContains(adapterText, "=> _microphoneControlsController.ApplyInitialVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.UpdateVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.StopRowAnimation();");
        AssertContains(mainWindowText, "InitializeMicrophoneControlsController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioBindingsText, "SetupMicrophoneVolumeBindings = SetupMicrophoneVolumeBindings,");
        AssertContains(audioBindingsText, "ApplyInitialMicrophoneControlsVisibility = ApplyInitialMicrophoneControlsVisibility,");
        AssertContains(audioControlBindingControllerInitialStateText, "_context.SetupMicrophoneVolumeBindings();");
        AssertContains(audioControlBindingControllerInitialStateText, "_context.ApplyInitialMicrophoneControlsVisibility();");
        AssertContains(propertyChangedText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(audioPropertyChangedText, "HandleMicrophoneEnabledChanged();");
        AssertContains(audioPropertyChangedText, "HandleMicrophoneVolumeChanged();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.HandleMicrophoneEnabledChanged();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.HandleMicrophoneVolumeChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.UpdateMicrophoneControlsVisibility();");
        AssertContains(audioControlPresentationControllerText, "_context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);");
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
        AssertDoesNotContain(bindingsText, "SetupMicrophoneVolumeBindings();");
        AssertDoesNotContain(bindingsText, "private void SyncMicrophoneVolumeControls(double volumePercent)");
        AssertDoesNotContain(bindingsText, "private Storyboard CreateMicMeterRowStoryboard(bool showing)");

        return Task.CompletedTask;
    }

}
