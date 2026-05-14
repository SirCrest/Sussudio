using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            {
                var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Snapshot retrieved.", snapshot: snapshot);
            }

            case AutomationCommandKind.GetAutomationManifest:
                return CreateResponse(
                    correlationId,
                    "Automation manifest retrieved.",
                    data: AutomationCommandCatalog.CreateManifest(),
                    includeSnapshot: false);

            case AutomationCommandKind.SetFullScreenEnabled:
            {
                var enabled = RequireBool(payload, "enabled");
                await _windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, $"Full screen {(enabled ? "enter" : "exit")} requested.");
            }

            case AutomationCommandKind.OpenRecordingsFolder:
                await _windowControl.OpenRecordingsFolderAsync(cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, "Recordings folder open requested.");

            case AutomationCommandKind.GetDiagnostics:
            {
                var maxEvents = GetInt(payload, "maxEvents") ?? 100;
                var events = _diagnosticsHub.GetRecentEvents(maxEvents);
                return CreateResponse(correlationId, "Diagnostics retrieved.", data: events);
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

            case AutomationCommandKind.GetCaptureOptions:
            {
                var options = await _viewModel.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Capture options retrieved.", data: options);
            }

            case AutomationCommandKind.SetMjpegDecoderCount:
            {
                var decoderCount = GetInt(payload, "decoderCount");
                if (!decoderCount.HasValue)
                {
                    throw new InvalidOperationException("Missing required integer property 'decoderCount'.");
                }

                await _viewModel.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, $"MJPEG decoder count change requested: {decoderCount.Value}.");
            }

            case AutomationCommandKind.SetStatsSectionVisible:
            {
                var section = RequireString(payload, "section");
                var visible = RequireBool(payload, "visible");
                await _viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, $"Stats section '{section}' {(visible ? "expanded" : "collapsed")}.");
            }

            case AutomationCommandKind.SetDeviceAudioMode:
            {
                var mode = RequireString(payload, "mode");
                await _viewModel.SetDeviceAudioModeAsync(mode, cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, $"Device audio mode changed: {mode}.");
            }

            case AutomationCommandKind.SetAnalogAudioGain:
            {
                var gain = RequireDouble(payload, "gain");
                await _viewModel.SetAnalogAudioGainAsync(gain, cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, $"Analog audio gain set to {gain:0.###}%.");
            }

            case AutomationCommandKind.FlashbackAction:
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
                if (!await _viewModel.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false))
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

            case AutomationCommandKind.FlashbackExport:
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
                var exportResult = await _viewModel.ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken).ConfigureAwait(false);
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

            case AutomationCommandKind.FlashbackGetSegments:
            {
                var segments = await _viewModel.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(
                    correlationId,
                    $"Found {segments.Count} segment(s).",
                    data: new { Segments = segments });
            }

            case AutomationCommandKind.VerifyFile:
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

            case AutomationCommandKind.SetOutputPath:
            {
                var outputPath = ValidatePathPayload(
                    AutomationCommandKind.SetOutputPath,
                    "outputPath",
                    RequireString(payload, "outputPath"));
                await _viewModel.SetOutputPathAsync(outputPath, cancellationToken).ConfigureAwait(false);
                return CreateAcknowledgedResponse(correlationId, $"Output path change requested: {outputPath}.");
            }

            case AutomationCommandKind.SetRecordingEnabled:
            {
                var enabled = RequireBool(payload, "enabled");
                await _viewModel.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, $"Recording {(enabled ? "started" : "stopped")}.", snapshot: snapshot);
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

            case AutomationCommandKind.WaitForCondition:
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

            case AutomationCommandKind.VerifyLastRecording:
            {
                var verifyStartedAt = Stopwatch.GetTimestamp();
                var verification = await _diagnosticsHub.VerifyLastRecordingAsync(cancellationToken).ConfigureAwait(false);
                var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
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

            case AutomationCommandKind.AssertSnapshot:
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

            case AutomationCommandKind.ProbeVideoSource:
            {
                var result = await _viewModel.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Video source probe completed.", data: result);
            }

            case AutomationCommandKind.ProbePreviewColor:
            {
                var result = await _viewModel.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Preview color probe completed.", data: result);
            }

            case AutomationCommandKind.CapturePreviewFrame:
            {
                var outputPath = ValidatePathPayload(
                    AutomationCommandKind.CapturePreviewFrame,
                    "outputPath",
                    GetString(payload, "outputPath")
                        ?? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp"));
                var result = await _viewModel.CapturePreviewFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
                return CreateResponse(
                    correlationId,
                    result.Message,
                    data: result,
                    success: result.Succeeded,
                    status: result.Succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
                    errorCode: result.Succeeded ? null : "capture-failed");
            }

            case AutomationCommandKind.CaptureWindowScreenshot:
            {
                var outputPath = ValidatePathPayload(
                    AutomationCommandKind.CaptureWindowScreenshot,
                    "outputPath",
                    GetString(payload, "outputPath")
                        ?? Path.Combine(Path.GetTempPath(), $"window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png"));
                var result = await _windowControl.CaptureWindowScreenshotAsync(outputPath, cancellationToken).ConfigureAwait(false);
                return CreateResponse(
                    correlationId,
                    result.Message,
                    data: result,
                    success: result.Succeeded,
                    status: result.Succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
                    errorCode: result.Succeeded ? null : "capture-failed");
            }

            case AutomationCommandKind.GetPerformanceTimeline:
            {
                var maxEntries = GetInt(payload, "maxEntries") ?? 240;
                var timeline = _diagnosticsHub.GetPerformanceTimeline(maxEntries);
                return CreateResponse(correlationId, "Performance timeline retrieved.", data: timeline);
            }

            case AutomationCommandKind.GetAudioRampTrace:
            {
                var maxEntries = GetInt(payload, "maxEntries") ?? 512;
                var trace = await _viewModel.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Audio ramp trace retrieved.", data: trace);
            }

            case AutomationCommandKind.RestartFlashback:
            {
                await _viewModel.RestartFlashbackAsync(cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, "Flashback restarted.");
            }

            case AutomationCommandKind.SetFlashbackEnabled:
            {
                var enabled = GetBool(payload, "enabled") ?? throw new InvalidOperationException("Missing 'enabled' parameter.");
                await _viewModel.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, $"Flashback {(enabled ? "enabled" : "disabled")}.");
            }

            case AutomationCommandKind.SetMicrophoneEnabled:
            {
                var enabled = GetBool(payload, "enabled") ?? throw new InvalidOperationException("Missing 'enabled' parameter.");
                await _viewModel.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
                return CreateResponse(correlationId, $"Microphone {(enabled ? "enabled" : "disabled")}.");
            }

            default:
                return CreateResponse(
                    correlationId,
                    $"Unsupported command: {request.Command}",
                    errorCode: "unsupported-command",
                    success: false,
                    status: AutomationResponseStatus.Error);
        }
    }
}
