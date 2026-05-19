using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteFlashbackActionCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var action = ParseFlashbackAction(payload);
        var positionMs = action switch
        {
            AutomationFlashbackAction.Play => GetDouble(payload, "positionMs"),
            AutomationFlashbackAction.Seek => GetDouble(payload, "positionMs") ?? 0,
            AutomationFlashbackAction.BeginScrub => RequireDouble(payload, "positionMs"),
            AutomationFlashbackAction.UpdateScrub => RequireDouble(payload, "positionMs"),
            AutomationFlashbackAction.EndScrub => GetDouble(payload, "positionMs"),
            _ => null
        };
        if (positionMs.HasValue &&
            (!double.IsFinite(positionMs.Value) ||
             positionMs.Value < 0 ||
             positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds))
        {
            throw new InvalidOperationException("Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        }

        var position = positionMs.HasValue
            ? TimeSpan.FromMilliseconds(positionMs.Value)
            : (TimeSpan?)null;
        if (!await _flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false))
        {
            return CreateFlashbackActionRejectedResponse(
                correlationId,
                action,
                positionMs,
                _diagnosticsHub.GetLatestSnapshot());
        }

        switch (action)
        {
            case AutomationFlashbackAction.Play:
                return CreateAcknowledgedResponse(correlationId,
                    positionMs.HasValue
                        ? $"Flashback play at {positionMs.Value:0}ms requested."
                        : "Flashback play requested.");
            case AutomationFlashbackAction.Pause:
                return CreateAcknowledgedResponse(correlationId, "Flashback pause requested.");
            case AutomationFlashbackAction.GoLive:
                return CreateAcknowledgedResponse(correlationId, "Flashback go-live requested.");
            case AutomationFlashbackAction.Seek:
                return CreateAcknowledgedResponse(correlationId, $"Flashback seek to {positionMs:0}ms requested.");
            case AutomationFlashbackAction.BeginScrub:
                return CreateAcknowledgedResponse(correlationId, $"Flashback scrub begin at {positionMs:0}ms requested.");
            case AutomationFlashbackAction.UpdateScrub:
                return CreateAcknowledgedResponse(correlationId, $"Flashback scrub update to {positionMs:0}ms requested.");
            case AutomationFlashbackAction.EndScrub:
                return CreateAcknowledgedResponse(correlationId,
                    positionMs.HasValue
                        ? $"Flashback scrub end at {positionMs.Value:0}ms requested."
                        : "Flashback scrub end requested.");
            case AutomationFlashbackAction.SetInPoint:
                return CreateAcknowledgedResponse(correlationId, "Flashback in point set.");
            case AutomationFlashbackAction.SetOutPoint:
                return CreateAcknowledgedResponse(correlationId, "Flashback out point set.");
            case AutomationFlashbackAction.ClearInOutPoints:
                return CreateAcknowledgedResponse(correlationId, "Flashback in/out points cleared.");
            default:
                throw new InvalidOperationException($"Unsupported flashback action '{action}'.");
        }
    }

    private async Task<AutomationCommandResponse> ExecuteFlashbackExportCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var seconds = GetDouble(payload, "seconds") ?? 300;
        if (!double.IsFinite(seconds) ||
            seconds <= 0 ||
            seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new InvalidOperationException("Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        var outputPath = ValidatePathPayload(
            AutomationCommandKind.FlashbackExport,
            "outputPath",
            RequireString(payload, "outputPath"));
        var useSelectionRange = GetBool(payload, "useSelectionRange") ?? false;
        var force = GetBool(payload, "force") ?? false;
        var exportResult = await _flashbackPort.ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken).ConfigureAwait(false);
        var failureKind = exportResult.Succeeded
            ? string.Empty
            : CaptureService.ClassifyFlashbackExportFailureKind(exportResult.StatusMessage);
        return CreateResponse(
            correlationId,
            exportResult.StatusMessage ?? (exportResult.Succeeded ? "Export complete." : "Export failed."),
            data: new
            {
                exportResult.Succeeded,
                exportResult.OutputPath,
                exportResult.StatusMessage,
                FailureKind = failureKind,
                FileSizeBytes = File.Exists(exportResult.OutputPath) ? new FileInfo(exportResult.OutputPath).Length : 0L
            },
            errorCode: exportResult.Succeeded ? null : "export-failed",
            success: exportResult.Succeeded,
            status: exportResult.Succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error);
    }

    private async Task<AutomationCommandResponse> ExecuteFlashbackGetSegmentsCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var segments = await _flashbackPort.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(
            correlationId,
            $"Found {segments.Count} segment(s).",
            data: new { Segments = segments });
    }

    private async Task<AutomationCommandResponse> ExecuteRestartFlashbackCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        await _flashbackPort.RestartFlashbackAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Flashback restarted.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetFlashbackEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = GetBool(payload, "enabled") ?? throw new InvalidOperationException("Missing 'enabled' parameter.");
        await _flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Flashback {(enabled ? "enabled" : "disabled")}.");
    }
}
