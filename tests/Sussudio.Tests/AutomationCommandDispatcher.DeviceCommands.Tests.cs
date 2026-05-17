using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_DeviceCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var deviceCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.DeviceCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.RefreshDevices:");
        AssertContains(customCommandsText, "ExecuteRefreshDevicesCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SelectDevice:");
        AssertContains(customCommandsText, "ExecuteSelectDeviceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SelectAudioInputDevice:");
        AssertContains(customCommandsText, "ExecuteSelectAudioInputDeviceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetCaptureOptions:");
        AssertContains(customCommandsText, "ExecuteGetCaptureOptionsCommandAsync(correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.RefreshDevicesForAutomationAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SelectDeviceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SelectAudioInputDeviceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.GetAutomationOptionsSnapshotAsync");

        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteRefreshDevicesCommandAsync(");
        AssertContains(deviceCommandsText, "_viewModel.RefreshDevicesForAutomationAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Device list refresh requested.");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "var deviceId = GetString(payload, \"deviceId\");");
        AssertContains(deviceCommandsText, "var deviceName = GetString(payload, \"deviceName\");");
        AssertContains(deviceCommandsText, "_viewModel.SelectDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectAudioInputDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "_viewModel.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetCaptureOptionsCommandAsync(");
        AssertContains(deviceCommandsText, "_viewModel.GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Capture options retrieved.");

        return Task.CompletedTask;
    }
}
