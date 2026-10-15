using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Ssctl;

// Human-readable renderings of the performance-telemetry responses (perf, knobs, log). The wire shape is documented in
// docs/architecture/performance-telemetry.md section 3; --json prints it unchanged, so these formatters are only for
// operators reading a terminal.
internal static class PerfFormatters
{
    public static string FormatReport(JsonElement response)
    {
        if (!TryGetData(response, out var report) || report.ValueKind != JsonValueKind.Object)
        {
            return FormatFailure(response, "Performance report not available.");
        }

        var builder = new StringBuilder();
        var window = AutomationSnapshotFormatter.GetString(report, "Window");
        builder.AppendLine(window is null
            ? "== Performance report (process lifetime) =="
            : $"== Performance report (window '{window}') ==");
        builder.AppendLine($"Duration: {Num(report, "DurationMs", 0, 1000)} s  Started: {Text(report, "StartedUtc")}  Generated: {Text(report, "GeneratedUtc")}");

        AppendMetrics(builder, report);
        AppendCounters(builder, report);
        AppendThreads(builder, report);
        AppendProcess(builder, report);
        AppendRuntimeSections(builder, report);
        AppendKnobsAndMarkers(builder, report);
        return builder.ToString().TrimEnd();
    }

    public static string FormatWindowList(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("Windows", out var windows) || windows.ValueKind != JsonValueKind.Array)
        {
            return FormatFailure(response, "Performance window list not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"== Performance windows ({AutomationSnapshotFormatter.GetLong(data, "Count")}/{AutomationSnapshotFormatter.GetLong(data, "MaxWindows")}) ==");
        var any = false;
        foreach (var window in windows.EnumerateArray())
        {
            any = true;
            builder.AppendLine($"{Text(window, "Name"),-24} started {Text(window, "StartedUtc")}  elapsed {Num(window, "ElapsedMs", 1, 1000)} s");
        }

        if (!any)
        {
            builder.AppendLine("No windows are open.");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatKnobs(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("Knobs", out var knobs) || knobs.ValueKind != JsonValueKind.Array)
        {
            return FormatFailure(response, "Tuning knobs not available.");
        }

        var rows = new List<string[]>
        {
            new[] { "NAME", "TYPE", "VALUE", "DEFAULT", "RANGE", "APPLY" },
        };
        foreach (var knob in knobs.EnumerateArray())
        {
            var overridden = AutomationSnapshotFormatter.GetBool(knob, "Overridden");
            var value = AutomationSnapshotFormatter.GetString(knob, "Value");
            rows.Add(new[]
            {
                (overridden ? "* " : "  ") + Text(knob, "Name"),
                Text(knob, "Type"),
                value ?? "<unset>",
                Text(knob, "Default"),
                Range(knob),
                Text(knob, "Apply"),
            });
        }

        var builder = new StringBuilder();
        builder.AppendLine($"== Tuning knobs ({AutomationSnapshotFormatter.GetLong(data, "Count")} listed, {AutomationSnapshotFormatter.GetLong(data, "Overridden")} overridden; * = differs from launch) ==");
        AppendAligned(builder, rows);
        if (data.TryGetProperty("Excluded", out var excluded) && excluded.ValueKind == JsonValueKind.Array && excluded.GetArrayLength() > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Not settable at runtime:");
            foreach (var exclusion in excluded.EnumerateArray())
            {
                builder.AppendLine($"  {Text(exclusion, "Name")}: {Text(exclusion, "Reason")}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatKnobChange(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return FormatFailure(response, "Tuning knob change not acknowledged.");
        }

        var builder = new StringBuilder();
        builder.AppendLine(Text(response, "Message"));
        if (AutomationSnapshotFormatter.IsSuccess(response))
        {
            builder.AppendLine($"  old: {AutomationSnapshotFormatter.GetString(data, "Old") ?? "<unset>"}");
            builder.AppendLine($"  new: {AutomationSnapshotFormatter.GetString(data, "New") ?? "<unset>"}");
            builder.AppendLine($"  apply: {Text(data, "Apply")}");
        }
        else
        {
            builder.AppendLine($"  failure: {Text(data, "Failure")}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatLogTail(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("Lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
        {
            return FormatFailure(response, "Log tail not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"== Log tail ({AutomationSnapshotFormatter.GetLong(data, "Returned")} of {AutomationSnapshotFormatter.GetLong(data, "TotalLines")} lines written) ==");
        foreach (var line in lines.EnumerateArray())
        {
            builder.AppendLine(line.GetString());
        }

        AppendCounts(builder, "Lines by caller", data, "ByCaller");
        AppendCounts(builder, "Lines by tag", data, "ByTag");
        return builder.ToString().TrimEnd();
    }

    private static void AppendMetrics(StringBuilder builder, JsonElement report)
    {
        if (!report.TryGetProperty("Metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Array || metrics.GetArrayLength() == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No metric samples in this interval (use --idle to list metrics with no samples).");
            return;
        }

        var rows = new List<string[]>
        {
            new[] { "METRIC", "UNIT", "COUNT", "RATE/S", "MEAN", "P50", "P90", "P99", "P99.9", "MAX" },
        };
        foreach (var metric in metrics.EnumerateArray())
        {
            var approx = AutomationSnapshotFormatter.GetBool(metric, "MaxApprox") ? "~" : string.Empty;
            rows.Add(new[]
            {
                Text(metric, "Name"),
                Text(metric, "Unit"),
                AutomationSnapshotFormatter.GetLong(metric, "Count").ToString(CultureInfo.InvariantCulture),
                Num(metric, "RatePerSec", 1),
                Num(metric, "Mean", 3),
                Num(metric, "P50", 3),
                Num(metric, "P90", 3),
                Num(metric, "P99", 3),
                Num(metric, "P999", 3),
                approx + Num(metric, "Max", 3),
            });
        }

        builder.AppendLine();
        builder.AppendLine("-- Metrics --");
        AppendAligned(builder, rows);
    }

    private static void AppendCounters(StringBuilder builder, JsonElement report)
    {
        if (!report.TryGetProperty("Counters", out var counters) || counters.ValueKind != JsonValueKind.Array || counters.GetArrayLength() == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("-- Counters --");
        foreach (var counter in counters.EnumerateArray())
        {
            builder.AppendLine($"{Text(counter, "Name"),-40} {AutomationSnapshotFormatter.GetLong(counter, "Delta"),12}  {Num(counter, "RatePerSec", 2)}/s");
        }
    }

    private static void AppendThreads(StringBuilder builder, JsonElement report)
    {
        if (!report.TryGetProperty("Threads", out var threads) || threads.ValueKind != JsonValueKind.Array || threads.GetArrayLength() == 0)
        {
            return;
        }

        var rows = new List<string[]>
        {
            new[] { "ROLE", "TID", "CPU%", "CYCLES", "PRIORITY", "MMCSS", "ALIVE" },
        };
        foreach (var thread in threads.EnumerateArray())
        {
            rows.Add(new[]
            {
                Text(thread, "Role"),
                AutomationSnapshotFormatter.GetLong(thread, "ThreadId").ToString(CultureInfo.InvariantCulture),
                Num(thread, "CpuPercent", 1),
                AutomationSnapshotFormatter.GetLong(thread, "Cycles").ToString(CultureInfo.InvariantCulture),
                Text(thread, "Priority"),
                AutomationSnapshotFormatter.GetBool(thread, "Mmcss") ? "yes" : "no",
                AutomationSnapshotFormatter.GetBool(thread, "Alive") ? "yes" : "no",
            });
        }

        builder.AppendLine();
        builder.AppendLine("-- Hot threads --");
        AppendAligned(builder, rows);
    }

    private static void AppendProcess(StringBuilder builder, JsonElement report)
    {
        if (!report.TryGetProperty("Process", out var process) || process.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("-- Process --");
        builder.AppendLine($"CPU {Num(process, "CpuPercentOfOneCore", 1)}% of one core  Private {Mb(process, "PrivateBytes")} MB  Working set {Mb(process, "WorkingSetBytes")} MB  Handles {AutomationSnapshotFormatter.GetLong(process, "Handles")}  Threads {AutomationSnapshotFormatter.GetLong(process, "Threads")}");
        builder.AppendLine($"IO read {Mb(process, "IoReadBytes")} MB  write {Mb(process, "IoWriteBytes")} MB");
    }

    private static void AppendRuntimeSections(StringBuilder builder, JsonElement report)
    {
        if (report.TryGetProperty("Gc", out var gc) && gc.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"GC gen0/1/2 {AutomationSnapshotFormatter.GetLong(gc, "Gen0")}/{AutomationSnapshotFormatter.GetLong(gc, "Gen1")}/{AutomationSnapshotFormatter.GetLong(gc, "Gen2")}  pause {Num(gc, "PauseMs", 2)} ms  allocated {Mb(gc, "AllocatedBytes")} MB");
        }

        if (report.TryGetProperty("Dwm", out var dwm) && dwm.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"DWM refresh {OrNa(dwm, "RefreshHz", 2)} Hz  composed {OrNa(dwm, "FramesComposedPerSec", 1)}/s");
        }

        if (report.TryGetProperty("Gpu", out var gpu) && gpu.ValueKind == JsonValueKind.Object &&
            gpu.TryGetProperty("Engines", out var engines) && engines.ValueKind == JsonValueKind.Array && engines.GetArrayLength() > 0)
        {
            builder.AppendLine();
            builder.AppendLine("-- GPU engines --");
            foreach (var engine in engines.EnumerateArray())
            {
                builder.AppendLine($"{Text(engine, "Type"),-16} mean {Num(engine, "MeanPercent", 1)}%  p95 {Num(engine, "P95Percent", 1)}%");
            }
        }

        if (report.TryGetProperty("Capture", out var capture) && capture.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Capture ingress clock: {Text(capture, "IngressClock")}");
        }

        if (report.TryGetProperty("Sensors", out var sensors) && sensors.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Sensors: {(AutomationSnapshotFormatter.GetBool(sensors, "Active") ? "active" : "idle")} ({Text(sensors, "Reason")})");
        }

        if (report.TryGetProperty("Trace", out var trace) && trace.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Frame trace: {(AutomationSnapshotFormatter.GetBool(trace, "Running") ? "running" : "stopped")}  {AutomationSnapshotFormatter.GetLong(trace, "Events")} events  capacity {AutomationSnapshotFormatter.GetLong(trace, "Capacity")}");
        }
    }

    private static void AppendKnobsAndMarkers(StringBuilder builder, JsonElement report)
    {
        if (report.TryGetProperty("Knobs", out var knobs) && knobs.ValueKind == JsonValueKind.Array && knobs.GetArrayLength() > 0)
        {
            builder.AppendLine();
            builder.AppendLine("-- Tuning overrides in effect --");
            foreach (var knob in knobs.EnumerateArray())
            {
                builder.AppendLine($"{Text(knob, "Name")} = {AutomationSnapshotFormatter.GetString(knob, "Value") ?? "<unset>"} (launch {AutomationSnapshotFormatter.GetString(knob, "Launch") ?? "<unset>"}, apply {Text(knob, "Apply")})");
            }
        }

        if (report.TryGetProperty("Markers", out var markers) && markers.ValueKind == JsonValueKind.Array && markers.GetArrayLength() > 0)
        {
            builder.AppendLine();
            builder.AppendLine("-- Markers --");
            foreach (var marker in markers.EnumerateArray())
            {
                builder.AppendLine($"{Text(marker, "TimestampUtc")}  {Text(marker, "Text")}");
            }
        }
    }

    private static void AppendCounts(StringBuilder builder, string title, JsonElement data, string property)
    {
        if (!data.TryGetProperty(property, out var counts) || counts.ValueKind != JsonValueKind.Array || counts.GetArrayLength() == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"-- {title} --");
        foreach (var count in counts.EnumerateArray())
        {
            builder.AppendLine($"{AutomationSnapshotFormatter.GetLong(count, "Count"),8}  {Text(count, "Key")}");
        }
    }

    private static void AppendAligned(StringBuilder builder, List<string[]> rows)
    {
        var columns = rows[0].Length;
        var widths = new int[columns];
        foreach (var row in rows)
        {
            for (var i = 0; i < columns; i++)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        foreach (var row in rows)
        {
            var line = new StringBuilder();
            for (var i = 0; i < columns; i++)
            {
                // First column is a label (left aligned); the rest are numbers or short tokens (right aligned).
                line.Append(i == 0 ? row[i].PadRight(widths[i]) : row[i].PadLeft(widths[i]));
                if (i < columns - 1)
                {
                    line.Append("  ");
                }
            }

            builder.AppendLine(line.ToString().TrimEnd());
        }
    }

    private static string Range(JsonElement knob)
    {
        if (knob.TryGetProperty("Choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var parts = new List<string>();
            foreach (var choice in choices.EnumerateArray())
            {
                parts.Add(choice.GetString() ?? string.Empty);
            }

            return string.Join("|", parts);
        }

        var min = AutomationSnapshotFormatter.GetString(knob, "Min");
        var max = AutomationSnapshotFormatter.GetString(knob, "Max");
        return min is null && max is null ? "-" : $"{min ?? "..."}..{max ?? "..."}";
    }

    private static string FormatFailure(JsonElement response, string fallback)
    {
        var message = AutomationSnapshotFormatter.Get(response, "Message", fallback);
        var errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty);
        return string.IsNullOrWhiteSpace(errorCode) ? message : $"{message}{Environment.NewLine}ErrorCode: {errorCode}";
    }

    private static bool TryGetData(JsonElement response, out JsonElement data)
    {
        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Data", out data) &&
            data.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        data = default;
        return false;
    }

    private static string Text(JsonElement element, string property)
        => AutomationSnapshotFormatter.Get(element, property, "-");

    private static string Num(JsonElement element, string property, int decimals, double divisor = 1)
    {
        var value = AutomationSnapshotFormatter.GetDouble(element, property, double.NaN);
        return double.IsFinite(value)
            ? (value / divisor).ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
            : "-";
    }

    private static string OrNa(JsonElement element, string property, int decimals)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? Num(element, property, decimals)
            : "n/a";

    private static string Mb(JsonElement element, string property)
        => Num(element, property, 1, 1024.0 * 1024.0);
}
