using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer
{
    public bool Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NamedPipeAutomationServer));
        }

        if (_serverTask != null)
        {
            return true;
        }

        if (IsDefaultSecurityDisallowed())
        {
            Logger.Log(
                "Automation pipe server disabled because explicit Windows pipe security is unavailable " +
                $"and {AutomationPipeProtocol.AutomationKeyEnvVar} is not configured ({_pipeSecurityMode}).");
            return false;
        }

        NamedPipeServerStream initialServer;
        try
        {
            initialServer = CreateServerStream();
        }
        catch (AutomationPipeSecurityException ex)
        {
            Logger.Log($"Automation pipe server disabled: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] security disabled: {ex}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe server startup failed: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] startup failed: {ex}");
            return false;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunServerLoopAsync(initialServer, _cts.Token));
        Logger.Log($"Automation pipe server started on '{_pipeName}' ({_pipeSecurityMode}).");
        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var serverTask = _serverTask;
        if (serverTask == null)
        {
            return;
        }

        _cts?.Cancel();
        _serverTask = null;

        try
        {
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Server loop didn't stop within 5s â€” proceed with cleanup.
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown â€” server loop cancelled via disposal */
        }

        _cts?.Dispose();
        _cts = null;
        Logger.Log("Automation pipe server stopped.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunServerLoopAsync(NamedPipeServerStream? initialServer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = initialServer ?? CreateServerStream();
                initialServer = null;

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleConnectionSafelyAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown â€” exit the accept loop */
                break;
            }
            catch (AutomationPipeSecurityException ex)
            {
                Logger.Log($"Automation pipe server disabled: {ex.Message}");
                TraceFallback($"[{DateTime.Now:O}] security disabled: {ex}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Log($"Automation pipe server loop error: {ex.Message}");
                TraceFallback($"[{DateTime.Now:O}] loop error: {ex}");
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleConnectionSafelyAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectionAsync(server, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown Ã¢â‚¬â€ connection cancelled while handling client */
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

    private static AutomationCommandResponse CreateErrorResponse(string message, string errorCode) => new()
    {
        Success = false,
        CorrelationId = Guid.NewGuid().ToString("N"),
        Status = AutomationResponseStatus.Error,
        CommandLifecycle = AutomationCommandLifecycle.Failed,
        Message = message,
        ErrorCode = errorCode
    };

    private AutomationCommandResponse CreateRequestTimeoutResponse()
        => CreateErrorResponse($"Request timed out after {_requestTimeoutMs} ms.", "request-timeout");

    private static void TraceFallback(string line)
    {
        try
        {
            var path = RuntimePaths.GetRepoLogFile("Sussudio_AutomationPipe.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in NamedPipeAutomationServer.TraceFallback: {ex.Message}");
        }
    }
}
