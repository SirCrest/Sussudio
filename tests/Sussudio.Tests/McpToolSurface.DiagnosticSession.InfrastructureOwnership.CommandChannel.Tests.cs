using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var waitConditionsText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs")
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
        AssertDoesNotContain(waitConditionsText, "SendCommandWithConnectRetryAsync(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var waitConditionsText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs")
            .Replace("\r\n", "\n");

        AssertContains(channelText, "internal sealed partial class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(waitConditionsText, "internal sealed partial class DiagnosticSessionCommandChannel");
        AssertContains(waitConditionsText, "internal async Task TryWaitAsync(string condition, int timeoutMs)");
        AssertContains(waitConditionsText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(waitConditionsText, "\"WaitForCondition\"");
        AssertContains(waitConditionsText, "[\"condition\"] = condition");
        AssertContains(waitConditionsText, "[\"timeoutMs\"] = timeoutMs");
        AssertContains(waitConditionsText, "[\"pollMs\"] = 250");
        AssertContains(waitConditionsText, "timeoutMs + 2_000");
        AssertContains(waitConditionsText, "$\"wait {condition}: {Get(response, \"Message\", \"not met\")}\"");
        AssertDoesNotContain(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertDoesNotContain(channelText, "\"WaitForCondition\"");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(runnerText, "CommandChannel.SendAsync");
        AssertContains(runnerText, "CommandChannel.SendWithTokenAsync");
        AssertContains(contextText, "CommandChannel.FailureCount");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(runnerText, "var commandFailureCount = 0;");
        AssertDoesNotContain(runnerText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(runnerText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(runnerText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }
}
