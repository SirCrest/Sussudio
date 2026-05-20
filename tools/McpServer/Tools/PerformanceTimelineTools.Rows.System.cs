using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void PopulateSystemTimelineRow(JsonElement item, TimelineRow row)
    {
        row.LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs");
        row.WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb");
        row.ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb");
        row.Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections");
        row.Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections");
        row.Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections");
        row.GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent");
        row.Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable");
        row.IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable");
    }
}
