using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools;

// Runs PresentMon and reduces the CSV into frame-pacing metrics that complement
// the app's internal D3D/cadence counters.
public static partial class PresentMonProbe
{
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
}

public readonly record struct PresentMonProbeCorrelation(
    string? SwapChainAddress,
    long? PresentId,
    long? SourceSequenceNumber,
    long? PresentUtcUnixMs);
