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
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Settings.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityRefreshText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingCapabilityRefresh.cs")
            .Replace("\r\n", "\n");
        var deviceManagementText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceManagement.cs")
            .Replace("\r\n", "\n");

        var initialize = ExtractMemberCode(settingsText, "InitializeAsync");
        AssertContains(initialize, "LoadSettings();");
        AssertContains(initialize, "StartRecordingCapabilityRefresh();");
        AssertContains(initialize, "return Task.CompletedTask;");
        AssertDoesNotContain(initialize, "await Task.WhenAll");
        AssertOccursBefore(initialize, "LoadSettings();", "StartRecordingCapabilityRefresh();");

        var startupRefresh = ExtractMemberCode(recordingCapabilityRefreshText, "StartRecordingCapabilityRefresh");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), \"recording formats\");");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), \"split encode modes\");");
        AssertDoesNotContain(settingsText, "private void StartRecordingCapabilityRefresh()");

        var recordingFormatRefresh = ExtractMemberCode(recordingCapabilityRefreshText, "RefreshRecordingFormatCapabilitiesAsync");
        AssertContains(recordingFormatRefresh, "support.HasH264Nvenc");
        AssertContains(recordingFormatRefresh, "support.HasHevcNvenc");
        AssertContains(recordingFormatRefresh, "support.HasAv1Nvenc");
        AssertDoesNotContain(recordingFormatRefresh, "support.HasAv1)");

        var splitEncodeRefresh = ExtractMemberCode(recordingCapabilityRefreshText, "RefreshSplitEncodeCapabilitiesAsync");
        AssertContains(splitEncodeRefresh, "if (!support.Supports2Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"2-way\");");
        AssertContains(splitEncodeRefresh, "if (!support.Supports3Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"3-way\");");
        AssertContains(splitEncodeRefresh, "SelectedSplitEncodeMode = \"Auto\";");

        var refreshDevices = ExtractMemberCode(deviceManagementText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();");
        AssertContains(refreshDevices, "var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);");
        AssertOccursBefore(refreshDevices, "var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();", "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);");
        AssertOccursBefore(refreshDevices, "var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);", "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);");
        AssertOccursBefore(refreshDevices, "await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);", "await StartPreviewAsync(userInitiated: false, cancellationToken);");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_StateLivesInPreviewStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalsText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupSignals.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");

        AssertContains(previewStartupText, "private enum PreviewStartupState");
        AssertContains(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertContains(previewStartupText, "private readonly Lazy<int> _previewStartupVisualTimeoutMs = new(static () =>");
        AssertContains(previewStartupText, "private DispatcherQueueTimer? _previewStartupWatchdogTimer;");
        AssertContains(previewStartupText, "private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;");
        AssertContains(previewFadeInText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInText, "private DispatcherQueueTimer? _previewFadeInTimer;");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewStartupSignalsText, "Preview startup readiness-signal tracking");
        AssertContains(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertContains(previewStartupSignalsText, "private bool IsPreviewStartupSignalWindowActive()");
        AssertContains(previewStartupSignalsText, "private void ResetPreviewSignalState()");
        AssertContains(previewStartupSignalsText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalsText, "private void LogPreviewStartupPlaybackSnapshot(string reason)");
        AssertContains(previewRuntimeSnapshotText, "_previewStartupState.ToString()");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertContains(propertyChangedText, "await HandlePreviewingChangedAsync();");
        AssertContains(propertyChangedText, "HandlePreviewReinitializingChanged();");
        AssertContains(previewPropertyChangedText, "Preview-specific ViewModel events and property projections");
        AssertContains(previewPropertyChangedText, "IsPreviewStartupFailedState(_previewStartupState)");
        AssertDoesNotContain(mainWindowText, "private enum PreviewStartupState");
        AssertDoesNotContain(mainWindowText, "_previewStartupVisualTimeoutMs");
        AssertDoesNotContain(mainWindowText, "_previewStartupWatchdogTimer");
        AssertDoesNotContain(mainWindowText, "ResetPreviewSignalState()");
        AssertDoesNotContain(previewStartupText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertDoesNotContain(previewStartupText, "private void SchedulePreviewFadeIn()");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs")
            .Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadRepoFile("Sussudio/MainWindow.PreviewAudioFade.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var previewTransitionText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewTransitionControllerText = ReadRepoFile("Sussudio/Controllers/PreviewTransitionAnimationController.cs")
            .Replace("\r\n", "\n");
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(propertyChangedText, "await HandlePreviewingChangedAsync();");

        var previewStartRequested = ExtractMemberCode(previewPropertyChangedText, "ViewModel_PreviewStartRequested");
        AssertContains(previewStartRequested, "BeginPreviewStartupAttempt();");
        AssertContains(previewStartRequested, "PrimePreviewAudioFadeIn();");
        AssertContains(previewStartRequested, "PreparePreviewStartupPresentation();");
        AssertOccursBefore(previewStartRequested, "PrimePreviewAudioFadeIn();", "PreparePreviewStartupPresentation();");

        var playEntranceAnimation = ExtractMemberCode(launchEntranceControllerText, "PlayEntranceAnimation");
        AssertContains(playEntranceAnimation, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(playEntranceAnimation, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(playEntranceAnimation, "Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);");

        var animatePreviewInAdapter = ExtractMemberCode(previewTransitionText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewInAdapter, "FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);");
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

        var schedulePreviewFadeIn = ExtractMemberCode(previewFadeInText, "SchedulePreviewFadeIn");
        AssertContains(schedulePreviewFadeIn, "StartPreviewAudioFadeIn();");
        AssertOccursBefore(schedulePreviewFadeIn, "_ = AnimatePreviewInAsync();", "StartPreviewAudioFadeIn();");

        var setupBindings = ExtractMemberCode(bindingsText, "SetupBindings");
        AssertContains(setupBindings, "ApplyInitialAudioControlBindings();");

        var initialAudioBindings = ExtractMemberCode(audioBindingsText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindings, "PrimePreviewAudioFadeIn();");
        AssertContains(initialAudioBindings, "CancelPreviewAudioFadeInForUser();");
        AssertOccursBefore(initialAudioBindings, "PrimePreviewAudioFadeIn();", "PreviewVolumeSlider.ValueChanged +=");

        var previewButtonClick = ExtractMemberCode(eventHandlersText, "PreviewButton_Click");
        AssertContains(previewButtonClick, "if (!ViewModel.IsPreviewing)\n                {\n                    RevealPreviewUnavailablePlaceholder();\n                }");

        var mainWindowLoaded = ExtractMemberCode(startupText, "MainWindow_Loaded");
        AssertOccursBefore(mainWindowLoaded, "PrimePreviewAudioFadeIn();", "await ViewModel.RefreshDevicesAsync();");
        AssertContains(mainWindowLoaded, "RevealPreviewUnavailablePlaceholder();");

        AssertDoesNotContain(xamlText, "No preview available");

        return Task.CompletedTask;
    }

    private static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/PreviewAudioFadeController.cs")
            .Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");

        var previewButtonClick = ExtractMemberCode(eventHandlersText, "PreviewButton_Click");
        AssertContains(previewButtonClick, "var audioFadeOutTask = StartPreviewAudioFadeOutAsync();");
        AssertContains(previewButtonClick, "var previewFadeOutTask = AnimatePreviewOutAsync();");
        AssertContains(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertOccursBefore(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);", "await ViewModel.StopPreviewAsync(userInitiated: true);");

        var uiFadeOut = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeOutAsync");
        AssertContains(uiFadeOut, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(uiFadeOut, "To = 0,");
        AssertContains(uiFadeOut, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(uiFadeOut, "PREVIEW_AUDIO_FADE_OUT_STARTED");

        var vmStopRamp = ExtractMemberCode(audioMonitoringText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "RampPreviewVolumeDownForAudioTransitionAsync(\"preview_stop\", cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioMonitoringText, "RampPreviewVolumeDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "PreviewVolume = startingVolume * eased;");
        AssertContains(vmRampDown, "PreviewVolume = 0;");

        var stopPreview = ExtractTextBetween(
            captureText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n\n    public async Task BrowseOutputPathAsync()");
        AssertContains(stopPreview, "await RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await RampPreviewVolumeDownForStopAsync(cancellationToken);", "PreviewStopRequested?.Invoke(this, EventArgs.Empty);");
        AssertOccursBefore(stopPreview, "await RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _sessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        var previewReinitStop = ExtractMemberCode(previewPropertyChangedText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "DisposeD3DPreviewRendererForReinit();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

}
