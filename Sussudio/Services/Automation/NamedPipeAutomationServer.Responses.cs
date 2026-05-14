using System;
using System.IO;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer
{
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
