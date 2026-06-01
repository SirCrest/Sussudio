using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
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

    private static Assembly LoadDiagnosticSessionRunnerAssembly()
    {
        return LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
    }

    private static object CreateDiagnosticSessionOptions(
        Assembly assembly,
        string scenario,
        int durationSeconds,
        int sampleIntervalMs,
        string outputDirectory)
    {
        var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
        var options = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");

        optionsType.GetProperty("Scenario")!.SetValue(options, scenario);
        optionsType.GetProperty("DurationSeconds")!.SetValue(options, durationSeconds);
        optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, sampleIntervalMs);
        optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);
        return options;
    }

    private static async Task<object> RunDiagnosticSessionRunnerAsync(
        Assembly assembly,
        object options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand,
        CancellationToken cancellationToken = default)
    {
        var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
            ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
        var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
        var task = runAsync.Invoke(null, new object?[] { options, sendCommand, cancellationToken }) as Task
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");
    }

    private static JsonElement ParseDiagnosticSessionJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    internal static async Task DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-unknown-initial-test-{Guid.NewGuid():N}");
        var commands = new List<string>();

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "preview-only",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                commands.Add(command);
                if (command is "SetPreviewEnabled" or "SetRecordingEnabled" or "SetFlashbackEnabled")
                {
                    throw new InvalidOperationException($"Unexpected state mutation command: {command}");
                }

                return Task.FromResult(ParseDiagnosticSessionJson(command == "GetPerformanceTimeline"
                    ? """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "ok"
                      }
                      """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic unknown initial result success");
            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "skipped state-mutating scenario");
            AssertEqual(false, commands.Contains("SetPreviewEnabled"), "diagnostic unknown initial did not start preview");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static Task DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var observationType = assembly.GetType("Sussudio.Tools.DiagnosticHealthObservation")
            ?? throw new InvalidOperationException("DiagnosticHealthObservation type was not found.");
        var sourceMetricsType = assembly.GetType("Sussudio.Tools.SourceCadenceSessionMetrics")
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics type was not found.");
        var sparseSourceWarning = healthPolicyType.GetMethod(
                "IsSparseSourceCaptureCadenceWarningRun",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sparse source-cadence classifier was not found.");

        var observation = Activator.CreateInstance(
                observationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "Warning", "source_capture", "source gaps=1 drops=1", 85_727L, 2 },
                culture: null)
            ?? throw new InvalidOperationException("DiagnosticHealthObservation instance could not be created.");
        var metrics = Activator.CreateInstance(sourceMetricsType, nonPublic: true)
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics instance could not be created.");
        sourceMetricsType.GetProperty("MaxSevereGapCountObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxDropPercentObserved")!.SetValue(metrics, 0.042);

        AssertEqual(
            true,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "sparse source cadence warning without source counter deltas");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 1L, 0L, 300, true })!,
            "source reader drop delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 1L, 300, true })!,
            "video ingest error delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, false })!,
            "unhealthy visual cadence blocks sparse source cadence tolerance");

        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 3L);
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "repeated source cadence drops block sparse source cadence tolerance");

        return Task.CompletedTask;
    }

    internal static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-connect-retry-test-{Guid.NewGuid():N}");
        var getSnapshotAttempts = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotAttempts++;
                    if (getSnapshotAttempts <= 2)
                    {
                        return Task.FromResult(ParseDiagnosticSessionJson("""
                            {
                              "Success": false,
                              "Status": "error",
                              "CommandLifecycle": "failed",
                              "Message": "Sussudio is not running or not responding. Start the app and try again.",
                              "ErrorCode": "pipe-connect-failed"
                            }
                            """));
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(true, GetBoolProperty(result, "Success"), "diagnostic synthetic connect retry result success");
            AssertEqual(true, getSnapshotAttempts >= 3, "diagnostic synthetic connect failure was retried");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic synthetic connect retry summary success");
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic synthetic connect retry terminal state");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-concurrent-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");

        // Simulate a concurrent in-flight diagnostic session by holding the same exclusive
        // lock file the runner uses. A second RunAsync against this OutputDirectory must
        // fail fast with InvalidOperationException rather than corrupt the artifact set.
        FileStream? holderLock = null;
        try
        {
            holderLock = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (_, _, _) =>
                Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "should-not-be-called"
                    }
                    """));

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");

            Exception? captured = null;
            try
            {
                var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                    ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                captured = ex.InnerException;
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            if (captured is null)
            {
                throw new InvalidOperationException("Assertion failed: expected concurrent invocation to throw, but RunAsync completed.");
            }

            AssertEqual(typeof(InvalidOperationException), captured.GetType(), "diagnostic concurrent invocation exception type");
            AssertContains(captured.Message ?? string.Empty, "Another diagnostic session");

            // Artifacts must NOT have been written; only the lock file should exist.
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "summary.json")), "diagnostic concurrent invocation must not write summary");
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "session-live.json")), "diagnostic concurrent invocation must not write live state");
        }
        finally
        {
            holderLock?.Dispose();
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-failure-test-{Guid.NewGuid():N}");
        var getSnapshotCount = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotCount++;
                    if (getSnapshotCount == 3)
                    {
                        throw new InvalidOperationException("simulated final snapshot failure");
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic failure result success");
            AssertEqual("failed", GetPropertyValue(result, "TerminalState") as string, "diagnostic failure terminal state");
            AssertEqual("final-snapshot", GetPropertyValue(result, "LastStage") as string, "diagnostic failure last stage");
            AssertContains(GetPropertyValue(result, "UnhandledException") as string ?? string.Empty, "InvalidOperationException");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic failure summary success");
            AssertEqual("failed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure summary terminal state");
            AssertEqual("final-snapshot", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure summary last stage");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "final-snapshot");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("failed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure live terminal state");
            AssertEqual("final-snapshot", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure live last stage");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static async Task DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-export-playback-test-{Guid.NewGuid():N}");
        var requests = new List<(string Command, Dictionary<string, object?>? Payload)>();
        var getSnapshotCount = 0;
        var goLiveRequested = false;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "flashback-export-playback",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);
            options.GetType().GetProperty("LeaveRunning")!.SetValue(options, true);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, payload, _) =>
            {
                requests.Add((command, payload));
                if (command == "FlashbackAction" &&
                    string.Equals(GetPayloadString(payload, "action"), "go-live", StringComparison.OrdinalIgnoreCase))
                {
                    goLiveRequested = true;
                }

                return Task.FromResult(command switch
                {
                    "GetSnapshot" => CreateSnapshotResponse(++getSnapshotCount),
                    "GetPerformanceTimeline" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """),
                    "WaitForCondition" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "condition met"
                        }
                        """),
                    "FlashbackExport" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Exported 120 packets from 1 segments"
                        }
                        """),
                    "VerifyFile" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Strict verification passed.",
                          "Data": {
                            "Succeeded": true,
                            "Message": "Strict verification passed."
                          }
                        }
                        """),
                    _ => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "ok"
                        }
                        """)
                });
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            if (!GetBoolProperty(result, "Success"))
            {
                var warnings = GetPropertyValue(result, "Warnings") as System.Collections.IEnumerable;
                var warningText = warnings == null
                    ? string.Empty
                    : string.Join(" | ", warnings.Cast<object?>().Select(item => item?.ToString() ?? string.Empty));
                throw new InvalidOperationException($"Assertion failed for flashback export playback diagnostic success: warnings={warningText}");
            }

            AssertEqual(true, requests.Any(request => request.Command == "SetFlashbackEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback enabled Flashback");
            AssertEqual(true, requests.Any(request => request.Command == "SetPreviewEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback started preview");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "pause"), "flashback export playback pauses before seek");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "seek" && GetPayloadDouble(request.Payload, "positionMs") == 1000d), "flashback export playback seeks to 1000ms");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "play"), "flashback export playback starts playback");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackExport" && GetPayloadDouble(request.Payload, "seconds") == 1d), "flashback export playback exports one second");
            AssertEqual(true, requests.Any(request => request.Command == "VerifyFile" && GetPayloadString(request.Payload, "verificationProfile") == "flashback-export"), "flashback export playback verifies export");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "go-live"), "flashback export playback returns live");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "flashback export playback summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export during playback verified");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export playback go-live requested");
            AssertEqual(0, summaryDocument.RootElement.GetProperty("Warnings").GetArrayLength(), "flashback export playback summary warning count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        JsonElement CreateSnapshotResponse(int snapshotIndex)
        {
            if (snapshotIndex == 1)
            {
                return ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Snapshot": {
                        "IsPreviewing": false,
                        "IsRecording": false,
                        "FlashbackActive": false,
                        "FlashbackPlaybackState": "Live",
                        "DiagnosticHealthStatus": "Healthy",
                        "DiagnosticLikelyStage": "none",
                        "DiagnosticSummary": "No degraded frame lane detected.",
                        "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                        "FrameLedgerRecentEvents": []
                      }
                    }
                    """);
            }

            var playbackState = !goLiveRequested && snapshotIndex >= 5 ? "Playing" : "Live";
            var playbackFrames = snapshotIndex <= 4 ? 0 : snapshotIndex * 16;
            return ParseDiagnosticSessionJson($$"""
                {
                  "Success": true,
                  "Snapshot": {
                    "IsPreviewing": true,
                    "IsRecording": false,
                    "FlashbackActive": true,
                    "FlashbackBufferedDurationMs": 12000,
                    "FlashbackEncodedFrames": 360,
                    "FlashbackPlaybackState": "{{playbackState}}",
                    "FlashbackPlaybackFrameCount": {{playbackFrames}},
                    "FlashbackPlaybackPendingCommands": 0,
                    "FlashbackPlaybackCommandsDropped": 0,
                    "FlashbackPlaybackCommandsSkippedNotReady": 0,
                    "FlashbackPlaybackSubmitFailures": 0,
                    "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                    "FlashbackPlaybackSeekCommandsCoalesced": 0,
                    "FlashbackExportActive": false,
                    "FlashbackExportStatus": "Succeeded",
                    "FlashbackExportMessage": "Exported 120 packets from 1 segments",
                    "FlashbackExportOutputPath": "flashback-export-playback.mp4",
                    "ExpectedCaptureFrameRate": 120,
                    "SelectedExactFrameRate": 120,
                    "PreviewCadenceObservedFps": 120,
                    "VisualCadenceChangeFps": 120,
                    "VisualCadenceRepeatPercent": 0,
                    "DiagnosticHealthStatus": "Healthy",
                    "DiagnosticLikelyStage": "none",
                    "DiagnosticSummary": "No degraded frame lane detected.",
                    "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                    "FrameLedgerRecentEvents": []
                  }
                }
                """);
        }

        static string? GetPayloadString(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) ? value?.ToString() : null;

        static bool? GetPayloadBool(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is bool boolValue ? boolValue : null;

        static double? GetPayloadDouble(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is IConvertible convertible
                ? convertible.ToDouble(CultureInfo.InvariantCulture)
                : null;

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "flashback export playback action array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected array to contain '{token}'.");
        }
    }

    internal static async Task McpDiagnosticSessionTool_RecordsSnapshotArtifacts()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-test-{Guid.NewGuid():N}");
        var result = string.Empty;
        object? toolResult = null;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": false,
                                 "IsRecording": false,
                                 "FlashbackActive": false,
                                 "DiagnosticHealthStatus": "Idle",
                                 "DiagnosticLikelyStage": "diagnostic_unavailable",
                                 "DiagnosticSummary": "Preview and recording are idle.",
                                 "DiagnosticEvidence": "Start preview or recording to collect live frame-lane diagnostics.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 4,
                                 "PreviewD3DFrameStatsFailureCount": 1,
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 7,
                                 "PreviewD3DFrameStatsFailureCount": 2,
                                 "PreviewD3DRecentSlowFrames": [
                                   {
                                     "SlowReason": "present_interval",
                                     "WorstOverBudgetMs": 1.5,
                                     "PresentIntervalMs": 9.8,
                                     "TotalFrameCpuMs": 4.2,
                                     "PresentCallMs": 0.7,
                                     "PendingFrameCount": 1
                                   }
                                 ],
                                 "FrameLedgerRecentEvents": [
                                   {
                                     "SourceSequence": 7,
                                     "Stage": "CaptureArrived",
                                     "QpcTimestamp": 123456,
                                     "Accepted": true
                                   }
                                 ]
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": [
                                 {
                                   "TimestampUtc": "2026-04-26T00:00:00Z",
                                   "PerformanceScore": 100
                                 }
                               ]
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(false, GetMcpToolResultIsError(toolResult), "diagnostic session success MCP isError");
            AssertContains(result, "== Diagnostic Session: PASS ==");
            AssertContains(result, "Health: Healthy | Stage: none");
            AssertContains(result, "Preview D3D Perf: onePercentLowFpsEnd=0 onePercentLowFpsMin=0 missedRefreshDelta=3 statsFailureDelta=1 maxRecentSlowFrames=1 latestSlowReason=present_interval overBudgetMs=1.5 presentIntervalMs=9.8 totalFrameCpuMs=4.2 presentCallMs=0.7 pending=1");
            AssertContains(result, "Frame Ledger:");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            var samplesPath = Path.Combine(outputDirectory, "samples.json");
            var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic session summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic session live artifact");
            AssertEqual(true, File.Exists(samplesPath), "diagnostic session samples artifact");
            AssertEqual(true, File.Exists(frameLedgerPath), "diagnostic session frame ledger artifact");
            AssertContains(result, $"Live: {livePath}");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic session terminal state");
            AssertEqual("summary", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic session last stage");
            AssertEqual(true, summaryDocument.RootElement.GetProperty("RunnerProcessId").GetInt32() > 0, "diagnostic session runner pid");
            AssertEqual(livePath, summaryDocument.RootElement.GetProperty("LivePath").GetString(), "diagnostic session live path");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("completed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic live terminal state");
            AssertEqual("summary-written", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic live last stage");
            AssertEqual("Healthy", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic live health status");
            AssertEqual("none", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic live likely stage");
            AssertEqual(0, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic live warning count");
            AssertEqual(string.Empty, liveDocument.RootElement.GetProperty("LastWarning").GetString(), "diagnostic live last warning");

            using var frameLedgerDocument = JsonDocument.Parse(File.ReadAllText(frameLedgerPath));
            AssertEqual(1, frameLedgerDocument.RootElement.GetProperty("EventCount").GetInt32(), "diagnostic session frame ledger event count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session-failure");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-health-test-{Guid.NewGuid():N}");
        object? toolResult = null;
        var result = string.Empty;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Critical",
                                 "DiagnosticLikelyStage": "flashback_playback",
                                 "DiagnosticSummary": "Playback cadence collapsed.",
                                 "DiagnosticEvidence": "1pctLow=5fps target=120fps",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": []
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(true, GetMcpToolResultIsError(toolResult), "diagnostic session failure MCP isError");
            AssertContains(result, "== Diagnostic Session: FAIL ==");
            AssertContains(result, "diagnostic health degraded during session");
            AssertContains(result, "health=Critical");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic health failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic health failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic health failure summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "diagnostic health degraded during session");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("Critical", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic health failure live health");
            AssertEqual("flashback_playback", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic health failure live stage");
            AssertEqual(1, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic health failure live warning count");
            AssertContains(liveDocument.RootElement.GetProperty("LastWarning").GetString() ?? string.Empty, "health=Critical");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic health warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "internal static class DiagnosticSessionFlashbackValidation");
        AssertContains(validationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(validationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(validationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(validationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(validationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(validationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(validationText, "\"flashback preview: present/display pressure \"");
        AssertContains(validationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("DiagnosticSessionSample type was not found.");
        var buildObservation = healthPolicyType.GetMethod(
                "BuildSessionDiagnosticHealthObservation",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildSessionDiagnosticHealthObservation was not found.");

        var samples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Warning", "flashback_playback", "startup 1% low")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var finalSnapshot = CreateDiagnosticSnapshot("Healthy", "none", "final");
        var transientWarningObservation = buildObservation.Invoke(
                null,
                new object?[] { samples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Transient warning observation was null.");
        AssertEqual("Healthy", GetPropertyValue(transientWarningObservation, "HealthStatus") as string, "flashback warmup health status");
        AssertEqual("none", GetPropertyValue(transientWarningObservation, "LikelyStage") as string, "flashback warmup likely stage");

        var criticalSamples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Critical", "flashback_playback", "startup crash")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var criticalObservation = buildObservation.Invoke(
                null,
                new object?[] { criticalSamples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Critical observation was null.");
        AssertEqual("Critical", GetPropertyValue(criticalObservation, "HealthStatus") as string, "flashback critical health status");
        AssertEqual("flashback_playback", GetPropertyValue(criticalObservation, "LikelyStage") as string, "flashback critical likely stage");

        return Task.CompletedTask;

        static object CreateDiagnosticSessionSampleList(Type sampleType, params (long OffsetMs, JsonElement Snapshot)[] values)
        {
            var listType = typeof(List<>).MakeGenericType(sampleType);
            var list = (System.Collections.IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("DiagnosticSessionSample list could not be created."));
            foreach (var value in values)
            {
                var sample = Activator.CreateInstance(sampleType)
                    ?? throw new InvalidOperationException("DiagnosticSessionSample instance could not be created.");
                sampleType.GetProperty("OffsetMs")!.SetValue(sample, value.OffsetMs);
                sampleType.GetProperty("TimestampUtc")!.SetValue(sample, DateTimeOffset.UtcNow);
                sampleType.GetProperty("Snapshot")!.SetValue(sample, value.Snapshot);
                list.Add(sample);
            }

            return list;
        }

        static JsonElement CreateDiagnosticSnapshot(string health, string stage, string evidence)
        {
            using var document = JsonDocument.Parse($$"""
                {
                  "DiagnosticHealthStatus": "{{health}}",
                  "DiagnosticLikelyStage": "{{stage}}",
                  "DiagnosticEvidence": "{{evidence}}"
                }
                """);
            return document.RootElement.Clone();
        }
    }

    internal static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        var waitsText = ReadDiagnosticSessionFlashbackWaitsSource();

        AssertContains(waitsText, "internal static class DiagnosticSessionFlashbackWaits");
        AssertDoesNotContain(waitsText, "internal static partial class DiagnosticSessionFlashbackWaits");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(waitsText, "FlashbackPlaybackPendingCommands");
        AssertContains(waitsText, "FlashbackPlaybackFrameCount");
        AssertContains(waitsText, "RecordingBackend");
        AssertContains(waitsText, "RecordingFileGrowing");
        AssertContains(waitsText, "FlashbackBufferedDurationMs");
        AssertContains(waitsText, "requiredEncodedFrames");
        AssertContains(waitsText, "string expectedState");
        AssertContains(waitsText, "positionMs >= boundaryMs + 1_500");
        AssertContains(waitsText, "FlashbackPlaybackTargetFps");
        AssertContains(waitsText, "SelectedExactFrameRate");
        AssertContains(waitsText, "Math.Abs(position - targetPositionMs) <= 1_500");
        AssertContains(setupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();

        AssertContains(stressText, "internal static class DiagnosticSessionFlashbackStressScenario");
        AssertDoesNotContain(stressText, "internal static partial class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(stressText, "ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "private static async Task VerifyFlashbackStressExportAsync(");
        AssertContains(stressText, "\"flashback-stress-export.mp4\"");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "flashback stress export verified");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "\"flashback playback warmed frames=");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterLastFallbackReason");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(stressText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(stressText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(stressText, "private readonly record struct FlashbackStressPlaybackDrainResult(");
        AssertContains(stressText, "private static async Task<FlashbackStressPlaybackDrainResult> WaitForFlashbackStressPlaybackCommandDrainAsync(");
        AssertContains(stressText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0");
        AssertContains(stressText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(stressText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(stressText, "return positions[^1];");
        AssertContains(stressText, "flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
        AssertContains(stressText, "private static async Task ValidateFlashbackScrubStressDrainAsync(");
        AssertContains(stressText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(stressText, "FlashbackScrubStressMaxPlaybackPendingCommands");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(stressText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(stressText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressText, "1,\n                \"flashback-stress-task\",");
        AssertContains(stressText, "3,\n                \"flashback-scrub-stress-task\",");
        AssertContains(stressText, "RunFlashbackStressAsync(");
        AssertContains(stressText, "RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "sendRawWithConnectRetryAsync");
        AssertContains(stressText, "actions.Add(\"flashback stress started\")");
        AssertContains(stressText, "actions.Add(\"flashback scrub stress started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(runnerText, "private const int FlashbackStressMaxPlaybackPendingCommands = 4;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var stressScenarioType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackStressScenario")
            ?? throw new InvalidOperationException("DiagnosticSessionFlashbackStressScenario type was not found.");
        var classify = stressScenarioType.GetMethod(
                "ClassifyFlashbackStressAudioMasterFallbackWarning",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Audio-master fallback classifier was not found.");

        AssertEqual((string?)null, Invoke(0, 0, 0, 0), "no audio-master fallback warning");
        AssertEqual((string?)null, Invoke(4, 4, 0, 0), "startup unavailable fallback allowance");

        var unavailable = Invoke(5, 5, 0, 0)
            ?? throw new InvalidOperationException("Expected unavailable fallback warning.");
        AssertContains(unavailable, "audio-master unavailable fallbacks exceeded startup allowance");
        AssertContains(unavailable, "unavailableDelta=5");
        AssertContains(unavailable, "allowance=4");
        AssertContains(unavailable, "totalDelta=5");

        var stale = Invoke(2, 0, 1, 0)
            ?? throw new InvalidOperationException("Expected stale fallback warning.");
        AssertContains(stale, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(stale, "staleDelta=1");
        AssertContains(stale, "driftOutlierDelta=0");

        var driftOutlier = Invoke(2, 0, 0, 1)
            ?? throw new InvalidOperationException("Expected drift-outlier fallback warning.");
        AssertContains(driftOutlier, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(driftOutlier, "staleDelta=0");
        AssertContains(driftOutlier, "driftOutlierDelta=1");

        var unclassified = Invoke(2, 0, 0, 0)
            ?? throw new InvalidOperationException("Expected unclassified fallback warning.");
        AssertContains(unclassified, "audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(unclassified, "delta=2");

        return Task.CompletedTask;

        string? Invoke(long totalDelta, long unavailableDelta, long staleDelta, long driftOutlierDelta)
            => classify.Invoke(null, new object?[] { totalDelta, unavailableDelta, staleDelta, driftOutlierDelta }) as string;
    }

    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var rootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportText = rootText;
        var playbackText = rootText;
        var rangeText = rootText;
        var scenariosTextWithoutSpaces = scenariosText.Replace(" ", string.Empty);

        AssertContains(scenariosText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "flashback disable during export: pending playback commands remained after disable");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "flashback export playback: expected Playing before export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "flashback export playback: playback frame count did not advance during export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackText, "flashback export playback: pending commands remained after go-live");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(rangeText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(rangeText, "private readonly record struct FlashbackSelectionRange(");
        AssertContains(rangeText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(rangeText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(rangeText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(rootText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(rootText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "backgroundTasks.AddScenario(");
        AssertContains(rootText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "6,\n            \"flashback-export-playback-task\",");
        AssertContains(rootText, "flashback export playback started");
        AssertContains(rootText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "8,\n                \"flashback-range-export-task\",");
        AssertContains(rootText, "9,\n                \"flashback-range-export-audio-switch-task\",");
        AssertContains(rootText, "flashback range export audio switch started");
        AssertContains(rootText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "10,\n                \"flashback-export-concurrent-task\",");
        AssertContains(rootText, "11,\n                \"flashback-disable-during-export-task\",");
        AssertContains(rootText, "12,\n                \"flashback-rotated-export-task\",");
        AssertContains(rootText, "flashback rotated export started");
        AssertContains(scenariosTextWithoutSpaces, "6,\n\"flashback-export-playback-task\",");
        AssertContains(scenariosTextWithoutSpaces, "8,\n\"flashback-range-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "9,\n\"flashback-range-export-audio-switch-task\",");
        AssertContains(scenariosTextWithoutSpaces, "10,\n\"flashback-export-concurrent-task\",");
        AssertContains(scenariosTextWithoutSpaces, "11,\n\"flashback-disable-during-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "12,\n\"flashback-rotated-export-task\",");
        AssertContains(scenariosText, "\"flashback-range-export-task\"");
        AssertContains(scenariosText, "actions.Add(\"flashback concurrent export started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRangeExportAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var exportHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportHelpersText, "internal static class DiagnosticSessionFlashbackExports");
        AssertDoesNotContain(exportHelpersText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(exportHelpersText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportHelpersText, "const string marker = \" from \";");
        AssertContains(exportHelpersText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportHelpersText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportHelpersText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportHelpersText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportHelpersText, "\"clear-in-out-points\"");
        AssertContains(exportHelpersText, "\"go-live\"");
        AssertContains(exportHelpersText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportHelpersText, "\"SetAudioEnabled\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentsText = ReadDiagnosticSessionFlashbackSegmentsSource();

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertDoesNotContain(segmentsText, "internal static partial class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "sendCommandAsync(\"FlashbackGetSegments\", null, null)");
        AssertContains(segmentsText, "sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertContains(segmentPlaybackText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static bool TryGetFlashbackSegments(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(cyclesText, "FlashbackPlaybackThreadAlive");
        AssertContains(cyclesText, "pending playback commands remained after restart");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackRestartCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);");
        AssertContains(cyclesText, "private static void ValidateFlashbackEncoderCycleSnapshot(");
        AssertContains(cyclesText, "post-cycle encoder did not reach readiness frame count");
        AssertContains(cyclesText, "playback state not clean after preset cycle");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder cycle export verified");
        AssertContains(cyclesText, "private static async Task RestoreFlashbackEncoderCyclePresetAsync(");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(cyclesText, "Flashback buffer did not become ready after preset restore");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(cyclesText, "4,\n                \"flashback-restart-cycle-task\",");
        AssertContains(cyclesText, "5,\n                \"flashback-encoder-cycle-task\",");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var flashbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs")
            .Replace("\r\n", "\n");
        var playbackCycleText = flashbackCycleText;
        var recordingCycleText = flashbackCycleText;

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle: Flashback frames did not advance while preview was off");
        AssertContains(flashbackCycleText, "private static async Task ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "VideoFramesFlowing");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackCycleText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackCycleText, "private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(playbackCycleText, "private static async Task ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "VideoFramesFlowing");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackCycleText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(recordingCycleText, "CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertContains(recordingCycleText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "WaitForFlashbackRecordingReadyAsync(");
        AssertContains(recordingCycleText, "WaitForPreviewActiveAsync(");
        AssertContains(recordingCycleText, "private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "flashback recording preview cycle: recording counters did not advance while preview was off");
        AssertContains(recordingCycleText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "VideoFramesFlowing");
        AssertContains(recordingCycleText, "flashback recording preview cycle: preview frames did not resume");
        AssertDoesNotContain(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertContains(cyclesText, "13,\n                \"flashback-preview-cycle-task\",");
        AssertContains(cyclesText, "14,\n                \"flashback-playback-preview-cycle-task\",");
        AssertContains(cyclesText, "15,\n                \"flashback-recording-preview-cycle-task\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")),
            "Flashback playback preview-cycle scenario stays with the preview-cycle scenario family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs")),
            "Flashback recording preview-cycle scenario stays with the preview-cycle scenario family");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var rejectedExportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedExportsText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(rejectedExportsText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "BufferInactive");
        AssertContains(rejectedExportsText, "Flashback buffer not active");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "UnavailableDuringRecording");
        AssertContains(rejectedExportsText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedExportsText, "RunSelectedRejectedExportScenariosAsync");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackRecordingExportRejected");
        AssertOccursBefore(dispatchText, "RunFlashbackExportRejectedAsync(", "RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(runnerText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackRejectedExports.cs")),
            "Flashback rejected-export scenarios stay folded into the export scenario owner");
        AssertDoesNotContain(runnerText, "DiagnosticSessionFlashbackRejectedExports.");
        AssertDoesNotContain(runnerText, "RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "RunFlashbackRecordingExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();

        AssertContains(segmentPlaybackText, "internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertDoesNotContain(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentPlaybackText, "no playable completed segment became available after recording-assisted rotation");
        AssertContains(segmentPlaybackText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackText, "private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(segmentPlaybackText, "\"go-live\"");
        AssertContains(segmentPlaybackText, "flashback segment playback go-live requested");
        AssertContains(segmentPlaybackText, "flashback segment playback: playback ended in state");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(segmentPlaybackText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackText, "scenarioPlan.RunFlashbackSegmentPlayback");
        AssertContains(segmentPlaybackText, "7,\n            \"flashback-segment-playback-task\",");
        AssertContains(segmentPlaybackText, "actions.Add(\"flashback segment playback started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingSettingsText = ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource();

        AssertContains(recordingSettingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(recordingSettingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertDoesNotContain(recordingSettingsText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(recordingSettingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset changed to");
        AssertContains(recordingSettingsText, "VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(recordingSettingsText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "Flashback recording backend did not remain active after mutations");
        AssertContains(recordingSettingsText, "recording counters did not advance after mutation attempts");
        AssertContains(recordingSettingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(recordingSettingsText, "private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertContains(recordingSettingsText, "\"SetPreset\"");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset restored to");
        AssertContains(recordingSettingsText, "selected preset was not restored");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadDiagnosticSessionFlashbackLifecycleScenariosSource();

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();
        var recordingText = metricsText;
        var playbackSessionText = metricsText;
        var playbackObservationText = metricsText;
        var playbackResultText = metricsText;
        var exportText = metricsText;

        AssertContains(metricsText, "internal static class DiagnosticSessionFlashbackMetrics");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.cs")), "Flashback metrics stay folded into DiagnosticSessionMetrics.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Recording.cs")), "Flashback recording metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Export.cs")), "Flashback export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs")), "Flashback playback observation metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.RecordingExport.cs")), "Flashback recording/export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackSession.cs")), "Flashback playback session metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackResult.cs")), "Flashback playback result metrics stay folded into the consolidated metrics owner");
        AssertContains(recordingText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(playbackSessionText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(playbackResultText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(exportText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(playbackSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(playbackSessionText, "public int MaxCommandQueueLatencyMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public double MaxSlowFramePercentObserved { get; set; }");
        AssertContains(playbackSessionText, "public long MinOnePercentLowAudioMasterFallbacks { get; set; }");
        AssertContains(playbackSessionText, "public string MaxDecodePhaseObserved { get; set; } = string.Empty;");
        AssertContains(playbackSessionText, "public double MaxAbsAvDriftMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public long SubmitFailuresDelta { get; set; }");
        AssertContains(playbackResultText, "public JsonElement EndSnapshot { get; init; }");
        AssertContains(playbackResultText, "public int PendingCommandsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public double OnePercentLowFpsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;");
        AssertContains(playbackResultText, "public long AudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(playbackResultText, "public long SeekForwardDecodeCapHitsDelta { get; init; }");
        AssertContains(exportText, "public long ForceRotateFallbacksAtEnd { get; set; }");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(playbackSessionText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(playbackObservationText, "private static void ObservePlaybackSnapshot(");
        AssertContains(playbackObservationText, "var relevance = BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private readonly record struct FlashbackPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(playbackObservationText, "GetInt(snapshot, \"FlashbackPlaybackPendingCommands\") > 0");
        AssertContains(playbackObservationText, "ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "ObservePlaybackAudioMasterMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackObservationText, "metrics.MaxDecodePhaseObserved = GetString(snapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(playbackObservationText, "GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(playbackResultText, "var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);");
        AssertContains(playbackResultText, "PendingCommandsAtEnd = commands.PendingCommandsAtEnd,");
        AssertContains(playbackResultText, "private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "PendingCommandsAtEnd: observed ? GetInt(endSnapshot, \"FlashbackPlaybackPendingCommands\") : 0");
        AssertContains(playbackResultText, "LastCommandFailureAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackLastCommandFailure\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackDroppedFrames\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(");
        AssertContains(playbackResultText, "GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackSeekForwardDecodeCapHits\")");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultStageMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, \"FlashbackExportForceRotateFallbacks\") ?? 0;");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksDelta = GetCounterDelta(");
        AssertContains(metricsText, "metrics.LastForceRotateFallbackSegmentsAtEnd =");
        AssertContains(exportText, "private static void ObserveExportSnapshot(");
        AssertContains(exportText, "var relevantToSession =");
        AssertContains(exportText, "metrics.MaxThroughputBytesPerSecObserved = Math.Max(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var metricsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackMetrics")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionFlashbackMetrics was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionSample was not found.");
        var buildMetrics = metricsType.GetMethod(
            "BuildFlashbackExportSessionMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics was not found.");

        using var initialDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 1,
              "FlashbackExportLastForceRotateFallbackSegments": 0
            }
            """);
        using var lastDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 3,
              "FlashbackExportLastForceRotateFallbackSegments": 2
            }
            """);

        var samples = Array.CreateInstance(sampleType, 0);
        var metrics = buildMetrics.Invoke(
            null,
            new object?[] { initialDocument.RootElement, samples, lastDocument.RootElement })
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics returned null.");

        AssertEqual(false, (bool)GetPropertyValue(metrics, "Observed")!, "Non-relevant export remains unobserved");
        AssertEqual(3L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksAtEnd")), "ForceRotateFallbacksAtEnd");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksDelta")), "ForceRotateFallbacksDelta");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "LastForceRotateFallbackSegmentsAtEnd")), "LastForceRotateFallbackSegmentsAtEnd");

        return Task.CompletedTask;
    }
}
