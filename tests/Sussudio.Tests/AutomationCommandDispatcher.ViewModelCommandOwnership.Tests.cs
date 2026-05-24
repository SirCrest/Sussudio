using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_AudioControlCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var audioControlCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetDeviceAudioMode:");
        AssertContains(customCommandsText, "ExecuteSetDeviceAudioModeCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetAnalogAudioGain:");
        AssertContains(customCommandsText, "ExecuteSetAnalogAudioGainCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetMicrophoneEnabled:");
        AssertContains(customCommandsText, "ExecuteSetMicrophoneEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetDeviceAudioModeAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetAnalogAudioGainAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetMicrophoneEnabledAsync");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetDeviceAudioModeCommandAsync(");
        AssertContains(audioControlCommandsText, "var mode = RequireString(payload, \"mode\");");
        AssertContains(audioControlCommandsText, "_audioPort.SetDeviceAudioModeAsync(mode, cancellationToken)");
        AssertContains(audioControlCommandsText, "Device audio mode changed: {mode}.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetAnalogAudioGainCommandAsync(");
        AssertContains(audioControlCommandsText, "var gain = RequireDouble(payload, \"gain\");");
        AssertContains(audioControlCommandsText, "_audioPort.SetAnalogAudioGainAsync(gain, cancellationToken)");
        AssertContains(audioControlCommandsText, "Analog audio gain set to {gain:0.###}%.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetMicrophoneEnabledCommandAsync(");
        AssertContains(audioControlCommandsText, "Missing 'enabled' parameter.");
        AssertContains(audioControlCommandsText, "_audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken)");
        AssertContains(audioControlCommandsText, "Microphone {(enabled ? \"enabled\" : \"disabled\")}.");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_CaptureControlCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var captureControlCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetMjpegDecoderCount:");
        AssertContains(customCommandsText, "ExecuteSetMjpegDecoderCountCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetOutputPath:");
        AssertContains(customCommandsText, "ExecuteSetOutputPathCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetRecordingEnabled:");
        AssertContains(customCommandsText, "ExecuteSetRecordingEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetMjpegDecoderCountAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetOutputPathAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetRecordingEnabledAsync");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetMjpegDecoderCountCommandAsync(");
        AssertContains(captureControlCommandsText, "var decoderCount = GetInt(payload, \"decoderCount\");");
        AssertContains(captureControlCommandsText, "Missing required integer property 'decoderCount'.");
        AssertContains(captureControlCommandsText, "_captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetOutputPathCommandAsync(");
        AssertContains(captureControlCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.SetOutputPath,");
        AssertContains(captureControlCommandsText, "_previewRecordingPort.SetOutputPathAsync(outputPath, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetRecordingEnabledCommandAsync(");
        AssertContains(captureControlCommandsText, "_previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken)");
        AssertContains(captureControlCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(captureControlCommandsText, "Recording {(enabled ? \"started\" : \"stopped\")}.");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_UiSettingsCommands_LiveWithPortMappedDispatch()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var portMappedDispatchText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetPreviewVolume]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetStatsVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetSettingsVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetFrameTimeOverlayVisible]");
        AssertContains(portMappedDispatchText, "[AutomationCommandKind.SetFlashbackTimelineVisible]");
        AssertContains(portMappedDispatchText, "previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken)");
        AssertContains(portMappedDispatchText, "uiHandler.InvokeAsync(_uiPort, payload, cancellationToken)");
        AssertContains(portMappedDispatchText, "if (command == AutomationCommandKind.SetStatsSectionVisible)");
        AssertContains(portMappedDispatchText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(portMappedDispatchText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(portMappedDispatchText, "var section = RequireString(payload, \"section\");");
        AssertContains(portMappedDispatchText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(portMappedDispatchText, "_uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(portMappedDispatchText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.UiSettingsCommands.cs")),
            "UI settings handlers folded into AutomationCommandDispatcher.PortMappedDispatch.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_DeviceCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var deviceCommandsText = customCommandsText;

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
        AssertContains(deviceCommandsText, "_deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Device list refresh requested.");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "var deviceId = GetString(payload, \"deviceId\");");
        AssertContains(deviceCommandsText, "var deviceName = GetString(payload, \"deviceName\");");
        AssertContains(deviceCommandsText, "_deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteSelectAudioInputDeviceCommandAsync(");
        AssertContains(deviceCommandsText, "_deviceSelectionPort.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken)");
        AssertContains(deviceCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetCaptureOptionsCommandAsync(");
        AssertContains(deviceCommandsText, "_snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertContains(deviceCommandsText, "Capture options retrieved.");

        return Task.CompletedTask;
    }
}
