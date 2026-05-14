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

    private static Task AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = ReadRepoFile("tools/Common/AutomationPipeClient.cs")
            .Replace("\r\n", "\n");
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
