using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs")
                .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationAudioText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationAudioText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationAudioText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(automationAudioText, "IsMicrophoneEnabled = enabled;\n                }\n                finally\n                {\n                    _suppressMicrophoneMonitorUpdate = false;\n                }\n\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);\n\n                _micMonitorEnabled = enabled;");

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
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationAudio.cs",
            "MainViewModel.AutomationDeviceAudio.cs",
            "MainViewModel.AutomationMicrophone.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale audio automation partial {stalePath}");
        }

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("Sussudio.ViewModels.MainViewModel");

        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        var setPreviewVolume = coordinatorType.GetMethod(
            "SetPreviewVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(setPreviewVolume, "CaptureSessionCoordinator.SetPreviewVolume");

        var updateAudioMonitoring = coordinatorType.GetMethod(
            "UpdateAudioMonitoringAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioMonitoring, "CaptureSessionCoordinator.UpdateAudioMonitoringAsync");

        var updateAudioInput = coordinatorType.GetMethod(
            "UpdateAudioInputAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioInput, "CaptureSessionCoordinator.UpdateAudioInputAsync");

        var startVideoPreview = coordinatorType.GetMethod(
            "StartVideoPreviewAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(startVideoPreview, "CaptureSessionCoordinator.StartVideoPreviewAsync");

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        AssertEqual(true,
            Enum.IsDefined(commandKindType, Enum.Parse(commandKindType, "SetAudioPreviewEnabled")),
            "AutomationCommandKind.SetAudioPreviewEnabled exists");

        return Task.CompletedTask;
    }
}
