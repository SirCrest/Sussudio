using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordingActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/Button/RecordingButtonActionController.cs").Replace("\r\n", "\n");

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
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlBindingBindingsText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Bindings.cs").Replace("\r\n", "\n");
        var audioControlBindingFamilyText = audioControlBindingControllerText + "\n" + audioControlBindingBindingsText;
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
        AssertContains(audioControlBindingControllerText, "internal sealed class AudioControlBindingControllerContext");
        AssertContains(audioControlBindingControllerText, "internal sealed partial class AudioControlBindingController");
        AssertContains(audioControlBindingBindingsText, "internal sealed partial class AudioControlBindingController");
        AssertContains(audioControlBindingBindingsText, "public void AttachAudioMeterActivationBindings()");
        AssertContains(audioControlBindingBindingsText, "public void ApplyInitialAudioControlBindings()");
        AssertContains(audioControlBindingBindingsText, "_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive()");
        AssertContains(audioControlBindingBindingsText, "_context.PreviewVolumeSlider.ValueChanged +=");
        AssertContains(audioControlBindingBindingsText, "_context.CancelPreviewAudioFadeInForUser();");
        AssertContains(audioControlBindingBindingsText, "public void ApplyInitialAudioMeterPresentation()");
        AssertContains(audioControlBindingBindingsText, "public void EnsureAudioControlSelections()");
        AssertContains(audioControlBindingBindingsText, "public void AttachAudioSelectionBindings()");
        AssertContains(audioControlBindingBindingsText, "public void AttachAudioRecordPreviewToggleBindings()");
        AssertContains(audioControlBindingBindingsText, "public void AttachAudioInputToggleBindings()");
        AssertContains(audioControlBindingBindingsText, "public void AttachDeviceAudioGainAndMeterBindings()");
        AssertDoesNotContain(audioControlBindingControllerText, "public void AttachAudioSelectionBindings()");
        AssertContains(audioControlBindingFamilyText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(propertyChangedText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(propertyChangedText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.PreviewVolume):");
        AssertContains(audioControlPresentationControllerText, "HandlePreviewVolumeChanged();");
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
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsCustomAudioInputEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsMicrophoneEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioPreviewEnabled):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(controllerText, "case nameof(MainViewModel.PreviewVolume):");
        AssertContains(controllerText, "case nameof(MainViewModel.MicrophoneVolume):");
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
        AssertDoesNotContain(audioPropertyChangedText, "case nameof(MainViewModel.");
        AssertDoesNotContain(audioPropertyChangedText, "=> _audioControlPresentationController.HandlePreviewVolumeChanged();");

        return Task.CompletedTask;
    }

    private static Task PreviewButtonPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewActions.cs").Replace("\r\n", "\n");
        var propertyChangedPreviewText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewReinit.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonPresentationController.cs").Replace("\r\n", "\n");
        var actionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs").Replace("\r\n", "\n");
        var reinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs").Replace("\r\n", "\n");

        AssertContains(propertyChangedPreviewText, "private PreviewButtonPresentationController _previewButtonPresentationController = null!;");
        AssertContains(propertyChangedPreviewText, "private void InitializePreviewButtonPresentationController()");
        AssertContains(propertyChangedPreviewText, "PreviewButton = PreviewButton,");
        AssertContains(propertyChangedPreviewText, "PreviewButtonIcon = PreviewButtonIcon,");
        AssertContains(propertyChangedPreviewText, "private void ShowStopPreviewButtonPresentation()");
        AssertContains(propertyChangedPreviewText, "=> _previewButtonPresentationController.ShowStopPreview();");
        AssertContains(propertyChangedPreviewText, "private void ShowStartPreviewButtonPresentation()");
        AssertContains(propertyChangedPreviewText, "=> _previewButtonPresentationController.ShowStartPreview();");
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
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Audio/MicrophoneControlsController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Bindings.cs").Replace("\r\n", "\n");
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
        AssertContains(audioControlBindingControllerText, "_context.SetupMicrophoneVolumeBindings();");
        AssertContains(audioControlBindingControllerText, "_context.ApplyInitialMicrophoneControlsVisibility();");
        AssertContains(propertyChangedText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(audioPropertyChangedText, "=> _audioControlPresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.IsMicrophoneEnabled):");
        AssertContains(audioControlPresentationControllerText, "case nameof(MainViewModel.MicrophoneVolume):");
        AssertContains(audioControlPresentationControllerText, "HandleMicrophoneEnabledChanged();");
        AssertContains(audioControlPresentationControllerText, "HandleMicrophoneVolumeChanged();");
        AssertContains(audioControlPresentationControllerText, "_context.UpdateMicrophoneControlsVisibility();");
        AssertContains(audioControlPresentationControllerText, "_context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);");
        AssertContains(shutdownCleanupControllerText, "_context.StopRecordingVisuals();");
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
