using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal sealed class DiagnosticSessionRunState
{
    private readonly string _sessionId;
    private readonly string _scenario;
    private readonly string _outputDirectory;
    private readonly DateTimeOffset _startedUtc;
    private readonly int _runnerProcessId;
    private readonly Func<bool> _isCancellationRequested;
    private readonly List<string> _warnings;
    private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;

    internal DiagnosticSessionRunState(
        string sessionId,
        string scenario,
        string outputDirectory,
        DateTimeOffset startedUtc,
        int runnerProcessId,
        Func<bool> isCancellationRequested,
        List<string> warnings)
    {
        _sessionId = sessionId;
        _scenario = scenario;
        _outputDirectory = outputDirectory;
        _startedUtc = startedUtc;
        _runnerProcessId = runnerProcessId;
        _isCancellationRequested = isCancellationRequested;
        _warnings = warnings;
        LivePath = Path.Combine(outputDirectory, "session-live.json");
    }

    internal string LivePath { get; }

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
                        SessionId = _sessionId,
                        Scenario = _scenario,
                        StartedUtc = _startedUtc,
                        UpdatedUtc = DateTimeOffset.UtcNow,
                        CompletedUtc = completedUtcOverride,
                        TerminalState = terminalStateOverride ?? (TerminalException is null ? "running" : GetTerminalState()),
                        LastStage = terminalStateOverride is null ? LastStage : GetResultLastStage(),
                        RunnerProcessId = _runnerProcessId,
                        OutputDirectory = _outputDirectory,
                        SummaryPath = Path.Combine(_outputDirectory, "summary.json"),
                        SampleCount = samples.Count,
                        HealthStatus = GetDiagnosticHealthStatus(liveHealthSnapshot),
                        LikelyStage = GetDiagnosticLikelyStage(liveHealthSnapshot),
                        CommandFailureCount = commandFailureCount,
                        WarningCount = _warnings.Count,
                        LastWarning = _warnings.Count > 0 ? _warnings[^1] : string.Empty,
                        UnhandledException = TerminalException is null ? null : FormatTerminalException(TerminalException)
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
