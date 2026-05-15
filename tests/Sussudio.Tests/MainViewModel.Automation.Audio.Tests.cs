using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
            .Replace("\r\n", "\n");
        var automationRootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationUi.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationAudioText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertDoesNotContain(automationRootText, "public Task SetAudioEnabledAsync");
        AssertDoesNotContain(automationRootText, "public Task SetAudioPreviewEnabledAsync");
        AssertDoesNotContain(automationRootText, "public Task SetPreviewVolumeAsync");
        AssertDoesNotContain(automationRootText, "public Task SetDeviceAudioModeAsync");
        AssertDoesNotContain(automationRootText, "public Task SetAnalogAudioGainAsync");
        AssertDoesNotContain(automationRootText, "public Task SetMicrophoneEnabledAsync");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");

        var microphoneUpdateIndex = automationAudioText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationAudioText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists only after monitor update");

        return Task.CompletedTask;
    }
}
