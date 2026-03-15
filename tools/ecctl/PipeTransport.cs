using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace EcCtl;

internal sealed class PipeTransport
{
    public const string DefaultPipeName = "ElgatoCaptureAutomation";
    private const int ConnectTimeoutMs = 5000;
    private const int ResponseTimeoutMs = 15000;
    private const int ExtendedResponseTimeoutMs = 60000;
    private const int NotReadyRetries = 15;
    private const int NotReadyDelayMs = 1000;

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
        ["SetStatsVisible"] = 35,
        ["SetDeviceAudioMode"] = 36,
        ["GetPerformanceTimeline"] = 37,
        ["SetStatsSectionVisible"] = 38,
        ["SetAnalogAudioGain"] = 39,
        ["SetSettingsVisible"] = 40
    };

    private readonly string _pipeName;
    private readonly int? _responseTimeoutOverrideMs;

    public PipeTransport(string pipeName, int? responseTimeoutOverrideMs = null)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName) ? DefaultPipeName : pipeName;
        _responseTimeoutOverrideMs = responseTimeoutOverrideMs;
    }

    public async Task<JsonElement> SendCommandAsync(
        string commandName,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        if (!CommandMap.TryGetValue(commandName, out var commandValue))
        {
            throw new UsageException($"Unknown automation command '{commandName}'.");
        }

        var effectivePayload = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        var effectiveTimeoutMs = responseTimeoutMs ?? _responseTimeoutOverrideMs ?? GetDefaultResponseTimeout(commandName);

        for (var attempt = 0; ; attempt++)
        {
            var request = new Dictionary<string, object?>
            {
                ["command"] = commandValue,
                ["correlationId"] = Guid.NewGuid().ToString("N"),
                ["authToken"] = null,
                ["payload"] = effectivePayload
            };

            var requestJson = JsonSerializer.Serialize(request);
            var responseLine = await SendAsync(requestJson, effectiveTimeoutMs).ConfigureAwait(false);

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
    }

    private async Task<string> SendAsync(string requestJson, int responseTimeoutMs)
    {
        using var client = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.None);

        client.Connect(ConnectTimeoutMs);

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

    private static int GetDefaultResponseTimeout(string commandName)
    {
        return commandName switch
        {
            "WaitForCondition" or "VerifyLastRecording" or "CapturePreviewFrame" or "CaptureWindowScreenshot" => ExtendedResponseTimeoutMs,
            _ => ResponseTimeoutMs
        };
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
}
