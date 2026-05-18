using System;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Sussudio.Models;

namespace Sussudio;

public static partial class Logger
{
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
                    Log($"RAM: {DisplayFormatters.FormatBytes((long)Math.Min(bytes, long.MaxValue))}");
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
                var ram = obj["AdapterRAM"] is uint adapterRam ? DisplayFormatters.FormatBytes(adapterRam) : "unknown";
                Log($"GPU: {name} | Driver={driverVersion} | DriverDate={driverDate} | VRAM={ram}");
            }
        }
        catch (Exception ex)
        {
            Log($"GPU info unavailable: {ex.Message}");
        }
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
}
