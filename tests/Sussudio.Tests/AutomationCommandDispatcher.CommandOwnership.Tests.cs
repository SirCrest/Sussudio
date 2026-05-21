using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
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

    internal static Task AutomationCommandDispatcher_IntrospectionCommands_LiveInFocusedPartial()
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

    internal static Task AutomationCommandDispatcher_EntryPipeline_LivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var preflightText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.Preflight.cs")
            .Replace("\r\n", "\n");
        var portMappedDispatchText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(rootText, "var preflightResponse = TryCreatePreflightResponse(request, correlationId);");
        AssertContains(rootText, "var portMappedResponse = await TryExecutePortMappedCommandAsync(");
        AssertContains(rootText, "return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)");
        AssertDoesNotContain(rootText, "AUTOMATION_MANIFEST_MISMATCH");
        AssertDoesNotContain(rootText, "TrivialDeviceSelectionHandlers.TryGetValue");

        AssertContains(preflightText, "private AutomationCommandResponse? TryCreatePreflightResponse(");
        AssertContains(preflightText, "AUTOMATION_MANIFEST_MISMATCH");
        AssertContains(preflightText, "request.Command == AutomationCommandKind.Authenticate");
        AssertContains(preflightText, "RequiresReadyDevices(request.Command) && !IsAutomationReady()");
        AssertContains(preflightText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertDoesNotContain(preflightText, "TrivialDeviceSelectionHandlers.TryGetValue");

        AssertContains(portMappedDispatchText, "private async Task<AutomationCommandResponse?> TryExecutePortMappedCommandAsync(");
        AssertContains(portMappedDispatchText, "TryExecuteUiSettingsCommandAsync(command, payload, correlationId, cancellationToken)");
        AssertContains(portMappedDispatchText, "TrivialDeviceSelectionHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialCaptureSettingsHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialAudioHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialPreviewRecordingHandlers.TryGetValue(command");
        AssertDoesNotContain(portMappedDispatchText, "ExecuteCustomCommandAsync");

        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.Preflight.cs`");
        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.Preflight.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.PortMappedDispatch.cs`");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
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

    internal static Task AutomationCommandDispatcher_VerificationCommands_LiveInFocusedPartial()
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

    internal static Task AutomationCommandDispatcher_VisualCaptureCommands_LiveInFocusedPartial()
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
        AssertContains(visualCaptureCommandsText, "_probePort.ProbeVideoSourceAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(");
        AssertContains(visualCaptureCommandsText, "_probePort.ProbePreviewColorAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CapturePreviewFrame");
        AssertContains(visualCaptureCommandsText, "_probePort.CapturePreviewFrameAsync(outputPath, cancellationToken)");
        AssertContains(visualCaptureCommandsText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(visualCaptureCommandsText, "window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
        AssertContains(visualCaptureCommandsText, "CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded)");
        AssertContains(visualCaptureCommandsText, "errorCode: succeeded ? null : \"capture-failed\"");

        return Task.CompletedTask;
    }
}
