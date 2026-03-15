using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class MemoryDiagnosticsTool
{
    [McpServerTool, Description("Get memory, GC, and thread pool diagnostics for the running application. Shows working set, managed heap, GC collection counts, pause time, fragmentation, and thread pool utilization.")]
    public static async Task<string> get_memory_diagnostics(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        if (!IsSuccess(response))
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
        builder.AppendLine($"Working Set:     {Get(snapshot, "MemoryWorkingSetMb")} MB");
        builder.AppendLine($"Private Bytes:   {Get(snapshot, "MemoryPrivateBytesMb")} MB");
        builder.AppendLine($"Managed Heap:    {Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {Get(snapshot, "MemoryTotalAllocatedMb")} MB (lifetime)");
        builder.AppendLine($"GC Heap Size:    {Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine();
        builder.AppendLine("== GC ==");
        builder.AppendLine($"Gen 0 Collections: {Get(snapshot, "MemoryGcGen0Collections")}");
        builder.AppendLine($"Gen 1 Collections: {Get(snapshot, "MemoryGcGen1Collections")}");
        builder.AppendLine($"Gen 2 Collections: {Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"Pause Time:        {Get(snapshot, "MemoryGcPauseTimePercent")}%");
        builder.AppendLine($"Fragmentation:     {Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine();
        builder.AppendLine("== Thread Pool ==");
        builder.AppendLine($"Worker Threads: {Get(snapshot, "ThreadPoolWorkerAvailable")} available / {Get(snapshot, "ThreadPoolWorkerMax")} max");
        builder.AppendLine($"IO Threads:     {Get(snapshot, "ThreadPoolIoAvailable")} available / {Get(snapshot, "ThreadPoolIoMax")} max");

        return builder.ToString().TrimEnd();
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    private static string GetMessage(JsonElement response)
    {
        return ResponseFormatter.Get(response, "Message", "Command failed.");
    }

    private static string Get(JsonElement el, string prop)
    {
        return ResponseFormatter.Get(el, prop);
    }
}
