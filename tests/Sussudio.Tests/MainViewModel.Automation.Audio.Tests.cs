using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
            .Replace("\r\n", "\n");
        var automationDeviceAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceAudio.cs")
            .Replace("\r\n", "\n");
        var automationMicrophoneText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationMicrophone.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationUi.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(automationAudioText, "public Task SetDeviceAudioModeAsync");
        AssertDoesNotContain(automationAudioText, "public Task SetAnalogAudioGainAsync");
        AssertContains(automationDeviceAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationDeviceAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(automationAudioText, "public Task SetMicrophoneEnabledAsync");
        AssertContains(automationMicrophoneText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationMicrophoneText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationMicrophoneText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationMicrophoneText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationMicrophoneText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationDeviceAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationDeviceAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");

        var microphoneUpdateIndex = automationMicrophoneText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationMicrophoneText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists only after monitor update");

        return Task.CompletedTask;
    }
}
