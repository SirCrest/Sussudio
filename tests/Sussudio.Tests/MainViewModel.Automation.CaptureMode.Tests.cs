using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureModeAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureMode.cs").Replace("\r\n", "\n");
        var frameRateAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationFrameRate.cs").Replace("\r\n", "\n");
        var videoFormatAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationVideoFormat.cs").Replace("\r\n", "\n");
        var mjpegDecoderCountAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationMjpegDecoderCount.cs").Replace("\r\n", "\n");

        AssertContains(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureModeAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureModeAutomationText, "await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = true;");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = false;");
        AssertContains(captureModeAutomationText, "return wasPreviewing && SelectedFormat != null;");
        AssertContains(captureModeAutomationText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureModeAutomationText, "_automationCaptureModeGate.Release();");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertDoesNotContain(captureModeAutomationText, "public Task SetFrameRateAsync");
        AssertContains(frameRateAutomationText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(frameRateAutomationText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(frameRateAutomationText, "FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate)");
        AssertContains(frameRateAutomationText, "SelectedFrameRate = matched.Value;");
        AssertDoesNotContain(captureModeAutomationText, "public Task SetVideoFormatAsync");
        AssertContains(videoFormatAutomationText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(videoFormatAutomationText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(videoFormatAutomationText, "SelectedVideoFormat = match;");
        AssertDoesNotContain(captureModeAutomationText, "public Task SetMjpegDecoderCountAsync");
        AssertContains(mjpegDecoderCountAutomationText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(mjpegDecoderCountAutomationText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(mjpegDecoderCountAutomationText, "MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);");

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
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }
}
