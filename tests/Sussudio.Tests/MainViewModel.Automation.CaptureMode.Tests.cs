using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureSettings.cs").Replace("\r\n", "\n");

        AssertContains(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureSettingsAutomationText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureSettingsAutomationText, "FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate)");
        AssertContains(captureSettingsAutomationText, "SelectedFrameRate = matched.Value;");
        AssertContains(captureSettingsAutomationText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureSettingsAutomationText, "SelectedVideoFormat = match;");
        AssertContains(captureSettingsAutomationText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(captureSettingsAutomationText, "MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);");
        AssertContains(captureSettingsAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationText, "await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationText, "_suppressFormatChangeReinitialize = true;");
        AssertContains(captureSettingsAutomationText, "_suppressFormatChangeReinitialize = false;");
        AssertContains(captureSettingsAutomationText, "return wasPreviewing && SelectedFormat != null;");
        AssertContains(captureSettingsAutomationText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureSettingsAutomationText, "_automationCaptureModeGate.Release();");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationCaptureMode.cs",
            "MainViewModel.AutomationCaptureModeGate.cs",
            "MainViewModel.AutomationFrameRate.cs",
            "MainViewModel.AutomationVideoFormat.cs",
            "MainViewModel.AutomationMjpegDecoderCount.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale capture settings automation partial {stalePath}");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationDeviceSelection_RoutesThroughApplyReinit()
    {
        var deviceSelectionAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceSelection.cs").Replace("\r\n", "\n");
        var audioInputSelectionAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudioInputSelection.cs").Replace("\r\n", "\n");
        var selectDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectDeviceAsync",
            "private CaptureDevice? ResolveDevice");
        var selectAudioDevice = ExtractTextBetween(
            audioInputSelectionAutomationText,
            "public Task SelectAudioInputDeviceAsync",
            "public Task SetCustomAudioInputEnabledAsync");

        AssertContains(deviceSelectionAutomationText, "public Task RefreshDevicesForAutomationAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertDoesNotContain(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertDoesNotContain(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(audioInputSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(audioInputSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(audioInputSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }
}
