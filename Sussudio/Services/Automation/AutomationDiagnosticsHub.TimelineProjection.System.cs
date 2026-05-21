using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineSystemProjection BuildPerformanceTimelineSystemProjection(
        AutomationSnapshot snapshot)
        => new(
            PipelineLatencyMs: snapshot.EstimatedPipelineLatencyMs,
            ProcessCpuPercent: snapshot.ProcessCpuPercent,
            MemoryWorkingSetMb: snapshot.MemoryWorkingSetMb,
            MemoryManagedHeapMb: snapshot.MemoryManagedHeapMb,
            GcGen0Collections: snapshot.MemoryGcGen0Collections,
            GcGen1Collections: snapshot.MemoryGcGen1Collections,
            GcGen2Collections: snapshot.MemoryGcGen2Collections,
            GcPauseTimePercent: snapshot.MemoryGcPauseTimePercent,
            ThreadPoolWorkerAvailable: snapshot.ThreadPoolWorkerAvailable,
            ThreadPoolIoAvailable: snapshot.ThreadPoolIoAvailable);

    private readonly record struct PerformanceTimelineSystemProjection(
        long PipelineLatencyMs,
        double ProcessCpuPercent,
        double MemoryWorkingSetMb,
        double MemoryManagedHeapMb,
        int GcGen0Collections,
        int GcGen1Collections,
        int GcGen2Collections,
        double GcPauseTimePercent,
        int ThreadPoolWorkerAvailable,
        int ThreadPoolIoAvailable);
}
