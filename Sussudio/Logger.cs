using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sussudio;

// Lightweight asynchronous debug logger. Logging must never block or crash
// capture paths, so messages go through a bounded channel with a direct
// best-effort fallback when the writer is saturated.
public static partial class Logger
{
    private static readonly string LogFilePath = RuntimePaths.GetRepoLogFile("Sussudio_Debug.log");

    private static readonly object LockObject = new();
    private static readonly Channel<string> LogChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(8192)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private static readonly CancellationTokenSource LogWriterCancellation = new();
    public static bool VerboseEnabled { get; set; }
    private static int _systemInfoLogged;
    private static long _droppedLogMessages;

    // Categorized init outcome so callers can distinguish "log file isn't being
    // written" from "log file rotation failed but writer started anyway".
    // Keeps the static-ctor-must-not-throw invariant by *recording* failure
    // rather than rethrowing. AccessViolationException remains uncatchable per
    // CLAUDE.md — this enum only covers ordinary I/O.
    public enum LoggerInitState
    {
        NotInitialized = 0,
        Healthy,
        FileIoFailed,
        WriterStartFailed,
    }

    public static LoggerInitState InitState { get; private set; } = LoggerInitState.NotInitialized;

    static Logger()
    {
#if DEBUG
        VerboseEnabled = true;
#else
        VerboseEnabled = false;
#endif
        var fileIoOk = true;
        try
        {
            RotatePriorLog();
            var header = $"=== Sussudio Debug Log ===\nStarted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nPID: {Environment.ProcessId}\n\n";
            File.WriteAllText(LogFilePath, header);
        }
        catch
        {
            // Best-effort: Logger init must not throw — if the log file is
            // locked we proceed without it. The InitState below records this.
            fileIoOk = false;
        }

        try
        {
            _ = Task.Run(RunLogWriterAsync);
            InitState = fileIoOk ? LoggerInitState.Healthy : LoggerInitState.FileIoFailed;
        }
        catch
        {
            InitState = LoggerInitState.WriterStartFailed;
        }
    }

    public static void Log(string message, [CallerMemberName] string caller = "")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{caller}] {message}\n";

        // Write to debug output
        System.Diagnostics.Debug.WriteLine(logMessage.TrimEnd());

        if (LogChannel.Writer.TryWrite(logMessage))
        {
            return;
        }

        var dropped = Interlocked.Increment(ref _droppedLogMessages);
        if (dropped == 1 || dropped % 100 == 0)
        {
            WriteDirect($"[{timestamp}] [Logger] Warning: log channel saturated, dropped messages={dropped}\n");
        }

        // Best-effort fallback path when the channel is saturated.
        WriteDirect(logMessage);
    }

    public static void LogVerbose(string message, [CallerMemberName] string caller = "")
    {
        if (!VerboseEnabled)
        {
            return;
        }

        Log(message, caller);
    }

    private static async Task RunLogWriterAsync()
    {
        try
        {
            while (await LogChannel.Reader.WaitToReadAsync(LogWriterCancellation.Token))
            {
                while (LogChannel.Reader.TryRead(out var entry))
                {
                    WriteDirect(entry);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in Logger.LogWriterLoop: {ex.Message}");
        }
    }

    private static void WriteDirect(string entry)
    {
        lock (LockObject)
        {
            try
            {
                File.AppendAllText(LogFilePath, entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in Logger.WriteDirect: {ex.Message}");
            }
        }
    }

    private static void RotatePriorLog()
    {
        if (!File.Exists(LogFilePath))
        {
            return;
        }

        var mtime = File.GetLastWriteTime(LogFilePath);
        var rotated = RuntimePaths.GetRepoLogFile($"Sussudio_Debug_{mtime:yyyyMMdd_HHmmss}.log");
        try
        {
            if (File.Exists(rotated))
            {
                File.Delete(LogFilePath);
            }
            else
            {
                File.Move(LogFilePath, rotated);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in Logger.RotatePriorLog: {ex.Message}");
        }
    }

    public static void LogEvent(string eventId, string message, [CallerMemberName] string caller = "")
    {
        Log($"[{eventId}] {message}", caller);
    }

    public static string GetLogFilePath() => LogFilePath;
}
