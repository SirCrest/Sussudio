using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSettings.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(automationSettingsText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);");
        AssertDoesNotContain(automationSettingsText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationController");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
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
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedFrameRate(matched.Value);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedVideoFormat(match);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetMjpegDecoderCount(Math.Clamp(decoderCount, 1, 8));");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(true);");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(false);");
        AssertContains(captureSettingsAutomationControllerText, "return wasPreviewing && _context.GetSelectedFormat() != null;");
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
            "MainViewModel.HdrModeChanges.cs",
            "MainViewModel.AutomationCaptureSettings.cs"
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
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs")
            .Replace("\r\n", "\n");
        var selectDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");
        var selectAudioDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectAudioInputDeviceAsync",
            "public Task SetCustomAudioInputEnabledAsync");

        AssertContains(deviceSelectionAutomationText, "public Task RefreshDevicesForAutomationAsync");
        AssertContains(deviceSelectionAutomationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(rootViewModelText, "public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs")),
            "shallow MainViewModel device-management partial");
        AssertContains(deviceRefreshControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                _viewModel.StatusText = \"Device scan canceled\";\n                throw;\n            }");
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
