using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// JSON command router for the named-pipe automation protocol. It authenticates
// requests, validates payloads, delegates mutations to the view model, and
// shapes every response with correlation and lifecycle fields for external
// harnesses.
public sealed partial class AutomationCommandDispatcher : IAutomationCommandDispatcher
{
    private readonly IAutomationViewModel _viewModel;
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly IAutomationWindowControl _windowControl;
    private readonly string? _authToken;
    private readonly object _closeArmLock = new();
    private bool _closeArmed;

    private const int DefaultWaitTimeoutMs = 10_000;
    private const int DefaultWaitPollMs = 250;

    public AutomationCommandDispatcher(
        IAutomationViewModel viewModel,
        IAutomationDiagnosticsHub diagnosticsHub,
        IAutomationWindowControl windowControl,
        string? authToken = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _diagnosticsHub = diagnosticsHub ?? throw new ArgumentNullException(nameof(diagnosticsHub));
        _windowControl = windowControl ?? throw new ArgumentNullException(nameof(windowControl));
        _authToken = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
    }

    public async Task<AutomationCommandResponse> ExecuteAsync(
        AutomationCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;
        var commandStartedAt = Stopwatch.GetTimestamp();

        try
        {
            if (request.ManifestRevision.HasValue)
            {
                if (request.ManifestRevision.Value != AutomationPipeProtocol.CommandManifestRevision)
                {
                    Logger.Log(
                        $"AUTOMATION_MANIFEST_MISMATCH command={request.Command} " +
                        $"clientRevision={request.ManifestRevision.Value} " +
                        $"serverRevision={AutomationPipeProtocol.CommandManifestRevision} " +
                        $"correlationId={correlationId}");
                    return CreateResponse(
                        correlationId,
                        $"Automation command manifest revision mismatch (client={request.ManifestRevision.Value}, server={AutomationPipeProtocol.CommandManifestRevision}). " +
                        "Rebuild ssctl/MCP/StreamDeck against the current Sussudio source to refresh the numeric command IDs.",
                        errorCode: "manifest-mismatch",
                        success: false,
                        status: AutomationResponseStatus.Error,
                        includeSnapshot: false);
                }
            }
            else
            {
                Logger.Log(
                    $"STALE_CLIENT_NO_MANIFEST_REVISION command={request.Command} correlationId={correlationId} " +
                    "(client predates manifest-revision handshake; allowing for back-compat but command IDs are not verified).");
            }

            var authorized = IsAuthorized(request);
            if (request.Command == AutomationCommandKind.Authenticate)
            {
                return CreateResponse(
                    correlationId,
                    authorized
                        ? "Authentication accepted."
                        : "Authentication rejected.",
                    errorCode: authorized ? null : "unauthorized",
                    success: authorized,
                    status: authorized ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
                    includeSnapshot: false);
            }

            if (!authorized)
            {
                return CreateResponse(
                    correlationId,
                    "Unauthorized command request.",
                    errorCode: "unauthorized",
                    success: false,
                    status: AutomationResponseStatus.Error,
                    includeSnapshot: false);
            }

            if (RequiresReadyDevices(request.Command) && !IsAutomationReady())
            {
                return CreateResponse(
                    correlationId,
                    "Automation is still initializing devices; retry shortly.",
                    errorCode: "not-ready",
                    success: false,
                    status: AutomationResponseStatus.NotReady,
                    retryAfterMs: 1000);
            }

            var payload = request.Payload;

            var uiSettingsResponse = await TryExecuteUiSettingsCommandAsync(request.Command, payload, correlationId, cancellationToken)
                .ConfigureAwait(false);
            if (uiSettingsResponse != null)
            {
                return uiSettingsResponse;
            }

            if (TrivialHandlers.TryGetValue(request.Command, out var trivialHandler))
            {
                await trivialHandler.InvokeAsync(_viewModel, payload, cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, trivialHandler.AcknowledgeMessage(request.Command, payload));
            }

            return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return CreateResponse(
                correlationId,
                "Command canceled.",
                errorCode: "canceled",
                success: false,
                status: AutomationResponseStatus.Error,
                includeSnapshot: false);
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"Automation command failed ({request.Command}) [correlationId={correlationId}] type={ex.GetType().Name}: {ex.Message}");
            Logger.LogException(ex);

            var message = string.IsNullOrWhiteSpace(ex.Message)
                ? $"{ex.GetType().Name} occurred while executing {request.Command}."
                : ex.Message;

            return CreateResponse(
                correlationId,
                message,
                errorCode: "command-failed",
                success: false,
                status: AutomationResponseStatus.Error,
                elapsedMs: (long)Math.Round(Stopwatch.GetElapsedTime(commandStartedAt).TotalMilliseconds));
        }
    }

    private bool IsAutomationReady()
    {
        return _viewModel.IsInitialized || _viewModel.Devices.Count > 0;
    }
}
