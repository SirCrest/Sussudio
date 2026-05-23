using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteSetFullScreenEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = RequireBool(payload, "enabled");
        await _windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Full screen {(enabled ? "enter" : "exit")} requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteOpenRecordingsFolderCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        await _windowControl.OpenRecordingsFolderAsync(cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Recordings folder open requested.");
    }

    private AutomationCommandResponse ExecuteArmCloseCommand(
        JsonElement payload,
        string correlationId)
    {
        var armed = GetBool(payload, "armed") ?? true;
        lock (_closeArmLock)
        {
            _closeArmed = armed;
        }

        return CreateAcknowledgedResponse(correlationId, $"Window close arm state requested: {(armed ? "armed" : "disarmed")}.");
    }

    private async Task<AutomationCommandResponse> ExecuteWindowActionCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var action = ParseWindowAction(payload);
        if (action == AutomationWindowAction.Close)
        {
            var armed = false;
            lock (_closeArmLock)
            {
                armed = _closeArmed;
                _closeArmed = false;
            }

            if (!armed)
            {
                return CreateResponse(
                    correlationId,
                    "Window close is disallowed until ArmClose is requested.",
                    errorCode: "window-close-not-armed",
                    success: false,
                    status: AutomationResponseStatus.Error);
            }

            await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, "Window close completed.");
        }

        await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Window action requested: {action}.");
    }

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
