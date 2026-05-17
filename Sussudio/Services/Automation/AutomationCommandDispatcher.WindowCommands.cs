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
}
