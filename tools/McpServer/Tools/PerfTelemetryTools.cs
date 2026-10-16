using System.ComponentModel;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP projection over the in-app performance telemetry (docs/architecture/performance-telemetry.md section 3): latency
// histograms, measurement windows, the frame-trace flight recorder, tuning knobs, the experiment lease and the log tail.
// Every tool returns the app's structured payload unchanged; none of them interprets or recomputes a measurement.
public static class PerfTelemetryTools
{
    [McpServerTool(UseStructuredContent = true), Description("Get the performance report: per-stage latency histograms (count, rate, mean, p50/p90/p99/p99.9, max), counters, hot-thread CPU, process memory/CPU/IO, GC, DWM refresh, GPU engines and active tuning overrides. Without window it covers the whole process lifetime. With window it returns the delta since that window began (see perf_window). Metrics with no samples are omitted unless includeIdle is true. Times are in milliseconds.")]
    public static async Task<CallToolResult> get_perf_report(
        PipeClient pipeClient,
        [Description("Optional measurement window name started with perf_window begin; the report covers the interval since it began without closing it.")] string? window = null,
        [Description("Optional metric-name prefix, for example 'capture.' or 'preview.render'.")] string? filter = null,
        [Description("Include metrics that recorded no samples (default false).")] bool includeIdle = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(window))
        {
            payload[AutomationPayloadKeys.Window] = window;
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            payload[AutomationPayloadKeys.Filter] = filter;
        }

        if (includeIdle)
        {
            payload[AutomationPayloadKeys.IncludeIdle] = true;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.GetPerfReport, payload, "Performance report was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Manage named performance measurement windows (up to 16). begin starts or restarts a window; read returns its report without closing it; end returns the report and closes it; cancel discards it; list shows open windows. Pair begin and end around a scenario to measure exactly that interval.")]
    public static async Task<CallToolResult> perf_window(
        PipeClient pipeClient,
        [Description("begin, read, end, cancel or list.")] string action,
        [Description("Window name (1 to 64 characters). Required for every action except list.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Action] = action };
        if (!string.IsNullOrWhiteSpace(name))
        {
            payload[AutomationPayloadKeys.Name] = name;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.PerfWindow, payload, "Performance window data was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Add an annotation to the performance timeline, recorded with the time it was added. Markers appear in perf reports and in frame trace dumps, so a scenario step can be located afterwards.")]
    public static async Task<CallToolResult> add_perf_marker(
        PipeClient pipeClient,
        [Description("Marker text (long text is truncated).")] string text,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Text] = text };
        return await SendStructuredAsync(pipeClient, AutomationCommandKind.AddPerfMarker, payload, "Marker acknowledgement was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Control the frame trace flight recorder (off by default, no cost while off). start allocates the ring (capacity is rounded to a power of two between 4096 and 4194304 events); stop freezes it; status reports it; dump writes Chrome Trace Event JSON that Perfetto or chrome://tracing opens. Dump without outputPath writes to a temporary file and returns its path.")]
    public static async Task<CallToolResult> frame_trace(
        PipeClient pipeClient,
        [Description("start, stop, status or dump.")] string action,
        [Description("Ring capacity in events for start (default 262144).")] int? capacity = null,
        [Description("Output path for dump. Defaults to a temporary file.")] string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Action] = action };
        if (capacity.HasValue)
        {
            payload[AutomationPayloadKeys.Capacity] = capacity.Value;
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            payload[AutomationPayloadKeys.OutputPath] = outputPath;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.FrameTrace, payload, "Frame trace data was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("List the SUSSUDIO_* tuning knobs with type, default, range, current and launch value, owner file and when a change takes effect (live, next preview start, next recording start, next app launch).")]
    public static async Task<CallToolResult> get_tuning_knobs(
        PipeClient pipeClient,
        [Description("Optional case-insensitive substring of the knob name.")] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            payload[AutomationPayloadKeys.Filter] = filter;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.GetTuningKnobs, payload, "Tuning knob data was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Set a tuning knob (process environment variable) or, with reset true, restore its launch value. Values of the wrong type or outside the range are rejected, never clamped. The response states what must restart for the change to take effect. Overrides are not persisted across app launches. While a perf experiment holds the knob (see perf_experiment_lease), the change is refused with invalid-state and the error names the experiment.")]
    public static async Task<CallToolResult> set_tuning_knob(
        PipeClient pipeClient,
        [Description("Knob name, for example SUSSUDIO_DIAGNOSTICS_POLL_MS.")] string name,
        [Description("New value. Omit when reset is true.")] string? value = null,
        [Description("Restore the knob's launch value instead of setting one.")] bool reset = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Name] = name };
        if (value is not null)
        {
            payload[AutomationPayloadKeys.Value] = value;
        }

        if (reset)
        {
            payload[AutomationPayloadKeys.Reset] = true;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.SetTuningKnob, payload, "Tuning knob acknowledgement was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Read or break the perf experiment lease. A running experiment (run_perf_experiment or ssctl perf experiment) holds the tuning knobs it varies; while it does, set_tuning_knob refuses them. status shows the holder, its knobs and when the lease expires, plus how the last lease ended and which knobs the app restored. release with force true ends the lease of an experiment that is stuck or dead and restores its knobs to their values from when it began; a running experiment then stops at its next renewal. An experiment that dies without releasing loses the lease by itself within its time to live (60 s by default).")]
    public static async Task<CallToolResult> perf_experiment_lease(
        PipeClient pipeClient,
        [Description("status (default) or release.")] string action = "status",
        [Description("Required with release: ends another experiment's lease and restores its knobs.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Action] = action };
        if (force)
        {
            payload[AutomationPayloadKeys.Force] = true;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.PerfExperimentLease, payload, "Perf experiment lease data was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    [McpServerTool(UseStructuredContent = true), Description("Get the most recent app log lines (up to 4096 retained) plus per-caller and per-tag line counts for spotting log spam, without opening the log file. Use exclude to drop a chatty source (for example a tag the per-tag counts show dominating) so the lines returned reach further back.")]
    public static async Task<CallToolResult> get_log_tail(
        PipeClient pipeClient,
        [Description("Maximum number of lines to return (default 200), counted after filtering.")] int? maxEntries = null,
        [Description("Optional case-insensitive substring filter: keep only lines that contain it.")] string? contains = null,
        [Description("Optional case-insensitive substring: drop lines that contain it.")] string? exclude = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = new Dictionary<string, object?>();
        if (maxEntries.HasValue)
        {
            payload[AutomationPayloadKeys.MaxEntries] = maxEntries.Value;
        }

        if (!string.IsNullOrWhiteSpace(contains))
        {
            payload[AutomationPayloadKeys.Contains] = contains;
        }

        if (!string.IsNullOrWhiteSpace(exclude))
        {
            payload[AutomationPayloadKeys.Exclude] = exclude;
        }

        return await SendStructuredAsync(pipeClient, AutomationCommandKind.GetLogTail, payload, "Log tail was not available.", cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<CallToolResult> SendStructuredAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        Dictionary<string, object?> payload,
        string missingMessage,
        CancellationToken cancellationToken)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
        return McpToolResultFactory.FromStructuredResponse(response, "Data", missingMessage);
    }
}
