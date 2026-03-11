using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace McpServer;

public sealed class PipeClient
{
    public const string PipeName = "ElgatoCaptureAutomation";
    public const int ConnectTimeoutMs = 5000;
    public const int ResponseTimeoutMs = 15000;
    public const int NotReadyRetries = 15;
    public const int NotReadyDelayMs = 1000;
    private const int ExtendedResponseTimeoutMs = 60000;

    private static readonly Dictionary<string, int> CommandMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Authenticate"] = 0,
        ["GetSnapshot"] = 1,
        ["GetDiagnostics"] = 2,
        ["RefreshDevices"] = 3,
        ["SelectDevice"] = 4,
        ["SelectAudioInputDevice"] = 5,
        ["SetCustomAudioInput"] = 6,
        ["SetResolution"] = 7,
        ["SetFrameRate"] = 8,
        ["SetRecordingFormat"] = 9,
        ["SetQuality"] = 10,
        ["SetCustomBitrate"] = 11,
        ["SetHdrEnabled"] = 12,
        ["SetAudioEnabled"] = 13,
        ["SetAudioPreviewEnabled"] = 14,
        ["SetOutputPath"] = 15,
        ["SetPreviewEnabled"] = 16,
        ["SetRecordingEnabled"] = 17,
        ["ArmClose"] = 18,
        ["WindowAction"] = 19,
        ["WaitForCondition"] = 20,
        ["VerifyLastRecording"] = 21,
        ["AssertSnapshot"] = 22,
        ["SetTrueHdrPreviewEnabled"] = 23,
        ["ProbeVideoSource"] = 24,
        ["ProbePreviewColor"] = 25,
        ["CapturePreviewFrame"] = 26,
        ["CaptureWindowScreenshot"] = 27,
        ["SetVideoFormat"] = 28,
        ["GetCaptureOptions"] = 29,
        ["SetPreset"] = 30,
        ["SetSplitEncodeMode"] = 31,
        ["SetMjpegDecoderCount"] = 32,
        ["SetShowAllCaptureOptions"] = 33,
        ["SetPreviewVolume"] = 34,
        ["SetStatsVisible"] = 35
    };

    public async Task<JsonElement> SendCommandAsync(
        string commandName,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        if (!CommandMap.TryGetValue(commandName, out var commandValue))
        {
            return CreateSyntheticError($"Unknown automation command '{commandName}'.");
        }

        var effectivePayload = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        var effectiveResponseTimeoutMs = responseTimeoutMs ?? GetDefaultResponseTimeout(commandName);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var request = new Dictionary<string, object?>
                {
                    ["command"] = commandValue,
                    ["correlationId"] = Guid.NewGuid().ToString("N"),
                    ["authToken"] = null,
                    ["payload"] = effectivePayload
                };

                var requestJson = JsonSerializer.Serialize(request);
                var responseLine = await SendAsync(
                    requestJson,
                    effectiveResponseTimeoutMs).ConfigureAwait(false);

                using var responseDocument = JsonDocument.Parse(responseLine);
                var response = responseDocument.RootElement.Clone();

                if (!TryReadResponseState(response, out var success, out var status, out var retryAfterMs))
                {
                    return response;
                }

                if (success)
                {
                    return response;
                }

                if (!string.Equals(status, "not_ready", StringComparison.OrdinalIgnoreCase) ||
                    attempt >= NotReadyRetries)
                {
                    return response;
                }

                var delayMs = Math.Clamp(retryAfterMs ?? NotReadyDelayMs, 100, 30000);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (PipeConnectException)
            {
                return CreateSyntheticError("ElgatoCapture is not running or not responding. Start the app and try again.");
            }
            catch (Exception ex)
            {
                return CreateSyntheticError(ex.Message);
            }
        }
    }

    private static bool TryReadResponseState(
        JsonElement response,
        out bool success,
        out string? status,
        out int? retryAfterMs)
    {
        success = false;
        status = null;
        retryAfterMs = null;

        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Success", out var successProperty) &&
            successProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            success = successProperty.GetBoolean();
        }

        if (response.TryGetProperty("Status", out var statusProperty) &&
            statusProperty.ValueKind == JsonValueKind.String)
        {
            status = statusProperty.GetString();
        }

        if (response.TryGetProperty("RetryAfterMs", out var retryAfterProperty))
        {
            if (retryAfterProperty.ValueKind == JsonValueKind.Number &&
                retryAfterProperty.TryGetInt32(out var numeric))
            {
                retryAfterMs = numeric;
            }
            else if (retryAfterProperty.ValueKind == JsonValueKind.String &&
                     int.TryParse(
                         retryAfterProperty.GetString(),
                         NumberStyles.Integer,
                         CultureInfo.InvariantCulture,
                         out var parsed))
            {
                retryAfterMs = parsed;
            }
        }

        return true;
    }

    private static int GetDefaultResponseTimeout(string commandName)
    {
        return commandName switch
        {
            "WaitForCondition" or "VerifyLastRecording" or "CapturePreviewFrame" or "CaptureWindowScreenshot" => ExtendedResponseTimeoutMs,
            _ => ResponseTimeoutMs
        };
    }

    private static JsonElement CreateSyntheticError(string message)
    {
        var response = new Dictionary<string, object?>
        {
            ["Success"] = false,
            ["CorrelationId"] = Guid.NewGuid().ToString("N"),
            ["TimestampUtc"] = DateTimeOffset.UtcNow,
            ["Status"] = "error",
            ["CommandLifecycle"] = "failed",
            ["RetryAfterMs"] = null,
            ["ElapsedMs"] = null,
            ["Message"] = string.IsNullOrWhiteSpace(message) ? "Unknown pipe client error." : message,
            ["ErrorCode"] = "pipe-client-error",
            ["Data"] = null,
            ["Snapshot"] = null
        };

        using var responseDocument = JsonDocument.Parse(JsonSerializer.Serialize(response));
        return responseDocument.RootElement.Clone();
    }

    private static async Task<string> SendAsync(string requestJson, int responseTimeoutMs)
    {
        using var client = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.None);

        try
        {
            client.Connect(ConnectTimeoutMs);
        }
        catch (Exception ex)
        {
            throw new PipeConnectException(ex.Message, ex);
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
        var responseLine = await reader.ReadLineAsync()
            .WaitAsync(TimeSpan.FromMilliseconds(responseTimeoutMs))
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new InvalidOperationException("No response received from automation pipe.");
        }

        return responseLine;
    }

    private sealed class PipeConnectException : Exception
    {
        public PipeConnectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
