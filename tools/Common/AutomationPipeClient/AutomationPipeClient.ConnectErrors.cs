using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal static partial class AutomationPipeClient
{
    private static async Task ConnectWithClassifiedErrorsAsync(
        NamedPipeClientStream client,
        string pipeName,
        int connectTimeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeConnectException(
                $"Timed out connecting to automation pipe '{pipeName}' after {connectTimeoutMs} ms.",
                "pipe-connect-timeout",
                ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AutomationPipeConnectException(
                $"Access denied connecting to automation pipe '{pipeName}'. The app is running, but this process is not allowed by the pipe security descriptor. Run the client from the same Windows user/elevation/session as the app, or restart the app with {AutomationPipeProtocol.AutomationKeyEnvVar} configured for token-gated fallback security.",
                "pipe-access-denied",
                ex);
        }
        catch (Exception ex)
        {
            throw new AutomationPipeConnectException(
                $"Failed to connect to automation pipe '{pipeName}': {ex.Message}",
                "pipe-connect-failed",
                ex);
        }
    }
}
