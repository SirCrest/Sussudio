using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionScenarioPlan_OwnsScenarioFlags()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var planText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
            .Replace("\r\n", "\n");

        AssertContains(planText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(planText, "internal static DiagnosticSessionScenarioPlan From(string scenario)");
        AssertContains(planText, "scenario == DiagnosticSessionScenarios.FlashbackPlayback");
        AssertContains(planText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertContains(planText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(planText, "internal bool ToleratesSourceSignalHealthWarning");
        AssertContains(planText, "internal bool ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(planText, "internal bool IsPreviewCycleScenario");
        AssertContains(runnerText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-playback\"");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-stress\"");
        AssertDoesNotContain(runnerText, "scenario == \"combined\"");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionScenarioSetup_OwnsInitialMutations()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var setupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(setupText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(setupText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsFlashback(scenario)");
        AssertContains(setupText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsPreview(scenario)");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsRecording(scenario)");
        AssertContains(setupText, "WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken)");
        AssertContains(setupText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupText, "actions.Add(\"preview started\")");
        AssertContains(setupText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupText, "actions.Add(\"recording started\")");
        AssertContains(setupText, "await tryWaitAsync(\"RecordingFileGrowing\", 20_000)");
        AssertContains(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(runnerText, "startedPreview = setupResult.StartedPreview;");
        AssertContains(runnerText, "startedRecording = setupResult.StartedRecording;");
        AssertContains(runnerText, "enabledFlashback = setupResult.EnabledFlashback;");
        AssertContains(runnerText, "disabledFlashback = setupResult.DisabledFlashback;");
        AssertDoesNotContain(runnerText, "actions.Add(\"flashback enabled\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"preview started\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"recording started\")");
        AssertDoesNotContain(runnerText, "WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var startupRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.Registrations.cs")
            .Replace("\r\n", "\n");
        var deferredSettingsStartupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.DeferredSettings.cs")
            .Replace("\r\n", "\n");
        var exportRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.ExportRegistrations.cs")
            .Replace("\r\n", "\n");
        var presentMonStartupText = ReadRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
            .Replace("\r\n", "\n");
        var tasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(startupText, "internal static partial class DiagnosticSessionScenarioStartup");
        AssertContains(startupText, "internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(");
        AssertContains(startupText, "internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback)");
        AssertContains(tasksText, "internal sealed partial class DiagnosticSessionBackgroundTasks");
        AssertContains(tasksText, "internal void AddScenario(int awaitOrder, string stage, Task task)");
        AssertContains(tasksText, "internal async Task AwaitScenarioTasksAsync()");
        AssertContains(tasksText, "internal async Task<PresentMonProbeResult?> AwaitPresentMonAsync(");
        AssertContains(tasksText, "internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(");
        AssertContains(tasksText, "presentmon-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(");
        AssertContains(runnerText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(startupText, "private static void RegisterFlashbackScenarioTasks(");
        AssertContains(startupText, "private static void RegisterDeferredFlashbackRecordingSettingsTask(");
        AssertContains(startupText, "private static async Task<bool> TryStartFlashbackPlaybackAsync(");
        AssertContains(startupText, "backgroundTasks.AddScenario(");
        AssertContains(startupText, "DiagnosticSessionPresentMonStartup.StartAsync(");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(");
        AssertContains(deferredSettingsStartupText, "backgroundTasks.SetRecordingSettingsDeferred(");
        AssertContains(deferredSettingsStartupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(deferredSettingsStartupText, "actions.Add(\"flashback recording settings deferred started\")");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(startupRegistrationText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(startupRegistrationText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(startupRegistrationText, "RegisterFlashbackExportCoordinationTasks(");
        AssertContains(exportRegistrationText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(exportRegistrationText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(exportRegistrationText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(exportRegistrationText, "RunFlashbackExportPlaybackAsync(");
        AssertContains(exportRegistrationText, "RunFlashbackRangeExportAsync(");
        AssertContains(exportRegistrationText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(exportRegistrationText, "RunFlashbackDisableDuringExportAsync(");
        AssertContains(exportRegistrationText, "RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(runnerText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(runnerText, "startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;");
        AssertContains(runnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertContains(runnerText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(runnerText, "Task? flashbackStressTask");
        AssertDoesNotContain(runnerText, "Task<PresentMonProbeResult>? presentMonTask");
        AssertDoesNotContain(runnerText, "async Task ObserveBackgroundTasksAfterFaultAsync()");
        AssertDoesNotContain(runnerText, "async Task ObserveTaskAfterFaultAsync(Task? task, string stage)");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch()
    {
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var presentMonStartupText = ReadRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
            .Replace("\r\n", "\n");

        AssertContains(presentMonStartupText, "internal static class DiagnosticSessionPresentMonStartup");
        AssertContains(presentMonStartupText, "internal static async Task StartAsync(");
        AssertContains(presentMonStartupText, "if (!options.IncludePresentMon)");
        AssertContains(presentMonStartupText, "var correlationSnapshotResponse = await sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(presentMonStartupText, "TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot)");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(new PresentMonProbeOptions");
        AssertContains(presentMonStartupText, "ProcessName = \"Sussudio\"");
        AssertContains(presentMonStartupText, "OutputFile = Path.Combine(outputDirectory, \"presentmon.csv\")");
        AssertContains(presentMonStartupText, "ExpectedSwapChainAddress = GetString(correlationSnapshot, \"PreviewD3DSwapChainAddress\")");
        AssertContains(presentMonStartupText, "AppPresentId = GetNullableLong(correlationSnapshot, \"PreviewD3DLastRenderedPreviewPresentId\")");
        AssertContains(presentMonStartupText, "actions.Add(\"presentmon capture started\")");
        AssertContains(startupText, "DiagnosticSessionPresentMonStartup.StartAsync(");
        AssertDoesNotContain(startupText, "PresentMonProbe.RunAsync(new PresentMonProbeOptions");
        AssertDoesNotContain(startupText, "PreviewD3DSwapChainAddress");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var cleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupActionsText, "internal static partial class DiagnosticSessionCleanupActions");
        AssertContains(cleanupActionsText, "internal static async Task<DiagnosticSessionCleanupResult> RunAsync(");
        AssertContains(cleanupActionsText, "internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification)");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-go-live\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-preview\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertContains(cleanupText, "internal static class DiagnosticSessionCleanupPolicy");
        AssertContains(cleanupText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(runnerText, "private static void ValidateCleanupLifecycleRestored(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingChecksText, "internal static class DiagnosticSessionRecordingChecks");
        AssertContains(recordingChecksText, "internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(");
        AssertContains(recordingChecksText, "internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification)");
        AssertContains(recordingChecksText, "setStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingChecksText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingChecksText, "setStage(\"recording-verification\")");
        AssertContains(recordingChecksText, "verificationCommand = \"VerifyFile\"");
        AssertContains(recordingChecksText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(recordingChecksText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(recordingChecksText, "setStage(\"recording-validation\")");
        AssertContains(recordingChecksText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");
        AssertContains(runnerText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(runnerText, "verification = recordingCheckResult.Verification;");
        AssertDoesNotContain(runnerText, "SetStage(\"settings-deferred-restore\")");
        AssertDoesNotContain(runnerText, "var verificationCommand = \"VerifyLastRecording\"");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertDoesNotContain(runnerText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertDoesNotContain(runnerText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var postRunText = ReadRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(postRunText, "internal static class DiagnosticSessionPostRunSnapshots");
        AssertContains(postRunText, "internal static async Task<DiagnosticSessionPostRunSnapshotResult> CaptureAsync(");
        AssertContains(postRunText, "internal readonly record struct DiagnosticSessionPostRunSnapshotResult(");
        AssertContains(postRunText, "JsonElement HealthSnapshot,");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "\"GetPerformanceTimeline\"");
        AssertContains(postRunText, "new Dictionary<string, object?> { [\"maxEntries\"] = 240 }");
        AssertContains(postRunText, "recordTerminalException(ex, \"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(postRunText, "sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(postRunText, "TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)");
        AssertContains(postRunText, "recordTerminalException(ex, \"final-snapshot\")");
        AssertContains(runnerText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertContains(runnerText, "postRunSnapshots.HealthSnapshot");
        AssertContains(runnerText, "postRunSnapshots.Timeline");
        AssertDoesNotContain(runnerText, "SetStage(\"timeline\")");
        AssertDoesNotContain(runnerText, "\"GetPerformanceTimeline\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"final-snapshot\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionSampler_OwnsSampleLoopOrdering()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var samplerText = ReadRepoFile("tools/Common/DiagnosticSessionSampler.cs")
            .Replace("\r\n", "\n");

        AssertContains(samplerText, "internal static class DiagnosticSessionSampler");
        AssertContains(samplerText, "internal static async Task SampleLoopAsync(");
        AssertContains(samplerText, "var response = await sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(samplerText, "samples.Add(new DiagnosticSessionSample");
        AssertContains(samplerText, "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertOccursBefore(samplerText, "samples.Add(new DiagnosticSessionSample", "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionSampler;");
        AssertDoesNotContain(runnerText, "private static async Task SampleLoopAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionMetrics_OwnsSessionMetricProjection()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionMetricsSource();

        AssertContains(metricsText, "internal static partial class DiagnosticSessionMetrics");
        AssertContains(metricsText, "internal sealed class SourceCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class VisualCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewD3DMetrics");
        AssertContains(metricsText, "internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewD3DMetrics BuildPreviewD3DMetrics(");
        AssertContains(metricsText, "internal static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertContains(metricsText, "internal static long GetResetAwareCounterDelta(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionMetrics;");
        AssertDoesNotContain(runnerText, "private sealed class SourceCadenceSessionMetrics");
        AssertDoesNotContain(runnerText, "private sealed class PreviewD3DMetrics");
        AssertDoesNotContain(runnerText, "private static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertDoesNotContain(runnerText, "private static long GetCounterDelta(");

        return Task.CompletedTask;
    }

}
