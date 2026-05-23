using System.Security.Cryptography;
using System.Text;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private AutomationCommandResponse? TryCreatePreflightResponse(
        AutomationCommandRequest request,
        string correlationId)
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

        return null;
    }

    private bool IsAutomationReady()
    {
        return _readinessPort.IsInitialized || _readinessPort.Devices.Count > 0;
    }

    private bool IsAuthorized(AutomationCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(_authToken))
        {
            return true;
        }

        var providedToken = request.AuthToken;
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            providedToken = GetString(request.Payload, "authToken");
        }

        // Constant-time comparison: even on a local pipe, sidechannel timing
        // hardening is cheap insurance and matches how token comparison is
        // expected to work in any future remote/transport variant.
        var expected = Encoding.UTF8.GetBytes(_authToken);
        var actual = Encoding.UTF8.GetBytes(providedToken ?? string.Empty);
        var ok = expected.Length == actual.Length
            && CryptographicOperations.FixedTimeEquals(expected, actual);
        if (!ok)
        {
            Logger.LogEvent("AUTH_FAILED", $"command={request.Command} correlationId={request.CorrelationId ?? "<none>"}");
        }
        return ok;
    }
}
