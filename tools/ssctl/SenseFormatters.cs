using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Ssctl;

// Human-readable renderings of the agent-sense responses (audio listen, ui tree, ui invoke). The wire shape is documented
// in docs/architecture/performance-telemetry.md sections 2.9 and 2.10; --json prints it unchanged, so these are only for
// operators reading a terminal. A failed response is rendered with the same detail as a successful one: the data of a
// "no-audio-rendered" capture or an ambiguous target is exactly what the reader needs.
internal static class SenseFormatters
{
    private const int MaxListedTimes = 10;

    public static string FormatAudioListen(JsonElement response)
    {
        if (!PerfFormatters.TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return PerfFormatters.FormatFailure(response, "Audio capture not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Audio output capture: {PerfFormatters.Text(data, "Status")}");
        builder.AppendLine($"  {PerfFormatters.Text(data, "Message")}");
        builder.AppendLine($"  requested {PerfFormatters.Num(data, "RequestedSeconds", 1)} s, captured {PerfFormatters.Num(data, "CapturedSeconds", 2)} s, pid {PerfFormatters.Text(data, "ProcessId")}");
        builder.AppendLine($"  source: {PerfFormatters.Text(data, "Source")}");
        if (AutomationSnapshotFormatter.GetString(data, "FailureStage") is { } stage)
        {
            builder.AppendLine($"  failure: stage {stage}, HRESULT {PerfFormatters.Text(data, "FailureHResult")}");
        }

        if (data.TryGetProperty("Analysis", out var analysis) && analysis.ValueKind == JsonValueKind.Object)
        {
            AppendAnalysis(builder, analysis);
        }

        if (data.TryGetProperty("Latency", out var latency) && latency.ValueKind == JsonValueKind.Object)
        {
            AppendLatency(builder, latency);
        }

        if (AutomationSnapshotFormatter.GetString(data, "WavPath") is { } wav)
        {
            builder.AppendLine($"  wav: {wav} ({PerfFormatters.Text(data, "WavBytes")} bytes)");
        }

        if (data.TryGetProperty("Diagnostics", out var diagnostics) && diagnostics.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine(
                $"  capture: {PerfFormatters.Text(diagnostics, "LoopbackPackets")} packets / {PerfFormatters.Text(diagnostics, "LoopbackFrames")} frames, "
                + $"activation {PerfFormatters.Num(diagnostics, "ActivationMs", 1)} ms, first packet {PerfFormatters.Num(diagnostics, "FirstPacketMs", 1)} ms, "
                + $"wait timeouts {PerfFormatters.Text(diagnostics, "WaitTimeouts")}"
                + (AutomationSnapshotFormatter.GetBool(diagnostics, "BufferTruncated") ? ", BUFFER TRUNCATED" : string.Empty));
            builder.AppendLine(
                $"  tap: {PerfFormatters.Text(diagnostics, "TapFrames")} frames / {PerfFormatters.Text(diagnostics, "TapPackets")} packets"
                + $" ({PerfFormatters.Text(diagnostics, "TapRejectedPackets")} rejected)");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendAnalysis(StringBuilder builder, JsonElement analysis)
    {
        builder.AppendLine($"  verdict: {PerfFormatters.Text(analysis, "Verdict")}");
        builder.AppendLine(
            $"  level: L rms {PerfFormatters.Num(analysis, "LeftRmsDbfs", 1)} / peak {PerfFormatters.Num(analysis, "LeftPeakDbfs", 1)} dBFS, "
            + $"R rms {PerfFormatters.Num(analysis, "RightRmsDbfs", 1)} / peak {PerfFormatters.Num(analysis, "RightPeakDbfs", 1)} dBFS, "
            + $"balance {PerfFormatters.Num(analysis, "ChannelBalanceDb", 1)} dB, silent blocks {PerfFormatters.Num(analysis, "SilentBlockPercent", 0)}%");
        builder.AppendLine(
            $"  defects: clipping {PerfFormatters.Text(analysis, "ClippedSamples")} samples in {PerfFormatters.Text(analysis, "ClipEvents")} events, "
            + $"clicks {PerfFormatters.Text(analysis, "Clicks")}{TimesSuffix(analysis, "ClickTimesMs")}, "
            + $"dropouts {PerfFormatters.Text(analysis, "Dropouts")} (longest {PerfFormatters.Num(analysis, "LongestDropoutMs", 1)} ms){TimesSuffix(analysis, "DropoutTimesMs")}");
        builder.AppendLine(
            $"  packets: {PerfFormatters.Text(analysis, "Packets")} (discontinuities {PerfFormatters.Text(analysis, "DiscontinuityPackets")}, "
            + $"silent {PerfFormatters.Text(analysis, "SilentPackets")}, timestamp errors {PerfFormatters.Text(analysis, "TimestampErrorPackets")}, "
            + $"max gap {PerfFormatters.Num(analysis, "MaxPacketGapMs", 1)} ms)");
        if (analysis.TryGetProperty("Findings", out var findings) && findings.ValueKind == JsonValueKind.Array)
        {
            foreach (var finding in findings.EnumerateArray())
            {
                builder.AppendLine($"  - {finding.GetString()}");
            }
        }
    }

    private static void AppendLatency(StringBuilder builder, JsonElement latency)
    {
        var status = PerfFormatters.Text(latency, "Status");
        if (status == "ok")
        {
            builder.AppendLine(
                $"  latency (capture delivery -> mixer): {PerfFormatters.Num(latency, "LatencyMs", 2)} ms "
                + $"[{PerfFormatters.Text(latency, "Resolution")}], confidence {PerfFormatters.Num(latency, "Confidence", 2)}, "
                + $"gain difference {PerfFormatters.Num(latency, "GainDifferenceDb", 1)} dB, overlap {PerfFormatters.Num(latency, "OverlapSeconds", 1)} s");
        }
        else
        {
            builder.AppendLine($"  latency: {status} - {PerfFormatters.Text(latency, "Reason")}");
        }

        builder.AppendLine($"  latency scope: {PerfFormatters.Text(latency, "Scope")}");
    }

    private static string TimesSuffix(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var times) || times.ValueKind != JsonValueKind.Array || times.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var shown = new List<string>();
        foreach (var time in times.EnumerateArray())
        {
            if (shown.Count == MaxListedTimes)
            {
                shown.Add("...");
                break;
            }

            shown.Add(time.ValueKind == JsonValueKind.Number
                ? time.GetDouble().ToString("F0", CultureInfo.InvariantCulture)
                : "?");
        }

        return $" at ms {string.Join(", ", shown)}";
    }

    public static string FormatUiTree(JsonElement response)
    {
        if (!PerfFormatters.TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return PerfFormatters.FormatFailure(response, "UI tree not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"== UI tree ({PerfFormatters.Text(data, "Status")}): {PerfFormatters.Text(data, "Message")} ==");
        if (AutomationSnapshotFormatter.GetBool(data, "Truncated"))
        {
            builder.AppendLine($"!! TRUNCATED: {PerfFormatters.Text(data, "TruncationReason")}");
        }

        builder.AppendLine(
            $"window {PerfFormatters.Num(data, "WindowWidth", 0)} x {PerfFormatters.Num(data, "WindowHeight", 0)} DIP, "
            + $"maxDepth {PerfFormatters.Text(data, "MaxDepth")}, interactiveOnly {AutomationSnapshotFormatter.GetBool(data, "InteractiveOnly")}");
        if (data.TryGetProperty("Roots", out var roots) && roots.ValueKind == JsonValueKind.Array)
        {
            foreach (var node in roots.EnumerateArray())
            {
                AppendNode(builder, node, 0);
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatUiInvoke(JsonElement response)
    {
        if (!PerfFormatters.TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return PerfFormatters.FormatFailure(response, "UI invoke not acknowledged.");
        }

        var builder = new StringBuilder();
        builder.AppendLine($"{PerfFormatters.Text(data, "Status")}: {PerfFormatters.Text(data, "Message")}");
        var action = AutomationSnapshotFormatter.GetString(data, "Action");
        var matchedBy = AutomationSnapshotFormatter.GetString(data, "MatchedBy");
        var via = AutomationSnapshotFormatter.GetString(data, "Via");
        if (action is not null || matchedBy is not null || via is not null)
        {
            builder.AppendLine($"  target \"{PerfFormatters.Text(data, "Target")}\" matched by {matchedBy ?? "-"}, action {action ?? "-"}, via {via ?? "-"}");
        }

        if (data.TryGetProperty("Overridden", out var overridden) && overridden.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in overridden.EnumerateArray())
            {
                builder.AppendLine($"  force overrode {item.GetString()}");
            }
        }

        if (data.TryGetProperty("Candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
        {
            var total = AutomationSnapshotFormatter.GetLong(data, "CandidateTotal");
            builder.AppendLine($"  candidates ({candidates.GetArrayLength()} of {total}):");
            foreach (var candidate in candidates.EnumerateArray())
            {
                builder.Append("    ").Append(PerfFormatters.Text(candidate, "Path")).Append("  [").Append(PerfFormatters.Text(candidate, "Type")).Append(']');
                AppendIf(builder, " #", AutomationSnapshotFormatter.GetString(candidate, "Name"));
                AppendIf(builder, " id=", AutomationSnapshotFormatter.GetString(candidate, "AutomationId"));
                AppendQuoted(builder, " name ", AutomationSnapshotFormatter.GetString(candidate, "AutomationName"));
                AppendQuoted(builder, " text ", AutomationSnapshotFormatter.GetString(candidate, "Text"));
                if (!AutomationSnapshotFormatter.GetBool(candidate, "IsVisible"))
                {
                    builder.Append(" HIDDEN");
                }

                if (!AutomationSnapshotFormatter.GetBool(candidate, "IsEnabled"))
                {
                    builder.Append(" DISABLED");
                }

                builder.AppendLine();
            }
        }

        if (data.TryGetProperty("Element", out var element) && element.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine("  element after the action:");
            AppendNode(builder, element, 2);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendNode(StringBuilder builder, JsonElement node, int depth)
    {
        builder.Append(' ', depth * 2).Append(PerfFormatters.Text(node, "Type"));
        AppendIf(builder, " #", AutomationSnapshotFormatter.GetString(node, "Name"));
        AppendIf(builder, " id=", AutomationSnapshotFormatter.GetString(node, "AutomationId"));
        AppendQuoted(builder, " name ", AutomationSnapshotFormatter.GetString(node, "AutomationName"));
        AppendQuoted(builder, " text ", AutomationSnapshotFormatter.GetString(node, "Text"));
        if (node.TryGetProperty("Patterns", out var patterns) && patterns.ValueKind == JsonValueKind.Array && patterns.GetArrayLength() > 0)
        {
            var names = new List<string>();
            foreach (var pattern in patterns.EnumerateArray())
            {
                names.Add(pattern.GetString() ?? "?");
            }

            builder.Append(" [").Append(string.Join(",", names)).Append(']');
        }

        if (!AutomationSnapshotFormatter.GetBool(node, "IsEnabled"))
        {
            builder.Append(" DISABLED(").Append(AutomationSnapshotFormatter.GetString(node, "DisabledReason")).Append(')');
        }

        if (!AutomationSnapshotFormatter.GetBool(node, "IsVisible"))
        {
            builder.Append(" HIDDEN(").Append(AutomationSnapshotFormatter.GetString(node, "HiddenReason")).Append(')');
        }

        AppendIf(builder, " toggle=", AutomationSnapshotFormatter.GetString(node, "ToggleState"));
        if (node.TryGetProperty("Range", out var range) && range.ValueKind == JsonValueKind.Object)
        {
            builder.Append(" range=").Append(PerfFormatters.Num(range, "Value", 2))
                .Append(" [").Append(PerfFormatters.Num(range, "Minimum", 2)).Append("..").Append(PerfFormatters.Num(range, "Maximum", 2)).Append(']');
        }

        AppendQuoted(builder, " value ", AutomationSnapshotFormatter.GetString(node, "Value"));
        if (AutomationSnapshotFormatter.GetString(node, "SelectedItem") is { } selected)
        {
            builder.Append(" selected #").Append(PerfFormatters.Text(node, "SelectedIndex")).Append(" \"").Append(selected).Append('"');
        }

        if (node.TryGetProperty("ItemCount", out var itemCount) && itemCount.ValueKind == JsonValueKind.Number)
        {
            builder.Append(" items(").Append(itemCount.GetInt32()).Append(')');
            if (node.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                var texts = new List<string>();
                foreach (var item in items.EnumerateArray())
                {
                    texts.Add(item.GetString() ?? string.Empty);
                }

                builder.Append(": ").Append(string.Join(" | ", texts));
            }
        }

        AppendIf(builder, " expand=", AutomationSnapshotFormatter.GetString(node, "ExpandState"));
        if (node.TryGetProperty("Accelerators", out var accelerators) && accelerators.ValueKind == JsonValueKind.Array && accelerators.GetArrayLength() > 0)
        {
            var keys = new List<string>();
            foreach (var accelerator in accelerators.EnumerateArray())
            {
                var modifiers = PerfFormatters.Text(accelerator, "Modifiers");
                var key = PerfFormatters.Text(accelerator, "Key");
                keys.Add(modifiers == "None" ? key : $"{modifiers}+{key}");
            }

            builder.Append(" accel ").Append(string.Join(",", keys));
        }

        if (node.TryGetProperty("Bounds", out var bounds) && bounds.ValueKind == JsonValueKind.Object)
        {
            builder.Append(" @").Append(PerfFormatters.Num(bounds, "X", 0)).Append(',').Append(PerfFormatters.Num(bounds, "Y", 0))
                .Append(' ').Append(PerfFormatters.Num(bounds, "Width", 0)).Append('x').Append(PerfFormatters.Num(bounds, "Height", 0));
        }

        // An element with no name, id, automation name or text can only be targeted by its path, so show it.
        if (AutomationSnapshotFormatter.GetString(node, "Name") is null
            && AutomationSnapshotFormatter.GetString(node, "AutomationId") is null
            && AutomationSnapshotFormatter.GetString(node, "AutomationName") is null
            && AutomationSnapshotFormatter.GetString(node, "Text") is null)
        {
            builder.Append(" path=").Append(PerfFormatters.Text(node, "Path"));
        }

        builder.AppendLine();

        if (node.TryGetProperty("Children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                AppendNode(builder, child, depth + 1);
            }
        }
    }

    private static void AppendIf(StringBuilder builder, string prefix, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(prefix).Append(value);
        }
    }

    private static void AppendQuoted(StringBuilder builder, string prefix, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(prefix).Append('"').Append(value).Append('"');
        }
    }
}
