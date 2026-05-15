using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Services.Gpu;

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
        var rows = BuildDecodeRows(mjpeg, _context.GetPendingPreviewFrameCount());
        _context.DiagnosticRowsController.UpdateDecodeRows(_context.DecodeContent, rows);
    }

    public void UpdateGpuSection()
    {
        var rows = BuildGpuRows(_context.GetNvmlSnapshot());
        _context.DiagnosticRowsController.UpdateGpuRows(_context.GpuContent, rows);
    }

    internal static IReadOnlyList<StatsDiagnosticSimpleRow> BuildDecodeRows(
        ParallelMjpegDecodePipeline.PipelineTimingMetrics mjpeg,
        int? pendingPreviewFrameCount)
    {
        var rows = new List<StatsDiagnosticSimpleRow>();

        void SetRow(string label, string value)
        {
            rows.Add(new StatsDiagnosticSimpleRow(label, value));
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
        if (pendingPreviewFrameCount.HasValue)
        {
            bufferInfo += $"  preview={pendingPreviewFrameCount.Value}";
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

    internal static IReadOnlyList<StatsDiagnosticSimpleRow> BuildGpuRows(NvmlSnapshot? nvml)
    {
        var rows = new List<StatsDiagnosticSimpleRow>();

        void SetRow(string label, string value)
        {
            rows.Add(new StatsDiagnosticSimpleRow(label, value));
        }

        if (nvml == null)
        {
            SetRow("Status", "NVML not available");
            return rows;
        }

        SetRow("GPU", string.IsNullOrWhiteSpace(nvml.GpuName) ? "\u2014" : nvml.GpuName);
        SetRow("Utilization", $"{nvml.GpuUtilizationPercent ?? 0}% (Mem: {nvml.GpuMemoryUtilizationPercent ?? 0}%)");
        SetRow("NVDEC", $"{nvml.NvdecUtilizationPercent ?? 0}%");
        SetRow("NVENC", $"{nvml.NvencUtilizationPercent ?? 0}%");
        SetRow("PCIe TX", $"{nvml.PcieTxMBps ?? 0:0.0} MB/s");
        SetRow("PCIe RX", $"{nvml.PcieRxMBps ?? 0:0.0} MB/s");
        SetRow("VRAM", $"{nvml.VramUsedMB ?? 0} / {nvml.VramTotalMB ?? 0} MB");
        SetRow("Temperature", $"{nvml.GpuTemperatureC ?? 0}\u00B0C");
        SetRow("Power", $"{nvml.GpuPowerW ?? 0:0.0}W");
        SetRow("Clocks", $"{nvml.GpuClockMHz ?? 0} MHz (Mem: {nvml.GpuMemClockMHz ?? 0} MHz)");
        return rows;
    }
}
