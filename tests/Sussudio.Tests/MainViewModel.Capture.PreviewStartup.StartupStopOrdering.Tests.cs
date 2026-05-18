using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
    {
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs")
            .Replace("\r\n", "\n");
        var deviceManagementText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceManagement.cs")
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
        AssertContains(recordingCapabilityControllerText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingCapabilityControllerText, "=> _recordingCapabilityController.Start();");

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
        AssertContains(splitEncodeRefresh, "SelectedSplitEncodeMode = \"Auto\";");

        var refreshDevices = ExtractMemberCode(deviceManagementText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();");
        AssertContains(refreshDevices, "var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);");
        AssertContains(refreshDevices, "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();", "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);");
        AssertOccursBefore(refreshDevices, "var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);", "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);");
        AssertOccursBefore(refreshDevices, "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);", "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "ApplyStartupAudioDeviceScan(", "ReplaceCollection(Devices, devices.ToList());");
        AssertOccursBefore(refreshDevices, "ReplaceCollection(Devices, devices.ToList());", "_deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertOccursBefore(refreshDevices, "_deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);", "var savedDeviceId = _pendingSavedDeviceId;");
        AssertOccursBefore(refreshDevices, "SelectedDevice = nextSelectedDevice;", "await StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(refreshDevices, "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);", "await StartPreviewAsync(userInitiated: false, cancellationToken);");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.Bindings.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewActions.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewFadeInController.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadRepoFile("Sussudio/MainWindow.PreviewAudioFade.cs")
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
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs")
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
        AssertOccursBefore(mainWindowLoaded, "PrimePreviewAudioFadeIn();", "await ViewModel.RefreshDevicesAsync();");
        AssertContains(mainWindowLoaded, "RevealPreviewUnavailablePlaceholder();");

        AssertDoesNotContain(xamlText, "No preview available");

        return Task.CompletedTask;
    }

    private static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadRepoFile("Sussudio/MainWindow.PreviewActions.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewReinit.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs")
            .Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");

        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewButtonActionController.cs")
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

        var vmStopRamp = ExtractMemberCode(audioMonitoringText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "_previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioVolumeTransitionText, "RampDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(0);");

        var stopPreview = ExtractTextBetween(
            previewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreview, "await _viewModel.RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await _viewModel.RampPreviewVolumeDownForStopAsync(cancellationToken);", "_viewModel.PreviewStopRequested?.Invoke(_viewModel, EventArgs.Empty);");
        AssertOccursBefore(stopPreview, "await _viewModel.RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _viewModel._sessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        AssertDoesNotContain(previewPropertyChangedText, "private Task ViewModel_PreviewRendererStopRequested()");
        var previewReinitStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "DisposeD3DPreviewRendererForReinit();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }
}
