using System.Globalization;
using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal sealed class DiagnosticSessionRunContext : IDisposable
{
    private readonly DiagnosticSessionLiveStateWriter _liveStateWriter;
    private bool _disposed;

    internal DiagnosticSessionRunContext(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken runCancellationToken)
    {
        RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);
        Scenario = RunBootstrap.Scenario;
        ScenarioPlan = RunBootstrap.ScenarioPlan;
        DurationSeconds = RunBootstrap.DurationSeconds;
        SampleIntervalMs = RunBootstrap.SampleIntervalMs;
        OutputDirectory = RunBootstrap.OutputDirectory;
        Actions = [];
        Warnings = [];
        Samples = [];
        RunState = new DiagnosticSessionRunState(
            () => runCancellationToken.IsCancellationRequested,
            Warnings);
        _liveStateWriter = new DiagnosticSessionLiveStateWriter(RunBootstrap, RunState, Warnings);
        LivePath = _liveStateWriter.LivePath;
        ScenarioCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(runCancellationToken);
        ScenarioCancellationToken = ScenarioCancellationSource.Token;
        CommandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, ScenarioCancellationToken, Warnings);

        InitializeUnknownSnapshotState();
    }

    internal DiagnosticSessionRunBootstrap RunBootstrap { get; }

    internal string Scenario { get; }

    internal DiagnosticSessionScenarioPlan ScenarioPlan { get; }

    internal int DurationSeconds { get; }

    internal int SampleIntervalMs { get; }

    internal string OutputDirectory { get; }

    internal List<string> Actions { get; }

    internal List<string> Warnings { get; }

    internal List<DiagnosticSessionSample> Samples { get; }

    internal DiagnosticSessionRunState RunState { get; }

    internal DiagnosticSessionCommandChannel CommandChannel { get; }

    internal CancellationTokenSource ScenarioCancellationSource { get; }

    internal CancellationToken ScenarioCancellationToken { get; }

    internal JsonElement InitialSnapshot { get; private set; }

    internal bool InitialSnapshotKnown { get; private set; }

    internal string LivePath { get; }

    internal void SetStage(string stage)
    {
        RunState.SetStage(stage);
    }

    internal void RecordTerminalException(Exception ex, string stage)
    {
        RunState.RecordTerminalException(ex, stage);
    }

    internal async Task CaptureInitialSnapshotAsync()
    {
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        var initialSnapshotResult = await CaptureInitialSnapshotCoreAsync().ConfigureAwait(false);
        InitialSnapshot = initialSnapshotResult.Snapshot;
        InitialSnapshotKnown = initialSnapshotResult.Known;
    }

    internal async Task WriteLiveStateBestEffortAsync(
        DateTimeOffset? completedUtcOverride = null,
        string? terminalStateOverride = null)
    {
        await _liveStateWriter.WriteLiveStateBestEffortAsync(
                Samples,
                InitialSnapshot,
                CommandChannel.FailureCount,
                completedUtcOverride,
                terminalStateOverride)
            .ConfigureAwait(false);
    }

    internal async Task WriteSamplingLiveStateBestEffortAsync()
    {
        await _liveStateWriter.WriteSamplingLiveStateBestEffortAsync(
                Samples,
                InitialSnapshot,
                CommandChannel.FailureCount)
            .ConfigureAwait(false);
    }

    internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(
        DiagnosticSessionOptions options,
        CancellationToken cancellationToken)
        => new DiagnosticSessionScenarioPhaseContext()
        {
            Options = options,
            Scenario = Scenario,
            ScenarioPlan = ScenarioPlan,
            DurationSeconds = DurationSeconds,
            SampleIntervalMs = SampleIntervalMs,
            OutputDirectory = OutputDirectory,
            InitialSnapshot = InitialSnapshot,
            InitialSnapshotKnown = InitialSnapshotKnown,
            Actions = Actions,
            Warnings = Warnings,
            Samples = Samples,
            CommandChannel = CommandChannel,
            ScenarioCancellationSource = ScenarioCancellationSource,
            ScenarioCancellationToken = ScenarioCancellationToken,
            RunCancellationToken = cancellationToken,
            SetStage = SetStage,
            GetLastStage = () => RunState.LastStage,
            RecordTerminalException = RecordTerminalException,
            WriteLiveStateBestEffortAsync = () => WriteLiveStateBestEffortAsync(),
            WriteSamplingLiveStateBestEffortAsync = WriteSamplingLiveStateBestEffortAsync,
        };

    internal DiagnosticSessionCompletionContext CreateCompletionContext(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPhaseResult scenarioPhase,
        bool stoppedRecordingForVerification,
        CancellationToken cancellationToken)
        => new DiagnosticSessionCompletionContext()
        {
            Options = options,
            RunBootstrap = RunBootstrap,
            LivePath = LivePath,
            InitialSnapshot = InitialSnapshot,
            Samples = Samples,
            ScenarioPhase = scenarioPhase,
            StoppedRecordingForVerification = stoppedRecordingForVerification,
            Actions = Actions,
            Warnings = Warnings,
            CommandChannel = CommandChannel,
            RunState = RunState,
            SetStage = SetStage,
            RecordTerminalException = RecordTerminalException,
            RunCancellationToken = cancellationToken,
            WriteLiveStateBestEffortAsync = (completedUtc, terminalState) =>
                WriteLiveStateBestEffortAsync(completedUtc, terminalState),
        };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CommandChannel.Dispose();
        ScenarioCancellationSource.Dispose();
        _disposed = true;
    }

    private void InitializeUnknownSnapshotState()
    {
        var unknownSnapshot = CreateUnknownInitialSnapshot();
        InitialSnapshot = unknownSnapshot.Snapshot;
        InitialSnapshotKnown = unknownSnapshot.Known;
    }

    private DiagnosticSessionInitialSnapshotResult CreateUnknownInitialSnapshot()
    {
        return new DiagnosticSessionInitialSnapshotResult(CreateEmptyJsonObject(), false);
    }

    private async Task<DiagnosticSessionInitialSnapshotResult> CaptureInitialSnapshotCoreAsync()
    {
        var unknownSnapshot = CreateUnknownInitialSnapshot();
        var initialSnapshot = unknownSnapshot.Snapshot;
        var initialSnapshotKnown = unknownSnapshot.Known;

        try
        {
            SetStage("initial-snapshot");
            var initialResponse = await CommandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null).ConfigureAwait(false);
            if (TryGetSnapshot(initialResponse, out var initial))
            {
                initialSnapshot = initial;
                initialSnapshotKnown = true;
            }
            else
            {
                CommandChannel.RecordFailure("initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped");
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "initial-snapshot");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return new DiagnosticSessionInitialSnapshotResult(initialSnapshot, initialSnapshotKnown);
    }
}

internal sealed class DiagnosticSessionLiveStateWriter
{
    private readonly DiagnosticSessionRunBootstrap _runBootstrap;
    private readonly DiagnosticSessionRunState _runState;
    private readonly IReadOnlyList<string> _warnings;
    private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;

    internal DiagnosticSessionLiveStateWriter(
        DiagnosticSessionRunBootstrap runBootstrap,
        DiagnosticSessionRunState runState,
        IReadOnlyList<string> warnings)
    {
        _runBootstrap = runBootstrap;
        _runState = runState;
        _warnings = warnings;
        LivePath = Path.Combine(runBootstrap.OutputDirectory, "session-live.json");
    }

    internal string LivePath { get; }

    internal async Task WriteLiveStateBestEffortAsync(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        int commandFailureCount,
        DateTimeOffset? completedUtcOverride = null,
        string? terminalStateOverride = null)
    {
        try
        {
            var liveHealthSnapshot = samples.Count > 0
                ? samples[^1].Snapshot
                : initialSnapshot;
            await WriteJsonAsync(
                    LivePath,
                    new
                    {
                        SessionId = _runBootstrap.SessionId,
                        Scenario = _runBootstrap.Scenario,
                        StartedUtc = _runBootstrap.StartedUtc,
                        UpdatedUtc = DateTimeOffset.UtcNow,
                        CompletedUtc = completedUtcOverride,
                        TerminalState = terminalStateOverride ?? (_runState.TerminalException is null ? "running" : _runState.GetTerminalState()),
                        LastStage = terminalStateOverride is null ? _runState.LastStage : _runState.GetResultLastStage(),
                        RunnerProcessId = _runBootstrap.RunnerProcessId,
                        OutputDirectory = _runBootstrap.OutputDirectory,
                        SummaryPath = Path.Combine(_runBootstrap.OutputDirectory, "summary.json"),
                        SampleCount = samples.Count,
                        HealthStatus = GetDiagnosticHealthStatus(liveHealthSnapshot),
                        LikelyStage = GetDiagnosticLikelyStage(liveHealthSnapshot),
                        CommandFailureCount = commandFailureCount,
                        WarningCount = _warnings.Count,
                        LastWarning = _warnings.Count > 0 ? _warnings[^1] : string.Empty,
                        UnhandledException = _runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(_runState.TerminalException)
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // The live-state file is diagnostic breadcrumbs only.
        }
    }

    internal async Task WriteSamplingLiveStateBestEffortAsync(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        int commandFailureCount)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastSamplingLiveStateUtc < TimeSpan.FromSeconds(5))
        {
            return;
        }

        _lastSamplingLiveStateUtc = now;
        await WriteLiveStateBestEffortAsync(samples, initialSnapshot, commandFailureCount).ConfigureAwait(false);
    }
}

internal sealed class DiagnosticSessionRunState
{
    private readonly Func<bool> _isCancellationRequested;
    private readonly List<string> _warnings;

    internal DiagnosticSessionRunState(
        Func<bool> isCancellationRequested,
        List<string> warnings)
    {
        _isCancellationRequested = isCancellationRequested;
        _warnings = warnings;
    }

    internal string LastStage { get; private set; } = "initializing";

    internal Exception? TerminalException { get; private set; }

    internal string? TerminalExceptionStage { get; private set; }

    internal void SetStage(string stage)
    {
        LastStage = stage;
    }

    internal void RecordTerminalException(Exception ex, string stage)
    {
        SetStage(stage);
        if (TerminalException is null)
        {
            TerminalException = ex;
            TerminalExceptionStage = stage;
        }

        _warnings.Add($"{stage}: {FormatTerminalException(ex)}");
    }

    internal static string FormatTerminalException(Exception ex)
    {
        return string.IsNullOrWhiteSpace(ex.Message)
            ? ex.GetType().Name
            : $"{ex.GetType().Name}: {ex.Message}";
    }

    internal string GetTerminalState()
    {
        if (TerminalException is OperationCanceledException || _isCancellationRequested())
        {
            return "canceled";
        }

        return TerminalException is null ? "completed" : "failed";
    }

    internal string GetResultLastStage()
        => TerminalExceptionStage ?? LastStage;

    internal async Task WriteArtifactBestEffortAsync<T>(string stage, string path, T value)
    {
        try
        {
            SetStage(stage);
            await WriteJsonAsync(path, value, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, stage);
        }
    }
}

internal sealed class DiagnosticSessionInitialSnapshotResult
{
    internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)
    {
        Snapshot = snapshot;
        Known = known;
    }

    internal JsonElement Snapshot { get; }
    internal bool Known { get; }
}

internal readonly record struct DiagnosticSessionRunBootstrap(
    string Scenario,
    DiagnosticSessionScenarioPlan ScenarioPlan,
    int DurationSeconds,
    int SampleIntervalMs,
    string SessionId,
    string OutputDirectory,
    DateTimeOffset StartedUtc,
    int RunnerProcessId)
{
    internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)
    {
        var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);
        var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);
        var durationSeconds = Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60);
        var sampleIntervalMs = Math.Clamp(options.SampleIntervalMs, 100, 60_000);
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "diagnostic-sessions", sessionId)
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        return new DiagnosticSessionRunBootstrap(
            scenario,
            scenarioPlan,
            durationSeconds,
            sampleIntervalMs,
            sessionId,
            outputDirectory,
            DateTimeOffset.UtcNow,
            Environment.ProcessId);
    }
}

internal static class DiagnosticSessionAutomationResponseJson
{
    internal static bool TryGetSnapshot(JsonElement response, out JsonElement snapshot)
    {
        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Snapshot", out snapshot) &&
            snapshot.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        snapshot = default;
        return false;
    }

    internal static bool TryGetVerification(JsonElement response, out JsonElement verification)
    {
        verification = default;
        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("Verification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return response.TryGetProperty("Snapshot", out var snapshot) &&
               snapshot.ValueKind == JsonValueKind.Object &&
               snapshot.TryGetProperty("LastVerification", out verification) &&
               verification.ValueKind == JsonValueKind.Object;
    }
}
