using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cycleScenariosText = ReadDiagnosticSessionFlashbackCycleScenariosSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressScenariosText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var segmentPlaybackScenariosText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var previewCycleScenariosText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var presentMonStartupText = ReadRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
            .Replace("\r\n", "\n");
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
        AssertContains(startupText, "DiagnosticSessionPresentMonStartup.StartAsync(");
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

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch()
    {
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var presentMonStartupText = ReadRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
            .Replace("\r\n", "\n");

        AssertContains(presentMonStartupText, "internal static class DiagnosticSessionPresentMonStartup");
        AssertContains(presentMonStartupText, "internal static async Task StartAsync(");
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
        AssertContains(startupText, "DiagnosticSessionPresentMonStartup.StartAsync(");
        AssertDoesNotContain(startupText, "PresentMonProbe.RunAsync(new PresentMonProbeOptions");
        AssertDoesNotContain(startupText, "PreviewD3DSwapChainAddress");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionSampler_OwnsSampleLoopOrdering()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            .Replace("\r\n", "\n");

        AssertContains(scenarioText, "private static async Task SampleLoopAsync(");
        AssertContains(scenarioText, "var response = await sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(scenarioText, "samples.Add(new DiagnosticSessionSample");
        AssertContains(scenarioText, "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertOccursBefore(scenarioText, "samples.Add(new DiagnosticSessionSample", "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertDoesNotContain(executionText, "private static async Task SampleLoopAsync(");

        return Task.CompletedTask;
    }
}
