using System;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture;

public static class Logger
{
    private static readonly string LogFilePath = RuntimePaths.GetRepoLogFile("ElgatoCapture_Debug.log");

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

    static Logger()
    {
#if DEBUG
        VerboseEnabled = true;
#else
        VerboseEnabled = false;
#endif
        // Clear log on startup
        try
        {
            File.WriteAllText(LogFilePath, $"=== ElgatoCapture Debug Log ===\n");
            File.AppendAllText(LogFilePath, $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
        }
        catch { /* Best-effort: Logger init must not throw — if the log file is locked we proceed without it */ }

        _ = Task.Run(RunLogWriterAsync);
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

    public static void LogSystemInfo()
    {
        if (!VerboseEnabled || Interlocked.Exchange(ref _systemInfoLogged, 1) == 1)
        {
            return;
        }

        Log("=== System Info ===");
        Log($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Log($"Process: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
        Log($".NET: {RuntimeInformation.FrameworkDescription}");
        Log($"Machine: {Environment.MachineName}");
        Log($"Logical processors: {Environment.ProcessorCount}");

        try
        {
            using var cpuSearcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
            foreach (var obj in cpuSearcher.Get())
            {
                Log($"CPU: {obj["Name"]} | Cores={obj["NumberOfCores"]} | Logical={obj["NumberOfLogicalProcessors"]} | MaxMHz={obj["MaxClockSpeed"]}");
            }
        }
        catch (Exception ex)
        {
            Log($"CPU info unavailable: {ex.Message}");
        }

        try
        {
            using var memSearcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in memSearcher.Get())
            {
                if (obj["TotalPhysicalMemory"] is ulong bytes)
                {
                    Log($"RAM: {FormatBytes(bytes)}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"RAM info unavailable: {ex.Message}");
        }

        try
        {
            using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, DriverDate, AdapterRAM FROM Win32_VideoController");
            foreach (var obj in gpuSearcher.Get())
            {
                var name = obj["Name"];
                var driverVersion = obj["DriverVersion"];
                var driverDate = obj["DriverDate"];
                var ram = obj["AdapterRAM"] is uint adapterRam ? FormatBytes(adapterRam) : "unknown";
                Log($"GPU: {name} | Driver={driverVersion} | DriverDate={driverDate} | VRAM={ram}");
            }
        }
        catch (Exception ex)
        {
            Log($"GPU info unavailable: {ex.Message}");
        }
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
        try
        {
            lock (LockObject)
            {
                File.AppendAllText(LogFilePath, entry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in Logger.WriteDirect: {ex.Message}");
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double scale = 1024;
        double value = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var unit = 0;
        while (value >= scale && unit < units.Length - 1)
        {
            value /= scale;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    public static void LogException(Exception ex, [CallerMemberName] string caller = "")
    {
        Log($"EXCEPTION: {ex.GetType().Name}", caller);
        Log($"  Message: {ex.Message}", caller);
        Log($"  StackTrace: {ex.StackTrace}", caller);
    }

    public static void LogStructured(string eventName, object payload, [CallerMemberName] string caller = "")
    {
        try
        {
            var json = payload switch
            {
                CaptureHealthSnapshot healthSnapshot =>
                    JsonSerializer.Serialize(healthSnapshot, LoggingJsonContext.Default.CaptureHealthSnapshot),
                CaptureDiagnosticsSnapshot diagnosticsSnapshot =>
                    JsonSerializer.Serialize(diagnosticsSnapshot, LoggingJsonContext.Default.CaptureDiagnosticsSnapshot),
                _ when JsonSerializer.IsReflectionEnabledByDefault =>
                    JsonSerializer.Serialize(payload),
                _ => payload.ToString() ?? "<null>"
            };
            Log($"{eventName}: {json}", caller);
        }
        catch (Exception ex)
        {
            Log($"Failed to serialize structured log '{eventName}': {ex.Message}", caller);
        }
    }

    public static void LogEvent(string eventId, string message, [CallerMemberName] string caller = "")
    {
        Log($"[{eventId}] {message}", caller);
    }

    public static void LogFatalBreadcrumb(string message, Exception? ex = null)
    {
        var utc = DateTime.UtcNow.ToString("O");
        var processId = Environment.ProcessId;
        var breadcrumb = $"[{utc}] [FATAL] [PID:{processId}] {message}";

        if (ex != null)
        {
            breadcrumb += $"\n[{utc}] [FATAL] Exception: {ex.GetType().Name}: {ex.Message}\n[{utc}] [FATAL] StackTrace: {ex.StackTrace}";
        }

        breadcrumb += "\n";

        WriteDirect(breadcrumb);

        try
        {
            System.Diagnostics.Debug.WriteLine(breadcrumb.TrimEnd());
        }
        catch (Exception debugEx)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in Logger.LogFatalBreadcrumb debug write: {debugEx.Message}");
        }
    }

    public static string GetLogFilePath() => LogFilePath;
}
