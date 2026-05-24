using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

// JSON command router for the named-pipe automation protocol. It authenticates
// requests, validates payloads, delegates mutations to the view model, and
// shapes every response with correlation and lifecycle fields for external
// harnesses.
public sealed partial class AutomationCommandDispatcher : IAutomationCommandDispatcher
{
    private readonly IAutomationReadinessPort _readinessPort;
    private readonly IAutomationDeviceSelectionPort _deviceSelectionPort;
    private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;
    private readonly IAutomationCaptureSettingsPort _captureSettingsPort;
    private readonly IAutomationAudioPort _audioPort;
    private readonly IAutomationPreviewRecordingPort _previewRecordingPort;
    private readonly IAutomationUiPort _uiPort;
    private readonly IAutomationFlashbackPort _flashbackPort;
    private readonly IAutomationProbePort _probePort;
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly IAutomationWindowControl _windowControl;
    private readonly string? _authToken;
    private readonly object _closeArmLock = new();
    private bool _closeArmed;

    private const int DefaultWaitTimeoutMs = 10_000;
    private const int DefaultWaitPollMs = 250;

    internal AutomationCommandDispatcher(
        AutomationViewModelPorts ports,
        IAutomationDiagnosticsHub diagnosticsHub,
        IAutomationWindowControl windowControl,
        string? authToken = null)
    {
        _readinessPort = ports.Readiness ?? throw new ArgumentNullException(nameof(ports));
        _deviceSelectionPort = ports.DeviceSelection ?? throw new ArgumentNullException(nameof(ports));
        _snapshotQueryPort = ports.SnapshotQuery ?? throw new ArgumentNullException(nameof(ports));
        _captureSettingsPort = ports.CaptureSettings ?? throw new ArgumentNullException(nameof(ports));
        _audioPort = ports.Audio ?? throw new ArgumentNullException(nameof(ports));
        _previewRecordingPort = ports.PreviewRecording ?? throw new ArgumentNullException(nameof(ports));
        _uiPort = ports.Ui ?? throw new ArgumentNullException(nameof(ports));
        _flashbackPort = ports.Flashback ?? throw new ArgumentNullException(nameof(ports));
        _probePort = ports.Probe ?? throw new ArgumentNullException(nameof(ports));
        _diagnosticsHub = diagnosticsHub ?? throw new ArgumentNullException(nameof(diagnosticsHub));
        _windowControl = windowControl ?? throw new ArgumentNullException(nameof(windowControl));
        _authToken = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
    }

    public async Task<AutomationCommandResponse> ExecuteAsync(
        AutomationCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;
        var commandStartedAt = Stopwatch.GetTimestamp();

        try
        {
            var preflightResponse = TryCreatePreflightResponse(request, correlationId);
            if (preflightResponse != null)
            {
                return preflightResponse;
            }

            var payload = request.Payload;

            var portMappedResponse = await TryExecutePortMappedCommandAsync(
                    request.Command,
                    payload,
                    correlationId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (portMappedResponse != null)
            {
                return portMappedResponse;
            }

            return await ExecuteCustomCommandAsync(request, payload, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return CreateResponse(
                correlationId,
                "Command canceled.",
                errorCode: "canceled",
                success: false,
                status: AutomationResponseStatus.Error,
                includeSnapshot: false);
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"Automation command failed ({request.Command}) [correlationId={correlationId}] type={ex.GetType().Name}: {ex.Message}");
            Logger.LogException(ex);

            var message = string.IsNullOrWhiteSpace(ex.Message)
                ? $"{ex.GetType().Name} occurred while executing {request.Command}."
                : ex.Message;

            return CreateResponse(
                correlationId,
                message,
                errorCode: "command-failed",
                success: false,
                status: AutomationResponseStatus.Error,
                elapsedMs: (long)Math.Round(Stopwatch.GetElapsedTime(commandStartedAt).TotalMilliseconds));
        }
    }

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
