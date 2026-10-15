using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sussudio.Tools.Performance;

namespace McpServer.Tools;

[McpServerToolType]
// Tool-side performance analysis (docs/architecture/performance-telemetry.md section 7). Neither tool is an app command:
// analyze_frame_trace reads a dump file on this machine, and run_perf_experiment drives the existing knob, preview,
// recording and performance-window commands through the shared runner in tools/Common/Performance. ssctl exposes the
// same two operations as `perf trace analyze` and `perf experiment`.
public static class PerfAnalysisTools
{
    private const int MaxTopFrames = 1000;

    // One experiment at a time per MCP server: two runs would fight over the same knobs and the same preview.
    private static readonly SemaphoreSlim ExperimentGate = new(1, 1);

    [McpServerTool(UseStructuredContent = true), Description("Analyze a frame trace dump (a file written by frame_trace dump) offline and report where frames spent their time: per-stage latency from frame arrival (p50/p99/max), derived latencies (arrival to present, source sample time to visible), the slowest frames with a per-stage breakdown, present-cadence hitches each with a named likely cause (an overlapping GC pause or marker, the stage that grew most, or a late arrival), events outside frames (GC, UI stalls, automation), and drops counted per reason. Needs no running app. Times are in milliseconds.")]
    public static Task<CallToolResult> analyze_frame_trace(
        [Description("Path of the trace dump (Chrome Trace Event JSON from frame_trace dump).")] string path,
        [Description("How many of the slowest frames to list with their stage breakdown (1 to 1000, default 10).")] int? topFrames = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(McpToolResultFactory.FromText("path is required.", isError: true));
        }

        if (topFrames is < 1 or > MaxTopFrames)
        {
            return Task.FromResult(McpToolResultFactory.FromText($"topFrames must be between 1 and {MaxTopFrames}.", isError: true));
        }

        try
        {
            var options = topFrames is { } count ? new FrameTraceAnalysisOptions { TopFrames = count } : null;
            var analysis = FrameTraceAnalyzer.AnalyzeFile(Path.GetFullPath(path), options);
            return Task.FromResult(StructuredResult(analysis.ToJson(), isError: false));
        }
        catch (Exception ex) when (ex is FrameTraceFormatException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Task.FromResult(McpToolResultFactory.FromText(ex.Message, isError: true));
        }
    }

    [McpServerTool(UseStructuredContent = true), Description("Run a knob A/B experiment against the live app and return per-metric verdicts. The spec names variants (each a set of SUSSUDIO_* tuning knob values; the first variant is the baseline), warmup and measure seconds, and repeats. The runner validates the spec against the app first (unknown, ill-typed or out-of-range knobs, app-restart knobs and needed-but-missing recording are reported before anything changes), then runs the variants interleaved (A B, B A, A B ...), restarting the preview and Flashback where a knob requires it, and measures one performance window per run. A verdict is 'better' or 'worse' only when the variants' per-repeat ranges of the verdict statistic (default p99) are separated; touching or overlapping ranges are 'inconclusive'. The app is restored afterwards (every touched knob reset, recording stopped, preview running) and the result says whether that was verified. This call can take minutes (variants x repeats x (warmup + measure) plus restarts) and drives the preview and, if the spec asks, recording of the connected capture device. Give exactly one of specPath or specJson. Artifacts (result.json, summary.md, one window report per run) are written under artifacts/perf-experiments/<timestamp>-<name> unless outputDirectory is set; the result names the directory.")]
    public static async Task<CallToolResult> run_perf_experiment(
        PipeClient pipeClient,
        [Description("Path of an experiment spec JSON file (see scripts/performance/experiments/example-jitter-depth.json).")] string? specPath = null,
        [Description("The experiment spec as a JSON string, instead of specPath.")] string? specJson = null,
        [Description("Directory for the artifacts. Defaults to artifacts/perf-experiments/<timestamp>-<name> under the server's working directory.")] string? outputDirectory = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hasPath = !string.IsNullOrWhiteSpace(specPath);
        var hasJson = !string.IsNullOrWhiteSpace(specJson);
        if (hasPath == hasJson)
        {
            return McpToolResultFactory.FromText("Give exactly one of specPath or specJson.", isError: true);
        }

        PerfExperimentSpec spec;
        string? resolvedOutput;
        try
        {
            spec = hasPath ? PerfExperimentSpec.Load(Path.GetFullPath(specPath!)) : PerfExperimentSpec.Parse(specJson!);
            resolvedOutput = string.IsNullOrWhiteSpace(outputDirectory) ? null : Path.GetFullPath(outputDirectory);
        }
        catch (PerfExperimentSpecException ex)
        {
            return McpToolResultFactory.FromText(
                "Invalid experiment spec:" + Environment.NewLine + string.Join(Environment.NewLine, ex.Problems.Select(problem => "- " + problem)),
                isError: true);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return McpToolResultFactory.FromText(ex.Message, isError: true);
        }

        if (!await ExperimentGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return McpToolResultFactory.FromText("A perf experiment is already running in this MCP server; wait for it to finish.", isError: true);
        }

        try
        {
            var result = await PerfExperimentRunner.RunAsync(
                    spec,
                    (command, payload, responseTimeoutMs, commandToken) =>
                        pipeClient.SendCommandAsync(command, payload, responseTimeoutMs, commandToken),
                    new PerfExperimentOptions { OutputDirectory = resolvedOutput },
                    cancellationToken)
                .ConfigureAwait(false);
            return StructuredResult(result.ToJson(), isError: !result.Succeeded);
        }
        finally
        {
            ExperimentGate.Release();
        }
    }

    // The analyzer and the runner both serialize to a JSON object; return it as the text and as structured content,
    // the same way the app-backed perf tools return the app's Data payload.
    private static CallToolResult StructuredResult(string json, bool isError)
    {
        using var document = JsonDocument.Parse(json);
        var result = McpToolResultFactory.FromText(json, isError);
        result.StructuredContent = document.RootElement.Clone();
        return result;
    }
}
