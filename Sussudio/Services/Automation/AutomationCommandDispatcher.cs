using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// JSON command router for the named-pipe automation protocol. It authenticates
// requests, validates payloads, delegates mutations to the view model, and
// shapes every response with correlation and lifecycle fields for external
// harnesses.
public sealed partial class AutomationCommandDispatcher : IAutomationCommandDispatcher
{
    // Trivial one-property capture and pipeline commands live with the ordered
    // port-mapped dispatcher that consumes them.
    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>> TrivialDeviceSelectionHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>>
        {
            [AutomationCommandKind.SetCustomAudioInput] = AutomationCommandHandler<IAutomationDeviceSelectionPort>.Bool(
                (vm, v, ct) => vm.SetCustomAudioInputEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> TrivialCaptureSettingsHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>>
        {
            [AutomationCommandKind.SetResolution] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetResolutionAsync(v, ct), "resolution"),
            [AutomationCommandKind.SetFrameRate] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Double(
                (vm, v, ct) => vm.SetFrameRateAsync(v, ct), "frameRate"),
            [AutomationCommandKind.SetVideoFormat] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetVideoFormatAsync(v, ct), "videoFormat"),
            [AutomationCommandKind.SetPreset] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetPresetAsync(v, ct), "preset"),
            [AutomationCommandKind.SetSplitEncodeMode] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetSplitEncodeModeAsync(v, ct), "splitEncodeMode"),
            [AutomationCommandKind.SetRecordingFormat] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetRecordingFormatAsync(v, ct), "format"),
            [AutomationCommandKind.SetQuality] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetQualityAsync(v, ct), "quality"),
            [AutomationCommandKind.SetCustomBitrate] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Double(
                (vm, v, ct) => vm.SetCustomBitrateAsync(v, ct), "bitrateMbps"),
            [AutomationCommandKind.SetHdrEnabled] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Bool(
                (vm, v, ct) => vm.SetHdrEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetTrueHdrPreviewEnabled] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Bool(
                (vm, v, ct) => vm.SetTrueHdrPreviewEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationAudioPort>> TrivialAudioHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationAudioPort>>
        {
            [AutomationCommandKind.SetAudioEnabled] = AutomationCommandHandler<IAutomationAudioPort>.Bool(
                (vm, v, ct) => vm.SetAudioEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetAudioPreviewEnabled] = AutomationCommandHandler<IAutomationAudioPort>.Bool(
                (vm, v, ct) => vm.SetAudioPreviewEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> TrivialPreviewRecordingHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>>
        {
            [AutomationCommandKind.SetPreviewEnabled] = AutomationCommandHandler<IAutomationPreviewRecordingPort>.Bool(
                (vm, v, ct) => vm.SetPreviewEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> UiPreviewRecordingHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>>
        {
            [AutomationCommandKind.SetPreviewVolume] = AutomationCommandHandler<IAutomationPreviewRecordingPort>.Double(
                (vm, v, ct) => vm.SetPreviewVolumeAsync(v, ct), "previewVolumePercent"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>> UiStateHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationUiPort>>
        {
            [AutomationCommandKind.SetStatsVisible] = AutomationCommandHandler<IAutomationUiPort>.Bool(
                (vm, v, ct) => vm.SetStatsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetSettingsVisible] = AutomationCommandHandler<IAutomationUiPort>.Bool(
                (vm, v, ct) => vm.SetSettingsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFrameTimeOverlayVisible] = AutomationCommandHandler<IAutomationUiPort>.Bool(
                (vm, v, ct) => vm.SetFrameTimeOverlayVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFlashbackTimelineVisible] = AutomationCommandHandler<IAutomationUiPort>.Bool(
                (vm, v, ct) => vm.SetFlashbackTimelineVisibleAsync(v, ct), "visible"),
        };

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

    private async Task<AutomationCommandResponse?> TryExecutePortMappedCommandAsync(
        AutomationCommandKind command,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var uiSettingsResponse = await TryExecuteUiSettingsCommandAsync(command, payload, correlationId, cancellationToken)
            .ConfigureAwait(false);
        if (uiSettingsResponse != null)
        {
            return uiSettingsResponse;
        }

        if (TrivialDeviceSelectionHandlers.TryGetValue(command, out var deviceSelectionHandler))
        {
            await deviceSelectionHandler.InvokeAsync(_deviceSelectionPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, deviceSelectionHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialCaptureSettingsHandlers.TryGetValue(command, out var captureSettingsHandler))
        {
            await captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, captureSettingsHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialAudioHandlers.TryGetValue(command, out var audioHandler))
        {
            await audioHandler.InvokeAsync(_audioPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, audioHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialPreviewRecordingHandlers.TryGetValue(command, out var previewRecordingHandler))
        {
            await previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, previewRecordingHandler.AcknowledgeMessage(command, payload));
        }

        return null;
    }

    private async Task<AutomationCommandResponse?> TryExecuteUiSettingsCommandAsync(
        AutomationCommandKind command,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (command == AutomationCommandKind.SetShowAllCaptureOptions)
        {
            _ = RequireBool(payload, "enabled");
            return CreateAcknowledgedResponse(correlationId, "Show-all capture options are always enabled.");
        }

        if (UiPreviewRecordingHandlers.TryGetValue(command, out var previewRecordingHandler))
        {
            await previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, previewRecordingHandler.AcknowledgeMessage(command, payload));
        }

        if (UiStateHandlers.TryGetValue(command, out var uiHandler))
        {
            await uiHandler.InvokeAsync(_uiPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, uiHandler.AcknowledgeMessage(command, payload));
        }

        if (command == AutomationCommandKind.SetStatsSectionVisible)
        {
            return await ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var section = RequireString(payload, "section");
        var visible = RequireBool(payload, "visible");
        await _uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Stats section '{section}' {(visible ? "expanded" : "collapsed")}.");
    }

    private AutomationCommandResponse? TryCreatePreflightResponse(
        AutomationCommandRequest request,
        string correlationId)
    {
        if (request.ManifestRevision.HasValue)
        {
            if (request.ManifestRevision.Value != AutomationPipeProtocol.CommandManifestRevision)
            {
                Logger.Log(
                    $"AUTOMATION_MANIFEST_MISMATCH command={request.Command} " +
                    $"clientRevision={request.ManifestRevision.Value} " +
                    $"serverRevision={AutomationPipeProtocol.CommandManifestRevision} " +
                    $"correlationId={correlationId}");
                return CreateResponse(
                    correlationId,
                    $"Automation command manifest revision mismatch (client={request.ManifestRevision.Value}, server={AutomationPipeProtocol.CommandManifestRevision}). " +
                    "Rebuild ssctl/MCP/StreamDeck against the current Sussudio source to refresh the numeric command IDs.",
                    errorCode: "manifest-mismatch",
                    success: false,
                    status: AutomationResponseStatus.Error,
                    includeSnapshot: false);
            }
        }
        else
        {
            Logger.Log(
                $"STALE_CLIENT_NO_MANIFEST_REVISION command={request.Command} correlationId={correlationId} " +
                "(client predates manifest-revision handshake; allowing for back-compat but command IDs are not verified).");
        }

        var authorized = IsAuthorized(request);
        if (request.Command == AutomationCommandKind.Authenticate)
        {
            return CreateResponse(
                correlationId,
                authorized
                    ? "Authentication accepted."
                    : "Authentication rejected.",
                errorCode: authorized ? null : "unauthorized",
                success: authorized,
                status: authorized ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
                includeSnapshot: false);
        }

        if (!authorized)
        {
            return CreateResponse(
                correlationId,
                "Unauthorized command request.",
                errorCode: "unauthorized",
                success: false,
                status: AutomationResponseStatus.Error,
                includeSnapshot: false);
        }

        if (RequiresReadyDevices(request.Command) && !IsAutomationReady())
        {
            return CreateResponse(
                correlationId,
                "Automation is still initializing devices; retry shortly.",
                errorCode: "not-ready",
                success: false,
                status: AutomationResponseStatus.NotReady,
                retryAfterMs: 1000);
        }

        return null;
    }

    private bool IsAutomationReady()
    {
        return _readinessPort.IsInitialized || _readinessPort.Devices.Count > 0;
    }

    private bool IsAuthorized(AutomationCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(_authToken))
        {
            return true;
        }

        var providedToken = request.AuthToken;
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            providedToken = GetString(request.Payload, "authToken");
        }

        // Constant-time comparison: even on a local pipe, sidechannel timing
        // hardening is cheap insurance and matches how token comparison is
        // expected to work in any future remote/transport variant.
        var expected = Encoding.UTF8.GetBytes(_authToken);
        var actual = Encoding.UTF8.GetBytes(providedToken ?? string.Empty);
        var ok = expected.Length == actual.Length
            && CryptographicOperations.FixedTimeEquals(expected, actual);
        if (!ok)
        {
            Logger.LogEvent("AUTH_FAILED", $"command={request.Command} correlationId={request.CorrelationId ?? "<none>"}");
        }
        return ok;
    }

    private static string RequireString(JsonElement payload, string propertyName)
    {
        var value = GetString(payload, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        return value;
    }

    private static bool RequireBool(JsonElement payload, string propertyName)
    {
        var value = GetBool(payload, propertyName);
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
        }

        return value.Value;
    }

    private static double RequireDouble(JsonElement payload, string propertyName)
    {
        var value = GetDouble(payload, propertyName);
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
        }

        return value.Value;
    }

    private static string? GetString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : null;
    }

    private static bool? GetBool(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when property.TryGetInt32(out var number) => number != 0,
            _ => null
        };
    }

    private static double? GetDouble(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numeric))
        {
            return double.IsFinite(numeric) ? numeric : null;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return double.IsFinite(parsed) ? parsed : null;
        }

        return null;
    }

    private static int? GetInt(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numeric))
        {
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool RequiresReadyDevices(AutomationCommandKind command)
        => AutomationCommandCatalog.Get(command).RequiresReadyDevices;

    private static string ValidatePathPayload(
        AutomationCommandKind command,
        string payloadKey,
        string path)
        => AutomationCommandCatalog.ValidatePath(command, payloadKey, path);

    private static AutomationWindowAction ParseWindowAction(JsonElement payload)
    {
        var raw = GetString(payload, "action");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AutomationWindowAction.Restore;
        }

        if (Enum.TryParse<AutomationWindowAction>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid window action: '{raw}'.");
    }

    private static AutomationFlashbackAction ParseFlashbackAction(JsonElement payload)
    {
        var raw = RequireString(payload, "action");
        var normalized = raw.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (Enum.TryParse<AutomationFlashbackAction>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"Invalid flashback action: '{raw}'. Expected play, pause, go-live, seek, begin-scrub, update-scrub, end-scrub, set-in-point, set-out-point, or clear-in-out-points.");
    }

    private static AutomationWaitCondition ParseWaitCondition(JsonElement payload)
    {
        var raw = GetString(payload, "condition");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AutomationWaitCondition.PreviewFramesActive;
        }

        if (Enum.TryParse<AutomationWaitCondition>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid wait condition: '{raw}'.");
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

// Holds a single trivial-handler delegate and the payload property name needed to
// extract the typed argument for the dispatcher tables above.
internal sealed record AutomationCommandHandler<TTarget>(
    Func<TTarget, JsonElement, CancellationToken, Task> Invoke,
    Func<AutomationCommandKind, JsonElement, string> AcknowledgeMessage,
    string PayloadFieldName,
    AutomationPayloadFieldType PayloadFieldType)
{
    public Task InvokeAsync(TTarget target, JsonElement payload, CancellationToken cancellationToken)
        => Invoke(target, payload, cancellationToken);

    public static AutomationCommandHandler<TTarget> Bool(
        Func<TTarget, bool, CancellationToken, Task> action,
        string propertyName)
        => new(
            (target, payload, ct) =>
            {
                var value = GetBoolRequired(payload, propertyName);
                return action(target, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.Boolean);

    public static AutomationCommandHandler<TTarget> String(
        Func<TTarget, string, CancellationToken, Task> action,
        string propertyName)
        => new(
            (target, payload, ct) =>
            {
                var value = GetStringRequired(payload, propertyName);
                return action(target, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.String);

    public static AutomationCommandHandler<TTarget> Double(
        Func<TTarget, double, CancellationToken, Task> action,
        string propertyName)
        => new(
            (target, payload, ct) =>
            {
                var value = GetDoubleRequired(payload, propertyName);
                return action(target, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.Number);

    private static bool GetBoolRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
        }

        var result = property.ValueKind switch
        {
            JsonValueKind.True => (bool?)true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when property.TryGetInt32(out var number) => number != 0,
            _ => null
        };

        return result ?? throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
    }

    private static string GetStringRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        var value = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ValueKind != JsonValueKind.Null ? property.ToString() : null;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        return value;
    }

    private static double GetDoubleRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numeric))
        {
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
    }
}
