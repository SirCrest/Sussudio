using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes()
    {
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        AssertEqual("SussudioAutomation", GetConstant<string>(protocolType, "DefaultPipeName"), "AutomationPipeProtocol.DefaultPipeName");
        AssertEqual("SUSSUDIO_AUTOMATION_TOKEN", GetConstant<string>(protocolType, "AutomationKeyEnvVar"), "AutomationPipeProtocol.AutomationKeyEnvVar");
        AssertEqual(5000, GetConstant<int>(protocolType, "DefaultConnectTimeoutMs"), "AutomationPipeProtocol.DefaultConnectTimeoutMs");
        AssertEqual(15000, GetConstant<int>(protocolType, "DefaultResponseTimeoutMs"), "AutomationPipeProtocol.DefaultResponseTimeoutMs");
        AssertEqual(60000, GetConstant<int>(protocolType, "ExtendedResponseTimeoutMs"), "AutomationPipeProtocol.ExtendedResponseTimeoutMs");
        AssertEqual(150000, GetConstant<int>(protocolType, "RecordingResponseTimeoutMs"), "AutomationPipeProtocol.RecordingResponseTimeoutMs");
        AssertEqual(305000, GetConstant<int>(protocolType, "FlashbackMutationResponseTimeoutMs"), "AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs");

        var resolveCommand = RequireNonPublicStaticMethod(protocolType, "ResolveCommand");
        AssertEqual(1, (int)resolveCommand.Invoke(null, new object[] { "GetSnapshot" })!, "ResolveCommand exact");
        AssertEqual(1, (int)resolveCommand.Invoke(null, new object[] { "get-snapshot" })!, "ResolveCommand normalized");
        AssertEqual(17, (int)resolveCommand.Invoke(null, new object[] { "17" })!, "ResolveCommand numeric");
        AssertThrows<ArgumentException>(
            () => resolveCommand.Invoke(null, new object[] { "not-a-command" }),
            "ResolveCommand unknown command");

        var tryGetCommandValue = RequireNonPublicStaticMethod(protocolType, "TryGetCommandValue");
        var valueArgs = new object?[] { "setrecordingenabled", null };
        AssertEqual(true, (bool)tryGetCommandValue.Invoke(null, valueArgs)!, "TryGetCommandValue case-insensitive");
        AssertEqual(17, (int)valueArgs[1]!, "TryGetCommandValue SetRecordingEnabled");

        var tryGetCommandName = RequireNonPublicStaticMethod(protocolType, "TryGetCommandName");
        var nameArgs = new object?[] { 17, null };
        AssertEqual(true, (bool)tryGetCommandName.Invoke(null, nameArgs)!, "TryGetCommandName known");
        AssertEqual("SetRecordingEnabled", (string)nameArgs[1]!, "TryGetCommandName SetRecordingEnabled");
        var unknownNameArgs = new object?[] { -1, null };
        AssertEqual(false, (bool)tryGetCommandName.Invoke(null, unknownNameArgs)!, "TryGetCommandName unknown");
        AssertEqual(string.Empty, (string)unknownNameArgs[1]!, "TryGetCommandName unknown output");

        var previousToken = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN");
        try
        {
            var getAuth = RequireNonPublicStaticMethod(protocolType, "GetConfiguredAuthToken");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "env-token");
            AssertEqual("explicit-token", (string)getAuth.Invoke(null, new object?[] { "explicit-token" })!, "GetConfiguredAuthToken explicit");
            AssertEqual("env-token", (string)getAuth.Invoke(null, new object?[] { null })!, "GetConfiguredAuthToken env");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "   ");
            AssertEqual(null, getAuth.Invoke(null, new object?[] { null }), "GetConfiguredAuthToken whitespace env");

            var defaultTimeout = RequireNonPublicStaticMethod(protocolType, "GetDefaultResponseTimeout");
            AssertEqual(15000, (int)defaultTimeout.Invoke(null, new object[] { "GetSnapshot" })!, "GetDefaultResponseTimeout default command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "FlashbackExport" })!, "GetDefaultResponseTimeout flashback export command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "SetFlashbackEnabled" })!, "GetDefaultResponseTimeout flashback command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "RestartFlashback" })!, "GetDefaultResponseTimeout flashback restart command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "SetRecordingEnabled" })!, "GetDefaultResponseTimeout recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "set-recording-enabled" })!, "GetDefaultResponseTimeout normalized recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "17" })!, "GetDefaultResponseTimeout numeric recording command");

            var createEnvelope = RequireNonPublicStaticMethod(protocolType, "CreateRequestEnvelope");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "env-token");
            var payload = new Dictionary<string, object?> { ["enabled"] = true };
            var envelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 17, payload, null })!;
            AssertEqual(17, (int)envelope["command"]!, "CreateRequestEnvelope command");
            AssertEqual(32, ((string)envelope["correlationId"]!).Length, "CreateRequestEnvelope correlation id length");
            AssertEqual("env-token", (string)envelope["authToken"]!, "CreateRequestEnvelope env auth");
            AssertEqual(true, ReferenceEquals(payload, envelope["payload"]), "CreateRequestEnvelope payload identity");

            var explicitEnvelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 1, null, "explicit-token" })!;
            AssertEqual("explicit-token", (string)explicitEnvelope["authToken"]!, "CreateRequestEnvelope explicit auth");
            AssertEqual(true, explicitEnvelope["payload"] is Dictionary<string, object?>, "CreateRequestEnvelope default payload shape");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", previousToken);
        }

        return Task.CompletedTask;
    }

    private static Task SharedProtocol_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var enumNames = Enum.GetNames(enumType);
        var expectedCommands = ExpectedAutomationCommands();
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");

        if (enumNames.Length == 0)
            throw new InvalidOperationException("AutomationCommandKind enum has no members.");

        var commandMapProperty = protocolType.GetProperty(
            "CommandMap",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap not found.");
        var commandMap = commandMapProperty.GetValue(null) as IReadOnlyDictionary<string, int>
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap has an unexpected shape.");

        AssertEqual(expectedCommands.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs golden command table");
        foreach (var (name, ordinal) in expectedCommands)
        {
            if (!commandMap.TryGetValue(name, out var mappedOrdinal))
            {
                throw new InvalidOperationException($"AutomationPipeProtocol.CommandMap missing '{name}'.");
            }

            AssertEqual(ordinal, mappedOrdinal, $"AutomationPipeProtocol.CommandMap[{name}]");
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
        }

        AssertEqual(enumNames.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs AutomationCommandKind enum count");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandMaps_StayAligned_ForAdvancedMcpControls()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1");
        var resolveCommand = protocolType.GetMethod(
            "ResolveCommand",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.ResolveCommand not found.");

        foreach (var (name, ordinal) in new[]
        {
            ("GetCaptureOptions", 29),
            ("SetPreset", 30),
            ("SetSplitEncodeMode", 31),
            ("SetMjpegDecoderCount", 32),
            ("SetShowAllCaptureOptions", 33),
            ("SetPreviewVolume", 34),
            ("SetStatsVisible", 35)
        })
        {
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
            AssertEqual(ordinal, Convert.ToInt32(resolveCommand.Invoke(null, new object?[] { name })), $"AutomationPipeProtocol.ResolveCommand({name})");
        }

        AssertContains(protocolText, "Enum.GetValues<AutomationCommandKind>()");

        AssertContains(scriptText, "AutomationClient\\AutomationClient.csproj");
        AssertContains(scriptText, "Get-AutomationClientInputWriteTimeUtc");
        AssertContains(scriptText, "Test-AutomationClientBuildFresh");
        AssertContains(scriptText, "AutomationClient build failed with exit code $LASTEXITCODE.");
        AssertContains(scriptText, "AutomationClient build output is stale after rebuild");
        AssertContains(scriptText, "$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"");
        AssertContains(scriptText, "\"--command\", $Command");
        AssertContains(scriptText, "$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))");
        AssertContains(scriptText, "\"--payload-base64\", $payloadBase64");
        AssertContains(scriptText, "[int]$ResponseTimeoutMs = 0");
        AssertContains(scriptText, "\"--response-timeout-ms\", $ResponseTimeoutMs");
        AssertDoesNotContain(scriptText, "function Resolve-AutomationCommand");

        return Task.CompletedTask;
    }

    private static Task SendAutomationCommand_HelperTracksAutomationContractsInputs()
    {
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1")
            .Replace("\r\n", "\n");

        AssertContains(scriptText, "Get-AutomationClientInputWriteTimeUtc");
        AssertContains(scriptText, "(Join-Path $PSScriptRoot \"AutomationClient\")");
        AssertContains(scriptText, "(Join-Path $PSScriptRoot \"Common\")");
        AssertContains(scriptText, "(Join-Path $repoRoot \"Sussudio.Automation.Contracts\")");
        AssertContains(scriptText, "$_.Extension -in @(\".cs\", \".csproj\", \".props\", \".targets\")");
        AssertContains(scriptText, "$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"");
        AssertDoesNotContain(scriptText, "Sussudio\\Models\\AutomationCommandKind.cs");
        AssertDoesNotContain(scriptText, "Models\\AutomationCommandKind.cs");

        return Task.CompletedTask;
    }

    private static Task PipeClient_UsesSharedProtocol_ForCommandResolution()
    {
        var pipeClientText = ReadRepoFile("tools/McpServer/PipeClient.cs");

        // PipeClient should delegate to AutomationPipeProtocol, not have its own CommandMap
        AssertContains(pipeClientText, "AutomationPipeProtocol");
        AssertDoesNotContain(pipeClientText, "CommandMap = new");

        return Task.CompletedTask;
    }

    private static Task AutomationClient_UsesSharedProtocol_ForCommandResolution()
    {
        var entryText = ReadRepoFile("tools/AutomationClient/Program.cs");
        var argumentsText = ReadRepoFile("tools/AutomationClient/Program.Arguments.cs");
        var payloadText = ReadRepoFile("tools/AutomationClient/Program.Payload.cs");
        var clientText = string.Join("\n", entryText, argumentsText, payloadText);

        // AutomationClient should delegate to AutomationPipeProtocol, not have its own CommandMap
        AssertContains(clientText, "AutomationPipeProtocol");
        AssertContains(entryText, "var options = ParseArgs(args);");
        AssertContains(entryText, "var payload = BuildPayload(options);");
        AssertContains(entryText, "public int? ResponseTimeoutMs { get; set; }");
        AssertDoesNotContain(entryText, "private static Options ParseArgs(string[] args)");
        AssertDoesNotContain(entryText, "private static object BuildPayload(Options options)");
        AssertContains(argumentsText, "private static Options ParseArgs(string[] args)");
        AssertContains(argumentsText, "private static void WriteHelp()");
        AssertContains(argumentsText, "--payload-base64");
        AssertContains(payloadText, "private static object BuildPayload(Options options)");
        AssertContains(payloadText, "Convert.FromBase64String(options.PayloadBase64)");
        AssertDoesNotContain(clientText, "CommandMap = new");

        return Task.CompletedTask;
    }

    private static Task AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = ReadAutomationPipeClientSource();
        var ssctlPipeText = ReadRepoFile("tools/ssctl/PipeTransport.cs")
            .Replace("\r\n", "\n");
        var mcpPipeText = ReadRepoFile("tools/McpServer/PipeClient.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionCommandChannelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionPipeRetryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(sharedClientText, "internal static partial class AutomationPipeClient");
        AssertContains(sharedClientText, "internal static async Task<string> SendRequestAsync(");
        AssertContains(sharedClientText, "internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(");
        AssertContains(sharedClientText, "internal static bool TryReadResponseState(");
        AssertContains(sharedClientText, "internal readonly record struct AutomationPipeCommandResult(");
        AssertContains(sharedClientText, "catch (UnauthorizedAccessException ex)");
        AssertContains(sharedClientText, "\"pipe-access-denied\"");
        AssertContains(sharedClientText, "AutomationPipeProtocol.AutomationKeyEnvVar");
        AssertContains(sharedClientText, "public string ErrorCode { get; }");

        AssertContains(ssctlPipeText, "catch (AutomationPipeConnectException ex)");
        AssertContains(ssctlPipeText, "CreateSyntheticError(ex.Message, ex.ErrorCode)");
        AssertDoesNotContain(ssctlPipeText, "Sussudio is not running or not responding. Start the app and try again.");

        AssertContains(mcpPipeText, "catch (AutomationPipeConnectException ex)");
        AssertContains(mcpPipeText, "CreateSyntheticError(ex.Message, ex.ErrorCode)");
        AssertDoesNotContain(mcpPipeText, "Sussudio is not running or not responding. Start the app and try again.");

        AssertContains(diagnosticSessionCommandChannelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(diagnosticSessionCommandChannelText, "SendCommandWithConnectRetryAsync(");
        AssertDoesNotContain(diagnosticSessionText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(diagnosticSessionPipeRetryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(diagnosticSessionPipeRetryText, "internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-connect-failed\"");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-connect-timeout\"");
        AssertContains(diagnosticSessionPipeRetryText, "IsPermanentPipeConnectFailure(ex.ErrorCode)");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-access-denied\"");
        AssertDoesNotContain(diagnosticSessionText, "private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(");

        return Task.CompletedTask;
    }

    private static Task AutomationResponseState_ParsesStatusAndRetryContracts()
    {
        var responseStateType = RequireSharedToolType("Sussudio.Tools.AutomationResponseState");
        var tryRead = RequireNonPublicStaticMethod(responseStateType, "TryRead");

        AssertResponseState(
            tryRead,
            "{\"Success\":true,\"Status\":\"ready\",\"RetryAfterMs\":250}",
            expectedRead: true,
            expectedSuccess: true,
            expectedStatus: "ready",
            expectedRetryAfterMs: 250,
            "numeric retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":false,\"RetryAfterMs\":\"500\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: 500,
            "string retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":\"true\",\"Status\":42,\"RetryAfterMs\":\"soon\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "malformed values");
        AssertResponseState(
            tryRead,
            "[]",
            expectedRead: false,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "non-object response");

        return Task.CompletedTask;
    }

    private static void AssertResponseState(
        MethodInfo tryRead,
        string json,
        bool expectedRead,
        bool expectedSuccess,
        string? expectedStatus,
        int? expectedRetryAfterMs,
        string fieldName)
    {
        using var document = JsonDocument.Parse(json);
        var args = new object?[] { document.RootElement, null, null, null };
        AssertEqual(expectedRead, (bool)tryRead.Invoke(null, args)!, $"{fieldName} read result");
        AssertEqual(expectedSuccess, (bool)args[1]!, $"{fieldName} success");
        AssertEqual(expectedStatus, (string?)args[2], $"{fieldName} status");
        var actualRetryAfterMs = args[3] is null ? (int?)null : Convert.ToInt32(args[3]);
        AssertEqual(expectedRetryAfterMs, actualRetryAfterMs, $"{fieldName} retryAfterMs");
    }
}
