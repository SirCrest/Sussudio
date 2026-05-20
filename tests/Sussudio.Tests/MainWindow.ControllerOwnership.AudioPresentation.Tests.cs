using System.Threading.Tasks;

static partial class Program
{
    internal static Task AudioControlPresentation_LivesInController()
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

    internal static Task MicrophoneControls_LiveInController()
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
