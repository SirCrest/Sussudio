using System.Text.Json;

namespace Sussudio.Tools;

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
