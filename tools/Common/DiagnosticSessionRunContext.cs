using System.Text.Json;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext : IDisposable
{
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

    internal void SetStage(string stage)
    {
        RunState.SetStage(stage);
    }

    internal void RecordTerminalException(Exception ex, string stage)
    {
        RunState.RecordTerminalException(ex, stage);
    }
}
