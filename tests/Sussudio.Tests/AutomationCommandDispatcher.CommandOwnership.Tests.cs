using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var waitConditionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs")
            .Replace("\r\n", "\n");
        var assertionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.WaitForCondition:");
        AssertContains(customCommandsText, "ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.AssertSnapshot:");
        AssertContains(customCommandsText, "ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "ParseWaitCondition(payload)");
        AssertDoesNotContain(customCommandsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertDoesNotContain(customCommandsText, "ParseAssertions(payload)");
        AssertDoesNotContain(customCommandsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");

        AssertContains(waitConditionsText, "private async Task<AutomationCommandResponse> ExecuteWaitForConditionCommandAsync(");
        AssertContains(waitConditionsText, "var condition = ParseWaitCondition(payload);");
        AssertContains(waitConditionsText, "Math.Clamp(GetInt(payload, \"timeoutMs\") ?? DefaultWaitTimeoutMs, 250, 300_000)");
        AssertContains(waitConditionsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertContains(waitConditionsText, "errorCode: met ? null : \"timeout\"");
        AssertContains(waitConditionsText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(");
        AssertContains(waitConditionsText, "private static bool ConditionSatisfied(");

        AssertContains(assertionsText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(assertionsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(assertionsText, "var assertions = ParseAssertions(payload);");
        AssertContains(assertionsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");
        AssertContains(assertionsText, "errorCode: passed ? null : \"assertion-failed\"");
        AssertContains(assertionsText, "private static List<SnapshotAssertion> ParseAssertions(");
        AssertContains(assertionsText, "private static bool TryEvaluateAssertion(");

        return Task.CompletedTask;
    }

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
        AssertContains(audioControlCommandsText, "_viewModel.SetDeviceAudioModeAsync(mode, cancellationToken)");
        AssertContains(audioControlCommandsText, "Device audio mode changed: {mode}.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetAnalogAudioGainCommandAsync(");
        AssertContains(audioControlCommandsText, "var gain = RequireDouble(payload, \"gain\");");
        AssertContains(audioControlCommandsText, "_viewModel.SetAnalogAudioGainAsync(gain, cancellationToken)");
        AssertContains(audioControlCommandsText, "Analog audio gain set to {gain:0.###}%.");
        AssertContains(audioControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetMicrophoneEnabledCommandAsync(");
        AssertContains(audioControlCommandsText, "Missing 'enabled' parameter.");
        AssertContains(audioControlCommandsText, "_viewModel.SetMicrophoneEnabledAsync(enabled, cancellationToken)");
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
        AssertContains(captureControlCommandsText, "_viewModel.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetOutputPathCommandAsync(");
        AssertContains(captureControlCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.SetOutputPath,");
        AssertContains(captureControlCommandsText, "_viewModel.SetOutputPathAsync(outputPath, cancellationToken)");
        AssertContains(captureControlCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetRecordingEnabledCommandAsync(");
        AssertContains(captureControlCommandsText, "_viewModel.SetRecordingEnabledAsync(enabled, cancellationToken)");
        AssertContains(captureControlCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(captureControlCommandsText, "Recording {(enabled ? \"started\" : \"stopped\")}.");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_IntrospectionCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var introspectionCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.IntrospectionCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.GetSnapshot:");
        AssertContains(customCommandsText, "ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetAutomationManifest:");
        AssertContains(customCommandsText, "ExecuteGetAutomationManifestCommand(correlationId)");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertDoesNotContain(customCommandsText, "AutomationCommandCatalog.CreateManifest()");

        AssertContains(introspectionCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(introspectionCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(introspectionCommandsText, "Snapshot retrieved.");
        AssertContains(introspectionCommandsText, "private AutomationCommandResponse ExecuteGetAutomationManifestCommand(string correlationId)");
        AssertContains(introspectionCommandsText, "Automation manifest retrieved.");
        AssertContains(introspectionCommandsText, "AutomationCommandCatalog.CreateManifest()");
        AssertContains(introspectionCommandsText, "includeSnapshot: false");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_UiSettingsCommands_OwnUiSettingsApplication()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var uiSettingsCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.UiSettingsCommands.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(uiSettingsCommandsText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler> UiSettingsHandlers");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetShowAllCaptureOptions]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetPreviewVolume]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetStatsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetSettingsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFrameTimeOverlayVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFlashbackTimelineVisible]");
        AssertContains(uiSettingsCommandsText, "if (command == AutomationCommandKind.SetStatsSectionVisible)");
        AssertContains(uiSettingsCommandsText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(uiSettingsCommandsText, "var section = RequireString(payload, \"section\");");
        AssertContains(uiSettingsCommandsText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(uiSettingsCommandsText, "_viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var windowCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs")
            .Replace("\r\n", "\n");
        var windowActionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowActions.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetFullScreenEnabled:");
        AssertContains(customCommandsText, "ExecuteSetFullScreenEnabledCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.OpenRecordingsFolder:");
        AssertContains(customCommandsText, "ExecuteOpenRecordingsFolderCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ArmClose:");
        AssertContains(customCommandsText, "ExecuteArmCloseCommand(payload, correlationId)");
        AssertContains(customCommandsText, "case AutomationCommandKind.WindowAction:");
        AssertContains(customCommandsText, "ExecuteWindowActionCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_windowControl.SetFullScreenEnabledAsync");
        AssertDoesNotContain(customCommandsText, "_windowControl.OpenRecordingsFolderAsync");
        AssertDoesNotContain(customCommandsText, "_closeArmed");
        AssertDoesNotContain(customCommandsText, "window-close-not-armed");

        AssertContains(windowCommandsText, "_windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken)");
        AssertContains(windowCommandsText, "Full screen {(enabled ? \"enter\" : \"exit\")} requested.");
        AssertContains(windowCommandsText, "_windowControl.OpenRecordingsFolderAsync(cancellationToken)");
        AssertContains(windowCommandsText, "Recordings folder open requested.");
        AssertContains(windowCommandsText, "var armed = GetBool(payload, \"armed\") ?? true;");
        AssertContains(windowCommandsText, "_closeArmed = armed;");
        AssertContains(windowCommandsText, "Window close arm state requested: {(armed ? \"armed\" : \"disarmed\")}.");
        AssertContains(windowCommandsText, "if (action == AutomationWindowAction.Close)");
        AssertContains(windowCommandsText, "window-close-not-armed");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);");

        AssertContains(windowActionsText, "private async Task ExecuteWindowActionAsync(");
        AssertDoesNotContain(windowActionsText, "Window close is disallowed until ArmClose is requested.");

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

    private static Task AutomationCommandDispatcher_VerificationCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var verificationCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.VerificationCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyFile:");
        AssertContains(customCommandsText, "ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyLastRecording:");
        AssertContains(customCommandsText, "ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken)");
        AssertContains(verificationCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(");
        AssertContains(verificationCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(");
        AssertContains(verificationCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.VerifyFile,\n            \"filePath\",");
        AssertContains(verificationCommandsText, "_diagnosticsHub\n            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)");
        AssertContains(verificationCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync(cancellationToken)");
        AssertContains(verificationCommandsText, "HdrParity = verification.HdrParity");
        AssertContains(verificationCommandsText, "errorCode: verification.Succeeded ? null : \"verification-failed\"");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub\n                    .VerifyFileAsync");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync");
        AssertDoesNotContain(customCommandsText, "HdrParity = verification.HdrParity");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_VisualCaptureCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var visualCaptureCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.VisualCaptureCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.ProbeVideoSource:");
        AssertContains(customCommandsText, "ExecuteProbeVideoSourceCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ProbePreviewColor:");
        AssertContains(customCommandsText, "ExecuteProbePreviewColorCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CapturePreviewFrame:");
        AssertContains(customCommandsText, "ExecuteCapturePreviewFrameCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CaptureWindowScreenshot:");
        AssertContains(customCommandsText, "ExecuteCaptureWindowScreenshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.ProbeVideoSourceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.ProbePreviewColorAsync");
        AssertDoesNotContain(customCommandsText, "Path.Combine(Path.GetTempPath()");
        AssertDoesNotContain(customCommandsText, "_windowControl.CaptureWindowScreenshotAsync");

        AssertContains(visualCaptureCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbeVideoSourceCommandAsync(");
        AssertContains(visualCaptureCommandsText, "_viewModel.ProbeVideoSourceAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(");
        AssertContains(visualCaptureCommandsText, "_viewModel.ProbePreviewColorAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CapturePreviewFrame");
        AssertContains(visualCaptureCommandsText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(visualCaptureCommandsText, "window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
        AssertContains(visualCaptureCommandsText, "CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded)");
        AssertContains(visualCaptureCommandsText, "errorCode: succeeded ? null : \"capture-failed\"");

        return Task.CompletedTask;
    }
}
