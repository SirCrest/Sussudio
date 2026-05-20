namespace Sussudio.Models;

public sealed partial class PerformanceTimelineEntry
{
    public long PipelineLatencyMs { get; init; }
    public double ProcessCpuPercent { get; init; }
    public double MemoryWorkingSetMb { get; init; }
    public double MemoryManagedHeapMb { get; init; }
    public int GcGen0Collections { get; init; }
    public int GcGen1Collections { get; init; }
    public int GcGen2Collections { get; init; }
    public double GcPauseTimePercent { get; init; }
    public int ThreadPoolWorkerAvailable { get; init; }
    public int ThreadPoolIoAvailable { get; init; }
}
