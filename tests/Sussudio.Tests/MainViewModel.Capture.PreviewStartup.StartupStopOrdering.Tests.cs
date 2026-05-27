using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStartupLifecycleEventOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();

        AssertContains(mainWindowText, "InitializePreviewLifecycleEventController();");
        AssertContains(previewFadeInText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(previewFadeInText, "private void InitializePreviewFadeInController()");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewFadeInControllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInControllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(previewFadeInControllerText, "public void Schedule()");
        AssertContains(previewFadeInControllerText, "public void Stop()");
        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewPropertyChangedText, "private PreviewLifecycleEventController _previewLifecycleEventController = null!;");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewLifecycleControllerText, "_context.HandlePreviewReinitializingChanged();");
        AssertContains(previewLifecycleControllerText, "if (_context.ShouldBeginPreviewStartupAttempt())");
        AssertContains(previewLifecycleControllerText, "_stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;");
        AssertContains(previewLifecycleControllerText, "_context.StartPreviewStartupWatchdog();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewReinitRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "HandlePreviewReinitializingChanged(");
        AssertDoesNotContain(previewReinitText, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");

        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var previewButtonClick = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(previewButtonClick, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(previewButtonClick, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertOccursBefore(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);", "await viewModel.StopPreviewAsync(userInitiated: true);");

        var uiFadeOut = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeOutAsync");
        AssertContains(uiFadeOut, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(uiFadeOut, "To = 0,");
        AssertContains(uiFadeOut, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(uiFadeOut, "PREVIEW_AUDIO_FADE_OUT_STARTED");

        var vmStopRamp = ExtractMemberCode(previewVolumeTransitionText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "_previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioVolumeTransitionText, "RampDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(0);");

        var stopPreview = ExtractTextBetween(
            previewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "_context.RaisePreviewStopRequested();");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        var previewReinitStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "=> _previewRendererHostController.StopRendererForReinitTeardownAsync();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
    {
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs")
            .Replace("\r\n", "\n");

        var initialize = ExtractMemberCode(settingsText, "InitializeAsync");
        AssertContains(initialize, "LoadSettings();");
        AssertContains(initialize, "StartRecordingCapabilityRefresh();");
        AssertContains(initialize, "return Task.CompletedTask;");
        AssertDoesNotContain(initialize, "await Task.WhenAll");
        AssertOccursBefore(initialize, "LoadSettings();", "StartRecordingCapabilityRefresh();");

        var startupRefresh = ExtractMemberCode(recordingCapabilityControllerText, "Start");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), \"recording formats\");");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), \"split encode modes\");");
        AssertDoesNotContain(settingsText, "private void StartRecordingCapabilityRefresh()");
        AssertDoesNotContain(recordingCapabilityControllerText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "=> _recordingCapabilityController.Start();");

        var recordingFormatRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshRecordingFormatCapabilitiesAsync");
        AssertContains(recordingFormatRefresh, "support.HasH264Nvenc");
        AssertContains(recordingFormatRefresh, "support.HasHevcNvenc");
        AssertContains(recordingFormatRefresh, "support.HasAv1Nvenc");
        AssertDoesNotContain(recordingFormatRefresh, "support.HasAv1)");

        var splitEncodeRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshSplitEncodeCapabilitiesAsync");
        AssertContains(splitEncodeRefresh, "if (!support.Supports2Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"2-way\");");
        AssertContains(splitEncodeRefresh, "if (!support.Supports3Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"3-way\");");
        AssertContains(splitEncodeRefresh, "_context.SetSelectedSplitEncodeMode(\"Auto\");");

        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");

        var refreshDevices = ExtractMemberCode(deviceRefreshControllerText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var discovery = await _context.EnumerateCaptureDeviceDiscoveryAsync()");
        AssertContains(refreshDevices, "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "ApplyStartupAudioDeviceScan(", "_context.ReplaceDevices(devices.ToList());");
        AssertOccursBefore(refreshDevices, "_context.ReplaceDevices(devices.ToList());", "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertOccursBefore(refreshDevices, "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);", "ApplySuccessfulDeviceScanAsync(");
        var successfulScan = ExtractTextBetween(
            deviceRefreshControllerText,
            "private async Task ApplySuccessfulDeviceScanAsync",
            "\n    }\n}");
        AssertOccursBefore(successfulScan, "var savedDeviceId = _context.GetPendingSavedDeviceId();", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(successfulScan, "_context.SetSelectedDevice(nextSelectedDevice);", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplySuccessfulDeviceScanAsync(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.ControlBindings.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewTransitionText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs")
            .Replace("\r\n", "\n");
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
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
        AssertContains(animatePreviewInAdapter, "_previewTransitionAnimationController.AnimatePreviewInAsync();");

        var animatePreviewIn = ExtractMemberCode(previewTransitionControllerText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);");
        AssertContains(animatePreviewIn, "AnimatePreviewShellInAsync(350)");
        AssertContains(animatePreviewIn, "AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut)");
        AssertOccursBefore(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);", "AnimatePreviewShellInAsync(350)");

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
        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
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
