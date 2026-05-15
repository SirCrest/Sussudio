using System.Globalization;
using System.Text.Json;
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

        using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);

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
        JsonElement? verification = null;
        PresentMonProbeResult? presentMon = null;
        var startedPreview = false;
        var startedRecording = false;
        var enabledFlashback = false;
        var disabledFlashback = false;
        var startedFlashbackPlayback = false;
        var stoppedRecordingForVerification = false;
        var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);
        var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState = default;
        using var scenarioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scenarioCancellationToken = scenarioCts.Token;
        using var commandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, scenarioCancellationToken, warnings);
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();

        var initialSnapshotResult = DiagnosticSessionInitialSnapshot.CreateUnknown();
        var initialSnapshot = initialSnapshotResult.Snapshot;
        var initialSnapshotKnown = initialSnapshotResult.Known;
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        initialSnapshotResult = await DiagnosticSessionInitialSnapshot.CaptureAsync(
                commandChannel,
                SetStage,
                RecordTerminalException,
                () => WriteLiveStateBestEffortAsync())
            .ConfigureAwait(false);
        initialSnapshot = initialSnapshotResult.Snapshot;
        initialSnapshotKnown = initialSnapshotResult.Known;

        try
        {
            SetStage("scenario-setup");
            if (!initialSnapshotKnown && scenario != DiagnosticSessionScenarios.Observe)
            {
                commandChannel.RecordFailure($"initial-snapshot: skipped state-mutating scenario '{scenario}' because the initial app state is unknown");
            }
            else
            {
            var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                    scenario,
                    scenarioPlan,
                    initialSnapshot,
                    actions,
                    warnings,
                    commandChannel.SendAsync,
                    commandChannel.TryWaitAsync,
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
                    commandChannel.SendAsync,
                    commandChannel.SendRawWithConnectRetryAsync,
                    commandChannel.SendAsync,
                    scenarioCancellationToken)
                .ConfigureAwait(false);
            startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

            SetStage("sampling");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
            await SampleLoopAsync(
                    durationSeconds,
                    sampleIntervalMs,
                    samples,
                    commandChannel.SendAsync,
                    scenarioCancellationToken,
                    WriteSamplingLiveStateBestEffortAsync)
                .ConfigureAwait(false);

            await backgroundTasks.AwaitScenarioTasksAsync().ConfigureAwait(false);
            flashbackRecordingSettingsDeferredPresetState = await backgroundTasks
                .AwaitRecordingSettingsDeferredAsync(flashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);

            await DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(
                    scenarioPlan,
                    outputDirectory,
                    actions,
                    warnings,
                    commandChannel.SendAsync,
                    cancellationToken)
                .ConfigureAwait(false);

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
                    commandChannel.SendWithTokenAsync,
                    commandChannel.TryWaitWithTokenAsync,
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
                commandChannel.SendAsync,
                SetStage,
                RecordTerminalException,
                cancellationToken)
            .ConfigureAwait(false);
        verification = recordingCheckResult.Verification;

        var postRunSnapshots = await DiagnosticSessionPostRunSnapshots.CaptureAsync(
                samples,
                initialSnapshot,
                commandChannel.SendAsync,
                SetStage,
                RecordTerminalException)
            .ConfigureAwait(false);

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
                    commandChannel.FailureCount,
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
                    commandChannel.FailureCount,
                    completedUtcOverride,
                    terminalStateOverride)
                .ConfigureAwait(false);
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            await runState.WriteSamplingLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandChannel.FailureCount)
                .ConfigureAwait(false);
        }
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

}
