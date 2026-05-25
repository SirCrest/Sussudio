using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var assertionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.WaitForCondition:");
        AssertContains(customCommandsText, "ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.AssertSnapshot:");
        AssertContains(customCommandsText, "ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "ParseAssertions(payload)");
        AssertDoesNotContain(customCommandsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteWaitForConditionCommandAsync(");
        AssertContains(customCommandsText, "var condition = ParseWaitCondition(payload);");
        AssertContains(customCommandsText, "Math.Clamp(GetInt(payload, \"timeoutMs\") ?? DefaultWaitTimeoutMs, 250, 300_000)");
        AssertContains(customCommandsText, "WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken)");
        AssertContains(customCommandsText, "errorCode: met ? null : \"timeout\"");
        AssertContains(customCommandsText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(");
        AssertContains(customCommandsText, "private static bool ConditionSatisfied(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WaitConditions.cs")),
            "wait-condition commands folded into AutomationCommandDispatcher.CustomCommands.cs");

        AssertContains(assertionsText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(assertionsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(assertionsText, "var assertions = ParseAssertions(payload);");
        AssertContains(assertionsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");
        AssertContains(assertionsText, "errorCode: passed ? null : \"assertion-failed\"");
        AssertContains(assertionsText, "private static List<SnapshotAssertion> ParseAssertions(");
        AssertContains(assertionsText, "private static bool TryEvaluateAssertion(");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_IntrospectionCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.GetSnapshot:");
        AssertContains(customCommandsText, "ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetAutomationManifest:");
        AssertContains(customCommandsText, "ExecuteGetAutomationManifestCommand(correlationId)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(customCommandsText, "Snapshot retrieved.");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetAutomationManifestCommand(string correlationId)");
        AssertContains(customCommandsText, "Automation manifest retrieved.");
        AssertContains(customCommandsText, "AutomationCommandCatalog.CreateManifest()");
        AssertContains(customCommandsText, "includeSnapshot: false");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.ReadbackCommands.cs")),
            "readback commands folded into AutomationCommandDispatcher.CustomCommands.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_EntryPipeline_LivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var preflightText = rootText;
        var portMappedDispatchText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(rootText, "var preflightResponse = TryCreatePreflightResponse(request, correlationId);");
        AssertContains(rootText, "var portMappedResponse = await TryExecutePortMappedCommandAsync(");
        AssertContains(rootText, "return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)");
        AssertDoesNotContain(rootText, "TrivialDeviceSelectionHandlers.TryGetValue");

        AssertContains(preflightText, "private AutomationCommandResponse? TryCreatePreflightResponse(");
        AssertContains(preflightText, "AUTOMATION_MANIFEST_MISMATCH");
        AssertContains(preflightText, "request.Command == AutomationCommandKind.Authenticate");
        AssertContains(preflightText, "private bool IsAuthorized(AutomationCommandRequest request)");
        AssertContains(preflightText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(preflightText, "RequiresReadyDevices(request.Command) && !IsAutomationReady()");
        AssertContains(preflightText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertDoesNotContain(preflightText, "TrivialDeviceSelectionHandlers.TryGetValue");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Authorization.cs")),
            "auth gate folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Preflight.cs")),
            "preflight gate folded into AutomationCommandDispatcher.cs");

        AssertContains(portMappedDispatchText, "private async Task<AutomationCommandResponse?> TryExecutePortMappedCommandAsync(");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>> TrivialDeviceSelectionHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> TrivialCaptureSettingsHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers");
        AssertContains(portMappedDispatchText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers");
        AssertContains(portMappedDispatchText, "TryExecuteUiSettingsCommandAsync(command, payload, correlationId, cancellationToken)");
        AssertContains(portMappedDispatchText, "TrivialDeviceSelectionHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialCaptureSettingsHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialAudioHandlers.TryGetValue(command");
        AssertContains(portMappedDispatchText, "TrivialPreviewRecordingHandlers.TryGetValue(command");
        AssertDoesNotContain(portMappedDispatchText, "ExecuteCustomCommandAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.TrivialHandlers.cs")),
            "trivial port handler tables folded into AutomationCommandDispatcher.PortMappedDispatch.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.UiSettingsCommands.cs")),
            "UI settings command tables folded into AutomationCommandDispatcher.PortMappedDispatch.cs");

        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.cs`");
        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.Payload.cs`");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var windowCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs")
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

        AssertContains(windowCommandsText, "private async Task ExecuteWindowActionAsync(");
        AssertContains(windowCommandsText, "_windowControl.MoveToAsync(mx, my, cancellationToken)");
        AssertContains(windowCommandsText, "_windowControl.ResizeToAsync(rw, rh, cancellationToken)");
        AssertContains(windowCommandsText, "_windowControl.SnapToRegionAsync(action, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WindowActions.cs")),
            "window action executor folded into AutomationCommandDispatcher.WindowCommands.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_VerificationCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyFile:");
        AssertContains(customCommandsText, "ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyLastRecording:");
        AssertContains(customCommandsText, "ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(");
        AssertContains(customCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.VerifyFile,\n            \"filePath\",");
        AssertContains(customCommandsText, "_diagnosticsHub\n            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)");
        AssertContains(customCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync(cancellationToken)");
        AssertContains(customCommandsText, "HdrParity = verification.HdrParity");
        AssertContains(customCommandsText, "errorCode: verification.Succeeded ? null : \"verification-failed\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.VerificationCommands.cs")),
            "verification commands folded into AutomationCommandDispatcher.CustomCommands.cs");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_VisualCaptureCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
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

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbeVideoSourceCommandAsync(");
        AssertContains(customCommandsText, "_probePort.ProbeVideoSourceAsync(cancellationToken)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(");
        AssertContains(customCommandsText, "_probePort.ProbePreviewColorAsync(cancellationToken)");
        AssertContains(customCommandsText, "AutomationCommandKind.CapturePreviewFrame");
        AssertContains(customCommandsText, "_probePort.CapturePreviewFrameAsync(outputPath, cancellationToken)");
        AssertContains(customCommandsText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
        AssertContains(customCommandsText, "AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(customCommandsText, "window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
        AssertContains(customCommandsText, "_windowControl.CaptureWindowScreenshotAsync");
        AssertContains(customCommandsText, "CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded)");
        AssertContains(customCommandsText, "errorCode: succeeded ? null : \"capture-failed\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.VisualCaptureCommands.cs")),
            "visual capture commands folded into AutomationCommandDispatcher.CustomCommands.cs");

        return Task.CompletedTask;
    }
}
