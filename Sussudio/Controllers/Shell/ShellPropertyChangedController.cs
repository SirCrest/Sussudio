using System;

namespace Sussudio.Controllers;

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
