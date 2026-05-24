using System.Text.Json;

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

        var postRunSnapshots = await DiagnosticSessionPostRunSnapshots.CaptureAsync(
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
