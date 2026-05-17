using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");
        var tolerancesText = ReadRepoFile("tools/Common/DiagnosticSessionHealthTolerances.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertDoesNotContain(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertDoesNotContain(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(tolerancesText, "internal static class DiagnosticSessionHealthTolerances");
        AssertContains(tolerancesText, "internal static bool IsSourceSignalDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsSourceCaptureDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsPreviewSchedulerDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(tolerancesText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(tolerancesText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(tolerancesText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthTolerances;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionScenarioPlan_OwnsScenarioFlags()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var bootstrapText = ReadRepoFile("tools/Common/DiagnosticSessionRunBootstrap.cs")
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
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(runnerText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-playback\"");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-stress\"");
        AssertDoesNotContain(runnerText, "scenario == \"combined\"");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionScenarioSetup_OwnsInitialMutations()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(setupText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(setupText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupText, "using Sussudio.Models;");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsFlashback(scenario)");
        AssertContains(setupText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsPreview(scenario)");
        AssertContains(setupText, "DiagnosticSessionScenarios.NeedsRecording(scenario)");
        AssertContains(setupText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(setupText, "WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken)");
        AssertContains(setupText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(setupText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(setupText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(setupText, "commandChannel.SendAsync(");
        AssertContains(setupText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupText, "actions.Add(\"preview started\")");
        AssertContains(setupText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupText, "actions.Add(\"recording started\")");
        AssertContains(setupText, "await tryWaitAsync(\"RecordingFileGrowing\", 20_000)");
        AssertContains(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(runnerText, "context.CommandChannel,");
        AssertContains(runnerText, "startedPreview = setupResult.StartedPreview;");
        AssertContains(runnerText, "startedRecording = setupResult.StartedRecording;");
        AssertContains(runnerText, "enabledFlashback = setupResult.EnabledFlashback;");
        AssertContains(runnerText, "disabledFlashback = setupResult.DisabledFlashback;");
        AssertDoesNotContain(runnerText, "actions.Add(\"flashback enabled\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"preview started\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"recording started\")");
        AssertDoesNotContain(runnerText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertDoesNotContain(setupText, "sendAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetRecordingEnabled\"");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var startupRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.Registrations.cs")
            .Replace("\r\n", "\n");
        var cycleScenariosText = ReadDiagnosticSessionFlashbackCycleScenariosSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressScenariosText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var segmentPlaybackScenariosText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var previewCycleScenariosText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
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
        AssertDoesNotContain(startupText, "backgroundTasks.AddScenario(");
        AssertContains(startupText, "DiagnosticSessionPresentMonStartup.StartAsync(");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(");
        AssertContains(startupRegistrationText, "backgroundTasks.SetRecordingSettingsDeferred(");
        AssertContains(startupRegistrationText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(startupRegistrationText, "actions.Add(\"flashback recording settings deferred started\")");
        AssertContains(startupRegistrationText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(startupRegistrationText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(startupRegistrationText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(startupRegistrationText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(startupRegistrationText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackEncoderCycleAsync(");
        AssertContains(cycleScenariosText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(stressScenariosText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressScenariosText, "backgroundTasks.AddScenario(");
        AssertContains(segmentPlaybackScenariosText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackScenariosText, "backgroundTasks.AddScenario(");
        AssertDoesNotContain(startupRegistrationText, "RegisterFlashbackExportPlaybackTask(");
        AssertDoesNotContain(startupRegistrationText, "RegisterFlashbackRangeExportTasks(");
        AssertDoesNotContain(startupRegistrationText, "RegisterFlashbackExportCoordinationTasks(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupRegistrationText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(exportScenariosText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(exportScenariosText, "RunFlashbackExportPlaybackAsync(");
        AssertContains(exportScenariosText, "RunFlashbackRangeExportAsync(");
        AssertContains(exportScenariosText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(exportScenariosText, "RunFlashbackDisableDuringExportAsync(");
        AssertContains(exportScenariosText, "RunFlashbackRotatedExportAsync(");
        AssertContains(previewCycleScenariosText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
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
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var cleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();
        var cleanupActionsRootText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.cs")
            .Replace("\r\n", "\n");
        var cleanupRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.Recording.cs")
            .Replace("\r\n", "\n");
        var cleanupStateRestoreText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.StateRestore.cs")
            .Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupActionsText, "internal static partial class DiagnosticSessionCleanupActions");
        AssertContains(cleanupActionsText, "internal static async Task<DiagnosticSessionCleanupResult> RunAsync(");
        AssertContains(cleanupActionsText, "internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification)");
        AssertContains(cleanupActionsRootText, "StopRecordingForCleanupAsync(");
        AssertContains(cleanupRecordingText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(cleanupRecordingText, "setStage(\"cleanup-stop-recording\")");
        AssertContains(cleanupRecordingText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertContains(cleanupStateRestoreText, "setStage(\"cleanup-go-live\")");
        AssertContains(cleanupStateRestoreText, "setStage(\"cleanup-stop-preview\")");
        AssertContains(cleanupStateRestoreText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertContains(cleanupStateRestoreText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertContains(cleanupActionsText, "using Sussudio.Models;");
        AssertContains(cleanupActionsText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(cleanupActionsText, "commandChannel.SendWithTokenAsync(");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.FlashbackAction,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(cleanupActionsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled)");
        AssertContains(cleanupText, "internal static class DiagnosticSessionCleanupPolicy");
        AssertContains(cleanupText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(runnerText, "stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(runnerText, "private static void ValidateCleanupLifecycleRestored(");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetRecordingEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"FlashbackAction\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "GetDefaultResponseTimeout(\"SetFlashbackEnabled\")");
        AssertDoesNotContain(cleanupActionsRootText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(cleanupActionsRootText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-restore-flashback-on\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingChecksText, "internal static class DiagnosticSessionRecordingChecks");
        AssertContains(recordingChecksText, "internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(");
        AssertContains(recordingChecksText, "internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification)");
        AssertContains(recordingChecksText, "setStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingChecksText, "DiagnosticSessionRecordingVerification.RunAsync(");
        AssertContains(recordingChecksText, "verification = await DiagnosticSessionRecordingVerification.RunAsync(");
        AssertContains(recordingChecksText, "setStage(\"recording-validation\")");
        AssertContains(recordingChecksText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");
        AssertContains(recordingVerificationText, "internal static class DiagnosticSessionRecordingVerification");
        AssertContains(recordingVerificationText, "internal static async Task<JsonElement?> RunAsync(");
        AssertContains(recordingVerificationText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(recordingVerificationText, "var verificationCommand = \"VerifyLastRecording\";");
        AssertContains(recordingVerificationText, "verificationCommand = \"VerifyFile\";");
        AssertContains(recordingVerificationText, "[\"strict\"] = true");
        AssertContains(recordingVerificationText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(recordingVerificationText, "sendAsync(verificationCommand, verificationPayload, 60_000)");
        AssertContains(recordingVerificationText, "return verificationElement.Clone();");
        AssertContains(recordingVerificationText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(recordingVerificationText, "recordTerminalException(ex, \"recording-verification\")");
        AssertContains(runnerText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertDoesNotContain(runnerText, "SetStage(\"settings-deferred-restore\")");
        AssertDoesNotContain(recordingChecksText, "var verificationCommand = \"VerifyLastRecording\"");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertDoesNotContain(recordingChecksText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertDoesNotContain(runnerText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
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
        var runnerText = ReadDiagnosticSessionRunnerSource();
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
        var runnerText = ReadDiagnosticSessionRunnerSource();
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
