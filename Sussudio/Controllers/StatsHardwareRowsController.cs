using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsHardwareRowsControllerContext
{
    public required UIElement DecodeSection { get; init; }
    public required StackPanel DecodeContent { get; init; }
    public required StackPanel GpuContent { get; init; }
    public required StatsDiagnosticRowsController DiagnosticRowsController { get; init; }
    public required Func<StatsHardwareDecodeRowsInput?> GetDecodeRowsInput { get; init; }
    public required Func<StatsHardwareGpuRowsInput?> GetGpuRowsInput { get; init; }
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
        var input = _context.GetDecodeRowsInput();
        if (!input.HasValue || input.Value.DecoderCount <= 0)
        {
            _context.DecodeSection.Visibility = Visibility.Collapsed;
            _context.DiagnosticRowsController.CollapseDecodeRows(_context.DecodeContent);
            return;
        }

        _context.DecodeSection.Visibility = Visibility.Visible;
        var rows = StatsPresentationBuilder.BuildHardwareDecodeRows(input.Value);
        _context.DiagnosticRowsController.UpdateDecodeRows(_context.DecodeContent, rows);
    }

    public void UpdateGpuSection()
    {
        var rows = StatsPresentationBuilder.BuildHardwareGpuRows(_context.GetGpuRowsInput());
        _context.DiagnosticRowsController.UpdateGpuRows(_context.GpuContent, rows);
    }
}
