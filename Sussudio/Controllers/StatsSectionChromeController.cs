using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Sussudio.Controllers;

internal sealed class StatsSectionChromeControllerContext
{
    public required Border StatsDockPanel { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
    public required Action RefreshDiagnosticsSection { get; init; }
}

internal sealed class StatsSectionChromeController
{
    private readonly StatsSectionChromeControllerContext _context;

    public StatsSectionChromeController(StatsSectionChromeControllerContext context)
    {
        _context = context;
    }

    public void ToggleFromHeader(object sender)
    {
        if (sender is not Grid header || header.Tag is not string contentName)
        {
            return;
        }

        var content = _context.StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        var collapsing = content.Visibility == Visibility.Visible;
        content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;

        var chevronName = contentName.Replace("_Content", "_Chevron", StringComparison.Ordinal);
        SetChevronExpanded(chevronName, expanded: !collapsing);

        if (!collapsing && ReferenceEquals(content, _context.DiagnosticsContent))
        {
            _context.RefreshDiagnosticsSection();
        }
    }

    public void SetVisible(string section, bool visible)
    {
        var contentName = section + "_Content";
        var content = _context.StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        var chevronName = section + "_Chevron";
        SetChevronExpanded(chevronName, visible);

        if (visible && contentName == "Diagnostics_Content")
        {
            _context.RefreshDiagnosticsSection();
        }
    }

    private void SetChevronExpanded(string chevronName, bool expanded)
    {
        if (_context.StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = expanded ? 0 : -90;
        }
    }
}
