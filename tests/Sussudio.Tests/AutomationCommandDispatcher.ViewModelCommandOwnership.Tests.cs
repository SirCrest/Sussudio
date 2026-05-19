using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_AudioControlCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var audioControlCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.AudioControlCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetDeviceAudioMode:");
        AssertContains(customCommandsText, "ExecuteSetDeviceAudioModeCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetAnalogAudioGain:");
        AssertContains(customCommandsText, "ExecuteSetAnalogAudioGainCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetMicrophoneEnabled:");
        AssertContains(customCommandsText, "ExecuteSetMicrophoneEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetDeviceAudioModeAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetAnalogAudioGainAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetMicrophoneEnabledAsync");
        AssertDoesNotContain(customCommandsText, "Missing 'enabled' parameter.");

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

    private static Task AutomationCommandDispatcher_CaptureControlCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var captureControlCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CaptureControlCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetMjpegDecoderCount:");
        AssertContains(customCommandsText, "ExecuteSetMjpegDecoderCountCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetOutputPath:");
        AssertContains(customCommandsText, "ExecuteSetOutputPathCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.SetRecordingEnabled:");
        AssertContains(customCommandsText, "ExecuteSetRecordingEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.SetMjpegDecoderCountAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetOutputPathAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetRecordingEnabledAsync");
        AssertDoesNotContain(customCommandsText, "Missing required integer property 'decoderCount'.");

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

    private static Task AutomationCommandDispatcher_UiSettingsCommands_OwnUiSettingsApplication()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var uiSettingsCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.UiSettingsCommands.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(uiSettingsCommandsText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> UiCaptureSettingsHandlers");
        AssertContains(uiSettingsCommandsText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers");
        AssertContains(uiSettingsCommandsText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetShowAllCaptureOptions]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetPreviewVolume]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetStatsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetSettingsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFrameTimeOverlayVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFlashbackTimelineVisible]");
        AssertContains(uiSettingsCommandsText, "captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "uiHandler.InvokeAsync(_uiPort, payload, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "if (command == AutomationCommandKind.SetStatsSectionVisible)");
        AssertContains(uiSettingsCommandsText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(uiSettingsCommandsText, "var section = RequireString(payload, \"section\");");
        AssertContains(uiSettingsCommandsText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(uiSettingsCommandsText, "_uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        return Task.CompletedTask;
    }

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
