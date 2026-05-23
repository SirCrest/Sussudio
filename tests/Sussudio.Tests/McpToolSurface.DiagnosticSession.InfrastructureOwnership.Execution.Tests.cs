using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();
        var phaseRunnerText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            .Replace("\r\n", "\n");
        var phaseContextText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseContext.cs")
            .Replace("\r\n", "\n");
        var phaseResultText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseResult.cs")
            .Replace("\r\n", "\n");
        var phaseStateText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseState.cs")
            .Replace("\r\n", "\n");
        var completionText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs")
            .Replace("\r\n", "\n");
        var backgroundTasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(phaseRunnerText, "internal static class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(phaseContextText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(phaseResultText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(phaseStateText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(scenarioText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "return scenarioPhase.ToResult();");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(phaseRunnerText, "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(phaseRunnerText, "private static async Task RunSamplingAndCompleteAsync(");
        AssertContains(phaseRunnerText, "context.SetStage(\"sampling\")");
        AssertContains(phaseRunnerText, "SampleLoopAsync(");
        AssertContains(phaseRunnerText, "DiagnosticSessionScenarioPhaseCompletion.CompleteAfterSamplingAsync(");
        AssertContains(completionText, "internal static class DiagnosticSessionScenarioPhaseCompletion");
        AssertContains(completionText, "internal static async Task CompleteAfterSamplingAsync(");
        AssertContains(completionText, ".CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)");
        AssertContains(completionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertContains(completionText, "backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings)");
        AssertContains(backgroundTasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(backgroundTasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(backgroundTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(backgroundTasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(scenarioText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(scenarioText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(phaseRunnerText, "DiagnosticSessionScenarioPhaseCompletion.DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(completionText, "internal static async Task DrainAfterFaultAsync(");
        AssertContains(completionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertDoesNotContain(phaseRunnerText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(contextText, "new DiagnosticSessionScenarioPhaseContext");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;");
        AssertContains(executionText, "scenarioPhase = await DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "scenarioPhase.StartedRecording");
        AssertContains(executionText, "scenarioPhase.StartedPreview");
        AssertContains(executionText, "scenarioPhase.EnabledFlashback");
        AssertContains(executionText, "scenarioPhase.DisabledFlashback");
        AssertContains(executionText, "scenarioPhase.StartedFlashbackPlayback");
        AssertContains(contextText, "ScenarioPhase = scenarioPhase,");
        AssertDoesNotContain(executionText, "new DiagnosticSessionScenarioPhaseState()");
        AssertDoesNotContain(scenarioText, "internal required DiagnosticSessionScenarioPhaseState PhaseState");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertDoesNotContain(executionText, "SampleLoopAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(executionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(phaseRunnerText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioStartup.StartAsync(", "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.RecordTerminalException(ex, context.GetLastStage())", "context.ScenarioCancellationSource.Cancel();");
        AssertOccursBefore(phaseRunnerText, "context.ScenarioCancellationSource.Cancel();", "DiagnosticSessionScenarioPhaseCompletion.DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.SetStage(\"sampling\")", "SampleLoopAsync(");
        AssertOccursBefore(phaseRunnerText, "SampleLoopAsync(", "DiagnosticSessionScenarioPhaseCompletion.CompleteAfterSamplingAsync(");
        AssertOccursBefore(backgroundTasksText, "await AwaitScenarioTasksAsync()", "return await AwaitRecordingSettingsDeferredAsync(");
        AssertOccursBefore(completionText, ".CompleteRegisteredScenarioWorkAsync(", "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.CompletePresentMonAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var completionRootText = ReadDiagnosticSessionRunExecutionCompletionRootSource();
        var completionContextText = ReadDiagnosticSessionRunExecutionCompletionContextSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
            .Replace("\r\n", "\n");
        var postRunText = ReadRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
            .Replace("\r\n", "\n");
        var resultBuilderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(completionRootText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)");
        AssertContains(completionContextText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(completionRootText, "var verification = recordingCheckResult.Verification;");
        AssertContains(completionRootText, "context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertContains(completionRootText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertContains(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionRootText, "CreateResultBuildRequest(");
        AssertContains(completionRootText, "context.ScenarioPhase.PresentMon");
        AssertContains(completionRootText, "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);");
        AssertContains(completionRootText, "postRunSnapshots.HealthSnapshot");
        AssertContains(completionRootText, "postRunSnapshots.Timeline");
        AssertContains(completionRootText, "runBootstrap.RunnerProcessId");
        AssertDoesNotContain(completionRootText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertDoesNotContain(completionContextText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(contextText, "new DiagnosticSessionCompletionContext");
        AssertContains(executionText, "return await RunCompletionPhaseAsync(");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertContains(executionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(executionText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertContains(executionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(resultBuilderText, "runState.SetStage(\"summary\")");
        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs` owns the");
        AssertContains(agentMapText, "post-cleanup evidence/result sequence, result-build");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunExecution.CompletionContext.cs` owns the completion");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunExecution.cs` owns the post-cleanup evidence/result sequence");
        AssertOccursBefore(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(", "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertOccursBefore(completionRootText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(", "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertOccursBefore(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(", "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState)");
        AssertOccursBefore(postRunText, "setStage(\"timeline\")", "setStage(\"final-snapshot\")");

        return Task.CompletedTask;
    }
}
