using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sussudio.Models;
using Vortice.DXGI;

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

    private static bool TryCreateDirectoryInfo(string startPath, [NotNullWhen(true)] out DirectoryInfo? directory)
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
// capture paths, so messages go through a bounded channel; saturation is
// counted and reported in aggregate by the background writer.
public static class Logger
{
    // Failures inside the logger itself report through Trace, never through
    // Logger.Log: routing them back into this class would re-enter the write
    // path that is already failing. Everywhere else in the app, diagnostics
    // go to Logger.Log so they reach the log file operators actually read.
    private const int MaxDrainBatchEntries = 256;
    private static string _logFilePath = string.Empty;

    private static readonly object LockObject = new();
    private static readonly Channel<string> LogChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(8192)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private static Task _logWriterTask = Task.CompletedTask;
    private static readonly object InitializationLock = new();
    private static int _initialized;
    public static bool VerboseEnabled { get; set; }
    private static int _systemInfoLogged;
    private static long _droppedLogMessages;
    private static long _unreportedDroppedLogMessages;

    // Categorized init outcome so callers can distinguish "log file isn't being
    // written" from "log file rotation failed but writer started anyway".
    // Keeps initialization non-throwing by recording failure
    // rather than rethrowing. AccessViolationException remains uncatchable per
    // CLAUDE.md — this enum only covers ordinary I/O.
    public enum LoggerInitState
    {
        NotInitialized = 0,
        Healthy,
        FileIoFailed,
        WriterStartFailed,
    }

    private static volatile LoggerInitState _initState;
    public static LoggerInitState InitState => _initState;
    private static long DroppedMessageCount => Interlocked.Read(ref _droppedLogMessages);

    static Logger()
    {
#if DEBUG
        VerboseEnabled = true;
#else
        VerboseEnabled = false;
#endif
    }

    // The admitted app or private probe selects one root for this process.
    // Repeated calls, including calls after shutdown or failure, never rotate
    // again or redirect an already selected log to another directory.
    public static void Initialize(string logRoot)
    {
        lock (InitializationLock)
        {
            if (_initialized != 0)
            {
                return;
            }

            _logFilePath = TryResolveLogFilePath(() =>
            {
                var directory = Path.GetFullPath(logRoot);
                Directory.CreateDirectory(directory);
                return Path.Combine(directory, "Sussudio_Debug.log");
            });
            var fileIoOk = !string.IsNullOrEmpty(_logFilePath);
            if (fileIoOk)
            {
                try
                {
                    RotatePriorLog();
                    var header = $"=== Sussudio Debug Log ===\nStarted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nPID: {Environment.ProcessId}\n\n";
                    File.WriteAllText(_logFilePath, header);
                }
                catch
                {
                    // Keep the resolved path so later writes can recover from a
                    // transient file lock. InitState still records the startup failure.
                    fileIoOk = false;
                }
            }

            try
            {
                _logWriterTask = Task.Run(RunLogWriterAsync);
                _initState = fileIoOk ? LoggerInitState.Healthy : LoggerInitState.FileIoFailed;
            }
            catch
            {
                _logWriterTask = Task.CompletedTask;
                _initState = LoggerInitState.WriterStartFailed;
            }
            Volatile.Write(ref _initialized, 1);
        }
    }

    internal static string TryResolveLogFilePath(Func<string> resolvePath)
    {
        try
        {
            return resolvePath();
        }
        catch (Exception ex)
        {
            TraceFallback($"Logger directory resolution failed: {ex.Message}");
            return string.Empty;
        }
    }

    private static void TraceFallback(string message)
    {
        try
        {
            Trace.WriteLine(message);
        }
        catch
        {
            // A failing diagnostic listener must not break the logging fallback.
        }
    }

    public static void Log(string message, [CallerMemberName] string caller = "")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{caller}] {message}\n";

        if (Volatile.Read(ref _initialized) == 0 || InitState == LoggerInitState.WriterStartFailed)
        {
            TraceFallback(logMessage);
            return;
        }

        // Write to debug output
        try
        {
            System.Diagnostics.Debug.WriteLine(logMessage.TrimEnd());
        }
        catch
        {
            // A diagnostic listener must not interrupt the caller or queued file logging.
        }

        if (LogChannel.Writer.TryWrite(logMessage))
        {
            return;
        }

        Interlocked.Increment(ref _droppedLogMessages);
        Interlocked.Increment(ref _unreportedDroppedLogMessages);
    }

    public static void LogVerbose(string message, [CallerMemberName] string caller = "")
    {
        if (!VerboseEnabled)
        {
            return;
        }

        Log(message, caller);
    }

    public static async Task ShutdownAsync(TimeSpan timeout)
    {
        lock (InitializationLock)
        {
            if (_initialized == 0)
            {
                return;
            }

            LogChannel.Writer.TryComplete();
        }
        try
        {
            await _logWriterTask.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            TraceFallback($"Logger shutdown drain timed out after {timeout.TotalMilliseconds:0} ms.");
        }
    }

    private static async Task RunLogWriterAsync()
    {
        try
        {
            while (await LogChannel.Reader.WaitToReadAsync())
            {
                var batch = new StringBuilder();
                var entries = 0;
                while (entries < MaxDrainBatchEntries && LogChannel.Reader.TryRead(out var entry))
                {
                    batch.Append(entry);
                    entries++;
                }

                var droppedInBatch = Interlocked.Exchange(ref _unreportedDroppedLogMessages, 0);
                if (droppedInBatch > 0)
                {
                    batch.Append('[')
                        .Append(DateTime.Now.ToString("HH:mm:ss.fff"))
                        .Append("] [Logger] Warning: log channel saturated, dropped_in_batch=")
                        .Append(droppedInBatch)
                        .Append(" dropped_total=")
                        .Append(DroppedMessageCount)
                        .Append('\n');
                }

                if (batch.Length > 0)
                {
                    WriteDirect(batch.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            TraceFallback($"Suppressed exception in Logger.{nameof(RunLogWriterAsync)} type={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void WriteDirect(string entry)
    {
        if (Volatile.Read(ref _initialized) == 0 || string.IsNullOrEmpty(_logFilePath))
        {
            TraceFallback(entry);
            return;
        }

        lock (LockObject)
        {
            try
            {
                File.AppendAllText(_logFilePath, entry);
            }
            catch (Exception ex)
            {
                TraceFallback($"Suppressed exception in Logger.WriteDirect: {ex.Message}");
            }
        }
    }

    private static void RotatePriorLog()
    {
        if (!File.Exists(_logFilePath))
        {
            return;
        }

        var mtime = File.GetLastWriteTime(_logFilePath);
        var rotated = Path.Combine(Path.GetDirectoryName(_logFilePath)!, $"Sussudio_Debug_{mtime:yyyyMMdd_HHmmss}.log");
        try
        {
            if (File.Exists(rotated))
            {
                File.Delete(_logFilePath);
            }
            else
            {
                File.Move(_logFilePath, rotated);
            }
        }
        catch (Exception ex)
        {
            TraceFallback($"Suppressed exception in Logger.RotatePriorLog: {ex.Message}");
        }
    }

    public static void LogEvent(string eventId, string message, [CallerMemberName] string caller = "")
    {
        Log($"[{eventId}] {message}", caller);
    }

    public static void LogSystemInfo()
    {
        if (Volatile.Read(ref _initialized) == 0)
        {
            TraceFallback("System diagnostics deferred until Logger.Initialize.");
            return;
        }

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
            using var cpuKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = (cpuKey?.GetValue("ProcessorNameString") as string)?.Trim();
            Log(string.IsNullOrEmpty(name) ? "CPU info unavailable: processor name missing" : $"CPU: {name}");
        }
        catch (Exception ex)
        {
            Log($"CPU info unavailable: {ex.Message}");
        }

        try
        {
            if (GetPhysicallyInstalledSystemMemory(out var totalKilobytes))
            {
                var bytes = (long)Math.Min(totalKilobytes, (ulong)long.MaxValue / 1024) * 1024;
                Log($"RAM: {DisplayFormatters.FormatBytes(bytes)} installed");
            }
            else
            {
                Log($"RAM info unavailable: Win32 error {Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex)
        {
            Log($"RAM info unavailable: {ex.Message}");
        }

        try
        {
            var result = DXGI.CreateDXGIFactory1<IDXGIFactory1>(out var factory);
            using (factory)
            {
                result.CheckError();
                if (factory is null)
                {
                    throw new InvalidOperationException("DXGI factory returned no interface.");
                }

                for (uint index = 0; ; index++)
                {
                    result = factory.EnumAdapters1(index, out var adapter);
                    using (adapter)
                    {
                        if (result == ResultCode.NotFound)
                        {
                            break;
                        }

                        result.CheckError();
                        var description = adapter.Description1;
                        var bytes = (long)Math.Min((ulong)description.DedicatedVideoMemory, (ulong)long.MaxValue);
                        Log($"GPU: {description.Description} | Vendor=0x{description.VendorId:X4} | Device=0x{description.DeviceId:X4} | Flags={description.Flags} | VRAM={DisplayFormatters.FormatBytes(bytes)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"GPU info unavailable: {ex.Message}");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalKilobytes);

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
            TraceFallback($"Suppressed exception in Logger.LogFatalBreadcrumb debug write: {debugEx.Message}");
        }
    }

    /// <summary>Returns the selected log path, or empty before initialization or if directory resolution failed.</summary>
    public static string GetLogFilePath() => Volatile.Read(ref _initialized) != 0 ? _logFilePath : string.Empty;
}

// Source-generated JSON metadata for diagnostic snapshots written to the log.
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CaptureHealthSnapshot))]
[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]
internal sealed partial class LoggingJsonContext : JsonSerializerContext
{
}
