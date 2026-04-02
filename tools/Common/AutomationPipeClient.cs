using System.IO.Pipes;
using System.Text;

namespace ElgatoCapture.Tools;

internal static class AutomationPipeClient
{
    internal static async Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.None);

        try
        {
            client.Connect(connectTimeoutMs);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeConnectException(
                $"Timed out connecting to automation pipe '{pipeName}' after {connectTimeoutMs} ms.",
                ex);
        }
        catch (Exception ex)
        {
            throw new AutomationPipeConnectException(
                $"Failed to connect to automation pipe '{pipeName}': {ex.Message}",
                ex);
        }

        using var writer = new StreamWriter(
            client,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        using var reader = new StreamReader(
            client,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        await writer.WriteLineAsync(requestJson).ConfigureAwait(false);
        string? responseLine;
        try
        {
            responseLine = await reader.ReadLineAsync()
                .WaitAsync(TimeSpan.FromMilliseconds(responseTimeoutMs))
                .ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeResponseTimeoutException(
                $"Timed out waiting for automation response after {responseTimeoutMs} ms.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new AutomationPipeProtocolException("No response received from automation pipe.");
        }

        return responseLine;
    }
}

internal class AutomationPipeException : Exception
{
    public AutomationPipeException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

internal sealed class AutomationPipeConnectException : AutomationPipeException
{
    public AutomationPipeConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal sealed class AutomationPipeResponseTimeoutException : AutomationPipeException
{
    public AutomationPipeResponseTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal sealed class AutomationPipeProtocolException : AutomationPipeException
{
    public AutomationPipeProtocolException(string message)
        : base(message)
    {
    }
}
