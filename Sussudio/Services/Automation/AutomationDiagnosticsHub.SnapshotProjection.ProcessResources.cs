namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)
        => new()
        {
            MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,
            MemoryPrivateBytesMb = processResources.MemoryPrivateBytesMb,
            MemoryManagedHeapMb = processResources.MemoryManagedHeapMb,
            MemoryTotalAllocatedMb = processResources.MemoryTotalAllocatedMb,
            ProcessCpuPercent = processResources.ProcessCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processResources.ProcessCpuTotalProcessorTimeMs,
            MemoryGcHeapSizeMb = processResources.MemoryGcHeapSizeMb,
            MemoryGcGen0Collections = processResources.MemoryGcGen0Collections,
            MemoryGcGen1Collections = processResources.MemoryGcGen1Collections,
            MemoryGcGen2Collections = processResources.MemoryGcGen2Collections,
            MemoryGcPauseTimePercent = processResources.MemoryGcPauseTimePercent,
            MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,
            ThreadPoolWorkerAvailable = processResources.ThreadPoolWorkerAvailable,
            ThreadPoolWorkerMax = processResources.ThreadPoolWorkerMax,
            ThreadPoolIoAvailable = processResources.ThreadPoolIoAvailable,
            ThreadPoolIoMax = processResources.ThreadPoolIoMax
        };

    private readonly record struct ProcessResourceProjection
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
