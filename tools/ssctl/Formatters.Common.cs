using System.Text.Json;
using System.Text;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// Console formatters for snapshots, diagnostics, and verification output. Keep
// these projection-only: command behavior belongs in CommandHandlers and
// protocol mechanics belong in the shared pipe client.
internal static partial class Formatters
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
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(data, "SelectedResolution")} | Frame Rate: {AutomationSnapshotFormatter.Get(data, "SelectedFrameRate")}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(data, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(data, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(data, "SelectedPreset")}");
        builder.AppendLine($"Split Encode: {AutomationSnapshotFormatter.Get(data, "SelectedSplitEncodeMode")} | Video Format: {AutomationSnapshotFormatter.Get(data, "SelectedVideoFormat")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(data, "MjpegDecoderCount")}");
        builder.AppendLine($"Preview Volume: {AutomationSnapshotFormatter.Get(data, "PreviewVolumePercent")}% | Stats Visible: {AutomationSnapshotFormatter.Get(data, "IsStatsVisible")}");
        builder.AppendLine();
        AppendNamedOptions(builder, "Devices", data, "Devices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Audio Input Devices", data, "AudioInputDevices", includeId: true);
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
}
