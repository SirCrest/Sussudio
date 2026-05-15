using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal static partial class AutomationPipeClient
{
    internal static Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs)
        => SendRequestAsync(
            pipeName,
            requestJson,
            connectTimeoutMs,
            responseTimeoutMs,
            CancellationToken.None);

    internal static async Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs,
        CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await ConnectWithClassifiedErrorsAsync(
                client,
                pipeName,
                connectTimeoutMs,
                cancellationToken)
            .ConfigureAwait(false);

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

        await writer.WriteLineAsync(requestJson).WaitAsync(cancellationToken).ConfigureAwait(false);
        string? responseLine;
        try
        {
            responseLine = await reader.ReadLineAsync()
                .WaitAsync(TimeSpan.FromMilliseconds(responseTimeoutMs), cancellationToken)
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
