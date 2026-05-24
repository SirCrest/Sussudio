using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
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
