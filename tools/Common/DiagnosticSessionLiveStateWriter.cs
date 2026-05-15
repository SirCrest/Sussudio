using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

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
