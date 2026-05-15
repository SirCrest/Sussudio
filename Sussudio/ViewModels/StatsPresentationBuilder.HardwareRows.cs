using System.Collections.Generic;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(
        StatsHardwareDecodeRowsInput mjpeg)
    {
        var rows = new List<StatsHardwareRowPresentation>();

        void SetRow(string label, string value)
        {
            rows.Add(new StatsHardwareRowPresentation(label, value));
        }

        var effectiveFrameTimeMs = mjpeg.DecodeAvgMs / mjpeg.DecoderCount;
        var effectiveFps = effectiveFrameTimeMs > 0 ? 1000.0 / effectiveFrameTimeMs : 0;

        SetRow("Throughput", $"{effectiveFrameTimeMs:0.00}ms ({effectiveFps:0}fps peak)");
        SetRow("Decode", $"{mjpeg.DecodeAvgMs:0.00} / {mjpeg.DecodeP95Ms:0.00}ms  avg/P95 ({mjpeg.DecoderCount}T)");
        SetRow("Reorder", $"{mjpeg.ReorderAvgMs:0.00} / {mjpeg.ReorderP95Ms:0.00}ms  avg/P95");
        SetRow("Pipeline", $"{mjpeg.PipelineAvgMs:0.00} / {mjpeg.PipelineP95Ms:0.00}ms  avg/P95");
        SetRow("Frames", $"{mjpeg.TotalEmitted:N0} emitted / {mjpeg.TotalDropped:N0} dropped");

        var compressedMb = mjpeg.CompressedQueueBytes / (1024.0 * 1024.0);
        var budgetMb = mjpeg.CompressedQueueByteBudget / (1024.0 * 1024.0);
        var bufferInfo = $"compressed={mjpeg.CompressedQueueDepth} ({compressedMb:0.0}/{budgetMb:0.0}MB)  reorder={mjpeg.ReorderBufferDepth}";
        if (mjpeg.PendingPreviewFrameCount.HasValue)
        {
            bufferInfo += $"  preview={mjpeg.PendingPreviewFrameCount.Value}";
        }

        if (mjpeg.ReorderSkips > 0)
        {
            bufferInfo += $"  skips={mjpeg.ReorderSkips:N0}";
        }

        SetRow("Buffers", bufferInfo);

        foreach (var worker in mjpeg.PerDecoder)
        {
            SetRow($"Thread {worker.WorkerIndex}", $"{worker.AvgMs:0.00} / {worker.P95Ms:0.00}ms");
        }

        return rows;
    }

    public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)
    {
        var rows = new List<StatsHardwareRowPresentation>();

        void SetRow(string label, string value)
        {
            rows.Add(new StatsHardwareRowPresentation(label, value));
        }

        if (nvml == null)
        {
            SetRow("Status", "NVML not available");
            return rows;
        }

        var gpu = nvml.Value;
        SetRow("GPU", string.IsNullOrWhiteSpace(gpu.GpuName) ? "\u2014" : gpu.GpuName);
        SetRow("Utilization", $"{gpu.GpuUtilizationPercent ?? 0}% (Mem: {gpu.GpuMemoryUtilizationPercent ?? 0}%)");
        SetRow("NVDEC", $"{gpu.NvdecUtilizationPercent ?? 0}%");
        SetRow("NVENC", $"{gpu.NvencUtilizationPercent ?? 0}%");
        SetRow("PCIe TX", $"{gpu.PcieTxMBps ?? 0:0.0} MB/s");
        SetRow("PCIe RX", $"{gpu.PcieRxMBps ?? 0:0.0} MB/s");
        SetRow("VRAM", $"{gpu.VramUsedMB ?? 0} / {gpu.VramTotalMB ?? 0} MB");
        SetRow("Temperature", $"{gpu.GpuTemperatureC ?? 0}\u00B0C");
        SetRow("Power", $"{gpu.GpuPowerW ?? 0:0.0}W");
        SetRow("Clocks", $"{gpu.GpuClockMHz ?? 0} MHz (Mem: {gpu.GpuMemClockMHz ?? 0} MHz)");
        return rows;
    }
}
