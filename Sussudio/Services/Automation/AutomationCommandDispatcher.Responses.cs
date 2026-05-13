using System.Globalization;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private AutomationCommandResponse CreateResponse(
        string correlationId,
        string message,
        object? data = null,
        string? errorCode = null,
        bool success = true,
        bool includeSnapshot = true,
        AutomationResponseStatus status = AutomationResponseStatus.Ok,
        AutomationCommandLifecycle? commandLifecycle = null,
        int? retryAfterMs = null,
        long? elapsedMs = null,
        AutomationSnapshot? snapshot = null)
    {
        var lifecycle = commandLifecycle ?? (success
            ? AutomationCommandLifecycle.Completed
            : AutomationCommandLifecycle.Failed);
        return new AutomationCommandResponse
        {
            Success = success,
            CorrelationId = correlationId,
            Status = status,
            CommandLifecycle = lifecycle,
            RetryAfterMs = retryAfterMs,
            ElapsedMs = elapsedMs,
            Message = message,
            ErrorCode = errorCode,
            Data = data,
            Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null
        };
    }

    private AutomationCommandResponse CreateAcknowledgedResponse(
        string correlationId,
        string message,
        object? data = null,
        bool includeSnapshot = true)
    {
        return CreateResponse(
            correlationId,
            message,
            data: data,
            includeSnapshot: includeSnapshot,
            status: AutomationResponseStatus.Ok,
            commandLifecycle: AutomationCommandLifecycle.Acknowledged);
    }

    private AutomationCommandResponse CreateFlashbackActionRejectedResponse(
        string correlationId,
        AutomationFlashbackAction action,
        double? requestedPositionMs,
        AutomationSnapshot snapshot)
    {
        var lastFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)
            ? "none"
            : snapshot.FlashbackPlaybackLastCommandFailure;
        var requestedPositionDetail = requestedPositionMs.HasValue
            ? $", requestedPositionMs={requestedPositionMs.Value.ToString("0.###", CultureInfo.InvariantCulture)}"
            : string.Empty;
        return CreateResponse(
            correlationId,
            $"Flashback action '{action}' was rejected (state={snapshot.FlashbackPlaybackState}, threadAlive={snapshot.FlashbackPlaybackThreadAlive}, pending={snapshot.FlashbackPlaybackPendingCommands}, lastFailure={lastFailure}, failureUtc={snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs}{requestedPositionDetail}).",
            data: new
            {
                Action = action.ToString(),
                RequestedPositionMs = requestedPositionMs,
                PlaybackState = snapshot.FlashbackPlaybackState,
                PlaybackThreadAlive = snapshot.FlashbackPlaybackThreadAlive,
                PendingCommands = snapshot.FlashbackPlaybackPendingCommands,
                LastCommandFailure = lastFailure,
                LastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs
            },
            errorCode: "flashback-action-failed",
            success: false,
            status: AutomationResponseStatus.Error,
            snapshot: snapshot);
    }
}
