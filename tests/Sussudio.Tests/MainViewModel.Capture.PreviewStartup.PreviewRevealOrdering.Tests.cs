using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Bindings.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewFadeInController.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewTransitionText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs")
            .Replace("\r\n", "\n");
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.cs")
            .Replace("\r\n", "\n");
        var launchStartupText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchStartupController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(propertyChangedText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");

        var previewStartRequested = ExtractMemberCode(previewLifecycleControllerText, "HandlePreviewStartRequested");
        AssertContains(previewStartRequested, "_context.BeginPreviewStartupAttempt();");
        AssertContains(previewStartRequested, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(previewStartRequested, "_context.PreparePreviewStartupPresentation();");
        AssertOccursBefore(previewStartRequested, "_context.PrimePreviewAudioFadeIn();", "_context.PreparePreviewStartupPresentation();");

        var playEntranceAnimation = ExtractMemberCode(launchEntranceShellText, "PlayEntranceAnimation");
        AssertContains(playEntranceAnimation, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(playEntranceAnimation, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(playEntranceAnimation, "Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);");

        var animatePreviewInAdapter = ExtractMemberCode(previewTransitionText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewInAdapter, "FadeInVideoFrameShadow(delayMs: 0, durationMs: 400);");
        AssertContains(animatePreviewInAdapter, "_previewTransitionAnimationController.AnimatePreviewInAsync();");

        var animatePreviewIn = ExtractMemberCode(previewTransitionControllerText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewIn, "AnimatePreviewShellInAsync(350)");
        AssertContains(animatePreviewIn, "AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut)");

        var preparePresentation = ExtractMemberCode(previewTransitionControllerText, "PrepareStartupPresentation");
        AssertContains(preparePresentation, "FadeOutElement(_context.NoDevicePlaceholder);");
        AssertContains(preparePresentation, "_context.StartPreviewStartupOverlay();");
        AssertContains(preparePresentation, "_context.PreviewContentGrid.Opacity = 0.0;");

        var revealUnavailable = ExtractMemberCode(previewTransitionControllerText, "RevealUnavailablePlaceholder");
        AssertContains(revealUnavailable, "AnimatePreviewShellInAsync(300)");
        AssertContains(revealUnavailable, "FadeInElement(_context.NoDevicePlaceholder);");

        var primeAudioAdapter = ExtractMemberCode(previewAudioFadeText, "PrimePreviewAudioFadeIn");
        AssertContains(primeAudioAdapter, "_previewAudioFadeController.PrimeFadeIn();");

        var primeAudio = ExtractMemberCode(previewAudioFadeControllerText, "PrimeFadeIn");
        AssertContains(primeAudio, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(primeAudio, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(primeAudio, "_context.PreviewVolumeSlider.Value = 0;");

        var startAudioFadeAdapter = ExtractMemberCode(previewAudioFadeText, "StartPreviewAudioFadeIn");
        AssertContains(startAudioFadeAdapter, "_previewAudioFadeController.StartFadeIn(durationMs);");

        var startAudioFade = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeIn");
        AssertContains(startAudioFade, "Storyboard.SetTarget(volumeAnimation, _context.PreviewVolumeSlider);");
        AssertContains(startAudioFade, "CompleteFadeIn(applyTarget: true)");

        AssertContains(previewFadeInText, "=> _previewFadeInController.Schedule();");
        var schedulePreviewFadeIn = ExtractMemberCode(previewFadeInControllerText, "Schedule");
        AssertContains(schedulePreviewFadeIn, "StartPreviewAudioFadeIn();");
        AssertOccursBefore(schedulePreviewFadeIn, "_ = _context.AnimatePreviewInAsync();", "_context.StartPreviewAudioFadeIn();");

        var setupBindings = ExtractMemberCode(bindingsText, "SetupBindings");
        AssertContains(setupBindings, "ApplyInitialAudioControlBindings();");

        var initialAudioBindingsAdapter = ExtractMemberCode(audioBindingsText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindingsAdapter, "_audioControlBindingController.ApplyInitialAudioControlBindings();");

        var initialAudioBindings = ExtractMemberCode(audioControlBindingControllerText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(initialAudioBindings, "_context.CancelPreviewAudioFadeInForUser();");
        AssertOccursBefore(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();", "_context.PreviewVolumeSlider.ValueChanged +=");

        var previewButtonClick = ExtractMemberCode(previewActionsText, "PreviewButton_Click");
        AssertContains(previewButtonClick, "RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click))");
        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs")
            .Replace("\r\n", "\n");
        var togglePreviewAsync = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(togglePreviewAsync, "if (!viewModel.IsPreviewing)\n        {\n            _context.RevealPreviewUnavailablePlaceholder();\n        }");

        var mainWindowLoaded = ExtractMemberCode(startupText, "MainWindow_Loaded");
        AssertContains(mainWindowLoaded, "=> _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));");
        var launchLoaded = ExtractMemberCode(launchStartupText, "HandleLoaded");
        AssertOccursBefore(launchLoaded, "_context.PrimePreviewAudioFadeIn();", "await _context.RefreshDevicesAsync();");
        AssertContains(launchLoaded, "_context.RevealPreviewUnavailablePlaceholder();");

        AssertDoesNotContain(xamlText, "No preview available");

        return Task.CompletedTask;
    }
}
