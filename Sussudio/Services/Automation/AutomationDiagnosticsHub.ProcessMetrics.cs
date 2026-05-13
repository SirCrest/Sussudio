using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private ProcessResourceSnapshot CaptureProcessResourceSnapshot()
    {
        // Memory & GC metrics are thread-safe and microsecond-cheap.
        _currentProcess.Refresh();
        var processCpuTotalMs = _currentProcess.TotalProcessorTime.TotalMilliseconds;
        var processCpuPercent = CalculateProcessCpuPercent(processCpuTotalMs);
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        ThreadPool.GetAvailableThreads(out var threadPoolWorkerAvailable, out var threadPoolIoAvailable);
        ThreadPool.GetMaxThreads(out var threadPoolWorkerMax, out var threadPoolIoMax);

        return new ProcessResourceSnapshot(
            MemoryWorkingSetMb: _currentProcess.WorkingSet64 / (1024.0 * 1024.0),
            MemoryPrivateBytesMb: _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0),
            MemoryManagedHeapMb: GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            MemoryTotalAllocatedMb: GC.GetTotalAllocatedBytes(precise: false) / (1024.0 * 1024.0),
            ProcessCpuPercent: processCpuPercent,
            ProcessCpuTotalProcessorTimeMs: processCpuTotalMs,
            MemoryGcHeapSizeMb: gcMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0),
            MemoryGcGen0Collections: GC.CollectionCount(0),
            MemoryGcGen1Collections: GC.CollectionCount(1),
            MemoryGcGen2Collections: GC.CollectionCount(2),
            MemoryGcPauseTimePercent: gcMemoryInfo.PauseTimePercentage,
            MemoryGcFragmentationPercent: gcMemoryInfo.HeapSizeBytes > 0
                ? gcMemoryInfo.FragmentedBytes * 100.0 / gcMemoryInfo.HeapSizeBytes
                : 0.0,
            ThreadPoolWorkerAvailable: threadPoolWorkerAvailable,
            ThreadPoolWorkerMax: threadPoolWorkerMax,
            ThreadPoolIoAvailable: threadPoolIoAvailable,
            ThreadPoolIoMax: threadPoolIoMax);
    }

    private double CalculateProcessCpuPercent(double processCpuTotalMs)
    {
        var nowTimestamp = Stopwatch.GetTimestamp();
        var previousTimestamp = _lastProcessCpuSampleTimestamp;
        var previousCpuTotalMs = _lastProcessCpuTotalMs;

        _lastProcessCpuSampleTimestamp = nowTimestamp;
        _lastProcessCpuTotalMs = processCpuTotalMs;

        if (previousTimestamp <= 0)
        {
            return 0.0;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(previousTimestamp, nowTimestamp).TotalMilliseconds;
        if (elapsedMs <= 0)
        {
            return 0.0;
        }

        var cpuDeltaMs = Math.Max(0.0, processCpuTotalMs - previousCpuTotalMs);
        var cpuCapacityMs = elapsedMs * Math.Max(1, Environment.ProcessorCount);
        return Math.Clamp(cpuDeltaMs * 100.0 / cpuCapacityMs, 0.0, 100.0);
    }

    private readonly record struct ProcessResourceSnapshot(
        double MemoryWorkingSetMb,
        double MemoryPrivateBytesMb,
        double MemoryManagedHeapMb,
        double MemoryTotalAllocatedMb,
        double ProcessCpuPercent,
        double ProcessCpuTotalProcessorTimeMs,
        double MemoryGcHeapSizeMb,
        int MemoryGcGen0Collections,
        int MemoryGcGen1Collections,
        int MemoryGcGen2Collections,
        double MemoryGcPauseTimePercent,
        double MemoryGcFragmentationPercent,
        int ThreadPoolWorkerAvailable,
        int ThreadPoolWorkerMax,
        int ThreadPoolIoAvailable,
        int ThreadPoolIoMax);
}
