using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio;

// App-wide path resolution used by logging, diagnostics, tools launched from
// staged builds, and repository-local temp artifacts.
public static class RuntimePaths
{
    private const string LogRootEnvVar = "SUSSUDIO_LOG_ROOT";
    private static readonly Lazy<string> RepoRoot = new(ResolveRepoRoot, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoTempRoot = new(
        () => EnsureDirectory(Path.Combine(RepoRoot.Value, "temp")),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoLogRoot = new(
        () => EnsureDirectory(ResolveLogRoot()),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static string GetRepoRoot() => RepoRoot.Value;
    public static string GetRepoTempRoot() => RepoTempRoot.Value;
    public static string GetRepoLogRoot() => RepoLogRoot.Value;
    public static string GetRepoTempFile(string fileName) => Path.Combine(GetRepoTempRoot(), fileName);
    public static string GetRepoLogFile(string fileName) => Path.Combine(GetRepoLogRoot(), fileName);

    private static string ResolveLogRoot()
    {
        var envOverride = Environment.GetEnvironmentVariable(LogRootEnvVar);
        if (TryEnsureFullPath(envOverride, out var envLogRoot, $"env var '{LogRootEnvVar}' path resolution failed"))
        {
            return envLogRoot;
        }

        // Prefer repo-local logs when we can identify a repo root (development scenario).
        var repoRoot = RepoRoot.Value;
        if (TryEnsureDirectory(Path.Combine(repoRoot, "temp", "logs"), out var repoLogRoot, "repo-local log dir creation failed"))
        {
            return repoLogRoot;
        }

        // Non-repo scenario: keep logs in a stable per-user location.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return EnsureDirectory(Path.Combine(localAppData, "Sussudio", "logs"));
    }

    private static string ResolveRepoRoot()
    {
        var searchStarts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddPathIfPresent(searchStarts, AppContext.BaseDirectory);
        AddPathIfPresent(searchStarts, Directory.GetCurrentDirectory());

        foreach (var start in searchStarts)
        {
            var found = FindRepoRoot(start);
            if (!string.IsNullOrWhiteSpace(found))
            {
                return found;
            }
        }

        if (TryResolveLatestBuildParent(out var latestBuildParent))
        {
            return latestBuildParent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindRepoRoot(string startPath)
    {
        if (!TryCreateDirectoryInfo(startPath, out var current))
        {
            return null;
        }

        while (current != null)
        {
            if (IsRepoMarkerDirectory(current, out var repoRoot))
            {
                return repoRoot;
            }

            current = current.Parent;
        }

        return null;
    }

    private static void AddPathIfPresent(ISet<string> paths, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        if (TryGetFullPath(candidate, out var full, $"candidate path '{candidate}' is malformed"))
        {
            paths.Add(full);
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool TryResolveLatestBuildParent(out string parentPath)
    {
        parentPath = string.Empty;

        if (!TryGetFullPath(AppContext.BaseDirectory, out var baseDir, "latest-build parent resolution failed"))
        {
            return false;
        }

        var baseName = Path.GetFileName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.Equals(baseName, "latest-build", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parent = Directory.GetParent(baseDir);
        if (parent == null)
        {
            return false;
        }

        parentPath = parent.FullName;
        return true;
    }

    private static bool TryCreateDirectoryInfo(string startPath, out DirectoryInfo? directory)
    {
        try
        {
            directory = new DirectoryInfo(startPath);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback($"path '{startPath}' is invalid or inaccessible", ex);
            directory = null;
            return false;
        }
    }

    private static bool IsRepoMarkerDirectory(DirectoryInfo current, out string repoRoot)
    {
        repoRoot = string.Empty;

        try
        {
            var full = current.FullName;
            if (Directory.Exists(Path.Combine(full, ".git")) ||
                File.Exists(Path.Combine(full, ".git")) ||
                Directory.Exists(Path.Combine(full, ".claude")) ||
                File.Exists(Path.Combine(full, "AGENTS.md")))
            {
                repoRoot = full;
                return true;
            }

            if (File.Exists(Path.Combine(full, "Sussudio.slnx")) ||
                File.Exists(Path.Combine(full, "Sussudio.sln")))
            {
                repoRoot = full;
                return true;
            }

            if (File.Exists(Path.Combine(full, "Sussudio.csproj")))
            {
                repoRoot = current.Parent?.FullName ?? full;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            TraceFallback("directory inaccessible during repo root search", ex);
            return false;
        }
    }

    private static bool TryEnsureFullPath(string? candidate, out string fullPath, string failureContext)
    {
        fullPath = string.Empty;

        if (!TryGetFullPath(candidate, out var normalizedPath, failureContext))
        {
            return false;
        }

        return TryEnsureDirectory(normalizedPath, out fullPath, failureContext);
    }

    private static bool TryEnsureDirectory(string? path, out string ensuredPath, string failureContext)
    {
        ensuredPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            ensuredPath = EnsureDirectory(path);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback(failureContext, ex);
            return false;
        }
    }

    private static bool TryGetFullPath(string? candidate, out string fullPath, string failureContext)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(candidate);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback(failureContext, ex);
            return false;
        }
    }

    private static void TraceFallback(string context, Exception exception) =>
        Trace.TraceWarning($"RuntimePaths: {context}, falling back: {exception.Message}");
}

// Lightweight asynchronous debug logger. Logging must never block or crash
// capture paths, so messages go through a bounded channel with a direct
// best-effort fallback when the writer is saturated.
public static class Logger
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

    public static string GetLogFilePath() => LogFilePath;
}

// Source-generated JSON metadata for diagnostic snapshots written to the log.
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CaptureHealthSnapshot))]
[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]
internal sealed partial class LoggingJsonContext : JsonSerializerContext
{
}
