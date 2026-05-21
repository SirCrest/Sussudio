using System;
using System.Threading.Tasks;

namespace Sussudio.Controllers;

internal sealed class MainWindowPropertyChangedRouterContext
{
    public required Func<string, bool> TryHandleCaptureSelection { get; init; }
    public required Func<string, bool> TryHandleStatusStrip { get; init; }
    public required Func<string, Task<bool>> TryHandlePreviewAsync { get; init; }
    public required Func<string, bool> TryHandleRecording { get; init; }
    public required Func<string, bool> TryHandleOutput { get; init; }
    public required Func<string, bool> TryHandleCaptureOption { get; init; }
    public required Func<string, bool> TryHandleAudio { get; init; }
    public required Func<string, bool> TryHandleShell { get; init; }
    public required Func<string, bool> TryHandleLiveSignal { get; init; }
    public required Func<string, bool> TryHandleFlashback { get; init; }
}

internal sealed class MainWindowPropertyChangedRouter
{
    private readonly MainWindowPropertyChangedRouterContext _context;

    public MainWindowPropertyChangedRouter(MainWindowPropertyChangedRouterContext context)
    {
        _context = context;
    }

    public async Task RouteAsync(string? propertyNameValue)
    {
        var propertyName = propertyNameValue ?? string.Empty;

        if (_context.TryHandleCaptureSelection(propertyName))
        {
            return;
        }

        if (_context.TryHandleStatusStrip(propertyName))
        {
            return;
        }

        if (await _context.TryHandlePreviewAsync(propertyName))
        {
            return;
        }

        if (_context.TryHandleRecording(propertyName))
        {
            return;
        }

        if (_context.TryHandleOutput(propertyName))
        {
            return;
        }

        if (_context.TryHandleCaptureOption(propertyName))
        {
            return;
        }

        if (_context.TryHandleAudio(propertyName))
        {
            return;
        }

        if (_context.TryHandleShell(propertyName))
        {
            return;
        }

        if (_context.TryHandleLiveSignal(propertyName))
        {
            return;
        }

        if (_context.TryHandleFlashback(propertyName))
        {
            return;
        }
    }
}
