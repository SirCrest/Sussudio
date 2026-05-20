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
        var phaseModelsText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs")
            .Replace("\r\n", "\n");
        var samplingText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs")
            .Replace("\r\n", "\n");
        var completionText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs")
            .Replace("\r\n", "\n");
        var backgroundTasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(phaseRunnerText, "internal static partial class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(phaseModelsText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(samplingText, "internal static partial class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(scenarioText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "return scenarioPhase.ToResult();");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(phaseRunnerText, "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(samplingText, "private static async Task RunSamplingAndCompleteAsync(");
        AssertContains(samplingText, "context.SetStage(\"sampling\")");
        AssertContains(samplingText, "SampleLoopAsync(");
        AssertContains(samplingText, "DiagnosticSessionScenarioPhaseCompletion.CompleteAfterSamplingAsync(");
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
        AssertDoesNotContain(phaseRunnerText, "SampleLoopAsync(");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(phaseRunnerText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(samplingText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(samplingText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(samplingText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioStartup.StartAsync(", "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.RecordTerminalException(ex, context.GetLastStage())", "context.ScenarioCancellationSource.Cancel();");
        AssertOccursBefore(phaseRunnerText, "context.ScenarioCancellationSource.Cancel();", "DiagnosticSessionScenarioPhaseCompletion.DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(samplingText, "context.SetStage(\"sampling\")", "SampleLoopAsync(");
        AssertOccursBefore(samplingText, "SampleLoopAsync(", "DiagnosticSessionScenarioPhaseCompletion.CompleteAfterSamplingAsync(");
        AssertOccursBefore(backgroundTasksText, "await AwaitScenarioTasksAsync()", "return await AwaitRecordingSettingsDeferredAsync(");
        AssertOccursBefore(completionText, ".CompleteRegisteredScenarioWorkAsync(", "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.CompletePresentMonAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var completionText = ReadDiagnosticSessionRunExecutionCompletionSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
            .Replace("\r\n", "\n");
        var postRunText = ReadRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
            .Replace("\r\n", "\n");
        var resultBuilderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(completionText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)");
        AssertContains(completionText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(completionText, "var verification = recordingCheckResult.Verification;");
        AssertContains(completionText, "context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertContains(completionText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertContains(completionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionText, "CreateResultBuildRequest(");
        AssertContains(completionText, "context.ScenarioPhase.PresentMon");
        AssertContains(completionText, "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);");
        AssertContains(contextText, "new DiagnosticSessionCompletionContext");
        AssertContains(executionText, "return await RunCompletionPhaseAsync(");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertDoesNotContain(executionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertDoesNotContain(executionText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertDoesNotContain(executionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(resultBuilderText, "runState.SetStage(\"summary\")");
        AssertOccursBefore(completionText, "DiagnosticSessionRecordingChecks.RunAsync(", "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(", "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(", "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState)");
        AssertOccursBefore(postRunText, "setStage(\"timeline\")", "setStage(\"final-snapshot\")");

        return Task.CompletedTask;
    }
}
