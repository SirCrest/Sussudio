namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    // === Memory & GC ===
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

    // === AV Sync ===
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }
}
