using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

// Runs PresentMon and reduces the CSV into frame-pacing metrics that complement
// the app's internal D3D/cadence counters.
public static partial class PresentMonProbe
{
    private static readonly string[] CandidateExeNames =
    [
        "PresentMon.exe",
        "PresentMon-2.4.1-x64.exe",
        "PresentMon-2.3.1-x64.exe",
        "PresentMon-2.3.0-x64.exe"
    ];

    public static PresentMonProbeOptions CreateOptions(
        int durationSeconds,
        int? processId = null,
        string? processName = null,
        string? swapChainAddress = null,
        long? appPresentId = null,
        long? appSourceSequenceNumber = null,
        long? appPresentUtcUnixMs = null,
        long? captureStartUtcUnixMs = null,
        string? presentMonPath = null,
        string? outputFile = null,
        bool keepCsv = false,
        bool trackGpuVideo = true,
        PresentMonProbeCorrelation correlation = default)
    {
        return new PresentMonProbeOptions
        {
            ProcessId = processId,
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Sussudio" : processName,
            DurationSeconds = durationSeconds,
            PresentMonPath = presentMonPath,
            OutputFile = outputFile,
            ExpectedSwapChainAddress = string.IsNullOrWhiteSpace(swapChainAddress)
                ? correlation.SwapChainAddress
                : swapChainAddress,
            AppPresentId = appPresentId ?? correlation.PresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber ?? correlation.SourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs ?? correlation.PresentUtcUnixMs,
            CaptureStartUtcUnixMs = captureStartUtcUnixMs,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        };
    }

    public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)
    {
        if (snapshot.ValueKind is not JsonValueKind.Object)
        {
            return default;
        }

        var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
        return new PresentMonProbeCorrelation(
            string.IsNullOrWhiteSpace(address) ? null : address,
            GetPositiveLong(snapshot, "PreviewD3DLastRenderedPreviewPresentId"),
            GetNonNegativeLong(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
            GetPositiveLong(snapshot, "PreviewD3DLastRenderedUtcUnixMs"));
    }

    public static async Task<PresentMonProbeResult> RunAsync(
        PresentMonProbeOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!OperatingSystem.IsWindows())
        {
            return Error("PresentMon capture is only supported on Windows.");
        }

        var durationSeconds = Math.Clamp(options.DurationSeconds, 1, 300);
        var targetProcess = ResolveTargetProcess(options);
        if (targetProcess == null)
        {
            return Error($"No running process matched pid={options.ProcessId?.ToString(CultureInfo.InvariantCulture) ?? "(none)"} name='{options.ProcessName}'.");
        }

        var presentMonPath = ResolvePresentMonPath(options.PresentMonPath);
        if (presentMonPath == null)
        {
            return Error(
                "PresentMon console executable was not found. Set SUSSUDIO_PRESENTMON_PATH or PRESENTMON_PATH, " +
                "or place PresentMon.exe / PresentMon-*-x64.exe under tools\\PresentMon.");
        }

        var outputPath = ResolveOutputPath(options.OutputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        var arguments = BuildArguments(targetProcess.Id, durationSeconds, outputPath, options.TrackGpuVideo);
        var captureStartUtcUnixMs = options.CaptureStartUtcUnixMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var run = await RunProcessAsync(
                presentMonPath,
                arguments,
                timeoutMs: Math.Max((durationSeconds + 15) * 1000, 30_000),
                cancellationToken)
            .ConfigureAwait(false);

        PresentMonCaptureSummary? summary = null;
        var parseMessage = string.Empty;
        if (File.Exists(outputPath))
        {
            try
            {
                summary = ParseCsv(outputPath, options.ExpectedSwapChainAddress, options, captureStartUtcUnixMs);
            }
            catch (Exception ex)
            {
                parseMessage = $" CSV parse failed: {ex.Message}";
            }
        }

        if (!options.KeepCsv && File.Exists(outputPath))
        {
            TryDelete(outputPath);
        }

        var success = run.ExitCode == 0 && summary is { SampleCount: > 0 };
        var message = BuildResultMessage(run, summary, targetProcess, parseMessage, success);

        return new PresentMonProbeResult
        {
            Success = success,
            Message = message,
            PresentMonPath = presentMonPath,
            TargetProcessId = targetProcess.Id,
            TargetProcessName = targetProcess.ProcessName,
            CsvPath = options.KeepCsv ? outputPath : null,
            ExitCode = run.ExitCode,
            TimedOut = run.TimedOut,
            CommandLine = $"{QuoteArgument(presentMonPath)} {arguments}",
            StdOut = run.StdOut,
            StdErr = run.StdErr,
            Summary = summary
        };
    }

    private static string BuildArguments(int processId, int durationSeconds, string outputPath, bool trackGpuVideo)
    {
        var args = new List<string>
        {
            "--process_id",
            processId.ToString(CultureInfo.InvariantCulture),
            "--output_file",
            outputPath,
            "--timed",
            durationSeconds.ToString(CultureInfo.InvariantCulture),
            "--terminate_after_timed",
            "--stop_existing_session",
            "--session_name",
            $"SussudioPresentMon{Guid.NewGuid():N}",
            "--v2_metrics",
            "--no_console_stats"
        };

        if (trackGpuVideo)
        {
            args.Add("--track_gpu_video");
        }

        return string.Join(" ", args.Select(QuoteArgument));
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;

    private static string BuildResultMessage(
        ProcessRun run,
        PresentMonCaptureSummary? summary,
        Process targetProcess,
        string parseMessage,
        bool success)
    {
        if (success && summary != null)
        {
            return $"Captured {summary.RawSampleCount} PresentMon frame rows for {targetProcess.ProcessName} ({targetProcess.Id}); selected {summary.SampleCount} rows from swap chain {summary.SelectedSwapChainAddress ?? "(none)"}.";
        }

        if (run.ExitCode == 0 &&
            summary != null &&
            !string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress) &&
            !summary.ExpectedSwapChainMatched)
        {
            return $"PresentMon captured {summary.RawSampleCount} frame rows for {targetProcess.ProcessName} ({targetProcess.Id}), but expected swap chain {summary.ExpectedSwapChainAddress} was not present.";
        }

        return $"PresentMon capture did not produce frame rows. exitCode={run.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "(none)"} timedOut={run.TimedOut}.{parseMessage}";
    }

    private static Process? ResolveTargetProcess(PresentMonProbeOptions options)
    {
        if (options.ProcessId.HasValue)
        {
            try
            {
                var process = Process.GetProcessById(options.ProcessId.Value);
                return process.HasExited ? null : process;
            }
            catch
            {
                return null;
            }
        }

        var name = string.IsNullOrWhiteSpace(options.ProcessName)
            ? "Sussudio"
            : Path.GetFileNameWithoutExtension(options.ProcessName.Trim());
        return Process.GetProcessesByName(name)
            .Where(process => !process.HasExited)
            .OrderByDescending(process =>
            {
                try
                {
                    return process.StartTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            })
            .FirstOrDefault();
    }

    private static string? ResolvePresentMonPath(string? explicitPath)
    {
        foreach (var candidate in EnumeratePresentMonCandidates(explicitPath))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumeratePresentMonCandidates(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath;
        }

        var envPath = Environment.GetEnvironmentVariable("SUSSUDIO_PRESENTMON_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath;
        }

        envPath = Environment.GetEnvironmentVariable("PRESENTMON_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath;
        }

        var baseDirectory = AppContext.BaseDirectory;
        foreach (var name in CandidateExeNames)
        {
            yield return Path.Combine(baseDirectory, "PresentMon", name);
            yield return Path.Combine(baseDirectory, "tools", "PresentMon", name);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "tools", "PresentMon", name);
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var name in CandidateExeNames)
            {
                yield return Path.Combine(directory, name);
            }
        }
    }

    private static string ResolveOutputPath(string? outputFile)
        => string.IsNullOrWhiteSpace(outputFile)
            ? Path.Combine(Path.GetTempPath(), $"sussudio_presentmon_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.csv")
            : Path.GetFullPath(outputFile);

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

    private static PresentMonProbeResult Error(string message)
        => new() { Success = false, Message = message };

    private static long? GetPositiveLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, 0);
        return value > 0 ? value : null;
    }

    private static long? GetNonNegativeLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, -1);
        return value >= 0 ? value : null;
    }

    private sealed class ProcessRun
    {
        public int? ExitCode { get; init; }
        public bool TimedOut { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
    }

    public static string Format(PresentMonProbeResult result)
    {
        if (!result.Success || result.Summary == null)
        {
            var detail = new StringBuilder(result.Message);
            if (result.Summary != null)
            {
                AppendSummaryContext(detail, result.Summary);
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                detail.AppendLine();
                detail.AppendLine(result.StdErr.Trim());
            }

            return detail.ToString();
        }

        var summary = result.Summary;
        var builder = new StringBuilder();
        builder.AppendLine(result.Message);
        builder.AppendLine($"Target: {result.TargetProcessName} ({result.TargetProcessId})");
        builder.AppendLine($"PresentMon: {result.PresentMonPath}");
        if (!string.IsNullOrWhiteSpace(summary.SelectedSwapChainAddress))
        {
            builder.AppendLine(
                $"Selected Swap Chain: {summary.SelectedSwapChainAddress} ({summary.SampleCount}/{summary.RawSampleCount} rows, excluded={summary.ExcludedSampleCount})");
        }
        if (!string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress))
        {
            builder.AppendLine(
                $"Expected Swap Chain: {summary.ExpectedSwapChainAddress} matched={summary.ExpectedSwapChainMatched}");
        }

        if (!string.IsNullOrWhiteSpace(result.CsvPath))
        {
            builder.AppendLine($"CSV: {result.CsvPath}");
        }

        foreach (var warning in summary.Warnings)
        {
            builder.AppendLine($"Warning: {warning}");
        }

        AppendMetric(builder, "Between Presents", summary.BetweenPresentsMs);
        AppendMetric(builder, "Display Change", summary.BetweenDisplayChangeMs);
        AppendMetric(builder, "Displayed Time", summary.DisplayedTimeMs);
        AppendMetric(builder, "Until Displayed", summary.UntilDisplayedMs);
        AppendMetric(builder, "In Present API", summary.InPresentApiMs);
        AppendMetric(builder, "CPU Busy", summary.CpuBusyMs);
        AppendMetric(builder, "GPU Busy", summary.GpuBusyMs);
        AppendMetric(builder, "GPU Time", summary.GpuTimeMs);
        AppendMetric(builder, "Display Latency", summary.DisplayLatencyMs);
        if (summary.DisplayedTimeColumnPresent)
        {
            builder.AppendLine($"Not Displayed: {summary.NotDisplayedFrameCount}/{summary.SampleCount} ({summary.NotDisplayedFramePercent:0.##}%)");
        }

        if (summary.DisplayChangeUnavailableCount > 0)
        {
            builder.AppendLine($"Display Change Unavailable: {summary.DisplayChangeUnavailableCount}/{summary.SampleCount} ({summary.DisplayChangeUnavailablePercent:0.##}%)");
        }

        AppendAppCorrelation(builder, summary.AppCorrelation);
        AppendCounts(builder, "Present Modes", summary.PresentModes);
        AppendCounts(builder, "Present Runtimes", summary.PresentRuntimes);
        AppendCounts(builder, "Sync Intervals", summary.SyncIntervals);
        AppendCounts(builder, "Allows Tearing", summary.AllowsTearing);
        AppendSwapChains(builder, summary.SwapChains);
        return builder.ToString().TrimEnd();
    }

    private static void AppendSummaryContext(StringBuilder builder, PresentMonCaptureSummary summary)
    {
        if (!string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress))
        {
            builder.AppendLine();
            builder.AppendLine($"Expected Swap Chain: {summary.ExpectedSwapChainAddress} matched={summary.ExpectedSwapChainMatched}");
        }

        if (!string.IsNullOrWhiteSpace(summary.SelectedSwapChainAddress))
        {
            builder.AppendLine($"Selected Swap Chain: {summary.SelectedSwapChainAddress} ({summary.SampleCount}/{summary.RawSampleCount} rows, excluded={summary.ExcludedSampleCount})");
        }

        foreach (var warning in summary.Warnings)
        {
            builder.AppendLine($"Warning: {warning}");
        }
    }

    private static void AppendMetric(StringBuilder builder, string label, PresentMonMetricSummary metric)
    {
        if (metric.SampleCount <= 0)
        {
            return;
        }

        builder.AppendLine(
            $"{label}: avg={metric.Average:0.###}ms p50={metric.P50:0.###}ms p95={metric.P95:0.###}ms p99={metric.P99:0.###}ms max={metric.Max:0.###}ms n={metric.SampleCount}");
    }

    private static void AppendAppCorrelation(StringBuilder builder, PresentMonAppCorrelation correlation)
    {
        if (!correlation.Available)
        {
            return;
        }

        builder.AppendLine(
            $"App Correlation: appPresent={correlation.AppPresentId} sourceSeq={correlation.AppSourceSequenceNumber} " +
            $"row={correlation.PresentMonRowIndex} delta={correlation.DeltaMs:0.###}ms outcome={correlation.Outcome} " +
            $"mode={correlation.PresentMode} untilDisplayed={FormatOptionalMs(correlation.UntilDisplayedMs)} displayLatency={FormatOptionalMs(correlation.DisplayLatencyMs)}");
    }

    private static string FormatOptionalMs(double? value)
        => value.HasValue ? $"{value.Value:0.###}ms" : "N/A";

    private static void AppendCounts(StringBuilder builder, string label, IReadOnlyDictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{label}: {string.Join(", ", counts.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key}={pair.Value}"))}");
    }

    private static void AppendSwapChains(StringBuilder builder, IReadOnlyList<PresentMonSwapChainSummary> swapChains)
    {
        if (swapChains.Count <= 1)
        {
            return;
        }

        builder.AppendLine("Swap Chains:");
        foreach (var swapChain in swapChains.OrderByDescending(item => item.Selected).ThenByDescending(item => item.SampleCount))
        {
            var marker = swapChain.Selected ? "*" : " ";
            var artifact = swapChain.Artifact ? " artifact" : string.Empty;
            builder.AppendLine(
                $"  {marker} {swapChain.Address}: rows={swapChain.SampleCount}{artifact} " +
                $"present_p95={swapChain.BetweenPresentsMs.P95:0.###}ms display_p95={swapChain.BetweenDisplayChangeMs.P95:0.###}ms");
        }
    }
}

public readonly record struct PresentMonProbeCorrelation(
    string? SwapChainAddress,
    long? PresentId,
    long? SourceSequenceNumber,
    long? PresentUtcUnixMs);

// Inputs for a short PresentMon capture. App-side present IDs and timestamps
// are optional correlation anchors used to connect Sussudio renderer telemetry
// with the OS-level present stream.
public sealed class PresentMonProbeOptions
{
    public int? ProcessId { get; init; }
    public string ProcessName { get; init; } = "Sussudio";
    public int DurationSeconds { get; init; } = 10;
    public string? PresentMonPath { get; init; }
    public string? OutputFile { get; init; }
    public string? ExpectedSwapChainAddress { get; init; }
    public long? AppPresentId { get; init; }
    public long? AppSourceSequenceNumber { get; init; }
    public long? AppPresentUtcUnixMs { get; init; }
    public long? CaptureStartUtcUnixMs { get; init; }
    public bool KeepCsv { get; init; }
    public bool TrackGpuVideo { get; init; } = true;
}

// Raw process result plus parsed presentation metrics. Callers should inspect
// Success/Message first, then Summary when PresentMon produced usable CSV.
public sealed class PresentMonProbeResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? PresentMonPath { get; init; }
    public int? TargetProcessId { get; init; }
    public string? TargetProcessName { get; init; }
    public string? CsvPath { get; init; }
    public int? ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public string CommandLine { get; init; } = string.Empty;
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public PresentMonCaptureSummary? Summary { get; init; }
}

// Aggregated view of one PresentMon run. The selected swap chain is either the
// expected renderer address from the app snapshot or the best non-artifact
// candidate when the address is unknown.
public sealed class PresentMonCaptureSummary
{
    public int SampleCount { get; init; }
    public int RawSampleCount { get; init; }
    public int ExcludedSampleCount { get; init; }
    public string? ExpectedSwapChainAddress { get; init; }
    public string? SelectedSwapChainAddress { get; init; }
    public bool ExpectedSwapChainMatched { get; init; }
    public PresentMonMetricSummary BetweenPresentsMs { get; init; } = new();
    public PresentMonMetricSummary BetweenDisplayChangeMs { get; init; } = new();
    public PresentMonMetricSummary DisplayedTimeMs { get; init; } = new();
    public PresentMonMetricSummary UntilDisplayedMs { get; init; } = new();
    public PresentMonMetricSummary InPresentApiMs { get; init; } = new();
    public PresentMonMetricSummary CpuBusyMs { get; init; } = new();
    public PresentMonMetricSummary GpuBusyMs { get; init; } = new();
    public PresentMonMetricSummary GpuTimeMs { get; init; } = new();
    public PresentMonMetricSummary DisplayLatencyMs { get; init; } = new();
    public int NotDisplayedFrameCount { get; init; }
    public double NotDisplayedFramePercent { get; init; }
    public IReadOnlyDictionary<string, int> PresentModes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> PresentRuntimes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> SyncIntervals { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> AllowsTearing { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<PresentMonSwapChainSummary> SwapChains { get; init; } = Array.Empty<PresentMonSwapChainSummary>();
    public PresentMonAppCorrelation AppCorrelation { get; init; } = new();
    public bool DisplayedTimeColumnPresent { get; init; }
    public int DisplayChangeUnavailableCount { get; init; }
    public double DisplayChangeUnavailablePercent { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class PresentMonAppCorrelation
{
    public bool Available { get; init; }
    public string Reason { get; init; } = "No app present timestamp was supplied.";
    public long AppPresentId { get; init; }
    public long AppSourceSequenceNumber { get; init; }
    public long AppPresentUtcUnixMs { get; init; }
    public double AppPresentOffsetMs { get; init; }
    public int PresentMonRowIndex { get; init; } = -1;
    public double PresentMonCpuStartTimeMs { get; init; }
    public double DeltaMs { get; init; }
    public string Outcome { get; init; } = "Unknown";
    public string PresentMode { get; init; } = string.Empty;
    public double? UntilDisplayedMs { get; init; }
    public double? DisplayLatencyMs { get; init; }
}

public sealed class PresentMonSwapChainSummary
{
    public string Address { get; init; } = string.Empty;
    public int SampleCount { get; init; }
    public bool Selected { get; init; }
    public bool Artifact { get; init; }
    public PresentMonMetricSummary BetweenPresentsMs { get; init; } = new();
    public PresentMonMetricSummary BetweenDisplayChangeMs { get; init; } = new();
    public PresentMonMetricSummary UntilDisplayedMs { get; init; } = new();
    public IReadOnlyDictionary<string, int> PresentModes { get; init; } = new Dictionary<string, int>();
}

public sealed class PresentMonMetricSummary
{
    public int SampleCount { get; init; }
    public double Average { get; init; }
    public double P50 { get; init; }
    public double P95 { get; init; }
    public double P99 { get; init; }
    public double Max { get; init; }
}
