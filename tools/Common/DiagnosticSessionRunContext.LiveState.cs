namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext
{
    private readonly DiagnosticSessionLiveStateWriter _liveStateWriter;

    internal string LivePath { get; }

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
}
