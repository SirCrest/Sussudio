using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsHardwareRowsControllerContext
{
    public required UIElement DecodeSection { get; init; }
    public required StackPanel DecodeContent { get; init; }
    public required StackPanel GpuContent { get; init; }
    public required StatsDiagnosticRowsController DiagnosticRowsController { get; init; }
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsHardwareRowsController
{
    private readonly StatsHardwareRowsControllerContext _context;

    public StatsHardwareRowsController(StatsHardwareRowsControllerContext context)
    {
        _context = context;
    }

    public void UpdateDecodeSection()
    {
        var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();
        if (mjpegMetrics is not { DecoderCount: > 0 } mjpeg)
        {
            _context.DecodeSection.Visibility = Visibility.Collapsed;
            _context.DiagnosticRowsController.CollapseDecodeRows(_context.DecodeContent);
            return;
        }

        _context.DecodeSection.Visibility = Visibility.Visible;
        var rows = StatsPresentationBuilder.BuildHardwareDecodeRows(
            CreateDecodeRowsInput(mjpeg, _context.GetPendingPreviewFrameCount()));
        _context.DiagnosticRowsController.UpdateDecodeRows(_context.DecodeContent, rows);
    }

    public void UpdateGpuSection()
    {
        var rows = StatsPresentationBuilder.BuildHardwareGpuRows(CreateGpuRowsInput(_context.GetNvmlSnapshot()));
        _context.DiagnosticRowsController.UpdateGpuRows(_context.GpuContent, rows);
    }

    private static StatsHardwareDecodeRowsInput CreateDecodeRowsInput(
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

    private static StatsHardwareGpuRowsInput? CreateGpuRowsInput(NvmlSnapshot? nvml)
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
