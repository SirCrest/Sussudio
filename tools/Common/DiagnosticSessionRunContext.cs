using System.Text.Json;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext : IDisposable
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

        var unknownSnapshot = DiagnosticSessionInitialSnapshot.CreateUnknown();
        InitialSnapshot = unknownSnapshot.Snapshot;
        InitialSnapshotKnown = unknownSnapshot.Known;
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

    internal async Task CaptureInitialSnapshotAsync()
    {
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        var initialSnapshotResult = await DiagnosticSessionInitialSnapshot.CaptureAsync(
                CommandChannel,
                SetStage,
                RecordTerminalException,
                () => WriteLiveStateBestEffortAsync())
            .ConfigureAwait(false);
        InitialSnapshot = initialSnapshotResult.Snapshot;
        InitialSnapshotKnown = initialSnapshotResult.Known;
    }

    internal void SetStage(string stage)
    {
        RunState.SetStage(stage);
    }

    internal void RecordTerminalException(Exception ex, string stage)
    {
        RunState.RecordTerminalException(ex, stage);
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
}
