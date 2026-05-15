using System;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class OutputPathDisplayControllerContext
{
    public required TextBox OutputPathTextBox { get; init; }
    public required Func<string?> GetOutputPath { get; init; }
}

internal sealed class OutputPathDisplayController
{
    private readonly OutputPathDisplayControllerContext _context;

    public OutputPathDisplayController(OutputPathDisplayControllerContext context)
    {
        _context = context;
    }

    public void Attach()
        => _context.OutputPathTextBox.SizeChanged += (_, _) => Update();

    public void Update()
    {
        var path = _context.GetOutputPath();
        if (string.IsNullOrEmpty(path))
        {
            _context.OutputPathTextBox.Text = string.Empty;
            return;
        }

        ToolTipService.SetToolTip(_context.OutputPathTextBox, path);

        var availableWidth = _context.OutputPathTextBox.ActualWidth;
        _context.OutputPathTextBox.Text = OutputPathDisplayTextFormatter.Format(path, availableWidth);
    }
}
