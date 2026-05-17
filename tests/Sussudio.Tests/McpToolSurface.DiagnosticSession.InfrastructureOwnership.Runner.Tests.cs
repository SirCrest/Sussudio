using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
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
        AssertContains(executionText, "internal static partial class DiagnosticSessionRunExecution");
        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "SampleLoopAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(initialSnapshotText, "internal static class DiagnosticSessionInitialSnapshot");
        AssertContains(initialSnapshotText, "using Sussudio.Models;");
        AssertContains(initialSnapshotText, "internal static DiagnosticSessionInitialSnapshotResult CreateUnknown()");
        AssertContains(initialSnapshotText, "internal static async Task<DiagnosticSessionInitialSnapshotResult> CaptureAsync(");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknown();");
        AssertContains(initialSnapshotText, "setStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "commandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "recordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await writeLiveStateAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(contextText, "var unknownSnapshot = DiagnosticSessionInitialSnapshot.CreateUnknown();");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "DiagnosticSessionInitialSnapshot.CaptureAsync(");
        AssertContains(contextText, "InitialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(contextText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertDoesNotContain(runnerText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(runnerText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(runnerText, "var initialResponse = await commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(runnerText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(runnerText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }
}
