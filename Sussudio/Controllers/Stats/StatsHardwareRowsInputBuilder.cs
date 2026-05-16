using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal static class StatsHardwareRowsInputBuilder
{
    public static StatsHardwareDecodeRowsInput BuildDecodeRowsInput(
        ParallelMjpegDecodePipeline.PipelineTimingMetrics mjpeg,
        int? pendingPreviewFrameCount)
    {
        var workers = new StatsHardwareDecodeWorkerRowInput[mjpeg.PerDecoder.Length];
        for (var i = 0; i < mjpeg.PerDecoder.Length; i++)
        {
            var worker = mjpeg.PerDecoder[i];
            workers[i] = new StatsHardwareDecodeWorkerRowInput(
                WorkerIndex: worker.WorkerIndex,
                AvgMs: worker.AvgMs,
                P95Ms: worker.P95Ms);
        }

        return new StatsHardwareDecodeRowsInput(
            DecoderCount: mjpeg.DecoderCount,
            DecodeAvgMs: mjpeg.DecodeAvgMs,
            DecodeP95Ms: mjpeg.DecodeP95Ms,
            ReorderAvgMs: mjpeg.ReorderAvgMs,
            ReorderP95Ms: mjpeg.ReorderP95Ms,
            PipelineAvgMs: mjpeg.PipelineAvgMs,
            PipelineP95Ms: mjpeg.PipelineP95Ms,
            TotalEmitted: mjpeg.TotalEmitted,
            TotalDropped: mjpeg.TotalDropped,
            CompressedQueueDepth: mjpeg.CompressedQueueDepth,
            CompressedQueueBytes: mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget: mjpeg.CompressedQueueByteBudget,
            ReorderBufferDepth: mjpeg.ReorderBufferDepth,
            ReorderSkips: mjpeg.ReorderSkips,
            PendingPreviewFrameCount: pendingPreviewFrameCount,
            PerDecoder: workers);
    }

    public static StatsHardwareGpuRowsInput? BuildGpuRowsInput(NvmlSnapshot? nvml)
        => nvml == null
            ? null
            : new StatsHardwareGpuRowsInput(
                GpuName: nvml.GpuName,
                GpuUtilizationPercent: nvml.GpuUtilizationPercent,
                GpuMemoryUtilizationPercent: nvml.GpuMemoryUtilizationPercent,
                NvdecUtilizationPercent: nvml.NvdecUtilizationPercent,
                NvencUtilizationPercent: nvml.NvencUtilizationPercent,
                PcieTxMBps: nvml.PcieTxMBps,
                PcieRxMBps: nvml.PcieRxMBps,
                VramUsedMB: nvml.VramUsedMB,
                VramTotalMB: nvml.VramTotalMB,
                GpuTemperatureC: nvml.GpuTemperatureC,
                GpuPowerW: nvml.GpuPowerW,
                GpuClockMHz: nvml.GpuClockMHz,
                GpuMemClockMHz: nvml.GpuMemClockMHz);
}
