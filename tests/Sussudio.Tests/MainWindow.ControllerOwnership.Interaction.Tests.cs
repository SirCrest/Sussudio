using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs").Replace("\r\n", "\n");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.RecordingActions.cs")),
            "recording button adapter folded into MainWindow.ControlBindings.cs");

        return Task.CompletedTask;
    }

    internal static Task PreviewAudioFadeState_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs").Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs").Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");
        var audioControlBindingFamilyText = audioControlBindingControllerText;
        var audioControlPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs").Replace("\r\n", "\n");

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
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewLifecycle.Composition.cs")),
            "preview audio fade adapter lives in the preview transitions composition partial");
        AssertContains(mainWindowText, "InitializeAudioControlBindingController();");
        AssertContains(bindingsText, "ApplyInitialAudioControlBindings();");
        AssertContains(audioControlBindingControllerText, "internal sealed class AudioControlBindingControllerContext");
        AssertContains(audioControlBindingControllerText, "internal sealed class AudioControlBindingController");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioMeterActivationBindings()");
        AssertContains(audioControlBindingControllerText, "public void ApplyInitialAudioControlBindings()");
        AssertContains(audioControlBindingControllerText, "_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive()");
        AssertContains(audioControlBindingControllerText, "_context.PreviewVolumeSlider.ValueChanged +=");
        AssertContains(audioControlBindingControllerText, "_context.CancelPreviewAudioFadeInForUser();");
        AssertContains(audioControlBindingControllerText, "public void ApplyInitialAudioMeterPresentation()");
        AssertContains(audioControlBindingControllerText, "public void EnsureAudioControlSelections()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioSelectionBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioRecordPreviewToggleBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachAudioInputToggleBindings()");
        AssertContains(audioControlBindingControllerText, "public void AttachDeviceAudioGainAndMeterBindings()");
        AssertContains(audioControlBindingFamilyText, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(propertyChangedText, "TryHandleAudio = TryHandleAudioPropertyChanged,");
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

    internal static Task PreviewButtonPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewActionsText = ReadMainWindowPreviewTransitionsAdapterSource();
        var propertyChangedPreviewText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs").Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var actionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs").Replace("\r\n", "\n");
        var controllerText = actionControllerText;
        var reinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

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
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(reinitTransitionControllerText, "context.ShowStartPreviewButtonPresentation();");
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
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewLifecycle.Composition.cs")),
            "preview button action adapter lives in the preview transitions composition partial");
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

}
