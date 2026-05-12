using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static class DiagnosticSessionPipeRetryPolicy
{
    internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        AutomationPipeConnectException? lastConnectException = null;
        string? lastSyntheticConnectFailureMessage = null;
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = (await sendCommandAsync(command, payload, responseTimeoutMs)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .Clone();
                if (IsSyntheticPipeConnectFailure(response))
                {
                    lastSyntheticConnectFailureMessage = GetString(response, "Message") ?? "pipe connect failed";
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return response;
            }
            catch (AutomationPipeConnectException ex)
            {
                if (IsPermanentPipeConnectFailure(ex.ErrorCode))
                {
                    return BuildLocalFailureResponse(command, ex.Message);
                }

                lastConnectException = ex;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)
            {
                return BuildLocalFailureResponse(command, ex.Message);
            }
            catch (JsonException ex)
            {
                return BuildLocalFailureResponse(command, ex.Message);
            }
        }

        if (lastConnectException is not null)
        {
            return BuildLocalFailureResponse(command, lastConnectException.Message);
        }

        if (!string.IsNullOrWhiteSpace(lastSyntheticConnectFailureMessage))
        {
            return BuildLocalFailureResponse(command, lastSyntheticConnectFailureMessage);
        }

        return BuildLocalFailureResponse(command, "command was not attempted before retry timeout elapsed");
    }

    internal static JsonElement BuildLocalFailureResponse(string command, string message)
    {
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["Success"] = false,
                ["Status"] = "error",
                ["CommandLifecycle"] = "failed",
                ["Message"] = $"{command}: {message}"
            }));
        return document.RootElement.Clone();
    }

    private static bool IsSyntheticPipeConnectFailure(JsonElement response)
    {
        if (IsSuccess(response))
        {
            return false;
        }

        var errorCode = GetString(response, "ErrorCode");
        return string.Equals(errorCode, "pipe-connect-failed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(errorCode, "pipe-connect-timeout", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPermanentPipeConnectFailure(string? errorCode)
        => string.Equals(errorCode, "pipe-access-denied", StringComparison.OrdinalIgnoreCase);
}
