using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.ViewModels;

namespace ElgatoCapture.Services;

public sealed class AutomationCommandDispatcher : IAutomationCommandDispatcher
{
    private readonly MainViewModel _viewModel;
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly IAutomationWindowControl _windowControl;
    private readonly string? _authToken;
    private readonly object _closeArmLock = new();
    private bool _closeArmed;

    private const int DefaultWaitTimeoutMs = 10_000;
    private const int DefaultWaitPollMs = 250;

    public AutomationCommandDispatcher(
        MainViewModel viewModel,
        IAutomationDiagnosticsHub diagnosticsHub,
        IAutomationWindowControl windowControl,
        string? authToken = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
            var authorized = IsAuthorized(request);
            if (request.Command == AutomationCommandKind.Authenticate)
            {
                return CreateSuccessResponse(
                    correlationId,
                    authorized
                        ? "Authentication accepted."
                        : "Authentication rejected.",
                    errorCode: authorized ? null : "unauthorized",
                    success: authorized,
                    status: authorized ? "ok" : "error",
                    includeSnapshot: false);
            }

            if (!authorized)
            {
                return CreateSuccessResponse(
                    correlationId,
                    "Unauthorized command request.",
                    errorCode: "unauthorized",
                    success: false,
                    status: "error",
                    includeSnapshot: false);
            }

            if (RequiresReadyDevices(request.Command) && !IsAutomationReady())
            {
                return CreateSuccessResponse(
                    correlationId,
                    "Automation is still initializing devices; retry shortly.",
                    errorCode: "not-ready",
                    success: false,
                    status: "not_ready",
                    retryAfterMs: 1000);
            }

            var payload = request.Payload;
            switch (request.Command)
            {
                case AutomationCommandKind.GetSnapshot:
                    return CreateSuccessResponse(correlationId, "Snapshot retrieved.");

                case AutomationCommandKind.GetDiagnostics:
                {
                    var maxEvents = GetInt(payload, "maxEvents") ?? 100;
                    var events = _diagnosticsHub.GetRecentEvents(maxEvents);
                    return CreateSuccessResponse(correlationId, "Diagnostics retrieved.", data: events);
                }

                case AutomationCommandKind.RefreshDevices:
                    await _viewModel.RefreshDevicesForAutomationAsync(cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, "Device list refresh requested.");

                case AutomationCommandKind.SelectDevice:
                {
                    var deviceId = GetString(payload, "deviceId");
                    var deviceName = GetString(payload, "deviceName");
                    await _viewModel.SelectDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, "Capture device selection requested.");
                }

                case AutomationCommandKind.SelectAudioInputDevice:
                {
                    var deviceId = GetString(payload, "deviceId");
                    var deviceName = GetString(payload, "deviceName");
                    await _viewModel.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, "Audio input device selection requested.");
                }

                case AutomationCommandKind.SetCustomAudioInput:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetCustomAudioInputEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Custom audio input {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetResolution:
                {
                    var resolution = RequireString(payload, "resolution");
                    await _viewModel.SetResolutionAsync(resolution, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Resolution change requested: {resolution}.");
                }

                case AutomationCommandKind.SetFrameRate:
                {
                    var frameRate = RequireDouble(payload, "frameRate");
                    await _viewModel.SetFrameRateAsync(frameRate, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Frame rate change requested: {frameRate:0.###}.");
                }

                case AutomationCommandKind.SetRecordingFormat:
                {
                    var format = RequireString(payload, "format");
                    await _viewModel.SetRecordingFormatAsync(format, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Recording format change requested: {format}.");
                }

                case AutomationCommandKind.SetQuality:
                {
                    var quality = RequireString(payload, "quality");
                    await _viewModel.SetQualityAsync(quality, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Quality change requested: {quality}.");
                }

                case AutomationCommandKind.SetCustomBitrate:
                {
                    var bitrate = RequireDouble(payload, "bitrateMbps");
                    await _viewModel.SetCustomBitrateAsync(bitrate, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Custom bitrate change requested: {bitrate:0.###} Mbps.");
                }

                case AutomationCommandKind.SetHdrEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetHdrEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"HDR {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetTrueHdrPreviewEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetTrueHdrPreviewEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"True HDR preview {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetAudioEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetAudioEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Audio capture {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetAudioPreviewEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetAudioPreviewEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Audio preview {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetOutputPath:
                {
                    var outputPath = RequireString(payload, "outputPath");
                    await _viewModel.SetOutputPathAsync(outputPath, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Output path change requested: {outputPath}.");
                }

                case AutomationCommandKind.SetPreviewEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetPreviewEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Preview {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetRecordingEnabled:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Recording {(enabled ? "start" : "stop")} requested.");
                }

                case AutomationCommandKind.ArmClose:
                {
                    var armed = GetBool(payload, "armed") ?? true;
                    lock (_closeArmLock)
                    {
                        _closeArmed = armed;
                    }
                    return CreateAcknowledgedResponse(correlationId, $"Window close arm state requested: {(armed ? "armed" : "disarmed")}.");
                }

                case AutomationCommandKind.WindowAction:
                {
                    var action = ParseWindowAction(payload);
                    if (action == AutomationWindowAction.Close)
                    {
                        var armed = false;
                        lock (_closeArmLock)
                        {
                            armed = _closeArmed;
                            _closeArmed = false;
                        }

                        if (!armed)
                        {
                            return CreateSuccessResponse(
                                correlationId,
                                "Window close is disallowed until ArmClose is requested.",
                                errorCode: "window-close-not-armed",
                                success: false,
                                status: "error");
                        }
                    }

                    if (action == AutomationWindowAction.Close)
                    {
                        _ = ExecuteWindowActionAsync(action, CancellationToken.None).ContinueWith(
                            closeTask =>
                            {
                                if (closeTask.Exception == null)
                                {
                                    return;
                                }

                                var closeException = closeTask.Exception.Flatten().InnerException ?? closeTask.Exception;
                                Logger.Log(
                                    $"Window close task failed asynchronously [correlationId={correlationId}] type={closeException.GetType().Name}: {closeException.Message}");
                                Logger.LogException(closeException);
                            },
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted,
                            TaskScheduler.Default);

                        return CreateAcknowledgedResponse(correlationId, "Window close requested.");
                    }

                    await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Window action requested: {action}.");
                }

                case AutomationCommandKind.WaitForCondition:
                {
                    var condition = ParseWaitCondition(payload);
                    var timeoutMs = Math.Clamp(GetInt(payload, "timeoutMs") ?? DefaultWaitTimeoutMs, 250, 300_000);
                    var pollMs = Math.Clamp(GetInt(payload, "pollMs") ?? DefaultWaitPollMs, 50, 5_000);
                    var met = await WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken).ConfigureAwait(false);

                    return CreateSuccessResponse(
                        correlationId,
                        met
                            ? $"Condition met: {condition}."
                            : $"Timed out waiting for condition: {condition}.",
                        data: new Dictionary<string, object?>
                        {
                            ["condition"] = condition.ToString(),
                            ["met"] = met,
                            ["timeoutMs"] = timeoutMs,
                            ["pollMs"] = pollMs
                        },
                        errorCode: met ? null : "timeout",
                        success: met,
                        status: met ? "ok" : "error");
                }

                case AutomationCommandKind.VerifyLastRecording:
                {
                    var verifyStartedAt = Stopwatch.GetTimestamp();
                    var verification = await _diagnosticsHub.VerifyLastRecordingAsync(cancellationToken).ConfigureAwait(false);
                    var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
                    return CreateSuccessResponse(
                        correlationId,
                        verification.Message,
                        data: new
                        {
                            Verification = verification,
                            HdrParity = verification.HdrParity
                        },
                        errorCode: verification.Succeeded ? null : "verification-failed",
                        success: verification.Succeeded,
                        status: verification.Succeeded ? "ok" : "error",
                        elapsedMs: elapsedMs);
                }

                case AutomationCommandKind.AssertSnapshot:
                {
                    var snapshot = _diagnosticsHub.GetLatestSnapshot();
                    var assertions = ParseAssertions(payload);
                    var failures = new List<string>();
                    foreach (var assertion in assertions)
                    {
                        if (!TryEvaluateAssertion(snapshot, assertion, out var failure))
                        {
                            failures.Add(failure ?? $"assertion-failed({assertion.Field})");
                        }
                    }

                    var passed = failures.Count == 0;
                    return CreateSuccessResponse(
                        correlationId,
                        passed
                            ? $"All {assertions.Count} snapshot assertions passed."
                            : $"{failures.Count} of {assertions.Count} snapshot assertions failed.",
                        data: new Dictionary<string, object?>
                        {
                            ["assertions"] = assertions.Count,
                            ["passed"] = passed,
                            ["failures"] = failures
                        },
                        errorCode: passed ? null : "assertion-failed",
                        success: passed,
                        status: passed ? "ok" : "error");
                }

                case AutomationCommandKind.ProbeVideoSource:
                {
                    var result = _viewModel.ProbeVideoSource();
                    return CreateSuccessResponse(correlationId, "Video source probe completed.", data: result);
                }

                case AutomationCommandKind.ProbePreviewColor:
                {
                    var result = _viewModel.ProbePreviewColor();
                    return CreateSuccessResponse(correlationId, "Preview color probe completed.", data: result);
                }

                case AutomationCommandKind.CapturePreviewFrame:
                {
                    var outputPath = GetString(payload, "outputPath")
                        ?? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
                    var result = await _viewModel.CapturePreviewFrameAsync(outputPath).ConfigureAwait(false);
                    return CreateSuccessResponse(
                        correlationId,
                        result.Message,
                        data: result,
                        success: result.Succeeded,
                        status: result.Succeeded ? "ok" : "error",
                        errorCode: result.Succeeded ? null : "capture-failed");
                }

                default:
                    return CreateSuccessResponse(
                        correlationId,
                        $"Unsupported command: {request.Command}",
                        errorCode: "unsupported-command",
                        success: false,
                        status: "error");
            }
        }
        catch (OperationCanceledException)
        {
            return CreateSuccessResponse(
                correlationId,
                "Command canceled.",
                errorCode: "canceled",
                success: false,
                status: "error",
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

            return CreateSuccessResponse(
                correlationId,
                message,
                errorCode: "command-failed",
                success: false,
                status: "error",
                elapsedMs: (long)Math.Round(Stopwatch.GetElapsedTime(commandStartedAt).TotalMilliseconds));
        }
    }

    private AutomationCommandResponse CreateSuccessResponse(
        string correlationId,
        string message,
        object? data = null,
        string? errorCode = null,
        bool success = true,
        bool includeSnapshot = true,
        string status = "ok",
        string? commandLifecycle = null,
        int? retryAfterMs = null,
        long? elapsedMs = null)
    {
        var lifecycle = commandLifecycle ?? (success ? "completed" : "failed");
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
            Snapshot = includeSnapshot ? _diagnosticsHub.GetLatestSnapshot() : null
        };
    }

    private AutomationCommandResponse CreateAcknowledgedResponse(
        string correlationId,
        string message,
        object? data = null,
        bool includeSnapshot = true)
    {
        return CreateSuccessResponse(
            correlationId,
            message,
            data: data,
            includeSnapshot: includeSnapshot,
            status: "ok",
            commandLifecycle: "acknowledged");
    }

    private bool IsAutomationReady()
    {
        return _viewModel.IsInitialized || _viewModel.Devices.Count > 0;
    }

    private static bool RequiresReadyDevices(AutomationCommandKind command)
    {
        return command switch
        {
            AutomationCommandKind.SelectDevice => true,
            AutomationCommandKind.SelectAudioInputDevice => true,
            AutomationCommandKind.SetCustomAudioInput => true,
            AutomationCommandKind.SetResolution => true,
            AutomationCommandKind.SetFrameRate => true,
            AutomationCommandKind.SetRecordingFormat => true,
            AutomationCommandKind.SetQuality => true,
            AutomationCommandKind.SetCustomBitrate => true,
            AutomationCommandKind.SetHdrEnabled => true,
            AutomationCommandKind.SetAudioEnabled => true,
            AutomationCommandKind.SetAudioPreviewEnabled => true,
            AutomationCommandKind.SetPreviewEnabled => true,
            AutomationCommandKind.SetRecordingEnabled => true,
            _ => false
        };
    }

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

    private async Task ExecuteWindowActionAsync(AutomationWindowAction action, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case AutomationWindowAction.Minimize:
                await _windowControl.MinimizeAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Maximize:
                await _windowControl.MaximizeAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Restore:
                await _windowControl.RestoreAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Close:
                // Closing should be best-effort even if the request token is canceled during shutdown.
                await _windowControl.CloseAsync(CancellationToken.None).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown window action: {action}");
        }
    }

    private async Task<bool> WaitForConditionAsync(
        AutomationWaitCondition condition,
        int timeoutMs,
        int pollMs,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        while ((DateTimeOffset.UtcNow - started).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = _diagnosticsHub.GetLatestSnapshot();
            if (ConditionSatisfied(condition, snapshot))
            {
                return true;
            }

            await Task.Delay(pollMs, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static bool ConditionSatisfied(AutomationWaitCondition condition, AutomationSnapshot snapshot)
    {
        return condition switch
        {
            AutomationWaitCondition.PreviewFramesActive =>
                snapshot.IsPreviewing && (snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0),
            AutomationWaitCondition.PreviewRendererHealthy =>
                snapshot.IsPreviewing &&
                !snapshot.PreviewBlankSuspected &&
                !snapshot.PreviewStalled &&
                (snapshot.PreviewGpuActive || snapshot.PreviewRendererAttached),
            AutomationWaitCondition.AudioSignalPresent =>
                snapshot.AudioSignalPresent,
            AutomationWaitCondition.RecordingFileGrowing =>
                snapshot.IsRecording && snapshot.RecordingFileGrowing,
            AutomationWaitCondition.RecordingStopped =>
                !snapshot.IsRecording,
            AutomationWaitCondition.VerificationReady =>
                snapshot.LastVerification != null,
            AutomationWaitCondition.HdrModeApplied =>
                snapshot.RequestedHdrEnabled.HasValue
                    ? snapshot.IsHdrEnabled == snapshot.RequestedHdrEnabled.Value
                    : snapshot.IsHdrEnabled,
            AutomationWaitCondition.PerformancePerfectionMet =>
                snapshot.PerformancePerfectionMet,
            AutomationWaitCondition.HdrVerificationReady =>
                snapshot.LastVerification is { } verification &&
                (!snapshot.HdrOutputActive ||
                 verification.HdrParity is { Requested: true, Verified: true } ||
                 verification.HdrMetadataPresent == true),
            AutomationWaitCondition.AudioFramesFlowing =>
                snapshot.AudioReaderActive && snapshot.AudioFramesArrived > 0,
            AutomationWaitCondition.VideoFramesFlowing =>
                snapshot.VideoReaderActive && snapshot.IngestVideoFramesArrived > 0,
            _ => false
        };
    }

    private static List<SnapshotAssertion> ParseAssertions(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("assertions", out var assertionsElement) ||
            assertionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("AssertSnapshot requires an 'assertions' array.");
        }

        var assertions = new List<SnapshotAssertion>();
        foreach (var item in assertionsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var field = GetString(item, "field");
            var op = GetString(item, "op") ?? "eq";
            var value = GetString(item, "value");
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            assertions.Add(new SnapshotAssertion
            {
                Field = field,
                Op = op,
                Value = value
            });
        }

        if (assertions.Count == 0)
        {
            throw new InvalidOperationException("AssertSnapshot requires at least one valid assertion object.");
        }

        return assertions;
    }

    private static bool TryEvaluateAssertion(
        AutomationSnapshot snapshot,
        SnapshotAssertion assertion,
        out string? failure)
    {
        var property = typeof(AutomationSnapshot).GetProperty(
            assertion.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null)
        {
            failure = $"field-not-found({assertion.Field})";
            return false;
        }

        var actual = property.GetValue(snapshot);
        var op = assertion.Op?.Trim().ToLowerInvariant() ?? "eq";
        var expected = assertion.Value ?? string.Empty;

        if (TryCompareNumeric(actual, expected, op, out var numericResult))
        {
            failure = numericResult
                ? null
                : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actual})";
            return numericResult;
        }

        if (TryCompareBoolean(actual, expected, op, out var boolResult))
        {
            failure = boolResult
                ? null
                : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actual})";
            return boolResult;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
        var result = op switch
        {
            "eq" => string.Equals(actualText, expected, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(actualText, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => actualText.Contains(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        failure = result
            ? null
            : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actualText})";
        return result;
    }

    private static bool TryCompareNumeric(object? actual, string expected, string op, out bool result)
    {
        result = false;
        if (!double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber))
        {
            return false;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture);
        if (!double.TryParse(actualText, NumberStyles.Float, CultureInfo.InvariantCulture, out var actualNumber))
        {
            return false;
        }

        result = op switch
        {
            "eq" => Math.Abs(actualNumber - expectedNumber) < 0.0001,
            "neq" => Math.Abs(actualNumber - expectedNumber) >= 0.0001,
            "gt" => actualNumber > expectedNumber,
            "gte" => actualNumber >= expectedNumber,
            "lt" => actualNumber < expectedNumber,
            "lte" => actualNumber <= expectedNumber,
            _ => false
        };
        return true;
    }

    private static bool TryCompareBoolean(object? actual, string expected, string op, out bool result)
    {
        result = false;
        if (!bool.TryParse(expected, out var expectedBool))
        {
            return false;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture);
        if (!bool.TryParse(actualText, out var actualBool))
        {
            return false;
        }

        result = op switch
        {
            "eq" => actualBool == expectedBool,
            "neq" => actualBool != expectedBool,
            _ => false
        };
        return true;
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

        return string.Equals(_authToken, providedToken, StringComparison.Ordinal);
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
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
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
}
