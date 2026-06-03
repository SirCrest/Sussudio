using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;

namespace Sussudio.Tools;

public static class DiagnosticSessionRunner
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarioCatalog.
    // RunAsync reads like a phase plan: scenario execution, cleanup,
    // verification, post-run snapshots, then summary.
    public static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);
        using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);

        var stoppedRecordingForVerification = false;
        var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;

        await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);
        var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);

        try
        {
            scenarioPhase = await DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext).ConfigureAwait(false);
        }
        finally
        {
            var cleanupResult = await DiagnosticSessionCleanupActions.RunAsync(
                    options,
                    runContext.InitialSnapshot,
                    scenarioPhase.StartedRecording,
                    scenarioPhase.StartedPreview,
                    scenarioPhase.EnabledFlashback,
                    scenarioPhase.DisabledFlashback,
                    scenarioPhase.StartedFlashbackPlayback,
                    runContext.Actions,
                    runContext.CommandChannel,
                    runContext.CommandChannel.TryWaitWithTokenAsync,
                    runContext.SetStage,
                    runContext.RecordTerminalException)
                .ConfigureAwait(false);
            stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;

            await runContext.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return await RunCompletionPhaseAsync(
                runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken))
            .ConfigureAwait(false);
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

    private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)
    {
        var recordingCheckResult = await DiagnosticSessionRecordingChecks.RunAsync(
                context.Options,
                context.RunBootstrap.ScenarioPlan,
                context.RunBootstrap.Scenario,
                context.RunBootstrap.OutputDirectory,
                context.InitialSnapshot,
                context.Samples,
                context.ScenarioPhase.StartedRecording,
                context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState,
                context.Actions,
                context.Warnings,
                context.CommandChannel.SendAsync,
                context.SetStage,
                context.RecordTerminalException,
                context.RunCancellationToken)
            .ConfigureAwait(false);
        var verification = recordingCheckResult.Verification;

        var postRunSnapshots = await CapturePostRunSnapshotsAsync(
                context.Samples,
                context.InitialSnapshot,
                context.CommandChannel.SendAsync,
                context.SetStage,
                context.RecordTerminalException)
            .ConfigureAwait(false);

        var result = await DiagnosticSessionResultBuilder.BuildAndWriteAsync(
                CreateResultBuildRequest(
                    context.Options,
                    context.RunBootstrap,
                    context.LivePath,
                    context.CommandChannel.FailureCount,
                    context.Samples,
                    context.InitialSnapshot,
                    postRunSnapshots,
                    verification,
                    context.ScenarioPhase.PresentMon,
                    context.ScenarioPhase.StartedPreview,
                    context.ScenarioPhase.EnabledFlashback,
                    context.ScenarioPhase.StartedFlashbackPlayback,
                    context.StoppedRecordingForVerification,
                    context.Actions,
                    context.Warnings),
                context.RunState)
            .ConfigureAwait(false);

        await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);
        return result;
    }

    private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(
        DiagnosticSessionOptions options,
        DiagnosticSessionRunBootstrap runBootstrap,
        string livePath,
        int commandFailureCount,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        DiagnosticSessionPostRunSnapshotResult postRunSnapshots,
        JsonElement? verification,
        PresentMonProbeResult? presentMon,
        bool startedPreview,
        bool enabledFlashback,
        bool startedFlashbackPlayback,
        bool stoppedRecordingForVerification,
        IReadOnlyList<string> actions,
        List<string> warnings)
    {
        return new DiagnosticSessionResultBuildRequest(
            options,
            runBootstrap.ScenarioPlan,
            runBootstrap.SessionId,
            runBootstrap.Scenario,
            runBootstrap.DurationSeconds,
            runBootstrap.SampleIntervalMs,
            runBootstrap.OutputDirectory,
            livePath,
            runBootstrap.StartedUtc,
            runBootstrap.RunnerProcessId,
            commandFailureCount,
            samples,
            initialSnapshot,
            postRunSnapshots.HealthSnapshot,
            postRunSnapshots.Timeline,
            verification,
            presentMon,
            startedPreview,
            enabledFlashback,
            startedFlashbackPlayback,
            stoppedRecordingForVerification,
            actions,
            warnings);
    }

    private static async Task<DiagnosticSessionPostRunSnapshotResult> CapturePostRunSnapshotsAsync(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        JsonElement? timeline = null;
        try
        {
            setStage("timeline");
            var timelineResponse = await sendAsync(
                    "GetPerformanceTimeline",
                    new Dictionary<string, object?> { ["maxEntries"] = 240 },
                    null)
                .ConfigureAwait(false);
            if (timelineResponse.TryGetProperty("Data", out var timelineData))
            {
                timeline = timelineData.Clone();
            }
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "timeline");
        }

        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = lastSnapshot;
        try
        {
            setStage("final-snapshot");
            var finalSnapshotResponse = await sendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            healthSnapshot = DiagnosticSessionAutomationResponseJson.TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)
                ? finalSnapshot
                : lastSnapshot;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "final-snapshot");
        }

        return new DiagnosticSessionPostRunSnapshotResult(healthSnapshot, timeline);
    }

    private static FileStream AcquireOutputLock(string outputDirectory)
    {
        // Per-output-directory exclusive lock. Prevents two concurrent diagnostic-session
        // invocations from corrupting the manifest, final.snapshot.json, and per-scenario
        // JSON files in the same OutputDirectory. FileShare.None blocks other openers;
        // DeleteOnClose self-cleans on normal exit, and the OS releases the handle on crash.
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");
        try
        {
            return new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Another diagnostic session is already running in '{outputDirectory}'. " +
                $"Wait for it to finish or choose a different output directory. ({ex.Message})",
                ex);
        }
    }
}

internal sealed class DiagnosticSessionCompletionContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required DiagnosticSessionRunBootstrap RunBootstrap { get; init; }

    internal required string LivePath { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required IReadOnlyList<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionScenarioPhaseResult ScenarioPhase { get; init; }

    internal required bool StoppedRecordingForVerification { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required DiagnosticSessionRunState RunState { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Func<DateTimeOffset?, string?, Task> WriteLiveStateBestEffortAsync { get; init; }
}

internal readonly record struct DiagnosticSessionPostRunSnapshotResult(
    JsonElement HealthSnapshot,
    JsonElement? Timeline);

internal static class DiagnosticSessionScenarioPhaseRunner
{
    internal static async Task<DiagnosticSessionScenarioPhaseResult> RunAsync(DiagnosticSessionScenarioPhaseContext context)
    {
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();
        var scenarioPhase = new DiagnosticSessionScenarioPhaseState();

        try
        {
            context.SetStage("scenario-setup");
            if (!context.InitialSnapshotKnown && context.Scenario != DiagnosticSessionScenarioCatalog.Observe)
            {
                context.CommandChannel.RecordFailure($"initial-snapshot: skipped state-mutating scenario '{context.Scenario}' because the initial app state is unknown");
            }
            else
            {
                var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                        context.Scenario,
                        context.ScenarioPlan,
                        context.InitialSnapshot,
                        context.Actions,
                        context.Warnings,
                        context.CommandChannel,
                        context.CommandChannel.TryWaitAsync,
                        context.ScenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedPreview = setupResult.StartedPreview;
                scenarioPhase.StartedRecording = setupResult.StartedRecording;
                scenarioPhase.EnabledFlashback = setupResult.EnabledFlashback;
                scenarioPhase.DisabledFlashback = setupResult.DisabledFlashback;

                var scenarioStartup = await DiagnosticSessionScenarioStartup.StartAsync(
                        context.Options,
                        context.ScenarioPlan,
                        context.DurationSeconds,
                        context.OutputDirectory,
                        backgroundTasks,
                        context.Actions,
                        context.Warnings,
                        context.CommandChannel.SendAsync,
                        context.CommandChannel.SendRawWithConnectRetryAsync,
                        context.CommandChannel.SendAsync,
                        context.ScenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

                await RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            context.RecordTerminalException(ex, context.GetLastStage());
            context.ScenarioCancellationSource.Cancel();
            await DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase).ConfigureAwait(false);
            await context.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return scenarioPhase.ToResult();
    }

    private static async Task RunSamplingAndCompleteAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        context.SetStage("sampling");
        await context.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        await SampleLoopAsync(
                context.DurationSeconds,
                context.SampleIntervalMs,
                context.Samples,
                context.CommandChannel.SendAsync,
                context.ScenarioCancellationToken,
                context.WriteSamplingLiveStateBestEffortAsync)
            .ConfigureAwait(false);

        await CompleteAfterSamplingAsync(
                context,
                backgroundTasks,
                scenarioPhase)
            .ConfigureAwait(false);
    }

    private static async Task CompleteAfterSamplingAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = await backgroundTasks
            .CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
            .ConfigureAwait(false);

        await DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(
                context.ScenarioPlan,
                context.OutputDirectory,
                context.Actions,
                context.Warnings,
                context.CommandChannel.SendAsync,
                context.RunCancellationToken)
            .ConfigureAwait(false);

        scenarioPhase.PresentMon = await backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings).ConfigureAwait(false);
    }

    private static async Task DrainAfterFaultAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        var backgroundTaskDrain = await backgroundTasks.ObserveAfterFaultAsync(
                context.Warnings,
                context.SetStage,
                context.RecordTerminalException,
                context.WriteLiveStateBestEffortAsync,
                scenarioPhase.PresentMon,
                scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
            .ConfigureAwait(false);
        scenarioPhase.PresentMon = backgroundTaskDrain.PresentMon;
        scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = backgroundTaskDrain.RecordingSettingsDeferredPresetState;
    }

    private static async Task SampleLoopAsync(
        int durationSeconds,
        int sampleIntervalMs,
        List<DiagnosticSessionSample> samples,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        Func<Task>? sampleCheckpointAsync = null)
    {
        var started = Stopwatch.GetTimestamp();
        var duration = TimeSpan.FromSeconds(durationSeconds);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                samples.Add(new DiagnosticSessionSample
                {
                    OffsetMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Snapshot = snapshot.Clone()
                });
                if (sampleCheckpointAsync is not null)
                {
                    await sampleCheckpointAsync().ConfigureAwait(false);
                }
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= duration)
            {
                break;
            }

            var remaining = duration - elapsed;
            var delay = TimeSpan.FromMilliseconds(Math.Min(sampleIntervalMs, Math.Max(1, remaining.TotalMilliseconds)));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(
    int AwaitOrder,
    string Stage,
    Task Task);

internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState RecordingSettingsDeferredPresetState);

internal sealed class DiagnosticSessionBackgroundTasks
{
    private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];
    private Task<PresentMonProbeResult>? _presentMonTask;
    private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;

    internal void AddScenario(int awaitOrder, string stage, Task task)
    {
        _scenarioTasks.Add(new DiagnosticSessionBackgroundTaskRegistration(awaitOrder, stage, task));
    }

    internal void SetPresentMon(Task<PresentMonProbeResult> task)
    {
        _presentMonTask = task;
    }

    internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)
    {
        _recordingSettingsDeferredTask = task;
    }

    internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        await AwaitScenarioTasksAsync().ConfigureAwait(false);
        return await AwaitRecordingSettingsDeferredAsync(recordingSettingsDeferredPresetState).ConfigureAwait(false);
    }

    internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(
        PresentMonProbeResult? presentMon,
        List<string> warnings)
    {
        return await AwaitPresentMonAsync(presentMon, warnings).ConfigureAwait(false);
    }

    internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(
        List<string> warnings,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException,
        Func<Task> writeLiveStateBestEffortAsync,
        PresentMonProbeResult? presentMon,
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        setStage("background-task-drain");
        foreach (var registration in _scenarioTasks.OrderBy(task => task.AwaitOrder))
        {
            await ObserveTaskAfterFaultAsync(
                    registration.Task,
                    registration.Stage,
                    warnings,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        presentMon = await ObservePresentMonAfterFaultAsync(
                warnings,
                recordTerminalException,
                presentMon)
            .ConfigureAwait(false);
        recordingSettingsDeferredPresetState = await ObserveRecordingSettingsDeferredAfterFaultAsync(
                warnings,
                recordTerminalException,
                recordingSettingsDeferredPresetState)
            .ConfigureAwait(false);
        await writeLiveStateBestEffortAsync().ConfigureAwait(false);

        return new DiagnosticSessionBackgroundTaskDrainResult(presentMon, recordingSettingsDeferredPresetState);
    }

    private async Task AwaitScenarioTasksAsync()
    {
        foreach (var registration in _scenarioTasks.OrderBy(task => task.AwaitOrder))
        {
            await registration.Task.ConfigureAwait(false);
        }
    }

    private async Task<PresentMonProbeResult?> AwaitPresentMonAsync(
        PresentMonProbeResult? current,
        List<string> warnings)
    {
        if (_presentMonTask is null)
        {
            return current;
        }

        var result = await _presentMonTask.ConfigureAwait(false);
        if (!result.Success)
        {
            warnings.Add($"PresentMon failed: {result.Message}");
        }

        return result;
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(
        FlashbackRecordingSettingsDeferredPresetState current)
    {
        return _recordingSettingsDeferredTask is null
            ? current
            : await _recordingSettingsDeferredTask.ConfigureAwait(false);
    }

    private async Task<PresentMonProbeResult?> ObservePresentMonAfterFaultAsync(
        List<string> warnings,
        Action<Exception, string> recordTerminalException,
        PresentMonProbeResult? presentMon)
    {
        if (_presentMonTask is null || _presentMonTask.IsCompletedSuccessfully)
        {
            if (_presentMonTask is { IsCompletedSuccessfully: true } && presentMon is null)
            {
                presentMon = await _presentMonTask.ConfigureAwait(false);
            }

            return presentMon;
        }

        try
        {
            var completedTask = _presentMonTask.IsCompleted
                ? _presentMonTask
                : await Task.WhenAny(_presentMonTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _presentMonTask))
            {
                warnings.Add("presentmon-task: task still running after diagnostic interruption");
                return presentMon;
            }

            presentMon = await _presentMonTask.ConfigureAwait(false);
            if (!presentMon.Success)
            {
                warnings.Add($"PresentMon failed: {presentMon.Message}");
            }

            return presentMon;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "presentmon-task");
            return presentMon;
        }
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredAfterFaultAsync(
        List<string> warnings,
        Action<Exception, string> recordTerminalException,
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        if (_recordingSettingsDeferredTask is null || _recordingSettingsDeferredTask.IsCompletedSuccessfully)
        {
            if (_recordingSettingsDeferredTask is { IsCompletedSuccessfully: true })
            {
                recordingSettingsDeferredPresetState = await _recordingSettingsDeferredTask.ConfigureAwait(false);
            }

            return recordingSettingsDeferredPresetState;
        }

        try
        {
            var completedTask = _recordingSettingsDeferredTask.IsCompleted
                ? _recordingSettingsDeferredTask
                : await Task.WhenAny(_recordingSettingsDeferredTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _recordingSettingsDeferredTask))
            {
                warnings.Add("flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
                return recordingSettingsDeferredPresetState;
            }

            return await _recordingSettingsDeferredTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "flashback-recording-settings-deferred-task");
            return recordingSettingsDeferredPresetState;
        }
    }

    private static async Task ObserveTaskAfterFaultAsync(
        Task? task,
        string stage,
        List<string> warnings,
        Action<Exception, string> recordTerminalException)
    {
        if (task is null || task.IsCompletedSuccessfully)
        {
            return;
        }

        try
        {
            var completedTask = task.IsCompleted
                ? task
                : await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, task))
            {
                warnings.Add($"{stage}: task still running after diagnostic interruption");
                return;
            }

            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, stage);
        }
    }
}

internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification);

internal static class DiagnosticSessionCleanupActions
{
    internal static async Task<DiagnosticSessionCleanupResult> RunAsync(
        DiagnosticSessionOptions options,
        JsonElement initialSnapshot,
        bool startedRecording,
        bool startedPreview,
        bool enabledFlashback,
        bool disabledFlashback,
        bool startedFlashbackPlayback,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(
                options,
                startedRecording,
                actions,
                commandChannel,
                tryWaitWithTokenAsync,
                setStage,
                recordTerminalException)
            .ConfigureAwait(false);

        if (!options.LeaveRunning)
        {
            await RestoreLiveFlashbackPlaybackAsync(
                    startedFlashbackPlayback,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await StopPreviewIfStartedAsync(
                    startedPreview,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await RestoreFlashbackEnabledStateAsync(
                    enabledFlashback,
                    disabledFlashback,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        return new DiagnosticSessionCleanupResult(stoppedRecordingForVerification);
    }

    private static CancellationTokenSource CreateCleanupCts(TimeSpan timeout)
        => new(timeout);

    private static async Task<bool> StopRecordingForCleanupAsync(
        DiagnosticSessionOptions options,
        bool startedRecording,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;
        if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))
        {
            return false;
        }

        try
        {
            setStage("cleanup-stop-recording");
            const int recordingCleanupTimeoutMs = 300_000;
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs));
            var stopResponse = await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetRecordingEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    recordingCleanupTimeoutMs,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add(shouldStopRecordingForVerification && options.LeaveRunning
                ? "recording stopped for verification"
                : "recording stopped");
            var stoppedRecordingForVerification = shouldStopRecordingForVerification &&
                                                  IsSuccess(stopResponse);
            if (IsSuccess(stopResponse))
            {
                await tryWaitWithTokenAsync("RecordingStopped", recordingCleanupTimeoutMs, cleanupCts.Token)
                    .ConfigureAwait(false);
            }

            return stoppedRecordingForVerification;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-recording");
            return false;
        }
    }

    private static async Task RestoreLiveFlashbackPlaybackAsync(
        bool startedFlashbackPlayback,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedFlashbackPlayback)
        {
            return;
        }

        try
        {
            setStage("cleanup-go-live");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "go-live" },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("flashback playback returned live");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-go-live");
        }
    }

    private static async Task StopPreviewIfStartedAsync(
        bool startedPreview,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedPreview || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return;
        }

        try
        {
            setStage("cleanup-stop-preview");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetPreviewEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("preview stopped");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-preview");
        }
    }

    private static async Task RestoreFlashbackEnabledStateAsync(
        bool enabledFlashback,
        bool disabledFlashback,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-off");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = false },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored off");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-off");
            }
        }

        if (disabledFlashback && GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-on");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = true },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored on");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-on");
            }
        }
    }
}

internal static class DiagnosticSessionRecordingChecks
{
    internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPlan scenarioPlan,
        string scenario,
        string outputDirectory,
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        bool startedRecording,
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException,
        CancellationToken cancellationToken)
    {
        var verification = default(JsonElement?);

        if (scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            try
            {
                setStage("settings-deferred-restore");
                await VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
                        actions,
                        warnings,
                        flashbackRecordingSettingsDeferredPresetState,
                        sendAsync,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "settings-deferred-restore");
            }
        }

        verification = await RunRecordingVerificationAsync(
                options,
                scenario,
                outputDirectory,
                startedRecording,
                actions,
                warnings,
                sendAsync,
                setStage,
                recordTerminalException)
            .ConfigureAwait(false);

        if (scenarioPlan.RequiresFlashbackRecordingValidation)
        {
            try
            {
                setStage("recording-validation");
                ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings);
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "recording-validation");
            }
        }

        return new DiagnosticSessionRecordingCheckResult(verification);
    }

    private static async Task<JsonElement?> RunRecordingVerificationAsync(
        DiagnosticSessionOptions options,
        string scenario,
        string outputDirectory,
        bool startedRecording,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var hasFlashbackExportVerificationPath = DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(
            scenario,
            outputDirectory,
            out var flashbackExportVerificationPath);
        var shouldRunVerification =
            startedRecording ||
            (options.VerifyRecording && hasFlashbackExportVerificationPath);
        if (!shouldRunVerification)
        {
            if (options.VerifyRecording)
            {
                actions.Add("recording verification skipped: scenario does not produce a recording or export artifact");
            }

            return null;
        }

        try
        {
            setStage("recording-verification");
            var verificationCommand = "VerifyLastRecording";
            Dictionary<string, object?>? verificationPayload = null;
            if (!startedRecording)
            {
                verificationCommand = "VerifyFile";
                verificationPayload = new Dictionary<string, object?>
                {
                    ["filePath"] = flashbackExportVerificationPath,
                    ["strict"] = true,
                    ["verificationProfile"] = "flashback-export"
                };
            }

            var verificationResponse = await sendAsync(verificationCommand, verificationPayload, 60_000).ConfigureAwait(false);
            if (TryGetVerification(verificationResponse, out var verificationElement))
            {
                return verificationElement.Clone();
            }

            warnings.Add(AutomationSnapshotFormatter.Get(verificationResponse, "Message", "Verification did not return data."));
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "recording-verification");
        }

        return null;
    }
}

internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification);
