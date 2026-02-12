using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ElgatoCapture.Services;

public sealed class ProcessSpec
{
    public required string FileName { get; init; }
    public string Arguments { get; init; } = string.Empty;
    public string? WorkingDirectory { get; init; }
    public int TimeoutMs { get; init; } = 30_000;
}

public sealed class ProcessRunResult
{
    public bool Started { get; init; }
    public bool TimedOut { get; init; }
    public bool ExitConfirmed { get; init; }
    public int? ProcessId { get; init; }
    public int? ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public Exception? StartException { get; init; }
}

public interface IProcessSupervisor
{
    Task<ProcessRunResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default);
}

public sealed class ProcessSupervisor : IProcessSupervisor
{
    public async Task<ProcessRunResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        if (spec.TimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spec.TimeoutMs), "Timeout must be greater than zero.");
        }

        Logger.LogEvent("CAP-PROC-START", $"{spec.FileName} timeoutMs={spec.TimeoutMs}");

        Process? process;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = spec.FileName,
                Arguments = spec.Arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(spec.WorkingDirectory))
            {
                startInfo.WorkingDirectory = spec.WorkingDirectory;
            }

            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Logger.LogEvent("CAP-PROC-START-FAIL", $"{spec.FileName} error={ex.Message}");
            return new ProcessRunResult
            {
                Started = false,
                StartException = ex
            };
        }

        if (process == null)
        {
            Logger.LogEvent("CAP-PROC-START-FAIL", $"{spec.FileName} process=null");
            return new ProcessRunResult
            {
                Started = false
            };
        }

        using (process)
        {
            var processId = process.Id;
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var timedOut = false;
            var outputReadTimeoutMs = Math.Clamp(spec.TimeoutMs, 1000, 5000);

            try
            {
                var exitTask = process.WaitForExitAsync(cancellationToken);
                var timeoutTask = Task.Delay(spec.TimeoutMs);
                var completed = await Task.WhenAny(exitTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    timedOut = true;
                    Logger.LogEvent("CAP-PROC-TIMEOUT", $"{spec.FileName} timeoutMs={spec.TimeoutMs}");
                    var killWaitMs = Math.Clamp(spec.TimeoutMs / 2, 250, 5000);
                    var exited = await TryTerminateAsync(process, spec.FileName, killWaitMs, "timeout");
                    if (!exited)
                    {
                        Logger.LogEvent("CAP-PROC-STILL-ALIVE", $"{spec.FileName} reason=timeout pid={processId}");
                    }
                }
                else
                {
                    await exitTask;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogEvent("CAP-PROC-CANCEL", $"{spec.FileName}");
                var cancelKillWaitMs = Math.Clamp(spec.TimeoutMs, 1000, 10000);
                var exited = await TryTerminateAsync(process, spec.FileName, cancelKillWaitMs, "canceled");
                if (!exited)
                {
                    Logger.LogEvent("CAP-PROC-STILL-ALIVE", $"{spec.FileName} reason=canceled pid={processId}");
                }
                throw;
            }

            var canReadOutputs = process.HasExited;
            var stdout = canReadOutputs
                ? await TryReadWithTimeoutAsync(stdoutTask, outputReadTimeoutMs)
                : string.Empty;
            var stderr = canReadOutputs
                ? await TryReadWithTimeoutAsync(stderrTask, outputReadTimeoutMs)
                : string.Empty;

            if (!canReadOutputs)
            {
                Logger.LogEvent("CAP-PROC-READ-SKIP", $"{spec.FileName} output skipped because process remained alive");
            }

            Logger.LogEvent("CAP-PROC-EXIT", $"{spec.FileName} exitCode={(process.HasExited ? process.ExitCode : -1)} timedOut={timedOut}");

            return new ProcessRunResult
            {
                Started = true,
                TimedOut = timedOut,
                ExitConfirmed = process.HasExited,
                ProcessId = processId,
                ExitCode = process.HasExited ? process.ExitCode : null,
                StdOut = stdout,
                StdErr = stderr
            };
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
        catch
        {
            // Best-effort process termination.
        }
    }

    private static async Task<bool> TryTerminateAsync(Process process, string fileName, int killWaitMs, string reason)
    {
        TryKill(process);

        if (await WaitForExitWithTimeoutAsync(process, killWaitMs))
        {
            return true;
        }

        Logger.LogEvent("CAP-PROC-KILL-TIMEOUT", $"{fileName} reason={reason} killWaitMs={killWaitMs}");

        // Retry once with an additional bounded wait window.
        TryKill(process);
        var recoveryWaitMs = Math.Clamp(killWaitMs / 2, 250, 5000);
        if (await WaitForExitWithTimeoutAsync(process, recoveryWaitMs))
        {
            Logger.LogEvent("CAP-PROC-KILL-RECOVERED", $"{fileName} reason={reason} recoveryWaitMs={recoveryWaitMs}");
            return true;
        }

        return false;
    }

    private static async Task<bool> WaitForExitWithTimeoutAsync(Process process, int timeoutMs)
    {
        if (process.HasExited)
        {
            return true;
        }

        var exitTask = process.WaitForExitAsync();
        var completed = await Task.WhenAny(exitTask, Task.Delay(timeoutMs));
        if (completed != exitTask)
        {
            return false;
        }

        await exitTask;
        return process.HasExited;
    }

    private static async Task<string> TryReadWithTimeoutAsync(Task<string> readTask, int timeoutMs)
    {
        try
        {
            var completed = await Task.WhenAny(readTask, Task.Delay(timeoutMs));
            if (completed != readTask)
            {
                return string.Empty;
            }

            return await readTask;
        }
        catch
        {
            return string.Empty;
        }
    }
}
