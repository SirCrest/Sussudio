using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// Hardware-oriented stats rows. The dynamic row element pools live in
// StatsDiagnosticRowsController; this partial only gathers current values.
public sealed partial class MainWindow
{
    private void UpdateDecodeSection()
    {
        var mjpegMetrics = ViewModel.GetMjpegPipelineTimingDetails();
        if (mjpegMetrics is not { DecoderCount: > 0 } mjpeg)
        {
            DecodeSection.Visibility = Visibility.Collapsed;
            _statsDiagnosticRowsController.CollapseDecodeRows(Decode_Content);
            return;
        }

        DecodeSection.Visibility = Visibility.Visible;
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
        if (_d3dRenderer != null)
        {
            bufferInfo += $"  preview={_d3dRenderer.PendingFrameCount}";
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

        _statsDiagnosticRowsController.UpdateDecodeRows(Decode_Content, rows);
    }

    private void UpdateGpuSection()
    {
        var nvml = _nvmlMonitor?.GetLatestSnapshot();
        var rows = new List<StatsDiagnosticSimpleRow>();

        void SetRow(string label, string value)
        {
            rows.Add(new StatsDiagnosticSimpleRow(label, value));
        }

        if (nvml == null)
        {
            SetRow("Status", "NVML not available");
            _statsDiagnosticRowsController.UpdateGpuRows(GPU_Content, rows);
            return;
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
        _statsDiagnosticRowsController.UpdateGpuRows(GPU_Content, rows);
    }
}
