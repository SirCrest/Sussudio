using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackRejectedExports;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;
using static Sussudio.Tools.DiagnosticSessionSampler;

namespace Sussudio.Tools;

public static class DiagnosticSessionRunner
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarios.
    // RunAsync reads like a phase plan: setup, optional background scenario
    // task, sampling loop, cleanup, then summary.

    public static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        var scenario = DiagnosticSessionScenarios.Normalize(options.Scenario);
        var durationSeconds = Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60);
        var sampleIntervalMs = Math.Clamp(options.SampleIntervalMs, 100, 60_000);
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "diagnostic-sessions", sessionId)
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);

        try
        {
        var startedUtc = DateTimeOffset.UtcNow;
        var runnerProcessId = Environment.ProcessId;

        var actions = new List<string>();
        var warnings = new List<string>();
        var samples = new List<DiagnosticSessionSample>();
        var runState = new DiagnosticSessionRunState(
            sessionId,
            scenario,
            outputDirectory,
            startedUtc,
            runnerProcessId,
            () => cancellationToken.IsCancellationRequested,
            warnings);
        var livePath = runState.LivePath;
        JsonElement? timeline = null;
        JsonElement? verification = null;
        PresentMonProbeResult? presentMon = null;
        var commandFailureCount = 0;
        var startedPreview = false;
        var startedRecording = false;
        var enabledFlashback = false;
        var disabledFlashback = false;
        var startedFlashbackPlayback = false;
        var stoppedRecordingForVerification = false;
        var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);
        var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;
        var runFlashbackRecordingExportRejected = scenarioPlan.RunFlashbackRecordingExportRejected;
        var runFlashbackExportRejected = scenarioPlan.RunFlashbackExportRejected;
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState = default;
        var commandSendGate = new SemaphoreSlim(1, 1);
        using var scenarioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scenarioCancellationToken = scenarioCts.Token;
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();

        async Task<JsonElement> SendRawWithConnectRetryAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs)
            => await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, scenarioCancellationToken).ConfigureAwait(false);

        async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            CancellationToken commandCancellationToken)
        {
            var response = await SendCommandWithConnectRetryAsync(
                    sendCommandAsync,
                    command,
                    payload,
                    responseTimeoutMs,
                    TimeSpan.FromSeconds(30),
                    commandCancellationToken)
                .ConfigureAwait(false);
            return response ?? BuildLocalFailureResponse(command, "no response after connect retry");
        }

        JsonElement initialSnapshot = CreateEmptyJsonObject();
        var initialSnapshotKnown = false;
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        try
        {
            SetStage("initial-snapshot");
            var initialResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(initialResponse, out var initial))
            {
                initialSnapshot = initial;
                initialSnapshotKnown = true;
            }
            else
            {
                commandFailureCount++;
                warnings.Add("initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped");
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "initial-snapshot");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        try
        {
            SetStage("scenario-setup");
            if (!initialSnapshotKnown && scenario != DiagnosticSessionScenarios.Observe)
            {
                commandFailureCount++;
                warnings.Add($"initial-snapshot: skipped state-mutating scenario '{scenario}' because the initial app state is unknown");
            }
            else
            {
            var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                    scenario,
                    scenarioPlan,
                    initialSnapshot,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    TryWaitAsync,
                    scenarioCancellationToken)
                .ConfigureAwait(false);
            startedPreview = setupResult.StartedPreview;
            startedRecording = setupResult.StartedRecording;
            enabledFlashback = setupResult.EnabledFlashback;
            disabledFlashback = setupResult.DisabledFlashback;

            var scenarioStartup = await DiagnosticSessionScenarioStartup.StartAsync(
                    options,
                    scenarioPlan,
                    durationSeconds,
                    outputDirectory,
                    backgroundTasks,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    SendRawWithConnectRetryAsync,
                    (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                    scenarioCancellationToken)
                .ConfigureAwait(false);
            startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

            SetStage("sampling");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
            await SampleLoopAsync(
                    durationSeconds,
                    sampleIntervalMs,
                    samples,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken,
                    WriteSamplingLiveStateBestEffortAsync)
                .ConfigureAwait(false);

            await backgroundTasks.AwaitScenarioTasksAsync().ConfigureAwait(false);
            flashbackRecordingSettingsDeferredPresetState = await backgroundTasks
                .AwaitRecordingSettingsDeferredAsync(flashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);

            if (runFlashbackExportRejected)
            {
                await RunFlashbackExportRejectedAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (runFlashbackRecordingExportRejected)
            {
                await RunFlashbackRecordingExportRejectedAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            presentMon = await backgroundTasks.AwaitPresentMonAsync(presentMon, warnings).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, runState.LastStage);
            scenarioCts.Cancel();
            var backgroundTaskDrain = await backgroundTasks.ObserveAfterFaultAsync(
                    warnings,
                    SetStage,
                    RecordTerminalException,
                    () => WriteLiveStateBestEffortAsync(),
                    presentMon,
                    flashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);
            presentMon = backgroundTaskDrain.PresentMon;
            flashbackRecordingSettingsDeferredPresetState = backgroundTaskDrain.RecordingSettingsDeferredPresetState;
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }
        finally
        {
            var cleanupResult = await DiagnosticSessionCleanupActions.RunAsync(
                    options,
                    initialSnapshot,
                    startedRecording,
                    startedPreview,
                    enabledFlashback,
                    disabledFlashback,
                    startedFlashbackPlayback,
                    actions,
                    SendWithTokenAsync,
                    TryWaitWithTokenAsync,
                    SetStage,
                    RecordTerminalException)
                .ConfigureAwait(false);
            stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;

            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        var recordingCheckResult = await DiagnosticSessionRecordingChecks.RunAsync(
                options,
                scenarioPlan,
                scenario,
                outputDirectory,
                initialSnapshot,
                samples,
                startedRecording,
                flashbackRecordingSettingsDeferredPresetState,
                actions,
                warnings,
                (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                SetStage,
                RecordTerminalException,
                cancellationToken)
            .ConfigureAwait(false);
        verification = recordingCheckResult.Verification;

        try
        {
            SetStage("timeline");
            var timelineResponse = await SendAsync(
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
            RecordTerminalException(ex, "timeline");
        }

        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = lastSnapshot;
        try
        {
            SetStage("final-snapshot");
            var finalSnapshotResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            healthSnapshot = TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)
                ? finalSnapshot
                : lastSnapshot;
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "final-snapshot");
        }

        var result = await DiagnosticSessionResultBuilder.BuildAndWriteAsync(
                new DiagnosticSessionResultBuildRequest(
                    options,
                    scenarioPlan,
                    sessionId,
                    scenario,
                    durationSeconds,
                    sampleIntervalMs,
                    outputDirectory,
                    livePath,
                    startedUtc,
                    runnerProcessId,
                    commandFailureCount,
                    samples,
                    initialSnapshot,
                    healthSnapshot,
                    timeline,
                    verification,
                    presentMon,
                    startedPreview,
                    enabledFlashback,
                    startedFlashbackPlayback,
                    stoppedRecordingForVerification,
                    actions,
                    warnings),
                runState)
            .ConfigureAwait(false);

        await WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);
        return result;

        void SetStage(string stage)
        {
            runState.SetStage(stage);
        }

        void RecordTerminalException(Exception ex, string stage)
        {
            runState.RecordTerminalException(ex, stage);
        }

        async Task WriteLiveStateBestEffortAsync(DateTimeOffset? completedUtcOverride = null, string? terminalStateOverride = null)
        {
            await runState.WriteLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandFailureCount,
                    completedUtcOverride,
                    terminalStateOverride)
                .ConfigureAwait(false);
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            await runState.WriteSamplingLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandFailureCount)
                .ConfigureAwait(false);
        }

        async Task<JsonElement> SendAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            bool allowFailure = false)
            => await SendWithTokenAsync(command, payload, responseTimeoutMs, allowFailure, scenarioCancellationToken).ConfigureAwait(false);

        async Task<JsonElement> SendWithTokenAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            bool allowFailure,
            CancellationToken commandCancellationToken)
        {
            await commandSendGate.WaitAsync(commandCancellationToken).ConfigureAwait(false);
            try
            {
                var response = await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, commandCancellationToken).ConfigureAwait(false);
                if (!AutomationSnapshotFormatter.IsSuccess(response) && !allowFailure)
                {
                    commandFailureCount++;
                    warnings.Add($"{command}: {AutomationSnapshotFormatter.Get(response, "Message", "command failed")}");
                }

                return response.Clone();
            }
            finally
            {
                commandSendGate.Release();
            }
        }

        async Task TryWaitAsync(string condition, int timeoutMs)
            => await TryWaitWithTokenAsync(condition, timeoutMs, scenarioCancellationToken).ConfigureAwait(false);

        async Task TryWaitWithTokenAsync(string condition, int timeoutMs, CancellationToken waitCancellationToken)
        {
            var response = await SendWithTokenAsync(
                    "WaitForCondition",
                    new Dictionary<string, object?>
                    {
                        ["condition"] = condition,
                        ["timeoutMs"] = timeoutMs,
                        ["pollMs"] = 250
                    },
                    timeoutMs + 2_000,
                    false,
                    waitCancellationToken)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add($"wait {condition}: {AutomationSnapshotFormatter.Get(response, "Message", "not met")}");
            }
        }
        }
        finally
        {
            sessionLock.Dispose();
        }
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

}
