using System.Threading.Tasks;

static partial class Program
{
    private static string ReadDiagnosticSessionBackgroundTasksSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionCleanupActionsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionPostRunActions.cs");

    private static string ReadDiagnosticSessionScenarioSetupSource()
        => ReadDiagnosticSessionScenarioStartupSource();

    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
        => ReadDiagnosticSessionFlashbackCycleScenariosSource();

    private static string ReadDiagnosticSessionFlashbackMetricsSource()
        => ReadDiagnosticSessionMetricsSource();

    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackScenarioTasks.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackScenarioTasks.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSupport.cs");

    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs");

    private static string ReadDiagnosticSessionFlashbackWaitsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSupport.cs");

    private static string ReadDiagnosticSessionMetricsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionMetrics.cs");

    private static string ReadDiagnosticSessionModelsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionResult.cs");

    private static string ReadDiagnosticSessionResultBuilderSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultBuilder.cs");

    private static string ReadDiagnosticSessionResultFormatterSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultFormatter.cs");

    private static string ReadDiagnosticSessionRunnerSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunExecutionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionRunContextSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunContextRootSource()
        => ReadDiagnosticSessionRunContextSource();

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionResult.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionRootSource()
        => ReadDiagnosticSessionRunExecutionRootSource();

    private static string ReadDiagnosticSessionRunExecutionCompletionContextSource()
        => ReadDiagnosticSessionRunExecutionRootSource();

    private static string ReadDiagnosticSessionScenarioStartupSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioCatalog.cs");

    private static string ReadNormalizedSourceFiles(params string[] paths)
    {
        var parts = new string[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            parts[i] = ReadNormalizedRepoFile(paths[i]);
        }

        return string.Join("\n", parts);
    }

    internal static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertContains(policyText, "internal static bool IsSourceSignalDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsSourceCaptureDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsPreviewSchedulerDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthTolerances;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionHealthTolerances.cs")),
            "diagnostic-session health tolerance classifiers folded into the health policy owner");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioPlan_OwnsScenarioFlags()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource();
        var catalogText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(catalogText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogText, "TryGetEntry(normalized, out _)");
        AssertContains(catalogText, "internal const string HelpList =");
        AssertContains(catalogText, "internal const string Description =");
        AssertContains(catalogText, "internal static IReadOnlyList<string> Names => Entries.Select");
        AssertContains(catalogText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(catalogText, "entry.FlashbackExportVerificationFileName");
        AssertContains(catalogText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(catalogText, ".. CreateCoreScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackPlaybackScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackExportScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackRecordingScenarioEntries(),");
        AssertContains(catalogText, "CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "internal readonly record struct DiagnosticSessionScenarioCatalogEntry(");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()");
        AssertContains(catalogText, "new(Observe)");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-stress-export.mp4\"");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()");
        AssertContains(catalogText, "FlashbackPlayback,");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true)");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()");
        AssertContains(catalogText, "RequiresRecording: true");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runCombined: true)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioCatalog.Entries.cs")),
            "Diagnostic session scenario entries folded into the catalog owner");
        AssertContains(catalogText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(catalogText, "internal static DiagnosticSessionScenarioPlan Create(");
        AssertContains(catalogText, "internal static DiagnosticSessionScenarioPlan From(string scenario)");
        AssertContains(catalogText, "DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)");
        AssertContains(catalogText, "? entry.Plan");
        AssertContains(catalogText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertContains(catalogText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(catalogText, "internal bool ToleratesSourceSignalHealthWarning");
        AssertContains(catalogText, "internal bool ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(catalogText, "internal bool IsPreviewCycleScenario");
        AssertContains(catalogText, "internal bool ToleratesSparsePreviewSchedulerStressTransitions");
        AssertContains(catalogText, "RunFlashbackSegmentPlayback");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPlan.cs")),
            "Diagnostic session scenario plan flags live with the catalog that constructs every plan");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(runnerText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-playback\"");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-stress\"");
        AssertDoesNotContain(runnerText, "scenario == \"combined\"");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioSetup_OwnsInitialMutations()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        AssertContains(setupText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(setupText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupText, "SetupFlashbackStateAsync(");
        AssertContains(setupText, "StartPreviewIfNeededAsync(");
        AssertContains(setupText, "StartRecordingIfNeededAsync(");
        AssertContains(setupText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupText, "private readonly record struct DiagnosticSessionFlashbackSetupResult(");
        AssertContains(setupText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(setupText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(setupText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(setupText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(setupText, "actions.Add(\"preview started\")");
        AssertContains(setupText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(setupText, "WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken)");
        AssertContains(setupText, "AutomationCommandKind.SetRecordingEnabled,");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioActivation.cs")),
            "diagnostic-session scenario setup/startup activation folded into the scenario catalog owner");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cycleScenariosText = ReadDiagnosticSessionFlashbackCycleScenariosSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressScenariosText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var segmentPlaybackScenariosText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var previewCycleScenariosText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var presentMonStartupText = startupText;
        var tasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(startupText, "internal static class DiagnosticSessionScenarioStartup");
        AssertDoesNotContain(startupText, "internal static partial class DiagnosticSessionScenarioStartup");
        AssertContains(startupText, "internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(");
        AssertContains(startupText, "internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback)");
        AssertContains(tasksText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertDoesNotContain(tasksText, "internal sealed partial class DiagnosticSessionBackgroundTasks");
        AssertContains(tasksText, "internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(");
        AssertContains(tasksText, "internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(");
        AssertContains(tasksText, "private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];");
        AssertContains(tasksText, "private Task<PresentMonProbeResult>? _presentMonTask;");
        AssertContains(tasksText, "private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;");
        AssertContains(tasksText, "internal void AddScenario(int awaitOrder, string stage, Task task)");
        AssertContains(tasksText, "internal void SetPresentMon(Task<PresentMonProbeResult> task)");
        AssertContains(tasksText, "internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)");
        AssertContains(tasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(tasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(tasksText, "internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(");
        AssertContains(tasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(tasksText, "_scenarioTasks.OrderBy(task => task.AwaitOrder)");
        AssertContains(tasksText, "private async Task<PresentMonProbeResult?> AwaitPresentMonAsync(");
        AssertContains(tasksText, "private async Task<PresentMonProbeResult?> ObservePresentMonAfterFaultAsync(");
        AssertContains(tasksText, "presentmon-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(tasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(tasksText, "flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "ObservePresentMonAfterFaultAsync(");
        AssertContains(tasksText, "ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(tasksText, "private static async Task ObserveTaskAfterFaultAsync(");
        AssertContains(runnerText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(startupText, "private static void RegisterFlashbackScenarioTasks(");
        AssertContains(startupText, "private static void RegisterDeferredFlashbackRecordingSettingsTask(");
        AssertContains(startupText, "private static async Task<bool> TryStartFlashbackPlaybackAsync(");
        AssertDoesNotContain(startupText, "backgroundTasks.AddScenario(");
        AssertContains(startupText, "StartPresentMonAsync(");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(");
        AssertContains(startupText, "backgroundTasks.SetRecordingSettingsDeferred(");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(startupText, "actions.Add(\"flashback recording settings deferred started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertContains(cycleScenariosText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(stressScenariosText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressScenariosText, "backgroundTasks.AddScenario(");
        AssertContains(segmentPlaybackScenariosText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackScenariosText, "backgroundTasks.AddScenario(");
        AssertDoesNotContain(startupText, "RegisterFlashbackExportPlaybackTask(");
        AssertDoesNotContain(startupText, "RegisterFlashbackRangeExportTasks(");
        AssertDoesNotContain(startupText, "RegisterFlashbackExportCoordinationTasks(");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
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
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(runnerText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(runnerText, "startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;");
        AssertContains(runnerText, ".CompleteRegisteredScenarioWorkAsync(");
        AssertContains(runnerText, "backgroundTasks.CompletePresentMonAsync(");
        AssertContains(runnerText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(runnerText, "Task? flashbackStressTask");
        AssertDoesNotContain(runnerText, "Task<PresentMonProbeResult>? presentMonTask");
        AssertDoesNotContain(runnerText, "async Task ObserveBackgroundTasksAfterFaultAsync()");
        AssertDoesNotContain(runnerText, "async Task ObserveTaskAfterFaultAsync(Task? task, string stage)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionBackgroundTasks.cs")),
            "diagnostic-session background task drain folded into DiagnosticSessionRunner.cs");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch()
    {
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var presentMonStartupText = startupText;

        AssertContains(presentMonStartupText, "private static async Task StartPresentMonAsync(");
        AssertContains(presentMonStartupText, "if (!options.IncludePresentMon)");
        AssertContains(presentMonStartupText, "var correlationSnapshotResponse = await sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(presentMonStartupText, "TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot)");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(");
        AssertContains(presentMonStartupText, "processName: \"Sussudio\"");
        AssertContains(presentMonStartupText, "outputFile: Path.Combine(outputDirectory, \"presentmon.csv\")");
        AssertContains(presentMonStartupText, "correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot)");
        AssertContains(presentMonStartupText, "actions.Add(\"presentmon capture started\")");
        AssertDoesNotContain(presentMonStartupText, "new PresentMonProbeOptions");
        AssertDoesNotContain(presentMonStartupText, "PreviewD3DSwapChainAddress");
        AssertContains(startupText, "StartPresentMonAsync(");
        AssertDoesNotContain(startupText, "PresentMonProbe.RunAsync(new PresentMonProbeOptions");
        AssertDoesNotContain(startupText, "PreviewD3DSwapChainAddress");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionSampler_OwnsSampleLoopOrdering()
    {
        var scenarioText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(scenarioText, "private static async Task SampleLoopAsync(");
        AssertContains(scenarioText, "var response = await sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(scenarioText, "samples.Add(new DiagnosticSessionSample");
        AssertContains(scenarioText, "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertOccursBefore(scenarioText, "samples.Add(new DiagnosticSessionSample", "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPhaseRunner.cs")),
            "diagnostic-session scenario phase runner folded into DiagnosticSessionRunner.cs");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionAnalysisValidation_OwnsCleanupRestoreWarnings()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var cleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupActionsText, "internal static class DiagnosticSessionCleanupActions");
        AssertDoesNotContain(cleanupActionsText, "internal static partial class DiagnosticSessionCleanupActions");
        AssertContains(cleanupActionsText, "internal static async Task<DiagnosticSessionCleanupResult> RunAsync(");
        AssertContains(cleanupActionsText, "internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification)");
        AssertContains(cleanupActionsText, "StopRecordingForCleanupAsync(");
        AssertContains(cleanupActionsText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "private static async Task RestoreLiveFlashbackPlaybackAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-go-live\")");
        AssertContains(cleanupActionsText, "private static async Task StopPreviewIfStartedAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-preview\")");
        AssertContains(cleanupActionsText, "private static async Task RestoreFlashbackEnabledStateAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertContains(cleanupActionsText, "using Sussudio.Models;");
        AssertContains(cleanupActionsText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(cleanupActionsText, "commandChannel.SendWithTokenAsync(");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.FlashbackAction,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(cleanupActionsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled)");
        AssertContains(cleanupText, "internal static class DiagnosticSessionResultBuilder");
        AssertContains(cleanupText, "private static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(runnerText, "stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
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

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingVerificationText = recordingChecksText;

        AssertContains(recordingChecksText, "internal static class DiagnosticSessionRecordingChecks");
        AssertContains(recordingChecksText, "internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(");
        AssertContains(recordingChecksText, "internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification)");
        AssertContains(recordingChecksText, "setStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingChecksText, "RunRecordingVerificationAsync(");
        AssertContains(recordingChecksText, "verification = await RunRecordingVerificationAsync(");
        AssertContains(recordingChecksText, "setStage(\"recording-validation\")");
        AssertContains(recordingChecksText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");
        AssertContains(recordingVerificationText, "private static async Task<JsonElement?> RunRecordingVerificationAsync(");
        AssertContains(recordingVerificationText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
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
        AssertContains(recordingChecksText, "var verificationCommand = \"VerifyLastRecording\"");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingChecksText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertDoesNotContain(runnerText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var postRunText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(postRunText, "private static async Task<DiagnosticSessionPostRunSnapshotResult> CapturePostRunSnapshotsAsync(");
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
        AssertContains(runnerText, "CapturePostRunSnapshotsAsync(");
        AssertContains(runnerText, "postRunSnapshots.HealthSnapshot");
        AssertContains(runnerText, "postRunSnapshots.Timeline");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionPostRunSnapshots.cs")),
            "post-run timeline and final-snapshot capture lives with the runner completion phase");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionMetrics_OwnsSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionMetricsSource();

        AssertContains(metricsText, "internal static class DiagnosticSessionMetrics");
        AssertContains(metricsText, "internal sealed class SourceCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class VisualCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewD3DMetrics");
        AssertContains(metricsText, "internal readonly record struct PlaybackCommandHealth(");
        AssertContains(metricsText, "internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(");
        AssertContains(metricsText, "private static void ObserveSourceCadenceSnapshot(");
        AssertContains(metricsText, "private static void ObservePreviewCadenceSnapshot(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(metricsText, "private static void ObserveVisualCadenceSnapshot(");
        AssertContains(metricsText, "internal static PreviewD3DMetrics BuildPreviewD3DMetrics(");
        AssertContains(metricsText, "CountArrayItems(sample.Snapshot, \"PreviewD3DRecentSlowFrames\")");
        AssertContains(metricsText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(metricsText, "private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)");
        AssertContains(metricsText, "private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)");
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


    internal static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "return await RunCompletionPhaseAsync(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertContains(runnerText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertDoesNotContain(runnerText, "internal static class DiagnosticSessionRunExecution");
        AssertDoesNotContain(runnerText, "DiagnosticSessionRunExecution.RunAsync(");
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

    internal static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var retryText = channelText;

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionPipeRetryPolicy.cs")),
            "diagnostic-session pipe retry policy lives with the command channel transport owner");
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

    internal static Task DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();
        var phaseRunnerText = ReadDiagnosticSessionRunExecutionRootSource();
        var phaseModelsText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs")
            .Replace("\r\n", "\n");
        var completionText = phaseRunnerText;
        var backgroundTasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(phaseRunnerText, "internal static class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(phaseModelsText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseState");
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
        AssertContains(phaseRunnerText, "CompleteAfterSamplingAsync(");
        AssertContains(completionText, "private static async Task CompleteAfterSamplingAsync(");
        AssertContains(completionText, ".CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)");
        AssertContains(completionText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertContains(completionText, "backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings)");
        AssertContains(backgroundTasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(backgroundTasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(backgroundTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(backgroundTasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(scenarioText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(scenarioText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(phaseRunnerText, "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(completionText, "private static async Task DrainAfterFaultAsync(");
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
        AssertDoesNotContain(scenarioText, "internal required DiagnosticSessionScenarioPhaseState PhaseState");
        AssertDoesNotContain(executionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPhaseRunner.cs")),
            "diagnostic-session scenario phase runner folded into DiagnosticSessionRunner.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionBackgroundTasks.cs")),
            "diagnostic-session background task drain folded into DiagnosticSessionRunner.cs");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioStartup.StartAsync(", "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.RecordTerminalException(ex, context.GetLastStage())", "context.ScenarioCancellationSource.Cancel();");
        AssertOccursBefore(phaseRunnerText, "context.ScenarioCancellationSource.Cancel();", "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.SetStage(\"sampling\")", "SampleLoopAsync(");
        AssertOccursBefore(phaseRunnerText, "SampleLoopAsync(", "CompleteAfterSamplingAsync(");
        AssertOccursBefore(backgroundTasksText, "await AwaitScenarioTasksAsync()", "return await AwaitRecordingSettingsDeferredAsync(");
        AssertOccursBefore(completionText, ".CompleteRegisteredScenarioWorkAsync(", "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.CompletePresentMonAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var completionRootText = ReadDiagnosticSessionRunExecutionCompletionRootSource();
        var completionContextText = ReadDiagnosticSessionRunExecutionCompletionContextSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingVerificationText = recordingChecksText;
        var postRunText = completionRootText;
        var resultBuilderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(completionRootText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)");
        AssertContains(completionContextText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionRootText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(completionRootText, "var verification = recordingCheckResult.Verification;");
        AssertContains(completionRootText, "context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertContains(completionRootText, "CapturePostRunSnapshotsAsync(");
        AssertContains(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionRootText, "CreateResultBuildRequest(");
        AssertContains(completionRootText, "context.ScenarioPhase.PresentMon");
        AssertContains(completionRootText, "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);");
        AssertContains(completionRootText, "postRunSnapshots.HealthSnapshot");
        AssertContains(completionRootText, "postRunSnapshots.Timeline");
        AssertContains(completionRootText, "runBootstrap.RunnerProcessId");
        AssertContains(contextText, "new DiagnosticSessionCompletionContext");
        AssertContains(executionText, "return await RunCompletionPhaseAsync(");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertContains(executionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(executionText, "CapturePostRunSnapshotsAsync(");
        AssertContains(executionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(resultBuilderText, "runState.SetStage(\"summary\")");
        AssertContains(agentMapText, "completion context handoff consumed by the post-cleanup completion phase");
        AssertContains(agentMapText, "post-cleanup evidence/result sequence, result-build");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the completion context handoff");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the post-cleanup evidence/result sequence");
        AssertOccursBefore(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(", "CapturePostRunSnapshotsAsync(");
        AssertOccursBefore(completionRootText, "CapturePostRunSnapshotsAsync(", "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertOccursBefore(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(", "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState)");
        AssertOccursBefore(postRunText, "setStage(\"timeline\")", "setStage(\"final-snapshot\")");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunState_OwnsTerminalState()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var runStateStart = contextText.IndexOf("internal sealed class DiagnosticSessionRunState", StringComparison.Ordinal);
        var runStateEnd = contextText.IndexOf("internal sealed class DiagnosticSessionInitialSnapshotResult", StringComparison.Ordinal);
        var runStateText = contextText[runStateStart..runStateEnd];

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(contextText, "internal string GetTerminalState()");
        AssertContains(contextText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(contextText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionRunState.cs")),
            "run state stays folded into DiagnosticSessionRunContext.cs");
        AssertDoesNotContain(runnerText, "var lastStage = \"initializing\";");
        AssertDoesNotContain(runnerText, "Exception? terminalException = null;");
        AssertDoesNotContain(runStateText, "DateTimeOffset.MinValue");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var liveStateWriterText = contextText;

        AssertContains(liveStateWriterText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(liveStateWriterText, "LivePath = Path.Combine(runBootstrap.OutputDirectory, \"session-live.json\");");
        AssertContains(liveStateWriterText, "internal string LivePath { get; }");
        AssertContains(liveStateWriterText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;");
        AssertContains(liveStateWriterText, "TerminalState = terminalStateOverride ?? (_runState.TerminalException is null ? \"running\" : _runState.GetTerminalState())");
        AssertContains(liveStateWriterText, "LastStage = terminalStateOverride is null ? _runState.LastStage : _runState.GetResultLastStage()");
        AssertContains(liveStateWriterText, "TimeSpan.FromSeconds(5)");
        AssertContains(liveStateWriterText, "The live-state file is diagnostic breadcrumbs only.");
        AssertContains(contextText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(RunBootstrap, RunState, Warnings);");
        AssertContains(contextText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(contextText, "_liveStateWriter.WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "_liveStateWriter.WriteSamplingLiveStateBestEffortAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionLiveStateWriter.cs")),
            "live-state writer stays folded into DiagnosticSessionRunContext.cs");
        AssertDoesNotContain(runnerText, "var livePath = runState.LivePath;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunContext_OwnsMutableRunInfrastructure()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "Actions = [];");
        AssertContains(contextText, "Warnings = [];");
        AssertContains(contextText, "Samples = [];");
        AssertContains(contextText, "ScenarioCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(runCancellationToken);");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, ScenarioCancellationToken, Warnings);");
        AssertContains(contextText, "InitializeUnknownSnapshotState();");
        AssertContains(contextText, "internal JsonElement InitialSnapshot { get; private set; }");
        AssertContains(contextText, "private void InitializeUnknownSnapshotState()");
        AssertContains(contextText, "InitialSnapshot = unknownSnapshot.Snapshot;");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "private readonly DiagnosticSessionLiveStateWriter _liveStateWriter;");
        AssertContains(contextText, "internal string LivePath { get; }");
        AssertContains(contextText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "internal async Task WriteSamplingLiveStateBestEffortAsync()");
        AssertContains(contextText, "public void Dispose()");
        AssertContains(contextText, "internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(");
        AssertContains(contextText, "internal DiagnosticSessionCompletionContext CreateCompletionContext(");
        AssertContains(contextText, "GetLastStage = () => RunState.LastStage,");
        AssertContains(contextText, "CommandChannel.Dispose();");
        AssertContains(contextText, "ScenarioCancellationSource.Dispose();");

        AssertContains(executionText, "using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertContains(executionText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertDoesNotContain(executionText, "new DiagnosticSessionRunState(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionLiveStateWriter(");

        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session core mutable run infrastructure");
        AssertContains(agentMapText, "initial snapshot state, live-state handoff, run context disposal, scenario/completion context construction");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunContext.cs`");
        AssertContains(cleanupPlanText, "owns the cohesive mutable per-run context");
        AssertContains(cleanupPlanText, "initial\nsnapshot state and capture, live-state writer handoff, disposal");
        AssertContains(cleanupPlanText, "scenario/completion context construction");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource()
            .Replace("\r\n", "\n");

        AssertContains(bootstrapText, "internal readonly record struct DiagnosticSessionRunBootstrap(");
        AssertContains(bootstrapText, "internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)");
        AssertContains(bootstrapText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(bootstrapText, "Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60)");
        AssertContains(bootstrapText, "Math.Clamp(options.SampleIntervalMs, 100, 60_000)");
        AssertContains(bootstrapText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\", CultureInfo.InvariantCulture)");
        AssertContains(bootstrapText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"diagnostic-sessions\", sessionId)");
        AssertContains(bootstrapText, "Path.GetFullPath(options.OutputDirectory)");
        AssertContains(bootstrapText, "Directory.CreateDirectory(outputDirectory);");
        AssertContains(bootstrapText, "Environment.ProcessId");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioCatalog.Normalize(options.Scenario)");
        AssertDoesNotContain(executionText, "Math.Clamp(options.DurationSeconds");
        AssertDoesNotContain(executionText, "Math.Clamp(options.SampleIntervalMs");
        AssertDoesNotContain(executionText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\"");
        AssertDoesNotContain(executionText, "Directory.CreateDirectory(outputDirectory);");
        AssertDoesNotContain(executionText, "var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(executionText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertContains(executionText, "\".sussudio-diag.lock\"");
        AssertContains(executionText, "FileShare.None");
        AssertContains(executionText, "FileOptions.DeleteOnClose");
        AssertContains(executionText, "Another diagnostic session is already running");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "sessionLock.Dispose();");

        return Task.CompletedTask;
    }
}
