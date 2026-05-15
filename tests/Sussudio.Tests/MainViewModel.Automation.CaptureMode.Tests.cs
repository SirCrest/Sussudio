using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs").Replace("\r\n", "\n");
        var captureModeAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureMode.cs").Replace("\r\n", "\n");

        AssertContains(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureModeAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureModeAutomationText, "await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = true;");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = false;");
        AssertContains(captureModeAutomationText, "return wasPreviewing && SelectedFormat != null;");
        AssertContains(captureModeAutomationText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureModeAutomationText, "_automationCaptureModeGate.Release();");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertDoesNotContain(automationText, "private async Task SetAutomationCaptureModeAsync(");

        return Task.CompletedTask;
    }

    private static Task AutomationDeviceSelection_RoutesThroughApplyReinit()
    {
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs").Replace("\r\n", "\n");
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
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");
        AssertDoesNotContain(automationText, "public Task RefreshDevicesForAutomationAsync");
        AssertDoesNotContain(automationText, "public Task SelectDeviceAsync");
        AssertDoesNotContain(automationText, "public Task SelectAudioInputDeviceAsync");
        AssertDoesNotContain(automationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertDoesNotContain(automationText, "private CaptureDevice? ResolveDevice");
        AssertDoesNotContain(automationText, "private AudioInputDevice? ResolveAudioDevice");

        return Task.CompletedTask;
    }
}
