using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// Console formatters for snapshots, diagnostics, and verification output. Keep
// these projection-only: command behavior belongs in CommandHandlers and
// protocol mechanics belong in the shared pipe client.
internal static class Formatters
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static string FormatResult(JsonElement response, bool includeData)
    {
        if (!includeData || !TryGetData(response, out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Command completed.");
        }

        return $"{AutomationSnapshotFormatter.Get(response, "Message", "Command completed.")}{Environment.NewLine}{PrettyJson(data)}";
    }

    public static string PrettyJson(JsonElement element)
        => JsonSerializer.Serialize(element, IndentedJsonOptions);


    private static bool TryGetData(JsonElement response, out JsonElement data)
    {
        if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("Data", out data))
        {
            return true;
        }

        data = default;
        return false;
    }

    public static string FormatDiagnostics(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "No diagnostics available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Diagnostics ==");
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            count++;
            var correlation = AutomationSnapshotFormatter.Get(item, "CorrelationId", string.Empty);
            var correlationSuffix = string.IsNullOrWhiteSpace(correlation) ? string.Empty : $" [{correlation}]";
            builder.AppendLine(
                $"{AutomationSnapshotFormatter.Get(item, "TimestampUtc", "?")} [{AutomationSnapshotFormatter.Get(item, "Severity", "Info")}] [{AutomationSnapshotFormatter.Get(item, "Category", "System")}] {AutomationSnapshotFormatter.Get(item, "Message", string.Empty)}{correlationSuffix}");
        }

        if (count == 0)
        {
            builder.AppendLine("No diagnostic events.");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatMemory(JsonElement response)
    {
        if (!response.TryGetProperty("Snapshot", out var snapshot) || snapshot.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuPercent")}%");
        builder.AppendLine($"Process CPU Time: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB");
        builder.AppendLine($"Private Bytes: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB");
        builder.AppendLine($"Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB");
        builder.AppendLine($"GC Heap Size: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail");
        builder.AppendLine($"ThreadPool IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
        return builder.ToString().TrimEnd();
    }

    public static string FormatOptions(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Capture options not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Capture Options ==");
        builder.AppendLine($"Selected Device: {AutomationSnapshotFormatter.Get(data, "SelectedDeviceId")}");
        builder.AppendLine($"Selected Audio Input: {AutomationSnapshotFormatter.Get(data, "SelectedAudioInputDeviceId")}");
        builder.AppendLine($"Selected Microphone: {AutomationSnapshotFormatter.Get(data, "SelectedMicrophoneDeviceId")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(data, "SelectedResolution")} | Frame Rate: {AutomationSnapshotFormatter.Get(data, "SelectedFrameRate")}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(data, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(data, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(data, "SelectedPreset")}");
        builder.AppendLine($"Split Encode: {AutomationSnapshotFormatter.Get(data, "SelectedSplitEncodeMode")} | Video Format: {AutomationSnapshotFormatter.Get(data, "SelectedVideoFormat")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(data, "MjpegDecoderCount")}");
        builder.AppendLine($"Preview Volume: {AutomationSnapshotFormatter.Get(data, "PreviewVolumePercent")}% | Stats Visible: {AutomationSnapshotFormatter.Get(data, "IsStatsVisible")}");
        builder.AppendLine($"Microphone Enabled: {AutomationSnapshotFormatter.Get(data, "IsMicrophoneEnabled")} | Volume: {AutomationSnapshotFormatter.Get(data, "MicrophoneVolumePercent")}%");
        builder.AppendLine($"Flashback: Enabled={AutomationSnapshotFormatter.Get(data, "IsFlashbackEnabled")} | Buffer={AutomationSnapshotFormatter.Get(data, "FlashbackBufferMinutes")}m | GPU Decode={AutomationSnapshotFormatter.Get(data, "FlashbackGpuDecode")}");
        builder.AppendLine();
        AppendNamedOptions(builder, "Devices", data, "Devices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Audio Input Devices", data, "AudioInputDevices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Microphone Devices", data, "MicrophoneDevices", includeId: true);
        builder.AppendLine();
        AppendResolutionOptions(builder, data);
        builder.AppendLine();
        AppendFrameRateOptions(builder, data);
        builder.AppendLine();
        AppendStringOptions(builder, "Recording Formats", data, "RecordingFormats");
        builder.AppendLine();
        AppendStringOptions(builder, "Qualities", data, "Qualities");
        builder.AppendLine();
        AppendStringOptions(builder, "Presets", data, "Presets");
        builder.AppendLine();
        AppendStringOptions(builder, "Split Encode Modes", data, "SplitEncodeModes");
        builder.AppendLine();
        AppendStringOptions(builder, "Video Formats", data, "VideoFormats");
        builder.AppendLine();
        AppendIntOptions(builder, "MJPEG Decoder Counts", data, "MjpegDecoderCounts");
        builder.AppendLine();
        AppendIntOptions(builder, "Flashback Buffer Minutes", data, "FlashbackBufferMinuteOptions");
        return builder.ToString().TrimEnd();
    }

    public static string FormatDeviceList(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Capture options not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Devices ==");
        AppendNamedOptions(builder, "Capture Devices", data, "Devices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Audio Input Devices", data, "AudioInputDevices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Microphone Devices", data, "MicrophoneDevices", includeId: true);
        return builder.ToString().TrimEnd();
    }

    private static void AppendNamedOptions(StringBuilder builder, string title, JsonElement data, string propertyName, bool includeId)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            var prefix = AutomationSnapshotFormatter.Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";
            var idSuffix = includeId ? $" ({AutomationSnapshotFormatter.Get(item, "Id")})" : string.Empty;
            builder.AppendLine($"{prefix} {AutomationSnapshotFormatter.Get(item, "Name")}{idSuffix}");
        }
    }

    private static void AppendResolutionOptions(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine("== Resolutions ==");
        if (!data.TryGetProperty("Resolutions", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Value")} ({AutomationSnapshotFormatter.Get(item, "Width")}x{AutomationSnapshotFormatter.Get(item, "Height")}){GetDisableSuffix(item)}");
        }
    }

    private static void AppendFrameRateOptions(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine("== Frame Rates ==");
        if (!data.TryGetProperty("FrameRates", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine(
                $"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "FriendlyValue")} fps ({AutomationSnapshotFormatter.Get(item, "Value")} exact, {AutomationSnapshotFormatter.Get(item, "ExactValueArg")}){GetDisableSuffix(item)}");
        }
    }

    private static void AppendStringOptions(StringBuilder builder, string title, JsonElement data, string propertyName)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Label", AutomationSnapshotFormatter.Get(item, "Value"))}{GetDisableSuffix(item)}");
        }
    }

    private static void AppendIntOptions(StringBuilder builder, string title, JsonElement data, string propertyName)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Value")}{GetDisableSuffix(item)}");
        }
    }

    private static string GetSelectionPrefix(JsonElement item)
        => AutomationSnapshotFormatter.Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";

    private static string GetDisableSuffix(JsonElement item)
    {
        var isEnabled = AutomationSnapshotFormatter.Get(item, "IsEnabled", "true");
        if (isEnabled.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return $" [disabled: {AutomationSnapshotFormatter.Get(item, "DisableReason", "Unavailable")}]";
    }

    public static string FormatTimeline(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "No timeline data available.");
        }

        var entries = ReadTimelineRows(data);
        if (entries.Count == 0)
        {
            return "No timeline entries collected yet.";
        }

        return RenderTimeline(entries);
    }

    private static List<TimelineRow> ReadTimelineRows(JsonElement data)
    {
        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            entries.Add(new TimelineRow
            {
                Timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc"),
                CaptureFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureFps"),
                PreviewFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewFps"),
                VidQueue = AutomationSnapshotFormatter.GetInt(item, "VideoQueueDepth"),
                VidDrops = AutomationSnapshotFormatter.GetLong(item, "VideoDrops"),
                CaptureAvgMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceAverageMs"),
                CaptureP95Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP95Ms"),
                CaptureP99Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP99Ms"),
                CaptureMaxMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceMaxMs"),
                CaptureOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceOnePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
                PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount"),
                PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms"),
                PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms"),
                PreviewD3DPipelineP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyP95Ms"),
                PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount"),
                PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms"),
                PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount"),
                LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs"),
                CpuPct = AutomationSnapshotFormatter.GetDouble(item, "ProcessCpuPercent"),
                WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb"),
                ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb"),
                Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections"),
                Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections"),
                Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections"),
                GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent"),
                Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable"),
                IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable")
            });
        }

        return entries;
    }

    private static string RenderTimeline(IReadOnlyList<TimelineRow> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Performance Timeline ({entries.Count} samples)");
        builder.AppendLine();
        builder.AppendLine("Timestamp                | CapAvg | CapP95 | CapP99 | Cap1% | PrvAvg | PrvP95 | PrvSlow | D3DQ | D3DPrs | D3DTot | D3DPipe | D3DMiss | VidQ | VidDrop | LatMs | CPU% | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 200));

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,6:F1} | {4,5:F1} | {5,6:F1} | {6,6:F1} | {7,7:F1} | {8,4} | {9,6:F1} | {10,6:F1} | {11,7:F1} | {12,7} | {13,4} | {14,7} | {15,5} | {16,5:F1} | {17,6:F1} | {18,6:F1} | {19,4} | {20,4} | {21,4} | {22,4:F1} | {23,4} | {24,4}",
                entry.Timestamp,
                entry.CaptureAvgMs,
                entry.CaptureP95Ms,
                entry.CaptureP99Ms,
                entry.CaptureOnePercentLowFps,
                entry.PreviewAvgMs,
                entry.PreviewP95Ms,
                entry.PreviewSlowPct,
                entry.PreviewD3DPending,
                entry.PreviewD3DPresentP95Ms,
                entry.PreviewD3DTotalP95Ms,
                entry.PreviewD3DPipelineP95Ms,
                entry.PreviewD3DRecentMissed,
                entry.VidQueue,
                entry.VidDrops,
                entry.LatencyMs,
                entry.CpuPct,
                entry.WorkingMb,
                entry.ManagedMb,
                entry.Gen0,
                entry.Gen1,
                entry.Gen2,
                entry.GcPause,
                entry.Workers,
                entry.IoThreads));
        }

        AppendTimelineTrendSummary(builder, entries);

        return builder.ToString().TrimEnd();
    }

    private static void AppendTimelineTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries)
    {
        if (entries.Count < 2)
        {
            return;
        }

        var first = entries[0];
        var last = entries[^1];
        builder.AppendLine();
        builder.AppendLine("== Trend Summary (first vs last sample) ==");
        builder.AppendLine($"Capture Avg:    {FormatOneDecimalInvariant(first.CaptureAvgMs)}ms -> {FormatOneDecimalInvariant(last.CaptureAvgMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureAvgMs - first.CaptureAvgMs)}ms)");
        builder.AppendLine($"Capture P95:    {FormatOneDecimalInvariant(first.CaptureP95Ms)}ms -> {FormatOneDecimalInvariant(last.CaptureP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureP95Ms - first.CaptureP95Ms)}ms)");
        builder.AppendLine($"Capture P99:    {FormatOneDecimalInvariant(first.CaptureP99Ms)}ms -> {FormatOneDecimalInvariant(last.CaptureP99Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureP99Ms - first.CaptureP99Ms)}ms)");
        builder.AppendLine($"Capture Max:    {FormatOneDecimalInvariant(first.CaptureMaxMs)}ms -> {FormatOneDecimalInvariant(last.CaptureMaxMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureMaxMs - first.CaptureMaxMs)}ms)");
        builder.AppendLine($"Preview Avg:    {FormatOneDecimalInvariant(first.PreviewAvgMs)}ms -> {FormatOneDecimalInvariant(last.PreviewAvgMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewAvgMs - first.PreviewAvgMs)}ms)");
        builder.AppendLine($"Preview P95:    {FormatOneDecimalInvariant(first.PreviewP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewP95Ms - first.PreviewP95Ms)}ms)");
        builder.AppendLine($"Preview Max:    {FormatOneDecimalInvariant(first.PreviewMaxMs)}ms -> {FormatOneDecimalInvariant(last.PreviewMaxMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewMaxMs - first.PreviewMaxMs)}ms)");
        builder.AppendLine($"Preview 1% Low: {FormatOneDecimalInvariant(first.PreviewOnePercentLowFps)}fps -> {FormatOneDecimalInvariant(last.PreviewOnePercentLowFps)}fps");
        builder.AppendLine($"Preview Slow%:  {FormatOneDecimalInvariant(first.PreviewSlowPct)}% -> {FormatOneDecimalInvariant(last.PreviewSlowPct)}% (delta: {FormatSignedOneDecimalInvariant(last.PreviewSlowPct - first.PreviewSlowPct)}%)");
        builder.AppendLine($"D3D Present P95:{FormatOneDecimalInvariant(first.PreviewD3DPresentP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DPresentP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms)}ms)");
        builder.AppendLine($"D3D Total P95:  {FormatOneDecimalInvariant(first.PreviewD3DTotalP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DTotalP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms)}ms)");
        builder.AppendLine($"D3D Pipe P95:   {FormatOneDecimalInvariant(first.PreviewD3DPipelineP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DPipelineP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DPipelineP95Ms - first.PreviewD3DPipelineP95Ms)}ms)");
        builder.AppendLine($"D3D Wait P95:   {FormatOneDecimalInvariant(first.PreviewD3DFrameLatencyWaitP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DFrameLatencyWaitP95Ms)}ms (timeouts: {first.PreviewD3DFrameLatencyWaitTimeouts} -> {last.PreviewD3DFrameLatencyWaitTimeouts})");
        builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
        builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
        builder.AppendLine($"Capture Rate:   {FormatOneDecimalInvariant(first.CaptureFps)}fps -> {FormatOneDecimalInvariant(last.CaptureFps)}fps (derived avg)");
        builder.AppendLine($"Capture 1% Low: {FormatOneDecimalInvariant(first.CaptureOnePercentLowFps)}fps -> {FormatOneDecimalInvariant(last.CaptureOnePercentLowFps)}fps");
        builder.AppendLine($"Preview Rate:   {FormatOneDecimalInvariant(first.PreviewFps)}fps -> {FormatOneDecimalInvariant(last.PreviewFps)}fps (derived avg)");
        builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
        builder.AppendLine($"Process CPU:    {FormatOneDecimalInvariant(first.CpuPct)}% -> {FormatOneDecimalInvariant(last.CpuPct)}% (delta: {FormatSignedOneDecimalInvariant(last.CpuPct - first.CpuPct)}%)");
        builder.AppendLine($"Working Set:    {FormatOneDecimalInvariant(first.WorkingMb)}MB -> {FormatOneDecimalInvariant(last.WorkingMb)}MB (delta: {FormatSignedOneDecimalInvariant(last.WorkingMb - first.WorkingMb)}MB)");
        builder.AppendLine($"Managed Heap:   {FormatOneDecimalInvariant(first.ManagedMb)}MB -> {FormatOneDecimalInvariant(last.ManagedMb)}MB (delta: {FormatSignedOneDecimalInvariant(last.ManagedMb - first.ManagedMb)}MB)");
        builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
        builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
        builder.AppendLine($"GC Pause%:      {FormatOneDecimalInvariant(first.GcPause)}% -> {FormatOneDecimalInvariant(last.GcPause)}% (delta: {FormatSignedOneDecimalInvariant(last.GcPause - first.GcPause)}%)");
    }

    private static string FormatOneDecimalInvariant(double value)
        => AutomationSnapshotFormatter.FormatNumber(value, "F1");

    private static string FormatSignedOneDecimalInvariant(double value)
        => AutomationSnapshotFormatter.FormatNumber(value, "+0.0;-0.0;0.0");

    private sealed class TimelineRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public double CaptureFps { get; init; }
        public double PreviewFps { get; init; }
        public int VidQueue { get; init; }
        public long VidDrops { get; init; }
        public double CaptureAvgMs { get; init; }
        public double CaptureP95Ms { get; init; }
        public double CaptureP99Ms { get; init; }
        public double CaptureMaxMs { get; init; }
        public double CaptureOnePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public double PreviewD3DPipelineP95Ms { get; init; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; init; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public long LatencyMs { get; init; }
        public double CpuPct { get; init; }
        public double WorkingMb { get; init; }
        public double ManagedMb { get; init; }
        public int Gen0 { get; init; }
        public int Gen1 { get; init; }
        public int Gen2 { get; init; }
        public double GcPause { get; init; }
        public int Workers { get; init; }
        public int IoThreads { get; init; }
    }

    public static string FormatSnapshot(JsonElement snapshotResponse)
    {
        if (snapshotResponse.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot response was not a JSON object.";
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(snapshotResponse, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        AppendSnapshotStateSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureSettingsSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotAudioSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotVideoPipelineSection(builder, snapshot);
        AppendSnapshotThreadHealthSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotRecordingSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotFlashbackSection(builder, snapshot);
        AppendSnapshotDiagnosticLanesSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotPerformanceSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotMemorySection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureCadenceSection(builder, snapshot);
        AppendSnapshotMjpegTimingSection(builder, snapshot);
        AppendSnapshotAvSyncSection(builder, snapshot);
        AppendSnapshotPreviewSection(builder, snapshot);
        AppendSnapshotSourceSection(builder, snapshot);

        return builder.ToString().TrimEnd();
    }

    private static void AppendSnapshotStateSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Sussudio State ==");
        builder.AppendLine($"Status: {AutomationSnapshotFormatter.Get(snapshot, "SessionState")} | {AutomationSnapshotFormatter.Get(snapshot, "StatusText")}");
        builder.AppendLine($"Capture Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandPendingCommands")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsEnqueued")} done={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCompleted")} fail={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsFailed")} cancel={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCanceled")} coalesced={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCoalesced")} last={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastCommand", "None")} outcome={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastOutcome", "None")} corr={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastCorrelationId", "")} error={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastError", "")}");
        builder.AppendLine($"Device: {AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceName")} ({AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {AutomationSnapshotFormatter.Get(snapshot, "IsInitialized")} | Previewing: {AutomationSnapshotFormatter.Get(snapshot, "IsPreviewing")} | Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")}");
    }

    private static void AppendSnapshotAudioSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioEnabled")} | Preview: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {AutomationSnapshotFormatter.Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {AutomationSnapshotFormatter.Get(snapshot, "AudioPeak")} | Clipping: {AutomationSnapshotFormatter.Get(snapshot, "AudioClipping")} | Signal: {AutomationSnapshotFormatter.Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "AudioReaderActive")} | Frames: {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesWrittenToSink")} to sink");
    }

    private static void AppendSnapshotCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)
    {
        var frameRateSummary = FormatSnapshotFrameRateSummary(snapshot);

        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(snapshot, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedVideoFormat")} | Split Encode: {AutomationSnapshotFormatter.Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrEnabled")} (Available: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrAvailable")}, Active: {AutomationSnapshotFormatter.Get(snapshot, "HdrOutputActive")}, State: {AutomationSnapshotFormatter.Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={AutomationSnapshotFormatter.Get(snapshot, "RequestedPipelineMode")} Active={AutomationSnapshotFormatter.Get(snapshot, "ActivePipelineMode")} Matched={AutomationSnapshotFormatter.Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Preview Volume={AutomationSnapshotFormatter.Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={AutomationSnapshotFormatter.Get(snapshot, "IsStatsVisible")}");
    }

    private static string FormatSnapshotFrameRateSummary(JsonElement snapshot)
    {
        var selectedFriendlyFrameRate = AutomationSnapshotFormatter.Get(snapshot, "SelectedFriendlyFrameRate", string.Empty);
        var selectedExactFrameRate = AutomationSnapshotFormatter.Get(snapshot, "SelectedExactFrameRate", string.Empty);
        var selectedExactFrameRateArg = AutomationSnapshotFormatter.Get(snapshot, "SelectedExactFrameRateArg", string.Empty);
        var frameRateBucket = string.IsNullOrWhiteSpace(selectedFriendlyFrameRate)
            ? AutomationSnapshotFormatter.Get(snapshot, "SelectedFrameRate")
            : selectedFriendlyFrameRate;
        var frameRateExactDetail = !string.IsNullOrWhiteSpace(selectedExactFrameRateArg)
            ? $"{selectedExactFrameRate} fps, {selectedExactFrameRateArg}"
            : !string.IsNullOrWhiteSpace(selectedExactFrameRate)
                ? $"{selectedExactFrameRate} fps"
                : string.Empty;
        return string.IsNullOrWhiteSpace(frameRateExactDetail)
            ? $"{frameRateBucket} fps"
            : $"{frameRateBucket} fps ({frameRateExactDetail})";
    }

    private static void AppendSnapshotVideoPipelineSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "VideoReaderActive")} | Ingest: {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {AutomationSnapshotFormatter.Get(snapshot, "FfmpegVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={AutomationSnapshotFormatter.Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesDropped")}");
        builder.AppendLine($"Freshness: reader {AutomationSnapshotFormatter.Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={AutomationSnapshotFormatter.Get(snapshot, "MemoryPreference")} ReqSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoRequestedSubtype")} NegSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoNegotiatedSubtype")} Errors={AutomationSnapshotFormatter.Get(snapshot, "VideoIngestErrorCount")}");
    }

    private static void AppendSnapshotThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        AppendSnapshotSourceReaderThreadHealthLine(builder, snapshot);
        AppendSnapshotWasapiCaptureThreadHealthLine(builder, snapshot);
        AppendSnapshotWasapiPlaybackThreadHealthLine(builder, snapshot);
    }

    private static void AppendSnapshotSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var sourceReaderLastFrameAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var sourceReaderOutstanding = AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderFrameChannelDepth")}");
    }

    private static void AppendSnapshotWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiCaptureLastCallbackAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        builder.AppendLine(
            $"WASAPI Capture: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
    }

    private static void AppendSnapshotWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiPlaybackLastRenderAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        builder.AppendLine(
            $"WASAPI Playback: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"drops={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
        builder.AppendLine(
            $"Audio Buffer: status={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferHealthStatus")} " +
            $"underrun={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferUnderrunDetected")} overrun={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferOverrunDetected")} " +
            $"underrunEvents={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferUnderrunEvents")} overrunEvents={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferOverrunEvents")} " +
            $"reason={AutomationSnapshotFormatter.Get(snapshot, "AudioBufferHealthReason", string.Empty)}");
    }

    private static void AppendSnapshotRecordingSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")} | Output: {AutomationSnapshotFormatter.Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {AutomationSnapshotFormatter.Get(snapshot, "RecordingTime")} | Size: {AutomationSnapshotFormatter.Get(snapshot, "RecordingSizeInfo")} | Bitrate: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBackend")} | Audio Path: {AutomationSnapshotFormatter.Get(snapshot, "AudioPathMode")} | Mux: {AutomationSnapshotFormatter.Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityStatus")} complete={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityComplete")} backend={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackend")} source={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySourceFrames")} accepted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioEnabled")} active={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {AutomationSnapshotFormatter.Get(snapshot, "LastOutputPath")} ({AutomationSnapshotFormatter.Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {AutomationSnapshotFormatter.Get(snapshot, "LastFinalizeStatus")}");
    }

    private static void AppendSnapshotDiagnosticLanesSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Diagnostics ==");
        builder.AppendLine($"Health: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticHealthStatus")} | Stage: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticLikelyStage")}");
        builder.AppendLine($"Summary: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticSummary")}");
        builder.AppendLine($"Evidence: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticEvidence")}");
        builder.AppendLine("Frame Lanes:");
        builder.AppendLine($"  Source: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticSourceLane")}");
        builder.AppendLine($"  Decode: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticDecodeLane")}");
        builder.AppendLine($"  Preview: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticPreviewLane")}");
        builder.AppendLine($"  Render: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticRenderLane")}");
        builder.AppendLine($"  Present: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticPresentLane")}");
        builder.AppendLine($"  Recording: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticRecordingLane")}");
        builder.AppendLine($"  Audio: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticAudioLane")}");
    }

    private static void AppendSnapshotPerformanceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceScore")} | Perfection: {AutomationSnapshotFormatter.Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {AutomationSnapshotFormatter.Get(snapshot, "EstimatedPipelineLatencyMs")}ms (app receive -> estimated visible)");
    }

    private static void AppendSnapshotMemorySection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuPercent")}% | CPU Time: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
    }

    private static void AppendSnapshotCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatFrameBudgetMs(snapshot, "ExpectedCaptureFrameRate")} avg={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP95IntervalMs")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP99IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms | Samples: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {AutomationSnapshotFormatter.Get(snapshot, "ExpectedCaptureFrameRate")} fps)");
        builder.AppendLine($"5% Low: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceOnePercentLowFps")} fps");
        builder.AppendLine($"Jitter: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        builder.AppendLine($"MJPEG Packet Fingerprint: input={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashInputObservedFps")} fps unique={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashUniqueObservedFps")} fps dup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashDuplicateFramePercent")}% pattern={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashPattern")} longestDup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashLongestDuplicateRun")}");
        builder.AppendLine($"Sampled Decoded Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionConfidence")}");
        builder.AppendLine($"Sampled Tight Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionConfidence")}");
    }

    private static void AppendSnapshotAvSyncSection(StringBuilder builder, JsonElement snapshot)
    {
        var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorr = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (string.IsNullOrWhiteSpace(avSyncDrift) && string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== AV Sync ==");
        builder.AppendLine(
            $"Capture Drift: {(string.IsNullOrWhiteSpace(avSyncDrift) ? "N/A" : avSyncDrift + "ms")} | " +
            $"Rate: {(string.IsNullOrWhiteSpace(avSyncRate) ? "N/A" : avSyncRate + "ms/s")}");
        if (!string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            builder.AppendLine(
                $"Encoder Drift: {avSyncEncoder}ms | " +
                $"Correction Samples: {(string.IsNullOrWhiteSpace(avSyncCorr) ? "N/A" : avSyncCorr)}");
        }
    }

    private static void AppendSnapshotPreviewSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = AutomationSnapshotFormatter.Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {AutomationSnapshotFormatter.Get(snapshot, "PreviewStartupState")} | First Visual: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPlaybackState")} | Video: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionEventCount")}");
        }
        else if (IsSnapshotPreviewD3DRendererMode(rendererMode))
        {
            AppendSnapshotPreviewD3DSection(builder, snapshot);
        }
        else
        {
            builder.AppendLine($"Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDisplayed")} displayed, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
            builder.AppendLine($"Pacing Classifier: stage={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
        }
    }

    private static bool IsSnapshotPreviewD3DRendererMode(string rendererMode)
        => rendererMode == "D3D11VideoProcessor" ||
           rendererMode == "Nv12Shader" ||
           rendererMode == "HdrShader" ||
           rendererMode == "HdrPassthrough";

    private static void AppendSnapshotPreviewD3DSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Swap Chain: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
        builder.AppendLine($"D3D Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesRendered")} rendered, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPendingFrameCount")}");
        var renderThreadFailures = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderThreadFailureCount", "0");
        if (renderThreadFailures != "0")
        {
            builder.AppendLine($"D3D Render Thread Failures: {renderThreadFailures} last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureType")} hr={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureHResult")} msg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureMessage")}");
        }

        builder.AppendLine($"Color: input={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputColorSpace")} output={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DOutputColorSpace")}");
        builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
        builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
        AppendSnapshotPreviewD3DCpuTiming(builder, snapshot);
        AppendSnapshotPreviewD3DPipelineLatency(builder, snapshot);
        AppendSnapshotPreviewD3DFrameLatencyWait(builder, snapshot);
        AppendSnapshotPreviewD3DFrameStats(builder, snapshot);
        AppendSnapshotPreviewD3DFrameOwnership(builder, snapshot);
        AppendSnapshotPreviewSlowFrameDiagnostics(builder, snapshot);
    }

    private static void AppendSnapshotPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)
    {
        AutomationSnapshotFormatter.AppendPreviewSlowFrameDiagnostics(builder, snapshot);
    }

    private static void AppendSnapshotPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D CPU timing: input/upload avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D pipeline latency: avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D frame-latency wait: enabled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D DXGI stats: ok={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
    }

    private static void AppendSnapshotPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Ownership: submitted present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDropReason")} dropPts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
    }

    private static void AppendSnapshotSourceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Source ==");
        var sourceFrameRate = AutomationSnapshotFormatter.Get(snapshot, "DetectedSourceFrameRate", string.Empty);
        var sourceFrameRateArg = AutomationSnapshotFormatter.Get(snapshot, "DetectedSourceFrameRateArg", string.Empty);
        var sourceFpsSummary = !string.IsNullOrWhiteSpace(sourceFrameRateArg)
            ? $"{sourceFrameRate}fps ({sourceFrameRateArg})"
            : !string.IsNullOrWhiteSpace(sourceFrameRate)
                ? $"{sourceFrameRate}fps"
                : "N/A";
        builder.AppendLine($"Source: {AutomationSnapshotFormatter.Get(snapshot, "SourceWidth")} x {AutomationSnapshotFormatter.Get(snapshot, "SourceHeight")} @ {sourceFpsSummary} HDR={AutomationSnapshotFormatter.Get(snapshot, "SourceIsHdr")}");
        builder.AppendLine($"Telemetry: {AutomationSnapshotFormatter.Get(snapshot, "SourceTelemetryAvailability")} ({AutomationSnapshotFormatter.Get(snapshot, "SourceTelemetryConfidence")})");
    }


    private static void AppendSnapshotFlashbackSection(StringBuilder builder, JsonElement snapshot)
    {
        var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, "FlashbackActive", "false");
        var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed", "false");
        if (!flashbackActive.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !flashbackFailed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine("== Flashback ==");
        AppendSnapshotFlashbackEncodingSection(builder, snapshot);
        AppendSnapshotFlashbackPlaybackStatusSection(builder, snapshot);
        AppendSnapshotFlashbackExportSection(builder, snapshot);
        AppendSnapshotFlashbackPlaybackMetricsSection(builder, snapshot);
        builder.AppendLine();
    }

    private static void AppendSnapshotFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)
    {
        AppendSnapshotFlashbackEncodingStatusSection(builder, snapshot);
        AppendSnapshotFlashbackEncodingHealthSection(builder, snapshot);
    }

    private static void AppendSnapshotFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        var encCodec = AutomationSnapshotFormatter.Get(snapshot, "EncoderCodecName");
        if (!string.IsNullOrEmpty(encCodec))
        {
            var encW = AutomationSnapshotFormatter.Get(snapshot, "EncoderWidth", "0");
            var encH = AutomationSnapshotFormatter.Get(snapshot, "EncoderHeight", "0");
            var encFps = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRate", "0");
            var encFpsNum = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRateNumerator", "");
            var encFpsDen = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRateDenominator", "");
            var encFpsDetail = !string.IsNullOrWhiteSpace(encFpsNum) &&
                               !string.IsNullOrWhiteSpace(encFpsDen) &&
                               encFpsDen != "0"
                ? $"{encFps} fps ({encFpsNum}/{encFpsDen})"
                : $"{encFps} fps";
            var encBr = AutomationSnapshotFormatter.GetDouble(snapshot, "EncoderTargetBitRate") / 1_000_000.0;
            builder.AppendLine($"Encoder: {encCodec} {encW}x{encH} @ {encFpsDetail} | Target: {AutomationSnapshotFormatter.FormatNumber(encBr, "0.#")} Mbps");
        }

        var fbDurationMs = AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackBufferedDurationMs");
        var fbDiskMb = AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackDiskBytes") / (1024.0 * 1024.0);
        builder.AppendLine($"Buffer: {AutomationSnapshotFormatter.FormatNumber(fbDurationMs / 1000.0, "F1")}s | Disk: {AutomationSnapshotFormatter.FormatNumber(fbDiskMb, "F1")} MB | Written: {AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTotalBytesWritten"))} | GPU Encode: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuEncoding")}");
        builder.AppendLine($"Temp Cache: cache={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBytes"))} budget={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBudgetBytes"))} free={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTempDriveFreeBytes"))} sessions={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheSessionCount")} deleted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheDeletedSessionCount")} freed={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheFreedBytes"))} overBudget={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheOverBudget")}");
        builder.AppendLine($"Encoded: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDroppedFrames")} | forceRotate={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateActive")} requested={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateRequested")} draining={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateDraining")} | VQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueMaxDepth")} AQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueCapacity")}");
        builder.AppendLine($"Flashback Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoSequenceGaps")} backendStale={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendSettingsStale")} staleReason={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendSettingsStaleReason", "")} active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendActiveFormat")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendActivePreset")} requested={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendRequestedFormat")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendRequestedPreset")}");
        builder.AppendLine($"Cleanup: fatal={AutomationSnapshotFormatter.Get(snapshot, "FatalCleanupInProgress")} flashback={AutomationSnapshotFormatter.Get(snapshot, "FlashbackCleanupInProgress")}");
    }

    private static void AppendSnapshotFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Flashback Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencySampleCount")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastRejectReason", "")}");
        builder.AppendLine($"Flashback Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Flashback Failure: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
        builder.AppendLine($"Flashback GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesDropped")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueLastRejectReason", "")}");
    }

    private static void AppendSnapshotFlashbackExportSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Export: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportActive")} status={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportStatus")} id={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportId")} lastResultId={AutomationSnapshotFormatter.Get(snapshot, "LastExportId")} kind={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportFailureKind", "None")} progress={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportPercent")}% segments={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportSegmentsProcessed")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportTotalSegments")} elapsed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={AutomationSnapshotFormatter.FormatBytes((long)AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportInPointMs")}ms out={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} forceRotateFallbacks={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportForceRotateFallbacks")} lastForceRotateFallbackSegments={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastForceRotateFallbackSegments")} lastForceRotateFallbackUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastForceRotateFallbackUtcUnixMs")} path={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutputPath")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportMessage", "")}");
    }

    private static void AppendSnapshotFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Playback: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackState")} | Pos: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPendingCommands")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms maxLatencyCommand={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand")} enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} coalescedSeek={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSeekCommandsCoalesced")} threadAlive={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
    }

    private static void AppendSnapshotFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)
    {
        var pbFps = AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackPlaybackObservedFps");
        var pbAvgMs = AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackPlaybackAvgFrameMs");
        var avDrift = AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackAvDriftMs");
        builder.AppendLine($"Playback Frame Time: avg={AutomationSnapshotFormatter.FormatNumber(pbAvgMs, "F2")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackP95FrameMs")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackP99FrameMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxFrameMs")}ms | Average Rate: {AutomationSnapshotFormatter.FormatNumber(pbFps, "F1")} fps | Target: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackTargetFps")} fps | 5% Low: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSampleDurationMs")}ms");
        builder.AppendLine($"Playback Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeMaxMs")}ms phase={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodePhase", "")} receive={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeReceiveMs")}ms feed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeFeedMs")}ms read={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeReadMs")}ms send={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeSendMs")}ms audio={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeAudioMs")}ms convert={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodeConvertMs")}ms maxPos={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxDecodePositionMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeSampleCount")} seekCapHits={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")} lastSeekCap={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap")}");
        builder.AppendLine($"Playback Frames: total={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFrameCount")} late={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLateFrames")} slow={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSlowFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSlowFramePercent")}%) dropped={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDroppedFrames")} audioMasterDouble={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterDelayDoubles")} audioMasterShrink={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterDelayShrinks")} audioMasterFallback={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterFallbacks")} unavailable={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks")} stale={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterStaleFallbacks")} driftOutlier={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks")} lastAudioFallback={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackReason", "")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs")}ms lastDrop={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastDropReason", "")} lastDropUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastDropUtcUnixMs")} submitFailures={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSubmitFailures")} lastSubmitFailure={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastSubmitFailure", "")} lastSubmitFailureUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastSubmitFailureUtcUnixMs")}");
        builder.AppendLine($"Playback Stages: switches={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSegmentSwitches")} fmp4Reopens={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFmp4Reopens")} writeHeadWaits={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackWriteHeadWaits")} nearLiveSnaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackNearLiveSnaps")} decodeErrorSnaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeErrorSnaps")} lastWriteHeadGap={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs")}ms");
        builder.AppendLine($"A/V Drift: {AutomationSnapshotFormatter.FormatNumber(avDrift, "+0.0;-0.0;0.0")}ms (+ = audio ahead) | Audio buffered={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackBufferedDurationMs")}ms queue={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDurationMs")}ms active={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")}ms endpoint={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")}ms streamLatency={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackStreamLatencyMs")}ms | File: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackFilePath")}");
    }


    private static void AppendSnapshotMjpegTimingSection(StringBuilder builder, JsonElement snapshot)
    {
        var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeSampleCount", "0");
        var mjpegDecoderCount = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount", "0");
        var hasCompressedActivity =
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesQueued", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesDequeued", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsQueueFull", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsByteBudget", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsDisposed", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueDepth", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueBytes", "0") != "0";
        if (mjpegDecodeSamples == "0" && mjpegDecoderCount == "0" && !hasCompressedActivity)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== MJPEG Pipeline Timing ==");
        AppendSnapshotMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);
        AppendSnapshotMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);
        AppendSnapshotMjpegPreviewJitterSection(builder, snapshot);
        AppendSnapshotMjpegPerDecoderTimingLines(builder, snapshot);
    }

    private static void AppendSnapshotMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)
    {
        if (mjpegDecodeSamples == "0")
        {
            return;
        }

        builder.AppendLine($"Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
        builder.AppendLine($"Interop Copy: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
        builder.AppendLine($"Total Callback: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackSampleCount")} samples)");
    }

    private static void AppendSnapshotMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)
    {
        builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDecoded")} Emitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalEmitted")} Dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDropped")}");
        builder.AppendLine(
            $"Compressed Queue: depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueDepth")} bytes={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueBytes")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
            $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesDequeued")} " +
            $"drops(full={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsDisposed")})");
        builder.AppendLine(
            $"MJPEG Drop Reasons: decode={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeFailures")} reorderCollision={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderCollisions")} emit={AutomationSnapshotFormatter.Get(snapshot, "MjpegEmitFailures")}");
        builder.AppendLine($"Reorder: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSkips")} Buffer={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderBufferDepth")} PeakBuffer={AutomationSnapshotFormatter.Get(snapshot, "MjpegPeakReorderDepth")} PeakCompressedBytes={AutomationSnapshotFormatter.Get(snapshot, "MjpegPeakCompressedQueueBytes")} ForceDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderRingForceDrops")}");
        builder.AppendLine($"Pipeline: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineSampleCount")} samples)");
    }

    private static void AppendSnapshotMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)
    {
        if (!string.Equals(AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine(
            $"Preview Jitter: target={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
            $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
            $"clearedDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterClearedDropCount")} deadlineDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterUnderflowCount")} resumeReprimes={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterResumeReprimeCount")} " +
            $"target+={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
        builder.AppendLine(
            $"Preview Jitter Input: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Output: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Latency: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Ownership: present={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
            $"sourceLatency={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
        builder.AppendLine(
            $"Preview Jitter Underflow: reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowReason")} " +
            $"queue={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowQueueDepth")} " +
            $"inputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs")}ms outputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs")}ms " +
            $"scheduleLateLast={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastScheduleLateMs")}ms scheduleLateMax={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxScheduleLateMs")}ms count={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterScheduleLateCount")}");
    }

    private static void AppendSnapshotMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) ||
            perDecoder.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var worker in perDecoder.EnumerateArray())
        {
            builder.AppendLine(
                $"Decoder[{AutomationSnapshotFormatter.Get(worker, "WorkerIndex", "?")}]: avg={AutomationSnapshotFormatter.Get(worker, "AvgMs")}ms " +
                $"P95={AutomationSnapshotFormatter.Get(worker, "P95Ms")}ms max={AutomationSnapshotFormatter.Get(worker, "MaxMs")}ms " +
                $"({AutomationSnapshotFormatter.Get(worker, "SampleCount")} samples)");
        }
    }
}
