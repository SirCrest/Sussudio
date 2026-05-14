using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
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

    private static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadDiagnosticSessionModelsSource();

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed class DiagnosticSessionResult");
        AssertContains(modelText, "public sealed class DiagnosticSessionSample");
        AssertContains(modelText, "public string TerminalState { get; set; }");
        AssertContains(modelText, "public JsonElement Snapshot { get; init; }");
        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionOptions");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionSample");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(initialSnapshotText, "internal static class DiagnosticSessionInitialSnapshot");
        AssertContains(initialSnapshotText, "internal static DiagnosticSessionInitialSnapshotResult CreateUnknown()");
        AssertContains(initialSnapshotText, "internal static async Task<DiagnosticSessionInitialSnapshotResult> CaptureAsync(");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknown();");
        AssertContains(initialSnapshotText, "setStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "commandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "recordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await writeLiveStateAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(runnerText, "var initialSnapshotResult = DiagnosticSessionInitialSnapshot.CreateUnknown();");
        AssertContains(runnerText, "DiagnosticSessionInitialSnapshot.CaptureAsync(");
        AssertContains(runnerText, "initialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(runnerText, "initialSnapshotKnown = initialSnapshotResult.Known;");
        AssertDoesNotContain(runnerText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(runnerText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(runnerText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(runnerText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();

        AssertContains(formatterText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "private static void AppendOverview(");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterText, "private static string FormatFrameRate(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(builderText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "var result = new DiagnosticSessionResult");
        AssertContains(builderText, "var artifactPaths = await WritePreSummaryAsync(");
        AssertContains(builderText, "SummaryPath = artifactPaths.SummaryPath");
        AssertContains(builderText, "SamplesPath = artifactPaths.SamplesPath");
        AssertContains(builderText, "FrameLedgerPath = artifactPaths.FrameLedgerPath");
        AssertContains(builderText, "TimelinePath = artifactPaths.TimelinePath");
        AssertContains(builderText, "runState.SetStage(\"summary\")");
        AssertContains(builderText, "return await WriteAsync(result, runState, warnings).ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(runnerText, "new DiagnosticSessionResultBuildRequest(");
        AssertDoesNotContain(runnerText, "SetStage(\"result-analysis\")");
        AssertDoesNotContain(runnerText, "var result = new DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "WriteArtifactBestEffortAsync(\"write-samples\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"summary-write\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionSummaryWriter_OwnsSummaryWriteFailures()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var writerText = ReadRepoFile("tools/Common/DiagnosticSessionSummaryWriter.cs")
            .Replace("\r\n", "\n");

        AssertContains(writerText, "internal static class DiagnosticSessionSummaryWriter");
        AssertContains(writerText, "internal static async Task<DiagnosticSessionResult> WriteAsync(");
        AssertContains(writerText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
        AssertContains(writerText, "runState.RecordTerminalException(ex, \"summary-write\")");
        AssertContains(writerText, "result.Success = false;");
        AssertContains(writerText, "result.CompletedUtc = DateTimeOffset.UtcNow;");
        AssertContains(writerText, "result.TerminalState = runState.GetTerminalState();");
        AssertContains(writerText, "result.LastStage = runState.GetResultLastStage();");
        AssertContains(writerText, "result.Warnings = warnings.ToArray();");
        AssertContains(writerText, "runState.SetStage(\"summary-written\")");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionSummaryWriter;");
        AssertContains(builderText, "WriteAsync(result, runState, warnings)");
        AssertDoesNotContain(builderText, "RecordTerminalException(ex, \"summary-write\")");
        AssertDoesNotContain(builderText, "SetStage(\"summary-written\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultArtifacts_OwnPreSummaryWrites()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionResultArtifacts");
        AssertContains(artifactsText, "internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(");
        AssertContains(artifactsText, "internal readonly record struct DiagnosticSessionResultArtifactPaths(");
        AssertContains(artifactsText, "SummaryPath: Path.Combine(outputDirectory, \"summary.json\")");
        AssertContains(artifactsText, "SamplesPath: Path.Combine(outputDirectory, \"samples.json\")");
        AssertContains(artifactsText, "FrameLedgerPath: Path.Combine(outputDirectory, \"frame-ledger.json\")");
        AssertContains(artifactsText, "TimelinePath: Path.Combine(outputDirectory, \"timeline.json\")");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-frame-ledger\", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples))");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-timeline\", paths.TimelinePath, timeline)");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionResultArtifacts;");
        AssertContains(builderText, "WritePreSummaryAsync(");
        AssertDoesNotContain(builderText, "Path.Combine(request.OutputDirectory, \"samples.json\")");
        AssertDoesNotContain(builderText, "BuildFrameLedgerTrace(request.SessionId, samples)");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionText_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");
        var textHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionText.cs")
            .Replace("\r\n", "\n");

        AssertContains(textHelpersText, "internal static class DiagnosticSessionText");
        AssertContains(textHelpersText, "internal static string FormatOptional(string value)");
        AssertContains(textHelpersText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(formatterText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var retryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");

        AssertContains(channelText, "internal sealed class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(channelText, "\"WaitForCondition\"");
        AssertContains(channelText, "[\"pollMs\"] = 250");
        AssertContains(runnerText, "using var commandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(runnerText, "commandChannel.SendAsync");
        AssertContains(runnerText, "commandChannel.SendWithTokenAsync");
        AssertContains(runnerText, "commandChannel.FailureCount");
        AssertDoesNotContain(runnerText, "var commandFailureCount = 0;");
        AssertDoesNotContain(runnerText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(runnerText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(runnerText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(artifactsText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(artifactsText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(artifactsText, "internal static object BuildFrameLedgerTrace(");
        AssertContains(artifactsText, "internal static bool TryGetSnapshot(");
        AssertContains(artifactsText, "internal static bool TryGetVerification(");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(runnerText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(runnerText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunState_OwnsTerminalAndLiveState()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var stateText = ReadRepoFile("tools/Common/DiagnosticSessionRunState.cs")
            .Replace("\r\n", "\n");

        AssertContains(stateText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(stateText, "internal string LivePath { get; }");
        AssertContains(stateText, "internal void SetStage(string stage)");
        AssertContains(stateText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(stateText, "internal string GetTerminalState()");
        AssertContains(stateText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(stateText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(stateText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertContains(stateText, "The live-state file is diagnostic breadcrumbs only.");
        AssertContains(runnerText, "var runState = new DiagnosticSessionRunState(");
        AssertContains(runnerText, "var livePath = runState.LivePath;");
        AssertDoesNotContain(runnerText, "var lastStage = \"initializing\";");
        AssertDoesNotContain(runnerText, "Exception? terminalException = null;");
        AssertDoesNotContain(runnerText, "DateTimeOffset.MinValue");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var lockText = ReadRepoFile("tools/Common/DiagnosticSessionOutputLock.cs")
            .Replace("\r\n", "\n");

        AssertContains(lockText, "internal static class DiagnosticSessionOutputLock");
        AssertContains(lockText, "internal static FileStream Acquire(string outputDirectory)");
        AssertContains(lockText, "\".sussudio-diag.lock\"");
        AssertContains(lockText, "FileShare.None");
        AssertContains(lockText, "FileOptions.DeleteOnClose");
        AssertContains(lockText, "Another diagnostic session is already running");
        AssertContains(runnerText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);");
        AssertDoesNotContain(runnerText, "sessionLock.Dispose();");
        AssertDoesNotContain(runnerText, "var lockPath = Path.Combine(outputDirectory, \".sussudio-diag.lock\")");
        AssertDoesNotContain(runnerText, "FileShare.None");
        AssertDoesNotContain(runnerText, "FileOptions.DeleteOnClose");

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
