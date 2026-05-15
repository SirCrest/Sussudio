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
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/LiveSignalInfoController.cs").Replace("\r\n", "\n");

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
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs").Replace("\r\n", "\n");
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
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioBindingsText, "IsPreviewAudioFadeInActive || IsPreviewAudioFadeAnimationActive");
        AssertContains(audioBindingsText, "PreviewVolumeSlider.ValueChanged +=");
        AssertContains(audioBindingsText, "CancelPreviewAudioFadeInForUser();");
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
        AssertDoesNotContain(bindingsText, "PreviewVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(animationsText, "private void PrimePreviewAudioFadeIn()");
        AssertDoesNotContain(animationsText, "private void CompletePreviewAudioFadeIn(");
        AssertDoesNotContain(animationsText, "private async Task StartPreviewAudioFadeOutAsync(");

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
        var controllerText = ReadRepoFile("Sussudio/Controllers/MicrophoneControlsController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private MicrophoneControlsController _microphoneControlsController = null!;");
        AssertContains(adapterText, "private void InitializeMicrophoneControlsController()");
        AssertContains(adapterText, "=> _microphoneControlsController.AttachVolumeBindings();");
        AssertContains(adapterText, "=> _microphoneControlsController.SyncVolumeControls(volumePercent);");
        AssertContains(adapterText, "=> _microphoneControlsController.ApplyInitialVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.UpdateVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.StopRowAnimation();");
        AssertContains(mainWindowText, "InitializeMicrophoneControlsController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioBindingsText, "SetupMicrophoneVolumeBindings();");
        AssertContains(audioBindingsText, "ApplyInitialMicrophoneControlsVisibility();");
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
        AssertDoesNotContain(bindingsText, "SetupMicrophoneVolumeBindings();");
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
}
