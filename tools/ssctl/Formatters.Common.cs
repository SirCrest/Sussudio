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
}
