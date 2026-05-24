using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Sussudio.Controllers;

internal sealed class ShellElevationControllerContext
{
    public required UIElement ControlBarBorder { get; init; }
    public required UIElement SettingsOverlayPanel { get; init; }
    public required UIElement RecordButton { get; init; }
}

internal sealed class ShellElevationController
{
    private readonly ShellElevationControllerContext _context;

    public ShellElevationController(ShellElevationControllerContext context)
    {
        _context = context;
    }

    public void Apply()
    {
        var controlBarShadow = new ThemeShadow();
        controlBarShadow.Receivers.Add(_context.SettingsOverlayPanel);
        _context.ControlBarBorder.Shadow = controlBarShadow;
        _context.ControlBarBorder.Translation = new Vector3(0, 0, 32);

        var recordButtonShadow = new ThemeShadow();
        _context.RecordButton.Shadow = recordButtonShadow;
        _context.RecordButton.Translation = new Vector3(0, 0, 16);
    }
}

internal sealed class ShellPropertyChangedControllerContext
{
    public required StatsOverlayCompositionController StatsOverlayComposition { get; init; }
    public required SettingsShelfController SettingsShelf { get; init; }
    public required Func<bool> IsStatsVisible { get; init; }
    public required Func<bool> IsSettingsVisible { get; init; }
}

internal sealed class ShellPropertyChangedController
{
    private readonly ShellPropertyChangedControllerContext _context;

    public ShellPropertyChangedController(ShellPropertyChangedControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        if (_context.StatsOverlayComposition.TryHandlePropertyChanged(propertyName, _context.IsStatsVisible()))
        {
            return true;
        }

        if (_context.SettingsShelf.TryHandlePropertyChanged(propertyName, _context.IsSettingsVisible()))
        {
            return true;
        }

        return false;
    }
}
