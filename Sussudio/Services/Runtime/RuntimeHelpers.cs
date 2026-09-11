using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sussudio.Services.Runtime;

// Persisted user preferences. Missing nullable values mean "use current app
// defaults" so new settings can be added without breaking older installs.
public class UserSettings
{
    public string? SelectedDeviceId { get; set; }
    public string? OutputPath { get; set; }
    public string? SelectedRecordingFormat { get; set; }
    public string? SelectedQuality { get; set; }
    public string? SelectedPreset { get; set; }
    public string? SelectedSplitEncodeMode { get; set; }
    public double? CustomBitrateMbps { get; set; }
    public bool? IsHdrEnabled { get; set; }
    public bool? IsAudioEnabled { get; set; }
    public bool? IsAudioPreviewEnabled { get; set; }
    public bool? IsCustomAudioInputEnabled { get; set; }
    public string? SelectedAudioInputDeviceId { get; set; }
    public bool? IsMicrophoneEnabled { get; set; }
    public string? SelectedMicrophoneDeviceId { get; set; }
    public double? MicrophoneVolume { get; set; }
    public double? PreviewVolume { get; set; }
    public bool? IsStatsVisible { get; set; }
    public string? SelectedDeviceAudioMode { get; set; }
    public double? AnalogAudioGainPercent { get; set; }
    public bool? FlashbackGpuDecode { get; set; }
    public int? FlashbackBufferMinutes { get; set; }
    public string? SelectedVideoFormat { get; set; }
}

[JsonSerializable(typeof(UserSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

// LocalAppData settings store for the WinUI app. Load is forgiving and Save is
// serialized so UI updates cannot corrupt the JSON file.
public static class SettingsService
{
    private static readonly object _lock = new();

    private static string GetSettingsDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sussudio");

    private static string GetSettingsFilePath()
        => Path.Combine(GetSettingsDirectory(), "settings.json");

    public static UserSettings Load()
    {
        var settingsFilePath = GetSettingsFilePath();
        lock (_lock)
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    Logger.Log("SETTINGS_LOAD: no settings file found, using defaults.");
                    return new UserSettings();
                }

                var json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.UserSettings);
                if (settings == null)
                {
                    Logger.Log("SETTINGS_LOAD: deserialization returned null, using defaults.");
                    return new UserSettings();
                }

                Logger.Log($"SETTINGS_LOAD: loaded from {settingsFilePath}");
                return settings;
            }
            catch (Exception ex)
            {
                Logger.Log($"SETTINGS_LOAD: failed to load ({ex.GetType().Name}: {ex.Message}), using defaults.");
                return new UserSettings();
            }
        }
    }

    public static bool Save(UserSettings settings, out string failure)
    {
        var settingsFilePath = GetSettingsFilePath();
        lock (_lock)
        {
            return SaveToFile(settings, settingsFilePath, out failure);
        }
    }

    internal static bool SaveToFile(UserSettings settings, string settingsFilePath, out string failure)
    {
        failure = string.Empty;
        try
        {
            var settingsDirectory = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrWhiteSpace(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.UserSettings);
            var tempPath = settingsFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, settingsFilePath, overwrite: true);
            Logger.Log($"SETTINGS_SAVE: saved to {settingsFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            failure = $"{ex.GetType().Name}: {ex.Message}";
            Logger.Log($"SETTINGS_SAVE: failed ({failure})");
            return false;
        }
    }
}

// Lock-free max-update helpers. Several diagnostic counters across capture,
// recording, and flashback need to track high-water marks under contention; the
// CAS-loop pattern was open-coded in four files before this consolidation.
internal static class AtomicMax
{
    public static void Update(ref int target, int candidate)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }

    public static void Update(ref long target, long candidate)
    {
        while (true)
        {
            var current = Interlocked.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }
}

// The mirror image of AtomicMax: lock-free saturating decrement for queue-depth and
// pending-work counters. The same clamped CAS loop was open-coded in six places across
// audio, capture, flashback, gpu and preview, and the underflow arms had already drifted
// (three returned silently, two logged different tags). Returning false on the clamped
// path keeps that reporting decision at the call site instead of duplicating the loop.
internal static class AtomicCounter
{
    public static bool TryDecrement(ref int target) => TrySubtract(ref target, 1);

    // Callers supply a positive amount; false means the full amount was unavailable.
    public static bool TrySubtract(ref int target, int amount)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (current <= 0)
            {
                return false;
            }

            var next = Math.Max(0, current - amount);
            if (Interlocked.CompareExchange(ref target, next, current) == current)
            {
                return current >= amount;
            }
        }
    }
}

// Bounded-queue admission shared by the recording and flashback encoder sinks.
// Both sinks publish onto a bounded Channel from a hot capture callback, and both
// need the same three steps: claim a depth slot, record the high-water mark when
// the write lands, and give the slot back when the channel refuses. That sequence
// was open-coded seven times across the two sinks (video, gpu, cuda and audio
// lanes) and every copy was byte-identical, so a drift in one lane would have been
// invisible. The rollback stays a caller-supplied delegate because each sink tags
// its underflow diagnostic with its own log prefix.
internal static class QueueAdmission
{
    // Matches the sinks' own `DecrementQueueDepth(ref int, string)`, so a method
    // group converts without allocating per call.
    internal delegate void DepthRollback(ref int depth, string queueName);

    // Video/gpu/cuda lanes: the observed depth feeds the lane's high-water mark.
    public static bool TryWrite<T>(
        Channel<T> queue,
        T packet,
        ref int depth,
        ref int maxDepth,
        string queueName,
        DepthRollback rollback)
    {
        var observed = Interlocked.Increment(ref depth);
        if (queue.Writer.TryWrite(packet))
        {
            AtomicMax.Update(ref maxDepth, observed);
            return true;
        }

        rollback(ref depth, $"{queueName}_write_failed");
        return false;
    }

    // Audio lanes track depth but no high-water mark.
    public static bool TryWrite<T>(
        Channel<T> queue,
        T packet,
        ref int depth,
        string queueName,
        DepthRollback rollback)
    {
        Interlocked.Increment(ref depth);
        if (queue.Writer.TryWrite(packet))
        {
            return true;
        }

        rollback(ref depth, $"{queueName}_write_failed");
        return false;
    }
}

// Typed environment-variable parsing helpers for experimental performance and
// diagnostics knobs.
internal static class EnvironmentHelpers
{
    public static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    public static double GetDoubleFromEnv(string variableName, double defaultValue, double minValue, double maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            return Math.Clamp(parsed, minValue, maxValue);
        }

        return defaultValue;
    }

    public static bool TryGetBoolFromEnv(string variableName, out bool value)
    {
        value = false;
        var raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (bool.TryParse(raw, out var boolValue))
        {
            value = boolValue;
            return true;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            value = intValue != 0;
            return true;
        }

        return false;
    }
}

// Append-and-snapshot helpers for the simple ring buffers used by the cadence
// trackers and jitter trackers. Callers own their own validation and locking;
// these methods only handle the ring index/count bookkeeping and the bounded
// copy-out, both of which were duplicated across four classes before.
internal static class RingBufferHelpers
{
    public static void Add(double[] window, ref int count, ref int index, double value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    public static void Add(int[] window, ref int count, ref int index, int value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    // maxCount: optional cap on samples returned; defaults to "all available".
    public static double[] Copy(double[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
        if (take <= 0)
        {
            return Array.Empty<double>();
        }

        var result = new double[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }

    public static int[] Copy(int[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
        if (take <= 0)
        {
            return Array.Empty<int>();
        }

        var result = new int[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }
}

// Shared join of an encoder sink's background encoding task during dispose.
// Both sinks keep the first failure so the caller can surface it later; the
// failure must never escape dispose, and neither sink may block differently
// from the other. Ownership of the retained exception stays with each sink.
internal static class EncodingTaskHelpers
{
    // Both encoder sinks defer the same drain when the encoding task outlives their
    // dispose timeout: await it off the dispose path, keep the first failure, then
    // finalize exactly once. Only the completion tag differs, so the async
    // exception-handling shape lives here rather than drifting in two copies.
    public static void DrainDeferred(
        Task encodingTask,
        Action<Exception> recordFailure,
        Action finalize,
        string completeTag)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                recordFailure(ex);
            }
            finally
            {
                finalize();
                Logger.Log(completeTag);
            }
        });
    }

    public static Exception? ObserveCompletion(Task encodingTask)
    {
        try
        {
            encodingTask.GetAwaiter().GetResult();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}

// Shared nearest-rank percentile selection for already-sorted telemetry
// samples. Collection, copying, and sorting remain with each metric owner so
// this helper cannot extend locks or change hot-path allocation behavior.
internal static class PercentileHelpers
{
    public static int NearestRankIndex(int sampleCount, double percentile)
    {
        if (sampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "Sample count must be positive.");
        }

        ValidatePercentile(percentile);
        return Math.Clamp((int)Math.Ceiling(percentile * sampleCount) - 1, 0, sampleCount - 1);
    }

    public static double FromSorted(ReadOnlySpan<double> sortedSamples, double percentile)
    {
        ValidatePercentile(percentile);
        return sortedSamples.IsEmpty
            ? 0
            : sortedSamples[NearestRankIndex(sortedSamples.Length, percentile)];
    }

    private static void ValidatePercentile(double percentile)
    {
        if (!double.IsFinite(percentile) || percentile <= 0 || percentile > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be greater than zero and no greater than one.");
        }
    }
}

// Common "how old is this telemetry sample" computation. Several diagnostics
// surfaces (snapshot builders, view-model age refresh, automation hub) need the
// same clamped, floor-rounded seconds-since-timestamp value, plus a short-circuit
// for already-reported ages from upstream telemetry sources.
internal static class TelemetryAgeHelper
{
    public static int? ComputeAgeSeconds(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (!timestampUtc.HasValue)
        {
            return null;
        }

        var age = nowUtc - timestampUtc.Value;
        return age < TimeSpan.Zero ? 0 : (int)Math.Floor(age.TotalSeconds);
    }

    public static int? ComputeAgeSeconds(int? reportedAgeSeconds, DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (reportedAgeSeconds.HasValue)
        {
            return Math.Max(0, reportedAgeSeconds.Value);
        }

        return ComputeAgeSeconds(timestampUtc, nowUtc);
    }
}

// Win32 WNDPROC subclass that enforces a minimum window size by intercepting
// WM_GETMINMAXINFO. WinUI 3 doesn't expose this without dropping to interop,
// and both the main window and the stats window need the same boilerplate.
internal static class MinSizeWindowSubclass
{
    public static MinSizeHandle Install(IntPtr hwnd, int minWidthDip, int minHeightDip)
    {
        var handle = new MinSizeHandle(minWidthDip, minHeightDip);
        handle.OriginalWndProc = SetWindowLongPtr(
            hwnd,
            GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(handle.Delegate));
        return handle;
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Held by the caller for the lifetime of the window; the GC must not
    // collect the delegate while Win32 still holds the function pointer.
    public sealed class MinSizeHandle
    {
        private readonly int _minWidthDip;
        private readonly int _minHeightDip;

        public MinSizeHandle(int minWidthDip, int minHeightDip)
        {
            _minWidthDip = minWidthDip;
            _minHeightDip = minHeightDip;
            Delegate = HandleMessage;
        }

        public WndProcDelegate Delegate { get; }
        public IntPtr OriginalWndProc { get; set; }

        private IntPtr HandleMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var dpi = GetDpiForWindow(hWnd);
                var scale = dpi / 96.0;
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                mmi.ptMinTrackSize.X = (int)(_minWidthDip * scale);
                mmi.ptMinTrackSize.Y = (int)(_minHeightDip * scale);
                Marshal.StructureToPtr(mmi, lParam, false);
            }

            return CallWindowProc(OriginalWndProc, hWnd, msg, wParam, lParam);
        }
    }

    private const int GWLP_WNDPROC = -4;
    private const uint WM_GETMINMAXINFO = 0x0024;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

#pragma warning disable CS0649 // Populated by Marshal.PtrToStructure for WM_GETMINMAXINFO.
    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
#pragma warning restore CS0649

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}

// Process execution contract used by verifiers and tooling wrappers. It keeps
// timeout, stdout/stderr capture, start failure, and priority behavior uniform.
public sealed class ProcessSpec
{
    public required string FileName { get; init; }
    public string Arguments { get; init; } = string.Empty;
    public string? WorkingDirectory { get; init; }
    public int TimeoutMs { get; init; } = 30_000;
    public ProcessPriorityClass? PriorityClass { get; init; }
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
    public Exception? StdOutReadException { get; init; }
    public Exception? StdErrReadException { get; init; }

    internal Exception? GetOutputReadFailure()
    {
        static IOException Describe(string stream, Exception cause)
            => new($"{stream} read failed ({cause.GetType().Name}: {cause.Message})", cause);

        var stdoutFailure = StdOutReadException is { } stdout ? Describe("stdout", stdout) : null;
        var stderrFailure = StdErrReadException is { } stderr ? Describe("stderr", stderr) : null;
        if (stdoutFailure != null && stderrFailure != null)
        {
            return new AggregateException("Process output reads failed.", stdoutFailure, stderrFailure);
        }

        return stdoutFailure ?? stderrFailure;
    }
}

public interface IProcessSupervisor
{
    Task<ProcessRunResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default);
}

// Small supervised process runner. It is deliberately conservative: no shell,
// bounded waits, and explicit timeout reporting for diagnostics.
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
            if (process != null && spec.PriorityClass.HasValue)
            {
                TrySetPriorityClass(process, spec.FileName, spec.PriorityClass.Value);
            }
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
                try
                {
                    await exitTask.WaitAsync(TimeSpan.FromMilliseconds(spec.TimeoutMs), cancellationToken);
                }
                catch (TimeoutException)
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
                : (Output: string.Empty, ReadException: (Exception?)null);
            var stderr = canReadOutputs
                ? await TryReadWithTimeoutAsync(stderrTask, outputReadTimeoutMs)
                : (Output: string.Empty, ReadException: (Exception?)null);

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
                StdOut = stdout.Output,
                StdErr = stderr.Output,
                StdOutReadException = stdout.ReadException,
                StdErrReadException = stderr.ReadException
            };
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (!HasExitedSafely(process))
        {
            // An already-exited process is the expected race and stays quiet; a live
            // one that resisted termination can hold handles and is worth recording.
            Logger.Log($"PROCESS_KILL_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        }
        catch (Exception ex)
        {
            // The process exited between the kill attempt and the liveness check.
            Logger.Log($"PROCESS_KILL_RACE_EXITED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static bool HasExitedSafely(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Exception)
        {
            // Treat an unreadable exit state as exited so the filter stays quiet.
            return true;
        }
    }

    private static void TrySetPriorityClass(Process process, string fileName, ProcessPriorityClass priorityClass)
    {
        try
        {
            process.PriorityClass = priorityClass;
            Logger.LogEvent("CAP-PROC-PRIORITY", $"{fileName} priority={priorityClass}");
        }
        catch (Exception ex)
        {
            Logger.LogEvent("CAP-PROC-PRIORITY-FAIL", $"{fileName} priority={priorityClass} error={ex.GetType().Name}:{ex.Message}");
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

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task<(string Output, Exception? ReadException)> TryReadWithTimeoutAsync(Task<string> readTask, int timeoutMs)
    {
        try
        {
            return (await readTask.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)), null);
        }
        catch (Exception ex)
        {
            return (string.Empty, ex);
        }
    }
}

// Best-effort MMCSS registration wrapper for timing-sensitive worker threads.
// Failure is logged but not fatal so the app still runs on systems without AVRT.
internal sealed class MmcssThreadRegistration : IDisposable
{
    private IntPtr _handle;

    private MmcssThreadRegistration(IntPtr handle)
    {
        _handle = handle;
    }

    public static MmcssThreadRegistration? TryRegister(string taskName, int priority, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return null;
        }

        try
        {
            var handle = AvSetMmThreadCharacteristics(taskName, out _);
            if (handle == IntPtr.Zero)
            {
                log?.Invoke($"MMCSS registration failed task={taskName} lastError={Marshal.GetLastWin32Error()}");
                return null;
            }

            var clampedPriority = (AvrtPriority)Math.Clamp(priority, -2, 2);
            if (!AvSetMmThreadPriority(handle, clampedPriority))
            {
                log?.Invoke($"MMCSS priority set failed task={taskName} priority={priority} lastError={Marshal.GetLastWin32Error()}");
            }

            log?.Invoke($"MMCSS registered task={taskName} priority={(int)clampedPriority}");
            return new MmcssThreadRegistration(handle);
        }
        catch (DllNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: avrt.dll not found.");
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: AVRT entry point not found.");
            return null;
        }
        catch (Exception ex)
        {
            log?.Invoke($"MMCSS registration failed type={ex.GetType().Name} msg={ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        var handle = _handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _handle = IntPtr.Zero;
        AvRevertMmThreadCharacteristics(handle);
    }

    private enum AvrtPriority
    {
        VeryLow = -2,
        Low = -1,
        Normal = 0,
        High = 1,
        Critical = 2
    }

    [DllImport("avrt.dll", EntryPoint = "AvSetMmThreadCharacteristicsW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr AvSetMmThreadCharacteristics(string taskName, out int taskIndex);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvSetMmThreadPriority(IntPtr avrtHandle, AvrtPriority priority);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvRevertMmThreadCharacteristics(IntPtr avrtHandle);
}
