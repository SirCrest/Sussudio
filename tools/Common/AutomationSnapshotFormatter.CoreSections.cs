using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendStateSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Sussudio State ==");
        builder.AppendLine($"Status: {Get(snapshot, "SessionState")} | {Get(snapshot, "StatusText")}");
        builder.AppendLine($"Capture Commands: pending={Get(snapshot, "CaptureCommandPendingCommands")} maxPending={Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={Get(snapshot, "CaptureCommandCommandsEnqueued")} done={Get(snapshot, "CaptureCommandCommandsCompleted")} fail={Get(snapshot, "CaptureCommandCommandsFailed")} cancel={Get(snapshot, "CaptureCommandCommandsCanceled")} coalesced={Get(snapshot, "CaptureCommandCommandsCoalesced")} last={Get(snapshot, "CaptureCommandLastCommand", "None")} outcome={Get(snapshot, "CaptureCommandLastOutcome", "None")} corr={Get(snapshot, "CaptureCommandLastCorrelationId", "")} error={Get(snapshot, "CaptureCommandLastError", "")}");
        builder.AppendLine($"Device: {Get(snapshot, "SelectedDeviceName")} ({Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {Get(snapshot, "IsInitialized")} | Previewing: {Get(snapshot, "IsPreviewing")} | Recording: {Get(snapshot, "IsRecording")}");
        builder.AppendLine();
    }

    private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {Get(snapshot, "IsAudioEnabled")} | Preview: {Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {Get(snapshot, "AudioPeak")} | Clipping: {Get(snapshot, "AudioClipping")} | Signal: {Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {Get(snapshot, "AudioReaderActive")} | Frames: {Get(snapshot, "AudioFramesArrived")} arrived, {Get(snapshot, "AudioFramesWrittenToSink")} to sink");
        builder.AppendLine();
    }

    private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {Get(snapshot, "EstimatedPipelineLatencyMs")}ms (app receive -> estimated visible)");
        builder.AppendLine();
    }

    private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {Get(snapshot, "ProcessCpuPercent")}% | CPU Time: {Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={Get(snapshot, "MemoryGcGen0Collections")} Gen1={Get(snapshot, "MemoryGcGen1Collections")} Gen2={Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {Get(snapshot, "ThreadPoolWorkerAvailable")}/{Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {Get(snapshot, "ThreadPoolIoAvailable")}/{Get(snapshot, "ThreadPoolIoMax")} avail");
        builder.AppendLine();
    }
}
