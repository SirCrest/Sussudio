using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class MemoryDiagnosticsTools
{
    [McpServerTool, Description("Get memory, GC, and thread pool diagnostics for the running application. Shows working set, managed heap, GC collection counts, pause time, fragmentation, and thread pool utilization.")]
    public static async Task<string> get_memory_diagnostics(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return GetMessage(response);
        }

        if (!response.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot data not available.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Memory ==");
        builder.AppendLine($"Working Set:     {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB");
        builder.AppendLine($"Private Bytes:   {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB");
        builder.AppendLine($"Managed Heap:    {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB (lifetime)");
        builder.AppendLine($"GC Heap Size:    {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine();
        builder.AppendLine("== GC ==");
        builder.AppendLine($"Gen 0 Collections: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")}");
        builder.AppendLine($"Gen 1 Collections: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")}");
        builder.AppendLine($"Gen 2 Collections: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"Pause Time:        {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}%");
        builder.AppendLine($"Fragmentation:     {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine();
        builder.AppendLine("== Thread Pool ==");
        builder.AppendLine($"Worker Threads: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")} available / {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} max");
        builder.AppendLine($"IO Threads:     {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")} available / {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} max");

        return builder.ToString().TrimEnd();
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

}
