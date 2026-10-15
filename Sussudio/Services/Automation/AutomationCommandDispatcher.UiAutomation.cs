using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// GetUiTree and InvokeUiElement: the agent's "fingers". Both delegate to the window's UI automation bridge, which walks
// the live WinUI tree and operates controls through their automation peers on the UI thread (see UiAutomationBridge). A
// request that names nothing usable (no match, ambiguous, hidden, disabled, unsupported action) is a failed response that
// still carries the full result, so the agent can see the candidates or the element state. Neither command takes an
// automation snapshot. Contract: docs/architecture/performance-telemetry.md section 2.10.
public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteGetUiTreeCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var root = GetString(payload, AutomationPayloadKeys.Root)?.Trim();
        var maxDepth = GetInt(payload, AutomationPayloadKeys.MaxDepth) ?? UiTreeLimits.DefaultMaxDepth;
        if (maxDepth < 1 || maxDepth > UiTreeLimits.MaxDepthLimit)
        {
            throw new AutomationRequestValidationException(
                $"maxDepth must be between 1 and {UiTreeLimits.MaxDepthLimit} (got {maxDepth}).");
        }

        var interactiveOnly = GetBool(payload, AutomationPayloadKeys.InteractiveOnly) ?? true;
        var result = await _windowControl
            .GetUiTreeAsync(new UiTreeRequest(string.IsNullOrEmpty(root) ? null : root, maxDepth, interactiveOnly), cancellationToken)
            .ConfigureAwait(false);
        if (result.Succeeded)
        {
            return CreateResponse(correlationId, result.Message, data: result, includeSnapshot: false);
        }

        return CreateResponse(
            correlationId,
            result.Message,
            data: result,
            errorCode: result.Status == UiTreeStatus.WindowUnavailable ? AutomationErrorCodes.CommandFailed : AutomationErrorCodes.InvalidRequest,
            success: false,
            status: AutomationResponseStatus.Error,
            includeSnapshot: false);
    }

    private async Task<AutomationCommandResponse> ExecuteInvokeUiElementCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var target = RequireString(payload, AutomationPayloadKeys.Target).Trim();
        var request = new UiInvokeRequest(
            target,
            GetString(payload, AutomationPayloadKeys.Action),
            GetString(payload, AutomationPayloadKeys.Value),
            GetBool(payload, AutomationPayloadKeys.Force) ?? false);
        var result = await _windowControl.InvokeUiElementAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return CreateResponse(correlationId, result.Message, data: result, includeSnapshot: false);
        }

        // Requests the agent can fix by changing the request are invalid-request; states of the live UI it cannot
        // change by rewording (hidden, disabled, the peer refusing, no window) are command-failed.
        var requestProblem = result.Status is UiActionStatus.NotFound
            or UiActionStatus.Ambiguous
            or UiActionStatus.InvalidAction
            or UiActionStatus.InvalidValue
            or UiActionStatus.ActionUnsupported;
        return CreateResponse(
            correlationId,
            result.Message,
            data: result,
            errorCode: requestProblem ? AutomationErrorCodes.InvalidRequest : AutomationErrorCodes.CommandFailed,
            success: false,
            status: AutomationResponseStatus.Error,
            includeSnapshot: false);
    }
}
