using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureSettings.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "=> _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);");
        AssertContains(captureSettingsAutomationText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "=> _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);");
        AssertContains(captureSettingsAutomationText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "=> _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);");
        AssertContains(captureSettingsAutomationText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationText, "=> _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);");
        AssertDoesNotContain(captureSettingsAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureSettingsAutomationControllerText, "FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate)");
        AssertContains(captureSettingsAutomationControllerText, "_viewModel.SelectedFrameRate = matched.Value;");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureSettingsAutomationControllerText, "_viewModel.SelectedVideoFormat = match;");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(captureSettingsAutomationControllerText, "_viewModel.MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationControllerText, "_viewModel._suppressFormatChangeReinitialize = true;");
        AssertContains(captureSettingsAutomationControllerText, "_viewModel._suppressFormatChangeReinitialize = false;");
        AssertContains(captureSettingsAutomationControllerText, "return wasPreviewing && _viewModel.SelectedFormat != null;");
        AssertContains(captureSettingsAutomationControllerText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureSettingsAutomationControllerText, "_captureModeGate.Release();");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(captureModeTransactionsText, "SetAutomationCaptureModeAsync(");
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
