using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer
{
    private async Task HandleConnectionSafelyAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectionAsync(server, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown â€” connection cancelled while handling client */
        }
        catch (IOException ioEx)
        {
            Logger.Log($"Automation pipe connection I/O error: {ioEx.Message}");
            TraceFallback($"[{DateTime.Now:O}] connection io error: {ioEx}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe connection error: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] connection error: {ex}");
        }
        finally
        {
            try
            {
                server.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in NamedPipeAutomationServer pipe dispose: {ex.Message}");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        var session = new ConnectionSession(this, server, cancellationToken);
        await session.RunAsync().ConfigureAwait(false);
    }
}
