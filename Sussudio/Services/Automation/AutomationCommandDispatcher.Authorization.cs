using System.Security.Cryptography;
using System.Text;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
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
