using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureSettings.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
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
        AssertDoesNotContain(captureSettingsAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureModeTransactionsText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureModeTransactionsText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureModeTransactionsText, "await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureModeTransactionsText, "_suppressFormatChangeReinitialize = true;");
        AssertContains(captureModeTransactionsText, "_suppressFormatChangeReinitialize = false;");
        AssertContains(captureModeTransactionsText, "return wasPreviewing && SelectedFormat != null;");
        AssertContains(captureModeTransactionsText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureModeTransactionsText, "_automationCaptureModeGate.Release();");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationCaptureMode.cs",
            "MainViewModel.AutomationCaptureModeGate.cs",
            "MainViewModel.AutomationFrameRate.cs",
            "MainViewModel.AutomationVideoFormat.cs",
            "MainViewModel.AutomationMjpegDecoderCount.cs",
            "MainViewModel.CaptureOptionVisibility.cs",
            "MainViewModel.HdrModeChanges.cs"
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
        var selectDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");
        var selectAudioDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectAudioInputDeviceAsync",
            "public Task SetCustomAudioInputEnabledAsync");

        AssertContains(deviceSelectionAutomationText, "public Task RefreshDevicesForAutomationAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationAudioInputSelection.cs")),
            "MainViewModel audio input automation partial");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }
}
