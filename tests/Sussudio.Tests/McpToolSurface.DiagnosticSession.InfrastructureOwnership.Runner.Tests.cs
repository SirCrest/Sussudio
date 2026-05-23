using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "return DiagnosticSessionRunExecution.RunAsync(options, sendCommandAsync, cancellationToken);");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(runnerText, "SampleLoopAsync(");
        AssertDoesNotContain(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(executionText, "internal static class DiagnosticSessionRunExecution");
        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "SampleLoopAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var initialSnapshotText = contextText;

        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(initialSnapshotText, "using Sussudio.Models;");
        AssertContains(initialSnapshotText, "private DiagnosticSessionInitialSnapshotResult CreateUnknownInitialSnapshot()");
        AssertContains(initialSnapshotText, "private async Task<DiagnosticSessionInitialSnapshotResult> CaptureInitialSnapshotCoreAsync()");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(initialSnapshotText, "SetStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "CommandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "CommandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "RecordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await WriteLiveStateBestEffortAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(contextText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "CaptureInitialSnapshotCoreAsync()");
        AssertContains(contextText, "InitialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(contextText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertDoesNotContain(executionText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(executionText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(executionText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }
}
