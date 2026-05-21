using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
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
        var tasksRootText = ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.cs")
            .Replace("\r\n", "\n");
        var presentMonTasksText = ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.PresentMon.cs")
            .Replace("\r\n", "\n");
        var recordingSettingsTasksText = ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.RecordingSettingsDeferred.cs")
            .Replace("\r\n", "\n");
        var faultDrainText = ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.FaultDrain.cs")
            .Replace("\r\n", "\n");

        AssertContains(startupText, "internal static partial class DiagnosticSessionScenarioStartup");
        AssertContains(startupText, "internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(");
        AssertContains(startupText, "internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback)");
        AssertContains(tasksText, "internal sealed partial class DiagnosticSessionBackgroundTasks");
        AssertContains(tasksRootText, "internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(");
        AssertContains(tasksRootText, "private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];");
        AssertContains(tasksRootText, "internal void AddScenario(int awaitOrder, string stage, Task task)");
        AssertContains(tasksRootText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(tasksRootText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(tasksRootText, "_scenarioTasks.OrderBy(task => task.AwaitOrder)");
        AssertContains(presentMonTasksText, "private Task<PresentMonProbeResult>? _presentMonTask;");
        AssertContains(presentMonTasksText, "internal void SetPresentMon(Task<PresentMonProbeResult> task)");
        AssertContains(presentMonTasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(presentMonTasksText, "private async Task<PresentMonProbeResult?> AwaitPresentMonAsync(");
        AssertContains(presentMonTasksText, "private async Task<PresentMonProbeResult?> ObservePresentMonAfterFaultAsync(");
        AssertContains(presentMonTasksText, "presentmon-task: task still running after diagnostic interruption");
        AssertContains(recordingSettingsTasksText, "private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;");
        AssertContains(recordingSettingsTasksText, "internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)");
        AssertContains(recordingSettingsTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(recordingSettingsTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(recordingSettingsTasksText, "flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
        AssertContains(faultDrainText, "internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(");
        AssertContains(faultDrainText, "internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(");
        AssertContains(faultDrainText, "ObservePresentMonAfterFaultAsync(");
        AssertContains(faultDrainText, "ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(faultDrainText, "private static async Task ObserveTaskAfterFaultAsync(");
        AssertDoesNotContain(tasksRootText, "private Task<PresentMonProbeResult>? _presentMonTask;");
        AssertDoesNotContain(tasksRootText, "private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;");
        AssertDoesNotContain(tasksRootText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertDoesNotContain(tasksRootText, "internal void SetPresentMon(");
        AssertDoesNotContain(tasksRootText, "internal void SetRecordingSettingsDeferred(");
        AssertDoesNotContain(tasksRootText, "ObserveAfterFaultAsync(");
        AssertDoesNotContain(tasksRootText, "internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(");
        AssertDoesNotContain(presentMonTasksText, "SetRecordingSettingsDeferred(");
        AssertDoesNotContain(recordingSettingsTasksText, "SetPresentMon(");
        AssertDoesNotContain(faultDrainText, "internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(");
        AssertDoesNotContain(faultDrainText, "presentmon-task: task still running after diagnostic interruption");
        AssertDoesNotContain(faultDrainText, "flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
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
}
