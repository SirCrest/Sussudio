namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(
        ProcessResourceProjection processResourceProjection)
        => new()
        {
            MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,
            MemoryPrivateBytesMb = processResourceProjection.MemoryPrivateBytesMb,
            MemoryManagedHeapMb = processResourceProjection.MemoryManagedHeapMb,
            MemoryTotalAllocatedMb = processResourceProjection.MemoryTotalAllocatedMb,
            ProcessCpuPercent = processResourceProjection.ProcessCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processResourceProjection.ProcessCpuTotalProcessorTimeMs,
            MemoryGcHeapSizeMb = processResourceProjection.MemoryGcHeapSizeMb,
            MemoryGcGen0Collections = processResourceProjection.MemoryGcGen0Collections,
            MemoryGcGen1Collections = processResourceProjection.MemoryGcGen1Collections,
            MemoryGcGen2Collections = processResourceProjection.MemoryGcGen2Collections,
            MemoryGcPauseTimePercent = processResourceProjection.MemoryGcPauseTimePercent,
            MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,
            ThreadPoolWorkerAvailable = processResourceProjection.ThreadPoolWorkerAvailable,
            ThreadPoolWorkerMax = processResourceProjection.ThreadPoolWorkerMax,
            ThreadPoolIoAvailable = processResourceProjection.ThreadPoolIoAvailable,
            ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax
        };

    private readonly record struct ProcessResourceFlattenedProjection
    {
        public double MemoryWorkingSetMb { get; init; }
        public double MemoryPrivateBytesMb { get; init; }
        public double MemoryManagedHeapMb { get; init; }
        public double MemoryTotalAllocatedMb { get; init; }
        public double ProcessCpuPercent { get; init; }
        public double ProcessCpuTotalProcessorTimeMs { get; init; }
        public double MemoryGcHeapSizeMb { get; init; }
        public int MemoryGcGen0Collections { get; init; }
        public int MemoryGcGen1Collections { get; init; }
        public int MemoryGcGen2Collections { get; init; }
        public double MemoryGcPauseTimePercent { get; init; }
        public double MemoryGcFragmentationPercent { get; init; }
        public int ThreadPoolWorkerAvailable { get; init; }
        public int ThreadPoolWorkerMax { get; init; }
        public int ThreadPoolIoAvailable { get; init; }
        public int ThreadPoolIoMax { get; init; }
    }
}
