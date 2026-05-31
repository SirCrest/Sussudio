using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal enum StatsDockSimpleRowPool
{
    Decode,
    Gpu
}

internal sealed class StatsDockPresentationControllerContext
{
    public required TextBlock SessionStateValue { get; init; }
    public required TextBlock SummaryCaptureValue { get; init; }
    public required TextBlock SummaryPreviewValue { get; init; }
    public required TextBlock SummaryRendererFpsValue { get; init; }
    public required TextBlock SummaryVisualFpsValue { get; init; }
    public required TextBlock SummaryLatencyValue { get; init; }
    public required TextBlock SourceResolutionValue { get; init; }
    public required TextBlock SourceFrameRateValue { get; init; }
    public required TextBlock SourceHdrValue { get; init; }
    public required TextBlock SourceFormatValue { get; init; }
    public required TextBlock TelemetryOriginValue { get; init; }
    public required TextBlock AdcOnOffValue { get; init; }
    public required TextBlock AdcGainValue { get; init; }
    public required TextBlock SourceFpsValue { get; init; }
    public required TextBlock SourceExpectedFpsValue { get; init; }
    public required TextBlock SourceAvgValue { get; init; }
    public required TextBlock SourceP95Value { get; init; }
    public required TextBlock SourceJitterValue { get; init; }
    public required TextBlock SourceGapsValue { get; init; }
    public required TextBlock SourceDropsValue { get; init; }
    public required TextBlock PreviewFpsValue { get; init; }
    public required TextBlock PreviewAvgValue { get; init; }
    public required TextBlock PreviewP95Value { get; init; }
    public required TextBlock PreviewSlowValue { get; init; }
    public required TextBlock VisualFpsValue { get; init; }
    public required TextBlock VisualMotionValue { get; init; }
    public required TextBlock PipelineLatencyValue { get; init; }
    public required TextBlock SourceDeliveredValue { get; init; }
    public required TextBlock SourceDroppedValue { get; init; }
    public required TextBlock RendererRenderedValue { get; init; }
    public required TextBlock RendererDroppedValue { get; init; }
    public required TextBlock PerformanceScoreValue { get; init; }
    public required TextBlock AvSyncDriftValue { get; init; }
    public required TextBlock AvSyncDriftRateValue { get; init; }
    public required UIElement AvSyncEncoderRow { get; init; }
    public required TextBlock AvSyncEncoderValue { get; init; }
    public required UIElement EncoderSection { get; init; }
    public required TextBlock EncoderCodecValue { get; init; }
    public required TextBlock EncoderResolutionValue { get; init; }
    public required TextBlock EncoderFrameRateValue { get; init; }
    public required TextBlock EncoderBitrateValue { get; init; }
}

internal sealed class StatsDockPresentationController
{
    private static readonly SolidColorBrush MetricNeutralBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF1));
    private static readonly SolidColorBrush MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B));
    private static readonly SolidColorBrush MetricInfoBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x55, 0xD6, 0xFF));
    private static readonly SolidColorBrush MetricWarningBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xC8, 0x57));
    private static readonly SolidColorBrush MetricBadBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));

    private readonly StatsDockPresentationControllerContext _context;

    public StatsDockPresentationController(StatsDockPresentationControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsDockPresentation presentation)
    {
        SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);
        SetTextIfChanged(_context.SummaryCaptureValue, presentation.SummaryCapture);
        SetTextIfChanged(_context.SummaryPreviewValue, presentation.SummaryPreview);
        SetTextIfChanged(_context.SummaryRendererFpsValue, presentation.SummaryRendererFps);
        SetTextIfChanged(_context.SummaryVisualFpsValue, presentation.SummaryVisualFps);
        SetTextIfChanged(_context.SummaryLatencyValue, presentation.SummaryLatency);
        SetMetricBrush(_context.SummaryCaptureValue, presentation.SummaryCaptureStatus);
        SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);
        SetMetricBrush(_context.SummaryVisualFpsValue, presentation.SummaryVisualFpsStatus);
        SetMetricBrush(_context.SummaryLatencyValue, presentation.SummaryLatencyStatus);
        SetTextIfChanged(_context.SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(_context.SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(_context.SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(_context.SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(_context.TelemetryOriginValue, presentation.TelemetryOrigin);
        SetTextIfChanged(_context.AdcOnOffValue, presentation.AdcOnOff);
        SetTextIfChanged(_context.AdcGainValue, presentation.AdcGain);
        SetTextIfChanged(_context.SourceFpsValue, presentation.SourceFps);
        SetTextIfChanged(_context.SourceExpectedFpsValue, presentation.SourceExpectedFps);
        SetTextIfChanged(_context.SourceAvgValue, presentation.SourceAvg);
        SetTextIfChanged(_context.SourceP95Value, presentation.SourceP95);
        SetTextIfChanged(_context.SourceJitterValue, presentation.SourceJitter);
        SetTextIfChanged(_context.SourceGapsValue, presentation.SourceGaps);
        SetTextIfChanged(_context.SourceDropsValue, presentation.SourceDrops);
        SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);
        SetTextIfChanged(_context.PreviewAvgValue, presentation.PreviewAvg);
        SetTextIfChanged(_context.PreviewP95Value, presentation.PreviewP95);
        SetTextIfChanged(_context.PreviewSlowValue, presentation.PreviewSlow);
        SetTextIfChanged(_context.VisualFpsValue, presentation.VisualFps);
        SetTextIfChanged(_context.VisualMotionValue, presentation.VisualMotion);
        SetMetricBrush(_context.VisualFpsValue, presentation.VisualFpsStatus);
        SetTextIfChanged(_context.PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(_context.SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(_context.SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(_context.RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(_context.RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(_context.PerformanceScoreValue, presentation.PerformanceScore);
        SetTextIfChanged(_context.AvSyncDriftValue, presentation.AvSyncDrift);
        SetTextIfChanged(_context.AvSyncDriftRateValue, presentation.AvSyncDriftRate);

        SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderDriftVisible)
        {
            SetTextIfChanged(_context.AvSyncEncoderValue, presentation.EncoderDrift);
        }

        SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderActive)
        {
            SetTextIfChanged(_context.EncoderCodecValue, presentation.EncoderCodec);
            SetTextIfChanged(_context.EncoderResolutionValue, presentation.EncoderResolution);
            SetTextIfChanged(_context.EncoderFrameRateValue, presentation.EncoderFrameRate);
            SetTextIfChanged(_context.EncoderBitrateValue, presentation.EncoderBitrate);
        }
    }

    private static void SetMetricBrush(TextBlock target, StatsMetricStatus status)
    {
        target.Foreground = status switch
        {
            StatsMetricStatus.Good => MetricGoodBrush,
            StatsMetricStatus.Info => MetricInfoBrush,
            StatsMetricStatus.Warning => MetricWarningBrush,
            StatsMetricStatus.Bad => MetricBadBrush,
            _ => MetricNeutralBrush
        };
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, System.StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}

internal sealed class StatsDockRowChromeControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
}

internal sealed class StatsDockRowChromeController
{
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();
    private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();

    public StatsDockRowChromeController(StatsDockRowChromeControllerContext context)
    {
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)
    {
        StatsDockRowChromePresenter.CollapseRows(GetSimpleRowPool(poolKind));
    }

    public void UpdateSimpleRows(
        StatsDockSimpleRowPool poolKind,
        StackPanel container,
        IReadOnlyList<StatsHardwareRowPresentation> rows,
        int minimumCapacity)
    {
        var pool = GetSimpleRowPool(poolKind);
        EnsureRowPool(container, pool, Math.Max(minimumCapacity, rows.Count));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            _rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);
    }

    private List<StatsDockRowChromeSlot> GetSimpleRowPool(StatsDockSimpleRowPool poolKind)
        => poolKind switch
        {
            StatsDockSimpleRowPool.Decode => _decodeRowPool,
            StatsDockSimpleRowPool.Gpu => _gpuRowPool,
            _ => throw new ArgumentOutOfRangeException(nameof(poolKind), poolKind, null)
        };

    private void EnsureRowPool(StackPanel container, List<StatsDockRowChromeSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var slot = _rowChrome.CreateRowSlot();
            pool.Add(slot);
            container.Children.Add(slot.Row);
        }
    }
}

internal sealed class StatsDiagnosticRowsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
}

internal sealed class StatsDiagnosticRowsController
{
    private readonly StatsDiagnosticRowsControllerContext _context;
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    public StatsDiagnosticRowsController(StatsDiagnosticRowsControllerContext context)
    {
        _context = context;
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)
    {
        EnsureDiagnosticsEmptyState();

        if (presentation.IsEmpty)
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
            CollapseDiagnosticsPoolSlots();
            return;
        }

        var slotIndex = 0;
        foreach (var row in presentation.Rows)
        {
            EnsureDiagnosticsPoolCapacity(slotIndex + 1);
            UpdateDiagnosticsPoolSlot(
                _diagnosticsRowPool[slotIndex],
                row.GroupHeader,
                row.Label,
                row.Value,
                row.IsAlternate);
            slotIndex++;
        }

        StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private void EnsureDiagnosticsEmptyState()
    {
        if (_diagnosticsEmptyStateTextBlock != null)
        {
            return;
        }

        _diagnosticsEmptyStateTextBlock = new TextBlock
        {
            Text = "No diagnostics available",
            Style = _rowChrome.GetStyle("DockStatsLabelStyle"),
            Visibility = Visibility.Collapsed
        };
        _context.DiagnosticsContent.Children.Add(_diagnosticsEmptyStateTextBlock);
    }

    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var rowSlot = _rowChrome.CreateRowSlot();
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            _context.DiagnosticsContent.Children.Add(header);
            _context.DiagnosticsContent.Children.Add(rowSlot.Row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(rowSlot, header));
        }
    }

    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (groupHeader != null)
        {
            StatsDockRowChromePresenter.SetTextIfChanged(slot.GroupHeader, groupHeader);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
        }
        else
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }

        _rowChrome.UpdateRowSlot(slot.RowSlot, label, value, alt);
    }

    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.RowSlot.Row, Visibility.Collapsed);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }
    }

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = _rowChrome.GetStyle("DockStatsSectionHeaderStyle")
        };
    }

    private sealed record DiagnosticsPoolSlot(
        StatsDockRowChromeSlot RowSlot,
        TextBlock GroupHeader);
}

internal sealed record StatsDockRowChromeSlot(Border Row, TextBlock Label, TextBlock Value);

internal sealed class StatsDockRowChromePresenter
{
    private readonly FrameworkElement _resourceOwner;

    public StatsDockRowChromePresenter(FrameworkElement resourceOwner)
    {
        _resourceOwner = resourceOwner;
    }

    public StatsDockRowChromeSlot CreateRowSlot(string label = "", string value = "", bool alt = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = GetStyle("DockStatsLabelStyle")
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = GetStyle("DockStatsValueStyle"),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        var row = new Border
        {
            Style = GetRowStyle(alt),
            Child = grid
        };
        return new StatsDockRowChromeSlot(row, labelBlock, valueBlock);
    }

    public void UpdateRowSlot(StatsDockRowChromeSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = GetRowStyle(alt);
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    public Style GetStyle(string key) => (Style)_resourceOwner.Resources[key];

    private Style GetRowStyle(bool alt)
        => GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle");

    public static void CollapseRows(IReadOnlyList<StatsDockRowChromeSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }

    public static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    public static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}

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
