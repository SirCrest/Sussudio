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

    private static Task PreviewStartup_StateLivesInPreviewStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupWatchdogText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupWatchdog.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs")
            .Replace("\r\n", "\n");
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/PreviewFadeInController.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalsText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupSignals.cs")
            .Replace("\r\n", "\n");
        var previewStartupFailureText = ReadRepoFile("Sussudio/Controllers/PreviewStartupFailureTextFormatter.cs")
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
        AssertContains(previewStartupText, "private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;");
        AssertContains(previewStartupWatchdogText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertContains(previewStartupWatchdogText, "private readonly Lazy<int> _previewStartupVisualTimeoutMs = new(static () =>");
        AssertContains(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupWatchdogTimer;");
        AssertContains(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupTelemetryTimer;");
        AssertContains(previewStartupWatchdogText, "private int _previewStartupFailureStopScheduled;");
        AssertContains(previewStartupWatchdogText, "private void StartPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertContains(previewFadeInText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(previewFadeInText, "private void InitializePreviewFadeInController()");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewFadeInControllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInControllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(previewFadeInControllerText, "public void Schedule()");
        AssertContains(previewFadeInControllerText, "public void Stop()");
        AssertContains(previewStartupSignalsText, "Preview startup readiness-signal tracking");
        AssertContains(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertContains(previewStartupSignalsText, "private bool IsPreviewStartupSignalWindowActive()");
        AssertContains(previewStartupSignalsText, "private void ResetPreviewSignalState()");
        AssertContains(previewStartupSignalsText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalsText, "private void LogPreviewStartupPlaybackSnapshot(string reason)");
        AssertContains(previewStartupSignalsText, "PreviewStartupSignalFormatter.FormatMissingSignals(");
        AssertContains(previewStartupSignalsText, "PreviewStartupSignalFormatter.FormatSignalList(");
        AssertContains(previewStartupFailureText, "internal static class PreviewStartupFailureTextFormatter");
        AssertContains(previewStartupFailureText, "public static string FormatTimeoutReason(int timeoutMs, string? missingSignals)");
        AssertContains(previewStartupFailureText, "public static string FormatTimeoutStatusText(string? missingSignals)");
        AssertContains(previewStartupFailureText, "public static string FormatFailureStopStatusText(string reason)");
        AssertContains(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertContains(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(");
        AssertContains(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(reason)");
        AssertContains(previewRuntimeSnapshotText, "_previewStartupState.ToString()");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertContains(propertyChangedText, "await HandlePreviewingChangedAsync();");
        AssertContains(propertyChangedText, "HandlePreviewReinitializingChanged();");
        AssertContains(previewPropertyChangedText, "Preview-specific ViewModel events and property projections");
        AssertContains(previewPropertyChangedText, "IsPreviewStartupFailedState(_previewStartupState)");
        AssertDoesNotContain(mainWindowText, "private enum PreviewStartupState");
        AssertDoesNotContain(mainWindowText, "_previewStartupVisualTimeoutMs");
        AssertDoesNotContain(mainWindowText, "_previewStartupWatchdogTimer");
        AssertDoesNotContain(previewStartupText, "private void StartPreviewStartupWatchdog()");
        AssertDoesNotContain(previewStartupText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertDoesNotContain(mainWindowText, "ResetPreviewSignalState()");
        AssertDoesNotContain(previewStartupText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertDoesNotContain(previewStartupText, "private void SchedulePreviewFadeIn()");
        AssertDoesNotContain(previewStartupSignalsText, "private static string BuildPreviewStartupSignalList");
        AssertDoesNotContain(previewStartupText, "no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms");
        AssertDoesNotContain(previewStartupText, "Preview failed to attach to UI (session started but no visual confirmation).");
        AssertDoesNotContain(previewStartupText, "Preview failed to start (missing readiness signal:");

        return Task.CompletedTask;
    }

    private static Task PreviewStartupSignalFormatter_PreservesSignalStrings()
    {
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
        var signalType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        var formatSignalList = formatterType.GetMethod("FormatSignalList", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatSignalList was not found.");
        var formatMissingSignals = formatterType.GetMethod("FormatMissingSignals", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatMissingSignals was not found.");

        object Signals(int value) => Enum.ToObject(signalType, value);

        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(0) })?.ToString(), "no startup signals");
        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(16) })?.ToString(), "unknown startup signals");
        AssertEqual(
            "MediaOpened+FirstCaptureFrame+PlaybackAdvancing+FirstVisual",
            formatSignalList.Invoke(null, new[] { Signals(1 | 2 | 4 | 8) })?.ToString(),
            "startup signal order");
        AssertEqual(
            "FirstCaptureFrame+FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2 | 4 | 8), Signals(1 | 4), false })?.ToString(),
            "missing startup signals");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2), Signals(1 | 2), false })?.ToString(),
            "no missing required startup signals");
        AssertEqual(
            "FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), false })?.ToString(),
            "first visual required when no explicit startup signals exist");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), true })?.ToString(),
            "first visual confirmed with no explicit startup signals");

        return Task.CompletedTask;
    }

    private static Task PreviewStartupFailureTextFormatter_PreservesFailureStrings()
    {
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupFailureTextFormatter");
        var formatTimeoutReason = formatterType.GetMethod("FormatTimeoutReason", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupFailureTextFormatter.FormatTimeoutReason was not found.");
        var formatTimeoutStatusText = formatterType.GetMethod("FormatTimeoutStatusText", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupFailureTextFormatter.FormatTimeoutStatusText was not found.");
        var formatFailureStopStatusText = formatterType.GetMethod("FormatFailureStopStatusText", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupFailureTextFormatter.FormatFailureStopStatusText was not found.");

        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, null })?.ToString(),
            "timeout reason without missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, string.Empty })?.ToString(),
            "timeout reason with empty missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "   " })?.ToString(),
            "timeout reason with whitespace missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout reason with missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { null })?.ToString(),
            "timeout status without missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "   " })?.ToString(),
            "timeout status with whitespace missing signals");
        AssertEqual(
            "Preview failed to start (missing readiness signal: FirstCaptureFrame+FirstVisual).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout status with missing signals");
        AssertEqual(
            "Preview startup failed: no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatFailureStopStatusText.Invoke(null, new object?[] { "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual" })?.ToString(),
            "failure stop status");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.AudioBindings.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/AudioControlBindingController.cs")
            .Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs")
            .Replace("\r\n", "\n");
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/PreviewFadeInController.cs")
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
        AssertContains(animatePreviewInAdapter, "CompositionShadowFadeAnimator.FadeIn(_videoShadowVisual, delayMs: 0, durationMs: 400);");
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
