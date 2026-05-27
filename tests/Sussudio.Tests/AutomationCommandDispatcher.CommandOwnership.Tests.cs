using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_AllCommandKinds_AreHandled()
    {
        // Every AutomationCommandKind value must be explicitly handled: either
        // as the pre-switch Authenticate check, as a handler-table key, as an
        // explicit focused-helper equality check, or as a case label in the
        // custom switch. This test reads the dispatcher source and verifies each
        // enum name appears in at least one of those locations.
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        var names = Enum.GetNames(commandKindType);

        foreach (var name in names)
        {
            var inTrivialHandlers = dispatcherText.Contains($"[AutomationCommandKind.{name}]");
            var inFocusedHelper = dispatcherText.Contains($"command == AutomationCommandKind.{name}");
            var inSwitchCase = dispatcherText.Contains($"case AutomationCommandKind.{name}:");
            var isAuthenticate = name == "Authenticate" &&
                dispatcherText.Contains("request.Command == AutomationCommandKind.Authenticate");

            AssertEqual(
                true,
                inTrivialHandlers || inFocusedHelper || inSwitchCase || isAuthenticate,
                $"AutomationCommandKind.{name} must be handled in a handler table, focused helper, switch case, or the pre-switch Authenticate check");
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WaitAndAssertCommands_LiveWithSupportOwners()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.WaitForCondition:");
        AssertContains(customCommandsText, "ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.AssertSnapshot:");
        AssertContains(customCommandsText, "ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken)");

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

        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(customCommandsText, "var assertions = ParseAssertions(payload);");
        AssertContains(customCommandsText, "TryEvaluateAssertion(snapshot, assertion, out var failure)");
        AssertContains(customCommandsText, "errorCode: passed ? null : \"assertion-failed\"");
        AssertContains(customCommandsText, "private static List<SnapshotAssertion> ParseAssertions(");
        AssertContains(customCommandsText, "private static bool TryEvaluateAssertion(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Assertions.cs")),
            "assert-snapshot command body folded into AutomationCommandDispatcher.CustomCommands.cs");

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

    internal static Task AutomationCommandDispatcher_UiSettingsCommands_LiveWithRootDispatch()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var portMappedDispatchText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
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
            "UI settings handlers folded into AutomationCommandDispatcher.cs");

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

    internal static Task AutomationCommandDispatcher_EntryPipeline_LivesInRootDispatcher()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var preflightText = rootText;
        var portMappedDispatchText = rootText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(rootText, "var preflightResponse = TryCreatePreflightResponse(request, correlationId);");
        AssertContains(rootText, "var portMappedResponse = await TryExecutePortMappedCommandAsync(");
        AssertContains(rootText, "return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)");

        AssertContains(preflightText, "private AutomationCommandResponse? TryCreatePreflightResponse(");
        AssertContains(preflightText, "AUTOMATION_MANIFEST_MISMATCH");
        AssertContains(preflightText, "request.Command == AutomationCommandKind.Authenticate");
        AssertContains(preflightText, "private bool IsAuthorized(AutomationCommandRequest request)");
        AssertContains(preflightText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(preflightText, "RequiresReadyDevices(request.Command) && !IsAutomationReady()");
        AssertContains(preflightText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Authorization.cs")),
            "auth gate folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Preflight.cs")),
            "preflight gate folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.Payload.cs")),
            "payload helpers folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.PortMappedDispatch.cs")),
            "port-mapped dispatch folded into AutomationCommandDispatcher.cs");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.TrivialHandlers.cs")),
            "trivial port handler tables folded into AutomationCommandDispatcher.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.UiSettingsCommands.cs")),
            "UI settings command tables folded into AutomationCommandDispatcher.cs");

        AssertContains(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs`");
        AssertContains(cleanupPlanText, "`AutomationCommandDispatcher.cs`");
        AssertDoesNotContain(cleanupPlanText, "`AutomationCommandDispatcher.PortMappedDispatch.cs`");
        AssertDoesNotContain(cleanupPlanText, "`AutomationCommandDispatcher.Payload.cs`");

        return Task.CompletedTask;
    }

    internal static async Task AutomationCommandDispatcher_AuthorizesConfiguredTokens()
    {
        var noTokenDispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var noTokenResponse = await ExecuteAutomationCommandAsync(
            noTokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(noTokenResponse, success: true, errorCode: null, status: "ok", "no configured token accepts unauthenticated authenticate");

        var tokenDispatcher = CreateAutomationCommandDispatcher(authToken: "secret");
        var matchingTopLevelResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "secret", payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingTopLevelResponse, success: true, errorCode: null, status: "ok", "matching top-level token is authorized");

        var matchingPayloadResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingPayloadResponse, success: true, errorCode: null, status: "ok", "payload fallback token is authorized");

        var missingTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(missingTokenResponse, success: false, errorCode: "unauthorized", status: "error", "missing token is rejected");

        var wrongTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "wrong", payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(wrongTokenResponse, success: false, errorCode: "unauthorized", status: "error", "wrong top-level token is rejected before payload fallback");

        var protectedCommandResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("GetSnapshot", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(protectedCommandResponse, success: false, errorCode: "unauthorized", status: "error", "missing token rejects non-authenticate command");

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(dispatcherText, "if (string.IsNullOrWhiteSpace(_authToken))\n        {\n            return true;\n        }");
        AssertContains(dispatcherText, "var providedToken = request.AuthToken;");
        AssertContains(dispatcherText, "providedToken = GetString(request.Payload, \"authToken\");");
        AssertContains(dispatcherText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(dispatcherText, "Logger.LogEvent(\"AUTH_FAILED\"");
        AssertContains(dispatcherText, "errorCode: authorized ? null : \"unauthorized\"");
        AssertContains(dispatcherText, "errorCode: \"unauthorized\"");
        AssertContains(dispatcherText, "status: authorized ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error");
    }

    internal static async Task AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent()
    {
        var dispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var response = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("GetAutomationManifest", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: true, errorCode: null, status: "ok", "manifest command succeeds without initialized devices");
        AssertEqual(null, GetPublicProperty(response, "Snapshot"), "manifest response omits snapshot");
        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("manifest response data was missing.");
        AssertEqual(1, (int)GetPublicProperty(data, "SchemaVersion")!, "manifest schema version");

        var commands = ((System.Collections.IEnumerable)GetPublicProperty(data, "Commands")!)
            .Cast<object>()
            .ToArray();
        var manifestCommand = commands.Single(command =>
            string.Equals((string)GetPublicProperty(command, "Name")!, "GetAutomationManifest", StringComparison.Ordinal));
        AssertEqual(51, (int)GetPublicProperty(manifestCommand, "Id")!, "manifest command id");
        AssertEqual("{}", (string)GetPublicProperty(manifestCommand, "PayloadShape")!, "manifest payload shape");
        AssertEqual(false, (bool)GetPublicProperty(manifestCommand, "RequiresReadyDevices")!, "manifest readiness flag");
        AssertEqual("None", (string)GetPublicProperty(manifestCommand, "PathPolicy")!, "manifest path policy");
        AssertEqual("manifest", (string)GetPublicProperty(manifestCommand, "CliHelp")!, "manifest CLI help");
        AssertEqual("Get automation command manifest.", (string)GetPublicProperty(manifestCommand, "McpDescription")!, "manifest MCP description");

        var diagnosticsCalls = 0;
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var mismatchDispatcher = CreateAutomationCommandDispatcher(
            CreateThrowingProxy(viewModelType),
            CreateConfiguredProxy(
                diagnosticsType,
                (method, _) =>
                {
                    diagnosticsCalls++;
                    return GetDefaultReturnValue(method);
                }),
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var mismatchResponse = await ExecuteAutomationCommandAsync(
                mismatchDispatcher,
                CreateAutomationCommandRequest(
                    "GetSnapshot",
                    authToken: null,
                    payloadJson: "{}",
                    manifestRevision: Sussudio.Tools.AutomationPipeProtocol.CommandManifestRevision + 1))
            .ConfigureAwait(false);

        AssertAutomationResponse(mismatchResponse, success: false, errorCode: "manifest-mismatch", status: "error", "manifest revision mismatch");
        AssertEqual(null, GetPublicProperty(mismatchResponse, "Snapshot"), "manifest mismatch response omits snapshot");
        AssertEqual(0, diagnosticsCalls, "manifest mismatch does not execute command");
    }

    internal static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var windowCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.SetFullScreenEnabled:");
        AssertContains(customCommandsText, "ExecuteSetFullScreenEnabledCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.OpenRecordingsFolder:");
        AssertContains(customCommandsText, "ExecuteOpenRecordingsFolderCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ArmClose:");
        AssertContains(customCommandsText, "ExecuteArmCloseCommand(payload, correlationId)");
        AssertContains(customCommandsText, "case AutomationCommandKind.WindowAction:");
        AssertContains(customCommandsText, "ExecuteWindowActionCommandAsync(payload, correlationId, cancellationToken)");

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
            "window action executor folded into AutomationCommandDispatcher.CustomCommands.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.WindowCommands.cs")),
            "window command bodies folded into AutomationCommandDispatcher.CustomCommands.cs");

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

    internal static async Task AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var actionType = RequireType("Sussudio.Models.AutomationFlashbackAction");

        var snapshot = Activator.CreateInstance(snapshotType)
                       ?? throw new InvalidOperationException("Failed to create AutomationSnapshot.");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackState", "Paused");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackThreadAlive", false);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackPendingCommands", 2);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailure", "thread_not_running:Pause");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 123456789L);

        var viewModel = CreateConfiguredProxy(viewModelType, (method, args) =>
        {
            if (method?.Name == "ExecuteFlashbackActionAsync")
            {
                AssertEqual(Enum.Parse(actionType, "Seek"), args![0], "dispatcher forwards seek action");
                AssertEqual(TimeSpan.FromMilliseconds(1234.5), args[1], "dispatcher forwards requested seek position");
                return Task.FromResult(false);
            }

            return GetDefaultReturnValue(method);
        });
        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
            method?.Name == "GetLatestSnapshot"
                ? snapshot
                : GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel,
            diagnostics,
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var response = await ExecuteAutomationCommandAsync(
            dispatcher,
            CreateAutomationCommandRequest("FlashbackAction", authToken: null, payloadJson: "{\"action\":\"seek\",\"positionMs\":1234.5}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: false, errorCode: "flashback-action-failed", status: "error", "failed flashback action includes structured error");
        var message = (string)GetPublicProperty(response, "Message")!;
        AssertContains(message, "Flashback action 'Seek' was rejected");
        AssertContains(message, "state=Paused");
        AssertContains(message, "threadAlive=False");
        AssertContains(message, "lastFailure=thread_not_running:Pause");
        AssertContains(message, "requestedPositionMs=1234.5");

        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("Flashback failure response data was missing.");
        AssertEqual("Seek", (string)GetPublicProperty(data, "Action")!, "flashback failure data action");
        AssertEqual(1234.5, (double)GetPublicProperty(data, "RequestedPositionMs")!, "flashback failure data requested position");
        AssertEqual("Paused", (string)GetPublicProperty(data, "PlaybackState")!, "flashback failure data playback state");
        AssertEqual(false, (bool)GetPublicProperty(data, "PlaybackThreadAlive")!, "flashback failure data thread alive");
        AssertEqual(2, (int)GetPublicProperty(data, "PendingCommands")!, "flashback failure data pending commands");
        AssertEqual("thread_not_running:Pause", (string)GetPublicProperty(data, "LastCommandFailure")!, "flashback failure data last command failure");
        AssertEqual(123456789L, (long)GetPublicProperty(data, "LastCommandFailureUtcUnixMs")!, "flashback failure data failure utc");
        AssertEqual(snapshot, GetPublicProperty(response, "Snapshot"), "flashback failure response reuses diagnostic snapshot");
    }

    internal static Task AutomationCommandDispatcher_FlashbackCommands_LiveWithCustomRouter()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var flashbackCommandsText = customCommandsText;

        AssertContains(customCommandsText, "case AutomationCommandKind.FlashbackAction:");
        AssertContains(customCommandsText, "ExecuteFlashbackActionCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteFlashbackExportCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteFlashbackGetSegmentsCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteRestartFlashbackCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "ExecuteSetFlashbackEnabledCommandAsync(payload, correlationId, cancellationToken)");

        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackActionCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackExportCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteFlashbackGetSegmentsCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteRestartFlashbackCommandAsync(");
        AssertContains(flashbackCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetFlashbackEnabledCommandAsync(");
        AssertContains(flashbackCommandsText, "_flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.GetFlashbackSegmentsAsync(cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.RestartFlashbackAsync(cancellationToken)");
        AssertContains(flashbackCommandsText, "_flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.FlashbackCommands.cs")),
            "Flashback command bodies folded into AutomationCommandDispatcher.CustomCommands.cs");

        return Task.CompletedTask;
    }

    private static string ReadAutomationCommandDispatcherFamilyText()
    {
        var files = EnumerateAutomationCommandDispatcherFamilyFiles();

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }

    private static string[] EnumerateAutomationCommandDispatcherFamilyFiles()
    {
        var repoRoot = GetRepoRoot();
        var automationDirectory = Path.Combine(repoRoot, "Sussudio", "Services", "Automation");
        return EnumerateSourceFiles(automationDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("AutomationCommandDispatcher", StringComparison.Ordinal))
            .OrderBy(file => AutomationCommandDispatcherFamilySortKey(file), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string AutomationCommandDispatcherFamilySortKey(string relativePath)
    {
        var fileName = GetRepoFileName(relativePath);
        return string.Equals(fileName, "AutomationCommandDispatcher.cs", StringComparison.Ordinal)
            ? "0"
            : "1" + fileName;
    }

    private static object CreateAutomationCommandDispatcher(string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var viewModel = CreateThrowingProxy(viewModelType);
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            CreateThrowingProxy(diagnosticsType),
            CreateThrowingProxy(windowControlType),
            authToken
        });
    }

    private static object CreateAutomationCommandDispatcher(
        object viewModel,
        object diagnosticsHub,
        object windowControl,
        string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            diagnosticsHub,
            windowControl,
            authToken
        });
    }

    private static ConstructorInfo GetAutomationCommandDispatcherConstructor(Type dispatcherType)
        => dispatcherType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType.FullName == "Sussudio.Services.Automation.AutomationViewModelPorts";
            });

    private static object CreateAutomationViewModelPorts(object viewModel)
    {
        var portsType = RequireType("Sussudio.Services.Automation.AutomationViewModelPorts");
        var fromMethod = portsType.GetMethod("From", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException("AutomationViewModelPorts.From was not found.");
        return fromMethod.Invoke(null, new[] { viewModel })
               ?? throw new InvalidOperationException("AutomationViewModelPorts.From returned null.");
    }

    private static object CreateConfiguredProxy(Type interfaceType, Func<MethodInfo?, object?[]?, object?> handler)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == "Create" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(ConfiguredAutomationProxy));
        var proxy = createMethod.Invoke(null, null)
                    ?? throw new InvalidOperationException($"Failed to create proxy for {interfaceType.FullName}.");
        ((ConfiguredAutomationProxy)proxy).Handler = handler;
        return proxy;
    }

    private static object? GetDefaultReturnValue(MethodInfo? method)
    {
        var returnType = method?.ReturnType ?? typeof(void);
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return fromResult.Invoke(null, new[] { result });
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static object CreateAutomationCommandRequest(
        string commandName,
        string? authToken,
        string payloadJson,
        int? manifestRevision = null)
    {
        var requestType = RequireType("Sussudio.Models.AutomationCommandRequest");
        var commandType = RequireType("Sussudio.Models.AutomationCommandKind");
        var request = Activator.CreateInstance(requestType)
                      ?? throw new InvalidOperationException("Failed to create AutomationCommandRequest.");
        using var payload = JsonDocument.Parse(payloadJson);
        SetPropertyBackingField(request, "Command", Enum.Parse(commandType, commandName));
        SetPropertyBackingField(request, "CorrelationId", Guid.NewGuid().ToString("N"));
        SetPropertyBackingField(request, "AuthToken", authToken);
        SetPropertyBackingField(request, "ManifestRevision", manifestRevision);
        SetPropertyBackingField(request, "Payload", payload.RootElement.Clone());
        return request;
    }

    private static async Task<object> ExecuteAutomationCommandAsync(object dispatcher, object request)
    {
        var execute = dispatcher.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public)
                      ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync was not found.");
        var task = (Task)execute.Invoke(dispatcher, new object[] { request, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
               ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync returned no result.");
    }

    private static void AssertAutomationResponse(
        object response,
        bool success,
        string? errorCode,
        string status,
        string scenario)
    {
        AssertEqual(success, (bool)GetPublicProperty(response, "Success")!, $"{scenario}: Success");
        AssertEqual(errorCode, (string?)GetPublicProperty(response, "ErrorCode"), $"{scenario}: ErrorCode");
        var actualStatus = GetPublicProperty(response, "Status")!;
        var actualStatusName = JsonNamingPolicy.SnakeCaseLower.ConvertName(actualStatus.ToString()!);
        AssertEqual(status, actualStatusName, $"{scenario}: Status");
    }

    private static object? GetPublicProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    public class ConfiguredAutomationProxy : DispatchProxy
    {
        public Func<MethodInfo?, object?[]?, object?> Handler { get; set; } =
            (_, _) => throw new NotSupportedException("No handler configured.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod, args);
    }

    private static object CreateTaskFromResult(Type resultType, object? result)
    {
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(resultType);
        return fromResult.Invoke(null, new[] { result })
               ?? throw new InvalidOperationException($"Failed to create Task<{resultType.Name}>.");
    }
}
