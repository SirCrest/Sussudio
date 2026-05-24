using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for retrieving the current app snapshot and high-level state.
public static class AppStateTools
{
    [McpServerTool, Description("Get the full application state snapshot including device, preview, recording, HDR, audio, and performance status")]
    public static async Task<CallToolResult> get_app_state(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        return McpToolResultFactory.FromResponse(
            response,
            AutomationSnapshotFormatter.FormatSnapshot(response, includeFlashback: true));
    }

    [McpServerTool(UseStructuredContent = true), Description("Get the raw structured application state snapshot for agent consumption.")]
    public static async Task<object> get_app_state_raw(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return CreateError(response);
        }

        if (response.TryGetProperty("Snapshot", out var snapshot))
        {
            return snapshot.Clone();
        }

        return new
        {
            success = false,
            message = "Snapshot data was not available."
        };
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = GetMessage(response),
            errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty),
            status = AutomationSnapshotFormatter.Get(response, "Status", "error")
        };
    }
}

[McpServerToolType]
// MCP tools for recent diagnostic events and health summaries.
public static class DiagnosticsTools
{
    [McpServerTool, Description("Get recent diagnostic events with severity, category, and timestamps")]
    public static async Task<CallToolResult> get_diagnostics(
        PipeClient pipeClient,
        [Description("Maximum number of events to return (default: 50)")] int maxEvents = 50)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEvents"] = maxEvents
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetDiagnostics, payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return McpToolResultFactory.FromText("No diagnostic events were returned.", isError: true);
        }

        var lines = new StringBuilder();
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            var timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc");
            var severity = AutomationSnapshotFormatter.Get(item, "Severity");
            var category = AutomationSnapshotFormatter.Get(item, "Category");
            var message = AutomationSnapshotFormatter.Get(item, "Message");
            lines.AppendLine($"[{timestamp}] [{severity}] [{category}] {message}");
            count++;
        }

        var text = count == 0
            ? "No diagnostic events were returned."
            : lines.ToString().TrimEnd();
        return McpToolResultFactory.FromResponse(response, text);
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }
}

[McpServerToolType]
// MCP tool for process memory, GC, and thread-pool diagnostics.
public static class MemoryDiagnosticsTools
{
    [McpServerTool, Description("Get memory, GC, and thread pool diagnostics for the running application. Shows working set, managed heap, GC collection counts, pause time, fragmentation, and thread pool utilization.")]
    public static async Task<CallToolResult> get_memory_diagnostics(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("Snapshot data not available.", isError: true);
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

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }
}
