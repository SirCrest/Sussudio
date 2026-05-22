using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var retryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");

        AssertContains(channelText, "internal sealed class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "using Sussudio.Models;");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "private static string CommandName(AutomationCommandKind kind)");
        AssertContains(channelText, "=> AutomationCommandCatalog.Get(kind).Name;");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "AutomationCommandKind kind,");
        AssertContains(channelText, "=> await SendRawWithConnectRetryAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendWithTokenAsync(CommandName(kind), payload, responseTimeoutMs, allowFailure, commandCancellationToken).ConfigureAwait(false);");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(channelText, "internal async Task TryWaitAsync(string condition, int timeoutMs)");
        AssertContains(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(channelText, "SendWithTokenAsync(\n                AutomationCommandKind.WaitForCondition,");
        AssertContains(channelText, "AutomationCommandKind.WaitForCondition");
        AssertContains(channelText, "[\"condition\"] = condition");
        AssertContains(channelText, "[\"timeoutMs\"] = timeoutMs");
        AssertContains(channelText, "[\"pollMs\"] = 250");
        AssertContains(channelText, "timeoutMs + 2_000");
        AssertContains(channelText, "$\"wait {condition}: {Get(response, \"Message\", \"not met\")}\"");
        AssertDoesNotContain(channelText, "\"WaitForCondition\"");
        AssertDoesNotContain(channelText, "\"GetSnapshot\"");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(runnerText, "context.CommandChannel,");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(contextText, "CommandChannel.FailureCount");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(runnerText, "var commandFailureCount = 0;");
        AssertDoesNotContain(runnerText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(runnerText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(runnerText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }
}
