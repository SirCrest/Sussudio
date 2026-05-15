using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

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
