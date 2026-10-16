using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Performance;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// CaptureAudioOutput: the agent's "ears". Records this process's own rendered audio through Windows process loopback
// (see AudioOutputProbe) and returns the analysis. It reads no view-model state and takes no automation snapshot.
// A capture that did not produce audible audio is a failed response with the full result attached, never a pass.
// Contract: docs/architecture/performance-telemetry.md section 2.9.
public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteCaptureAudioOutputCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var seconds = ReadProbeSeconds(payload);
        var measureLatency = GetBool(payload, AutomationPayloadKeys.MeasureLatency) ?? false;

        string? wavPath = null;
        var requestedWav = GetString(payload, AutomationPayloadKeys.OutputPath);
        if (!string.IsNullOrWhiteSpace(requestedWav))
        {
            wavPath = Path.GetFullPath(ValidatePathPayload(
                AutomationCommandKind.CaptureAudioOutput,
                AutomationPayloadKeys.OutputPath,
                requestedWav));
            if (!string.Equals(Path.GetExtension(wavPath), ".wav", StringComparison.OrdinalIgnoreCase))
            {
                throw new AutomationRequestValidationException(
                    $"outputPath must end in .wav (the capture is written as a float WAV): '{wavPath}'.");
            }
        }

        var result = await AudioOutputProbe.CaptureAsync(seconds, wavPath, measureLatency, cancellationToken).ConfigureAwait(false);
        if (result.Captured)
        {
            return CreateResponse(correlationId, result.Message, data: result, includeSnapshot: false);
        }

        return CreateResponse(
            correlationId,
            result.Message,
            data: result,
            errorCode: AutomationErrorCodes.CommandFailed,
            success: false,
            status: AutomationResponseStatus.Error,
            includeSnapshot: false);
    }

    // seconds is bounded, not clamped: a typo such as 300 must be rejected rather than silently shortened.
    private static double ReadProbeSeconds(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(AutomationPayloadKeys.Seconds, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return AudioOutputProbe.DefaultSeconds;
        }

        var seconds = GetDouble(payload, AutomationPayloadKeys.Seconds)
            ?? throw new AutomationRequestValidationException("seconds must be a finite number.");
        if (seconds < AudioOutputProbe.MinSeconds || seconds > AudioOutputProbe.MaxSeconds)
        {
            throw new AutomationRequestValidationException(
                $"seconds must be between {AudioOutputProbe.MinSeconds} and {AudioOutputProbe.MaxSeconds} (got {seconds}).");
        }

        return seconds;
    }
}
