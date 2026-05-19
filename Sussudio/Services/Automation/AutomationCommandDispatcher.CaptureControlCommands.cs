using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteSetMjpegDecoderCountCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var decoderCount = GetInt(payload, "decoderCount");
        if (!decoderCount.HasValue)
        {
            throw new InvalidOperationException("Missing required integer property 'decoderCount'.");
        }

        await _captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"MJPEG decoder count change requested: {decoderCount.Value}.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetOutputPathCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var outputPath = ValidatePathPayload(
            AutomationCommandKind.SetOutputPath,
            "outputPath",
            RequireString(payload, "outputPath"));
        await _previewRecordingPort.SetOutputPathAsync(outputPath, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Output path change requested: {outputPath}.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetRecordingEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = RequireBool(payload, "enabled");
        await _previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Recording {(enabled ? "started" : "stopped")}.", snapshot: snapshot);
    }
}
