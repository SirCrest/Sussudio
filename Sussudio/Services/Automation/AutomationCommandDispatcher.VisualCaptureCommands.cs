using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteProbeVideoSourceCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _probePort.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Video source probe completed.", data: result);
    }

    private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _probePort.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Preview color probe completed.", data: result);
    }

    private async Task<AutomationCommandResponse> ExecuteCapturePreviewFrameCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var outputPath = ValidatePathPayload(
            AutomationCommandKind.CapturePreviewFrame,
            "outputPath",
            GetString(payload, "outputPath")
                ?? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp"));
        var result = await _probePort.CapturePreviewFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
        return CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded);
    }

    private async Task<AutomationCommandResponse> ExecuteCaptureWindowScreenshotCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var outputPath = ValidatePathPayload(
            AutomationCommandKind.CaptureWindowScreenshot,
            "outputPath",
            GetString(payload, "outputPath")
                ?? Path.Combine(Path.GetTempPath(), $"window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png"));
        var result = await _windowControl.CaptureWindowScreenshotAsync(outputPath, cancellationToken).ConfigureAwait(false);
        return CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded);
    }

    private AutomationCommandResponse CreateCaptureResponse(
        string correlationId,
        string message,
        object result,
        bool succeeded)
    {
        return CreateResponse(
            correlationId,
            message,
            data: result,
            success: succeeded,
            status: succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            errorCode: succeeded ? null : "capture-failed");
    }
}
