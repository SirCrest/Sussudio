using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task ExecuteWindowActionAsync(
        AutomationWindowAction action,
        CancellationToken cancellationToken,
        JsonElement payload = default)
    {
        switch (action)
        {
            case AutomationWindowAction.Minimize:
                await _windowControl.MinimizeAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Maximize:
                await _windowControl.MaximizeAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Restore:
                await _windowControl.RestoreAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Close:
                await _windowControl.CloseAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Move:
                var mx = GetInt(payload, "x") ?? throw new InvalidOperationException("Move requires 'x' parameter.");
                var my = GetInt(payload, "y") ?? throw new InvalidOperationException("Move requires 'y' parameter.");
                await _windowControl.MoveToAsync(mx, my, cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Resize:
                var rw = GetInt(payload, "width") ?? throw new InvalidOperationException("Resize requires 'width' parameter.");
                var rh = GetInt(payload, "height") ?? throw new InvalidOperationException("Resize requires 'height' parameter.");
                await _windowControl.ResizeToAsync(rw, rh, cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.SnapLeft:
            case AutomationWindowAction.SnapRight:
            case AutomationWindowAction.SnapTopLeft:
            case AutomationWindowAction.SnapTopRight:
            case AutomationWindowAction.SnapBottomLeft:
            case AutomationWindowAction.SnapBottomRight:
            case AutomationWindowAction.Center:
                await _windowControl.SnapToRegionAsync(action, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown window action: {action}");
        }
    }
}
