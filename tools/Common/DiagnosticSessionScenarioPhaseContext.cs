using System.Text.Json;

namespace Sussudio.Tools;

internal sealed class DiagnosticSessionScenarioPhaseContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required string Scenario { get; init; }

    internal required DiagnosticSessionScenarioPlan ScenarioPlan { get; init; }

    internal required int DurationSeconds { get; init; }

    internal required int SampleIntervalMs { get; init; }

    internal required string OutputDirectory { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required bool InitialSnapshotKnown { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required List<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required CancellationTokenSource ScenarioCancellationSource { get; init; }

    internal required CancellationToken ScenarioCancellationToken { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Func<string> GetLastStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required Func<Task> WriteLiveStateBestEffortAsync { get; init; }

    internal required Func<Task> WriteSamplingLiveStateBestEffortAsync { get; init; }
}
