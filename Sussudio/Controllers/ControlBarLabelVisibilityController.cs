using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class ControlBarLabelVisibilityControllerContext
{
    public required Border ControlBarBorder { get; init; }
    public required UIElement[] ControlBarLabels { get; init; }
}

internal sealed class ControlBarLabelVisibilityController
{
    private readonly ControlBarLabelVisibilityControllerContext _context;
    private bool _toggleLabelsVisible;

    public ControlBarLabelVisibilityController(ControlBarLabelVisibilityControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);
    }

    private void ApplyControlBarWidth(double controlBarWidth)
    {
        var showLabels = ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);
        if (showLabels == _toggleLabelsVisible)
        {
            return;
        }

        _toggleLabelsVisible = showLabels;
        var visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
        foreach (var label in _context.ControlBarLabels)
        {
            label.Visibility = visibility;
        }
    }
}
