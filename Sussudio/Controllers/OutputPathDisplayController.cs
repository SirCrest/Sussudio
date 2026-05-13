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
        if (availableWidth <= 0)
        {
            _context.OutputPathTextBox.Text = path;
            return;
        }

        // FontSize 12 is about 7px per char, minus internal padding.
        var maxChars = (int)((availableWidth - 20) / 7);
        if (path.Length <= maxChars)
        {
            _context.OutputPathTextBox.Text = path;
            return;
        }

        var parts = path.Split('\\', '/');
        if (parts.Length <= 2)
        {
            _context.OutputPathTextBox.Text = path;
            return;
        }

        // Progressively truncate: keep root, show as many trailing segments as fit.
        var root = parts[0];
        for (int tailCount = parts.Length - 1; tailCount >= 1; tailCount--)
        {
            var tail = string.Join("\\", parts[^tailCount..]);
            var candidate = $"{root}\\...\\{tail}";
            if (candidate.Length <= maxChars)
            {
                _context.OutputPathTextBox.Text = candidate;
                return;
            }
        }

        _context.OutputPathTextBox.Text = $"{root}\\...\\{parts[^1]}";
    }
}
