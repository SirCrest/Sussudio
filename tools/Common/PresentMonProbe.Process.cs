using System.Diagnostics;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static async Task<ProcessRun> RunProcessAsync(
        string fileName,
        string arguments,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var timedOut = false;

        try
        {
            await process.WaitForExitAsync(cancellationToken)
                .WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            timedOut = true;
            TryKill(process);
        }

        var stdout = await TryReadAsync(stdoutTask).ConfigureAwait(false);
        var stderr = await TryReadAsync(stderrTask).ConfigureAwait(false);

        return new ProcessRun
        {
            ExitCode = process.HasExited ? process.ExitCode : null,
            TimedOut = timedOut,
            StdOut = stdout,
            StdErr = stderr
        };
    }

    private static async Task<string> TryReadAsync(Task<string> task)
    {
        try
        {
            return await task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"PresentMonProbe.TryReadAsync swallowed: {ex.GetType().Name}: {ex.Message}");
            return string.Empty;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"PresentMonProbe.TryKill swallowed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"PresentMonProbe.TryDelete swallowed for '{path}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    private sealed class ProcessRun
    {
        public int? ExitCode { get; init; }
        public bool TimedOut { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
    }
}
