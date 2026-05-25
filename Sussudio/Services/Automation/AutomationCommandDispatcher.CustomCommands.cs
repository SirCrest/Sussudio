using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteCustomCommandAsync(
        AutomationCommandRequest request,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        switch (request.Command)
        {
            case AutomationCommandKind.GetSnapshot:
                return await ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.GetAutomationManifest:
                return ExecuteGetAutomationManifestCommand(correlationId);

            case AutomationCommandKind.SetFullScreenEnabled:
                return await ExecuteSetFullScreenEnabledCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.OpenRecordingsFolder:
                return await ExecuteOpenRecordingsFolderCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.GetDiagnostics:
                return ExecuteGetDiagnosticsCommand(payload, correlationId);

            case AutomationCommandKind.RefreshDevices:
                return await ExecuteRefreshDevicesCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SelectDevice:
                return await ExecuteSelectDeviceCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SelectAudioInputDevice:
                return await ExecuteSelectAudioInputDeviceCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.GetCaptureOptions:
                return await ExecuteGetCaptureOptionsCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetMjpegDecoderCount:
                return await ExecuteSetMjpegDecoderCountCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetDeviceAudioMode:
                return await ExecuteSetDeviceAudioModeCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetAnalogAudioGain:
                return await ExecuteSetAnalogAudioGainCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.FlashbackAction:
                return await ExecuteFlashbackActionCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.FlashbackExport:
                return await ExecuteFlashbackExportCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.FlashbackGetSegments:
                return await ExecuteFlashbackGetSegmentsCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.VerifyFile:
                return await ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetOutputPath:
                return await ExecuteSetOutputPathCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetRecordingEnabled:
                return await ExecuteSetRecordingEnabledCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.ArmClose:
                return ExecuteArmCloseCommand(payload, correlationId);

            case AutomationCommandKind.WindowAction:
                return await ExecuteWindowActionCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.WaitForCondition:
                return await ExecuteWaitForConditionCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.VerifyLastRecording:
                return await ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.AssertSnapshot:
                return await ExecuteAssertSnapshotCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.ProbeVideoSource:
                return await ExecuteProbeVideoSourceCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.ProbePreviewColor:
                return await ExecuteProbePreviewColorCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.CapturePreviewFrame:
                return await ExecuteCapturePreviewFrameCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.CaptureWindowScreenshot:
                return await ExecuteCaptureWindowScreenshotCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.GetPerformanceTimeline:
                return ExecuteGetPerformanceTimelineCommand(payload, correlationId);

            case AutomationCommandKind.GetAudioRampTrace:
                return await ExecuteGetAudioRampTraceCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.RestartFlashback:
                return await ExecuteRestartFlashbackCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetFlashbackEnabled:
                return await ExecuteSetFlashbackEnabledCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.SetMicrophoneEnabled:
                return await ExecuteSetMicrophoneEnabledCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            default:
                return CreateResponse(
                    correlationId,
                    $"Unsupported command: {request.Command}",
                    errorCode: "unsupported-command",
                    success: false,
                status: AutomationResponseStatus.Error);
        }
    }

    private async Task<AutomationCommandResponse> ExecuteRefreshDevicesCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        await _deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Device list refresh requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteSelectDeviceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var deviceId = GetString(payload, "deviceId");
        var deviceName = GetString(payload, "deviceName");
        await _deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Capture device selection requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteSelectAudioInputDeviceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var deviceId = GetString(payload, "deviceId");
        var deviceName = GetString(payload, "deviceName");
        await _deviceSelectionPort.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Audio input device selection requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteGetCaptureOptionsCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var options = await _snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Capture options retrieved.", data: options);
    }

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

    private async Task<AutomationCommandResponse> ExecuteSetDeviceAudioModeCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var mode = RequireString(payload, "mode");
        await _audioPort.SetDeviceAudioModeAsync(mode, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Device audio mode changed: {mode}.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetAnalogAudioGainCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var gain = RequireDouble(payload, "gain");
        await _audioPort.SetAnalogAudioGainAsync(gain, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Analog audio gain set to {gain:0.###}%.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetMicrophoneEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = GetBool(payload, "enabled") ?? throw new InvalidOperationException("Missing 'enabled' parameter.");
        await _audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Microphone {(enabled ? "enabled" : "disabled")}.");
    }

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

    private async Task<AutomationCommandResponse> ExecuteSetFullScreenEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = RequireBool(payload, "enabled");
        await _windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Full screen {(enabled ? "enter" : "exit")} requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteOpenRecordingsFolderCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        await _windowControl.OpenRecordingsFolderAsync(cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Recordings folder open requested.");
    }

    private AutomationCommandResponse ExecuteArmCloseCommand(
        JsonElement payload,
        string correlationId)
    {
        var armed = GetBool(payload, "armed") ?? true;
        lock (_closeArmLock)
        {
            _closeArmed = armed;
        }

        return CreateAcknowledgedResponse(correlationId, $"Window close arm state requested: {(armed ? "armed" : "disarmed")}.");
    }

    private async Task<AutomationCommandResponse> ExecuteWindowActionCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
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
                return CreateResponse(
                    correlationId,
                    "Window close is disallowed until ArmClose is requested.",
                    errorCode: "window-close-not-armed",
                    success: false,
                    status: AutomationResponseStatus.Error);
            }

            await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, "Window close completed.");
        }

        await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Window action requested: {action}.");
    }

    private async Task ExecuteWindowActionAsync(
        AutomationWindowAction action,
        CancellationToken cancellationToken,
        JsonElement payload = default)
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
                await _windowControl.CloseAsync(cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Move:
                var mx = GetInt(payload, "x") ?? throw new InvalidOperationException("Move requires 'x' parameter.");
                var my = GetInt(payload, "y") ?? throw new InvalidOperationException("Move requires 'y' parameter.");
                await _windowControl.MoveToAsync(mx, my, cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.Resize:
                var rw = GetInt(payload, "width") ?? throw new InvalidOperationException("Resize requires 'width' parameter.");
                var rh = GetInt(payload, "height") ?? throw new InvalidOperationException("Resize requires 'height' parameter.");
                await _windowControl.ResizeToAsync(rw, rh, cancellationToken).ConfigureAwait(false);
                break;
            case AutomationWindowAction.SnapLeft:
            case AutomationWindowAction.SnapRight:
            case AutomationWindowAction.SnapTopLeft:
            case AutomationWindowAction.SnapTopRight:
            case AutomationWindowAction.SnapBottomLeft:
            case AutomationWindowAction.SnapBottomRight:
            case AutomationWindowAction.Center:
                await _windowControl.SnapToRegionAsync(action, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown window action: {action}");
        }
    }
}

public sealed partial class AutomationCommandDispatcher
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> SnapshotPropertyCache = new(StringComparer.OrdinalIgnoreCase);

    private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
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
        return CreateResponse(
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
            status: passed ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            snapshot: snapshot);
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
        var property = SnapshotPropertyCache.GetOrAdd(
            assertion.Field,
            field => typeof(AutomationSnapshot).GetProperty(
                field,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
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

    private async Task<AutomationCommandResponse> ExecuteWaitForConditionCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var condition = ParseWaitCondition(payload);
        var timeoutMs = Math.Clamp(GetInt(payload, "timeoutMs") ?? DefaultWaitTimeoutMs, 250, 300_000);
        var pollMs = Math.Clamp(GetInt(payload, "pollMs") ?? DefaultWaitPollMs, 50, 5_000);
        var (met, snapshot) = await WaitForConditionAsync(condition, timeoutMs, pollMs, cancellationToken).ConfigureAwait(false);

        return CreateResponse(
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
            status: met ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            snapshot: snapshot);
    }

    private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(
        AutomationWaitCondition condition,
        int timeoutMs,
        int pollMs,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var snapshot = _diagnosticsHub.GetLatestSnapshot();
        while (Stopwatch.GetElapsedTime(started).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = _diagnosticsHub.GetLatestSnapshot();
            if (ConditionSatisfied(condition, snapshot))
            {
                return (true, snapshot);
            }

            await Task.Delay(pollMs, cancellationToken).ConfigureAwait(false);
        }

        return (false, _diagnosticsHub.GetLatestSnapshot());
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
                snapshot.PreviewFirstVisualConfirmed &&
                (snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0),
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

    private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Snapshot retrieved.", snapshot: snapshot);
    }

    private AutomationCommandResponse ExecuteGetAutomationManifestCommand(string correlationId)
    {
        return CreateResponse(
            correlationId,
            "Automation manifest retrieved.",
            data: AutomationCommandCatalog.CreateManifest(),
            includeSnapshot: false);
    }

    private AutomationCommandResponse ExecuteGetDiagnosticsCommand(
        JsonElement payload,
        string correlationId)
    {
        var maxEvents = GetInt(payload, "maxEvents") ?? 100;
        var events = _diagnosticsHub.GetRecentEvents(maxEvents);
        return CreateResponse(correlationId, "Diagnostics retrieved.", data: events);
    }

    private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(
        JsonElement payload,
        string correlationId)
    {
        var maxEntries = GetInt(payload, "maxEntries") ?? 240;
        var timeline = _diagnosticsHub.GetPerformanceTimeline(maxEntries);
        return CreateResponse(correlationId, "Performance timeline retrieved.", data: timeline);
    }

    private async Task<AutomationCommandResponse> ExecuteGetAudioRampTraceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var maxEntries = GetInt(payload, "maxEntries") ?? 512;
        var trace = await _snapshotQueryPort.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Audio ramp trace retrieved.", data: trace);
    }

    private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var filePath = ValidatePathPayload(
            AutomationCommandKind.VerifyFile,
            "filePath",
            RequireString(payload, "filePath"));
        var verificationProfile = GetString(payload, "verificationProfile");
        var verifyStartedAt = Stopwatch.GetTimestamp();
        var verification = await _diagnosticsHub
            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)
            .ConfigureAwait(false);
        var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
        return CreateVerificationResponse(correlationId, verification, elapsedMs);
    }

    private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var verifyStartedAt = Stopwatch.GetTimestamp();
        var verification = await _diagnosticsHub.VerifyLastRecordingAsync(cancellationToken).ConfigureAwait(false);
        var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
        return CreateVerificationResponse(correlationId, verification, elapsedMs);
    }

    private AutomationCommandResponse CreateVerificationResponse(
        string correlationId,
        RecordingVerificationResult verification,
        long elapsedMs)
    {
        return CreateResponse(
            correlationId,
            verification.Message,
            data: new
            {
                Verification = verification,
                HdrParity = verification.HdrParity
            },
            errorCode: verification.Succeeded ? null : "verification-failed",
            success: verification.Succeeded,
            status: verification.Succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            elapsedMs: elapsedMs);
    }

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
