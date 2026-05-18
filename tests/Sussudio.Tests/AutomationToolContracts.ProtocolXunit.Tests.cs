using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationToolContractsProtocolXunitTests
{
    [Fact]
    public void SendAutomationCommand_HelperTracksAutomationContractsInputs()
    {
        var scriptText = RuntimeContractSource.ReadRepoFile("tools/send-automation-command.ps1")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("Get-AutomationClientInputWriteTimeUtc", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"AutomationClient\")", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"Common\")", scriptText);
        Assert.Contains("(Join-Path $repoRoot \"Sussudio.Automation.Contracts\")", scriptText);
        Assert.Contains("$_.Extension -in @(\".cs\", \".csproj\", \".props\", \".targets\")", scriptText);
        Assert.Contains("$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"", scriptText);
        Assert.DoesNotContain("Sussudio\\Models\\AutomationCommandKind.cs", scriptText);
        Assert.DoesNotContain("Models\\AutomationCommandKind.cs", scriptText);
    }

    [Fact]
    public void PipeClient_UsesSharedProtocol_ForCommandResolution()
    {
        var pipeClientText = RuntimeContractSource.ReadRepoFile("tools/McpServer/PipeClient.cs");

        Assert.Contains("AutomationPipeProtocol", pipeClientText);
        Assert.DoesNotContain("CommandMap = new", pipeClientText);
    }

    [Fact]
    public void UiAutomationAdapters_UseEnumCommands_WithoutChangingLabelsOrWireNames()
    {
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/PipeTransport.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlTransportText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.Transport.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlUiText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlFlashbackText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/PipeClient.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var formatterText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var uiSettingsToolsText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/UiSettingsTools.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("SendCommandAsync(\n        AutomationCommandKind kind,", ssctlPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n            _pipeName,\n            kind,", ssctlPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", ssctlPipeText);
        Assert.Contains("HandleSimpleCommandAsync(\n        CommandContext context,\n        AutomationCommandKind kind,", ssctlTransportText);
        Assert.Contains("SendCommandAsync(\n        AutomationCommandKind kind,", mcpPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n            _pipeName,\n            kind,", mcpPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", mcpPipeText);
        Assert.Contains("Optional(AutomationCommandKind kind, string label,", formatterText);
        Assert.Contains("ExecuteAndFormatResultAsync(\n        PipeClient pipeClient,\n        AutomationCommandKind kind,", formatterText);
        Assert.Contains("pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs)", formatterText);

        Assert.Contains("AutomationCommandKind.SetStatsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetStatsSectionVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetSettingsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFrameTimeOverlayVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFlashbackTimelineVisible", ssctlFlashbackText);
        Assert.DoesNotContain("\"SetStatsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetStatsSectionVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetSettingsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFrameTimeOverlayVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFlashbackTimelineVisible\"", ssctlFlashbackText);

        Assert.Contains("ToolCommandFormatter.Optional(AutomationCommandKind.SetStatsVisible, \"SetStatsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetSettingsVisible, \"SetSettingsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFrameTimeOverlayVisible, \"SetFrameTimeOverlayVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFlashbackTimelineVisible, \"SetFlashbackTimelineVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetStatsSectionVisible, \"SetStatsSectionVisible\"", uiSettingsToolsText);
    }

    [Fact]
    public void AutomationClient_UsesSharedProtocol_ForCommandResolution()
    {
        var entryText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs");
        var argumentsText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.Arguments.cs");
        var payloadText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.Payload.cs");
        var clientText = string.Join("\n", entryText, argumentsText, payloadText);

        Assert.Contains("AutomationPipeProtocol", clientText);
        Assert.Contains("var options = ParseArgs(args);", entryText);
        Assert.Contains("var payload = BuildPayload(options);", entryText);
        Assert.Contains("public int? ResponseTimeoutMs { get; set; }", entryText);
        Assert.DoesNotContain("private static Options ParseArgs(string[] args)", entryText);
        Assert.DoesNotContain("private static object BuildPayload(Options options)", entryText);
        Assert.Contains("private static Options ParseArgs(string[] args)", argumentsText);
        Assert.Contains("private static void WriteHelp()", argumentsText);
        Assert.Contains("--payload-base64", argumentsText);
        Assert.Contains("private static object BuildPayload(Options options)", payloadText);
        Assert.Contains("Convert.FromBase64String(options.PayloadBase64)", payloadText);
        Assert.DoesNotContain("CommandMap = new", clientText);
    }

    [Fact]
    public void AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = RuntimeContractSource.ReadAutomationPipeClientSource();
        var pipeClientTransportText = RuntimeContractSource.ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var pipeClientConnectErrorsText = RuntimeContractSource.ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/PipeTransport.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/PipeClient.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionText = ReadDiagnosticSessionRunnerSource();
        var diagnosticSessionCommandChannelText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionPipeRetryText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("internal static partial class AutomationPipeClient", sharedClientText);
        Assert.Contains("internal static async Task<string> SendRequestAsync(", sharedClientText);
        Assert.Contains("internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(", sharedClientText);
        Assert.Contains("AutomationCommandKind kind", sharedClientText);
        Assert.Contains("=> SendCommandWithResultAsync(\n            pipeName,\n            (int)kind,", sharedClientText);
        Assert.Contains("internal static bool TryReadResponseState(", sharedClientText);
        Assert.Contains("internal readonly record struct AutomationPipeCommandResult(", sharedClientText);
        Assert.Contains("ConnectWithClassifiedErrorsAsync(", pipeClientTransportText);
        Assert.Contains("await writer.WriteLineAsync(requestJson)", pipeClientTransportText);
        Assert.DoesNotContain("catch (UnauthorizedAccessException ex)", pipeClientTransportText);
        Assert.DoesNotContain("\"pipe-access-denied\"", pipeClientTransportText);
        Assert.Contains("private static async Task ConnectWithClassifiedErrorsAsync(", pipeClientConnectErrorsText);
        Assert.Contains("await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);", pipeClientConnectErrorsText);
        Assert.Contains("catch (TimeoutException ex)", pipeClientConnectErrorsText);
        Assert.Contains("\"pipe-connect-timeout\"", pipeClientConnectErrorsText);
        Assert.Contains("catch (OperationCanceledException)\n        {\n            throw;\n        }", pipeClientConnectErrorsText);
        Assert.Contains("catch (UnauthorizedAccessException ex)", pipeClientConnectErrorsText);
        Assert.Contains("\"pipe-access-denied\"", pipeClientConnectErrorsText);
        Assert.Contains("AutomationPipeProtocol.AutomationKeyEnvVar", pipeClientConnectErrorsText);
        Assert.Contains("catch (Exception ex)", pipeClientConnectErrorsText);
        Assert.Contains("\"pipe-connect-failed\"", pipeClientConnectErrorsText);
        Assert.Contains("public string ErrorCode { get; }", sharedClientText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", ssctlPipeText);
        Assert.Contains("kind,", ssctlPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ThrowArgumentException", ssctlPipeText);
        Assert.Contains("throw new UsageException(ex.Message);", ssctlPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", ssctlPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", ssctlPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", ssctlPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", ssctlPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", ssctlPipeText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", mcpPipeText);
        Assert.Contains("kind,", mcpPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError", mcpPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", mcpPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", mcpPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", mcpPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", mcpPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", mcpPipeText);
        Assert.Contains("internal static class AutomationCommandTransport", sharedClientText);
        Assert.Contains("internal enum AutomationUnknownCommandHandling", sharedClientText);
        Assert.Contains("AutomationPipeProtocol.GetDefaultResponseTimeout(kind)", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex.Message, \"unknown-command\")", sharedClientText);
        Assert.Contains("catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex)", sharedClientText);
        Assert.Contains("internal static class AutomationSyntheticErrorResponse", sharedClientText);
        Assert.Contains("[\"CommandLifecycle\"] = \"failed\"", sharedClientText);
        Assert.Contains("[\"Snapshot\"] = null", sharedClientText);
        Assert.Contains("public static bool CanCreateFromException(Exception exception)", sharedClientText);
        Assert.Contains("public static JsonElement Create(Exception exception)", sharedClientText);
        Assert.Contains("AutomationPipeConnectException ex => Create(ex.Message, ex.ErrorCode)", sharedClientText);
        Assert.Contains("AutomationPipeResponseTimeoutException ex => Create(ex.Message, \"pipe-response-timeout\")", sharedClientText);
        Assert.Contains("AutomationPipeProtocolException ex => Create(ex.Message, \"pipe-protocol-error\")", sharedClientText);
        Assert.Contains("\"pipe-invalid-json\"", sharedClientText);
        Assert.Contains("\"pipe-io-error\"", sharedClientText);
        Assert.Contains("\"pipe-canceled\"", sharedClientText);

        Assert.Contains("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionCommandChannelText);
        Assert.Contains("SendCommandWithConnectRetryAsync(", diagnosticSessionCommandChannelText);
        Assert.DoesNotContain("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionText);
        Assert.Contains("internal static class DiagnosticSessionPipeRetryPolicy", diagnosticSessionPipeRetryText);
        Assert.Contains("internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-failed\"", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-timeout\"", diagnosticSessionPipeRetryText);
        Assert.Contains("IsPermanentPipeConnectFailure(ex.ErrorCode)", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-access-denied\"", diagnosticSessionPipeRetryText);
        Assert.DoesNotContain("private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionText);
    }

    [Fact]
    public void AutomationResponseState_ParsesStatusAndRetryContracts()
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
    }

    private static string ReadDiagnosticSessionRunnerSource()
        => string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRunner*.cs")
                .Concat(Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRun*.cs"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)));

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = ToolFormatterTestAssembly.Load(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

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
        Assert.Equal(expectedRead, (bool)tryRead.Invoke(null, args)!);
        Assert.Equal(expectedSuccess, (bool)args[1]!);
        Assert.Equal(expectedStatus, (string?)args[2]);
        var actualRetryAfterMs = args[3] is null ? (int?)null : Convert.ToInt32(args[3]);
        Assert.Equal(expectedRetryAfterMs, actualRetryAfterMs);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
