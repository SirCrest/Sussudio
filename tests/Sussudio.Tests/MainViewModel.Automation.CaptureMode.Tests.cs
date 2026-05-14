using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs").Replace("\r\n", "\n");
        var captureModeAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureMode.cs").Replace("\r\n", "\n");

        AssertContains(viewModelText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
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
        var selectDevice = ExtractTextBetween(
            automationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");

        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");

        return Task.CompletedTask;
    }
}
