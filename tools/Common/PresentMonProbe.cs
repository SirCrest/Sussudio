using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Sussudio.Tools;

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

// Runs PresentMon and reduces the CSV into frame-pacing metrics that complement
// the app's internal D3D/cadence counters.
public static class PresentMonProbe
{
    private static readonly string[] CandidateExeNames =
    [
        "PresentMon.exe",
        "PresentMon-2.4.1-x64.exe",
        "PresentMon-2.3.1-x64.exe",
        "PresentMon-2.3.0-x64.exe"
    ];

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

    private static PresentMonProbeResult Error(string message)
        => new() { Success = false, Message = message };

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
        catch
        {
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
        catch
        {
        }
    }

    private static PresentMonCaptureSummary ParseCsv(string path)
        => ParseCsv(path, expectedSwapChainAddress: null);

    private static PresentMonCaptureSummary ParseCsv(string path, string? expectedSwapChainAddress)
        => ParseCsv(path, expectedSwapChainAddress, options: null, captureStartUtcUnixMs: null);

    private static PresentMonCaptureSummary ParseCsv(
        string path,
        string? expectedSwapChainAddress,
        PresentMonProbeOptions? options,
        long? captureStartUtcUnixMs)
    {
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new PresentMonCaptureSummary();
        }

        var headers = SplitCsvLine(headerLine);
        var index = headers
            .Select((name, i) => (Name: NormalizeHeader(name), Index: i))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
        var displayedTimeColumnPresent = HasAnyColumn(index, "DisplayedTime");
        var displayChangeColumnPresent = HasAnyColumn(index, "MsBetweenDisplayChange");

        var rows = new List<PresentMonRow>();

        string? line;
        var rowIndex = 0;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            rows.Add(new PresentMonRow(
                RowIndex: rowIndex++,
                SwapChainAddress: NormalizeSwapChainAddress(ReadField(fields, index, "SwapChainAddress")) ?? string.Empty,
                PresentMode: ReadField(fields, index, "PresentMode"),
                PresentRuntime: ReadField(fields, index, "PresentRuntime"),
                SyncInterval: ReadField(fields, index, "SyncInterval"),
                AllowsTearing: ReadField(fields, index, "AllowsTearing"),
                CpuStartTimeMs: ReadMetric(fields, index, "CPUStartTime"),
                BetweenPresentsMs: ReadMetric(fields, index, "MsBetweenPresents", "FrameTime"),
                BetweenDisplayChangeMs: ReadMetric(fields, index, "MsBetweenDisplayChange"),
                DisplayedTimeMs: ReadMetric(fields, index, "DisplayedTime"),
                UntilDisplayedMs: ReadMetric(fields, index, "MsUntilDisplayed"),
                InPresentApiMs: ReadMetric(fields, index, "MsInPresentAPI"),
                CpuBusyMs: ReadMetric(fields, index, "MsCPUBusy", "CPUBusy"),
                GpuBusyMs: ReadMetric(fields, index, "MsGPUBusy", "GPUBusy"),
                GpuTimeMs: ReadMetric(fields, index, "MsGPUTime", "GPUTime"),
                DisplayLatencyMs: ReadMetric(fields, index, "DisplayLatency")));
        }

        var normalizedExpectedSwapChain = NormalizeSwapChainAddress(expectedSwapChainAddress);
        var selectedSwapChain = SelectPrimarySwapChain(rows, normalizedExpectedSwapChain);
        var expectedSwapChainMatched = !string.IsNullOrWhiteSpace(normalizedExpectedSwapChain) &&
                                       string.Equals(selectedSwapChain, normalizedExpectedSwapChain, StringComparison.OrdinalIgnoreCase);
        var selectedRows = selectedSwapChain == null
            ? new List<PresentMonRow>()
            : rows.Where(row => string.Equals(row.SwapChainAddress, selectedSwapChain, StringComparison.OrdinalIgnoreCase)).ToList();
        var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);
        var notDisplayed = displayedTimeColumnPresent
            ? selectedRows.Count(row => !row.DisplayedTimeMs.HasValue)
            : 0;
        var displayChangeUnavailable = displayChangeColumnPresent
            ? selectedRows.Count(row => !row.BetweenDisplayChangeMs.HasValue)
            : 0;
        var warnings = BuildWarnings(
            rows,
            selectedRows,
            swapChains,
            displayedTimeColumnPresent,
            displayChangeColumnPresent,
            normalizedExpectedSwapChain,
            expectedSwapChainMatched);
        var appCorrelation = BuildAppCorrelation(selectedRows, options, captureStartUtcUnixMs);

        return new PresentMonCaptureSummary
        {
            SampleCount = selectedRows.Count,
            RawSampleCount = rows.Count,
            ExcludedSampleCount = Math.Max(0, rows.Count - selectedRows.Count),
            ExpectedSwapChainAddress = normalizedExpectedSwapChain,
            SelectedSwapChainAddress = selectedSwapChain,
            ExpectedSwapChainMatched = expectedSwapChainMatched,
            BetweenPresentsMs = Summarize(selectedRows.Select(row => row.BetweenPresentsMs)),
            BetweenDisplayChangeMs = Summarize(selectedRows.Select(row => row.BetweenDisplayChangeMs)),
            DisplayedTimeMs = Summarize(selectedRows.Select(row => row.DisplayedTimeMs)),
            UntilDisplayedMs = Summarize(selectedRows.Select(row => row.UntilDisplayedMs)),
            InPresentApiMs = Summarize(selectedRows.Select(row => row.InPresentApiMs)),
            CpuBusyMs = Summarize(selectedRows.Select(row => row.CpuBusyMs)),
            GpuBusyMs = Summarize(selectedRows.Select(row => row.GpuBusyMs)),
            GpuTimeMs = Summarize(selectedRows.Select(row => row.GpuTimeMs)),
            DisplayLatencyMs = Summarize(selectedRows.Select(row => row.DisplayLatencyMs)),
            NotDisplayedFrameCount = notDisplayed,
            NotDisplayedFramePercent = selectedRows.Count <= 0 ? 0 : notDisplayed * 100.0 / selectedRows.Count,
            DisplayedTimeColumnPresent = displayedTimeColumnPresent,
            DisplayChangeUnavailableCount = displayChangeUnavailable,
            DisplayChangeUnavailablePercent = selectedRows.Count <= 0 ? 0 : displayChangeUnavailable * 100.0 / selectedRows.Count,
            PresentModes = CountValues(selectedRows.Select(row => row.PresentMode)),
            PresentRuntimes = CountValues(selectedRows.Select(row => row.PresentRuntime)),
            SyncIntervals = CountValues(selectedRows.Select(row => row.SyncInterval)),
            AllowsTearing = CountValues(selectedRows.Select(row => row.AllowsTearing)),
            SwapChains = swapChains,
            AppCorrelation = appCorrelation,
            Warnings = warnings
        };
    }

    private static PresentMonAppCorrelation BuildAppCorrelation(
        IReadOnlyList<PresentMonRow> selectedRows,
        PresentMonProbeOptions? options,
        long? captureStartUtcUnixMs)
    {
        if (options?.AppPresentUtcUnixMs is not long appPresentUtcUnixMs || appPresentUtcUnixMs <= 0)
        {
            return new PresentMonAppCorrelation();
        }

        var startUtcUnixMs = options.CaptureStartUtcUnixMs ?? captureStartUtcUnixMs;
        if (!startUtcUnixMs.HasValue || startUtcUnixMs.Value <= 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Capture start timestamp was unavailable.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs
            };
        }

        var appOffsetMs = appPresentUtcUnixMs - startUtcUnixMs.Value;
        var candidates = selectedRows
            .Where(row => row.CpuStartTimeMs.HasValue)
            .Select(row => new
            {
                Row = row,
                DeltaMs = Math.Abs(row.CpuStartTimeMs!.Value - appOffsetMs)
            })
            .OrderBy(candidate => candidate.DeltaMs)
            .ToList();

        if (candidates.Count == 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "No selected PresentMon rows exposed CPUStartTime.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs
            };
        }

        var best = candidates[0];
        if (best.DeltaMs > 50.0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Nearest PresentMon row was outside the 50ms app-present correlation window.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs,
                PresentMonRowIndex = best.Row.RowIndex,
                PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
                DeltaMs = best.DeltaMs,
                Outcome = ClassifyPresentOutcome(best.Row),
                PresentMode = best.Row.PresentMode,
                UntilDisplayedMs = best.Row.UntilDisplayedMs,
                DisplayLatencyMs = best.Row.DisplayLatencyMs
            };
        }

        return new PresentMonAppCorrelation
        {
            Available = true,
            Reason = "Nearest selected PresentMon row by app UTC present timestamp.",
            AppPresentId = options.AppPresentId ?? 0,
            AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            AppPresentOffsetMs = appOffsetMs,
            PresentMonRowIndex = best.Row.RowIndex,
            PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
            DeltaMs = best.DeltaMs,
            Outcome = ClassifyPresentOutcome(best.Row),
            PresentMode = best.Row.PresentMode,
            UntilDisplayedMs = best.Row.UntilDisplayedMs,
            DisplayLatencyMs = best.Row.DisplayLatencyMs
        };
    }

    private static string ClassifyPresentOutcome(PresentMonRow row)
    {
        if (!row.DisplayedTimeMs.HasValue)
        {
            return "SupersededOrNotDisplayed";
        }

        if (row.UntilDisplayedMs.GetValueOrDefault() >= 16.0)
        {
            return "DisplayedLate";
        }

        return "Displayed";
    }

    private static string NormalizeHeader(string value)
        => value.Trim().TrimStart('\uFEFF');

    private static bool HasAnyColumn(IReadOnlyDictionary<string, int> index, params string[] names)
        => names.Any(index.ContainsKey);

    private static double? ReadMetric(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        params string[] names)
    {
        var found = false;
        var fieldIndex = -1;
        foreach (var name in names)
        {
            if (index.TryGetValue(name, out fieldIndex))
            {
                found = true;
                break;
            }
        }

        if (!found || fieldIndex >= fields.Count)
        {
            return null;
        }

        var field = fields[fieldIndex].Trim();
        if (field.Length == 0 || string.Equals(field, "NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !double.IsNaN(value) &&
            !double.IsInfinity(value))
        {
            return value;
        }

        return null;
    }

    private static string ReadField(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        string name)
    {
        if (!index.TryGetValue(name, out var fieldIndex) || fieldIndex >= fields.Count)
        {
            return string.Empty;
        }

        return fields[fieldIndex].Trim();
    }

    private static string? SelectPrimarySwapChain(IReadOnlyList<PresentMonRow> rows, string? expectedSwapChainAddress)
    {
        if (!string.IsNullOrWhiteSpace(expectedSwapChainAddress))
        {
            return rows.Any(row => string.Equals(row.SwapChainAddress, expectedSwapChainAddress, StringComparison.OrdinalIgnoreCase))
                ? expectedSwapChainAddress
                : null;
        }

        var selected = rows
            .Where(row => !IsArtifactSwapChain(row.SwapChainAddress))
            .GroupBy(row => row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        return selected?.Key;
    }

    private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(
        IReadOnlyList<PresentMonRow> rows,
        string? selectedSwapChain)
    {
        return rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.SwapChainAddress) ? "(missing)" : row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var groupRows = group.ToArray();
                return new PresentMonSwapChainSummary
                {
                    Address = group.Key,
                    SampleCount = groupRows.Length,
                    Selected = string.Equals(group.Key, selectedSwapChain, StringComparison.OrdinalIgnoreCase),
                    Artifact = IsArtifactSwapChain(group.Key),
                    BetweenPresentsMs = Summarize(groupRows.Select(row => row.BetweenPresentsMs)),
                    BetweenDisplayChangeMs = Summarize(groupRows.Select(row => row.BetweenDisplayChangeMs)),
                    UntilDisplayedMs = Summarize(groupRows.Select(row => row.UntilDisplayedMs)),
                    PresentModes = CountValues(groupRows.Select(row => row.PresentMode))
                };
            })
            .OrderByDescending(item => item.Selected)
            .ThenByDescending(item => item.SampleCount)
            .ToArray();
    }

    private static bool IsArtifactSwapChain(string? swapChainAddress)
        => string.IsNullOrWhiteSpace(swapChainAddress) ||
           string.Equals(swapChainAddress.Trim(), "0x0", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeSwapChainAddress(string? swapChainAddress)
    {
        if (string.IsNullOrWhiteSpace(swapChainAddress))
        {
            return null;
        }

        var value = swapChainAddress.Trim();
        if (value.Equals("0x0", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var digits = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value;
        if (ulong.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric == 0 ? null : $"0x{numeric:X}";
        }

        return value.ToUpperInvariant();
    }

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<PresentMonRow> rawRows,
        IReadOnlyList<PresentMonRow> selectedRows,
        IReadOnlyList<PresentMonSwapChainSummary> swapChains,
        bool displayedTimeColumnPresent,
        bool displayChangeColumnPresent,
        string? expectedSwapChainAddress,
        bool expectedSwapChainMatched)
    {
        var warnings = new List<string>();
        var excludedRows = rawRows.Count - selectedRows.Count;
        if (excludedRows > 0)
        {
            warnings.Add($"Excluded {excludedRows} non-selected PresentMon row(s), usually secondary or artifact swap-chain events.");
        }

        if (!string.IsNullOrWhiteSpace(expectedSwapChainAddress) && !expectedSwapChainMatched)
        {
            warnings.Add($"Expected swap chain {expectedSwapChainAddress} was not present; no fallback swap chain was selected.");
        }
        else if (selectedRows.Count == 0)
        {
            warnings.Add("No non-artifact swap-chain rows were found; preview pacing metrics are unavailable.");
        }

        if (!displayedTimeColumnPresent)
        {
            warnings.Add("DisplayedTime column is absent in this PresentMon schema; NotDisplayedFrameCount is unavailable.");
        }

        if (!displayChangeColumnPresent)
        {
            warnings.Add("MsBetweenDisplayChange column is absent; display-change pacing is unavailable.");
        }

        if (swapChains.Count > 1 && string.IsNullOrWhiteSpace(expectedSwapChainAddress))
        {
            warnings.Add("Multiple swap-chain addresses were present; summary uses the dominant nonzero swap chain.");
        }

        return warnings;
    }

    private static IReadOnlyDictionary<string, int> CountValues(IEnumerable<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in values.Select(value => value.Trim()).Where(value => value.Length > 0))
        {
            counts.TryGetValue(field, out var count);
            counts[field] = count + 1;
        }

        return counts;
    }

    private static PresentMonMetricSummary Summarize(IEnumerable<double?> values)
    {
        var sorted = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        if (sorted.Count == 0)
        {
            return new PresentMonMetricSummary();
        }

        sorted.Sort();
        return new PresentMonMetricSummary
        {
            SampleCount = sorted.Count,
            Average = sorted.Average(),
            P50 = Percentile(sorted, 0.50),
            P95 = Percentile(sorted, 0.95),
            P99 = Percentile(sorted, 0.99),
            Max = sorted[^1]
        };
    }

    private sealed record PresentMonRow(
        int RowIndex,
        string SwapChainAddress,
        string PresentMode,
        string PresentRuntime,
        string SyncInterval,
        string AllowsTearing,
        double? CpuStartTimeMs,
        double? BetweenPresentsMs,
        double? BetweenDisplayChangeMs,
        double? DisplayedTimeMs,
        double? UntilDisplayedMs,
        double? InPresentApiMs,
        double? CpuBusyMs,
        double? GpuBusyMs,
        double? GpuTimeMs,
        double? DisplayLatencyMs);

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var position = (sortedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return sortedValues[lower];
        }

        var fraction = position - lower;
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * fraction;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(ch);
            }
        }

        result.Add(builder.ToString());
        return result;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
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
