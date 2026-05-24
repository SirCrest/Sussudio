using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsDockRefreshControllerContext
{
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsStatsDockVisible { get; init; }
    public required Func<bool> IsDiagnosticsSectionVisible { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required StatsDockPresentationController DockPresentationController { get; init; }
    public required StatsDiagnosticRowsController DiagnosticRowsController { get; init; }
    public required StatsHardwareRowsController HardwareRowsController { get; init; }
}

internal sealed class StatsDockRefreshController
{
    private readonly StatsDockRefreshControllerContext _context;

    public StatsDockRefreshController(StatsDockRefreshControllerContext context)
    {
        _context = context;
    }

    public void RefreshDock()
    {
        if (_context.IsWindowClosing() || !_context.IsStatsDockVisible())
        {
            return;
        }

        var snapshot = _context.GetStatsSnapshot();
        var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);

        _context.DockPresentationController.Apply(presentation);

        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        _context.HardwareRowsController.UpdateDecodeSection();
        _context.HardwareRowsController.UpdateGpuSection();
    }

    public void RefreshDiagnosticsSection()
    {
        var snapshot = _context.GetStatsSnapshot();
        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
    }

    private void UpdateDiagnosticsSection(IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails, string? diagnosticSummary)
    {
        if (!_context.IsDiagnosticsSectionVisible())
        {
            return;
        }

        var presentation = StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary);
        _context.DiagnosticRowsController.UpdateDiagnostics(presentation);
    }
}

internal sealed class StatsHardwareRowsControllerContext
{
    public required UIElement DecodeSection { get; init; }
    public required StackPanel DecodeContent { get; init; }
    public required StackPanel GpuContent { get; init; }
    public required StatsDockRowChromeController RowChromeController { get; init; }
    public required StatsHardwareRowsInputProvider InputProvider { get; init; }
}

internal sealed class StatsHardwareRowsInputProviderContext
{
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsHardwareRowsInputProvider
{
    private readonly StatsHardwareRowsInputProviderContext _context;

    public StatsHardwareRowsInputProvider(StatsHardwareRowsInputProviderContext context)
    {
        _context = context;
    }

    public StatsHardwareDecodeRowsInput? GetDecodeRowsInput()
    {
        var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();
        if (!mjpegMetrics.HasValue || mjpegMetrics.Value.DecoderCount <= 0)
        {
            return null;
        }

        return StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(
            mjpegMetrics.Value,
            _context.GetPendingPreviewFrameCount());
    }

    public StatsHardwareGpuRowsInput? GetGpuRowsInput()
        => StatsHardwareRowsInputBuilder.BuildGpuRowsInput(_context.GetNvmlSnapshot());
}

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

internal sealed class StatsHardwareRowsController
{
    private const int MaxExpectedDecodeRowCount = 14;
    private const int FixedGpuRowCount = 10;

    private readonly StatsHardwareRowsControllerContext _context;

    public StatsHardwareRowsController(StatsHardwareRowsControllerContext context)
    {
        _context = context;
    }

    public void UpdateDecodeSection()
    {
        var input = _context.InputProvider.GetDecodeRowsInput();
        if (!input.HasValue || input.Value.DecoderCount <= 0)
        {
            _context.DecodeSection.Visibility = Visibility.Collapsed;
            _context.RowChromeController.CollapseSimpleRows(StatsDockSimpleRowPool.Decode);
            return;
        }

        _context.DecodeSection.Visibility = Visibility.Visible;
        var rows = StatsPresentationBuilder.BuildHardwareDecodeRows(input.Value);
        _context.RowChromeController.UpdateSimpleRows(
            StatsDockSimpleRowPool.Decode,
            _context.DecodeContent,
            rows,
            MaxExpectedDecodeRowCount);
    }

    public void UpdateGpuSection()
    {
        var rows = StatsPresentationBuilder.BuildHardwareGpuRows(_context.InputProvider.GetGpuRowsInput());
        _context.RowChromeController.UpdateSimpleRows(
            StatsDockSimpleRowPool.Gpu,
            _context.GpuContent,
            rows,
            FixedGpuRowCount);
    }
}
