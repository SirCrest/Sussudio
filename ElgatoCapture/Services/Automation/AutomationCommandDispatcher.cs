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
using ElgatoCapture.Models;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Automation;

public sealed class AutomationCommandDispatcher : IAutomationCommandDispatcher
{
    private readonly IAutomationViewModel _viewModel;
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly IAutomationWindowControl _windowControl;
    private readonly string? _authToken;
    private readonly object _closeArmLock = new();
    private bool _closeArmed;

    private static readonly ConcurrentDictionary<string, PropertyInfo?> SnapshotPropertyCache = new(StringComparer.OrdinalIgnoreCase);

    private const int DefaultWaitTimeoutMs = 10_000;
    private const int DefaultWaitPollMs = 250;

    public AutomationCommandDispatcher(
        IAutomationViewModel viewModel,
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
                return CreateResponse(
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
                return CreateResponse(
                    correlationId,
                    "Unauthorized command request.",
                    errorCode: "unauthorized",
                    success: false,
                    status: "error",
                    includeSnapshot: false);
            }

            if (RequiresReadyDevices(request.Command) && !IsAutomationReady())
            {
                return CreateResponse(
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
                {
                    var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
                    return CreateResponse(correlationId, "Snapshot retrieved.", snapshot: snapshot);
                }

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

                case AutomationCommandKind.SetVideoFormat:
                {
                    var videoFormat = RequireString(payload, "videoFormat");
                    await _viewModel.SetVideoFormatAsync(videoFormat, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Video format change requested: {videoFormat}.");
                }

                case AutomationCommandKind.GetCaptureOptions:
                {
                    var options = await _viewModel.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    return CreateResponse(correlationId, "Capture options retrieved.", data: options);
                }

                case AutomationCommandKind.SetPreset:
                {
                    var preset = RequireString(payload, "preset");
                    await _viewModel.SetPresetAsync(preset, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Preset change requested: {preset}.");
                }

                case AutomationCommandKind.SetSplitEncodeMode:
                {
                    var splitEncodeMode = RequireString(payload, "splitEncodeMode");
                    await _viewModel.SetSplitEncodeModeAsync(splitEncodeMode, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Split encode mode change requested: {splitEncodeMode}.");
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

                case AutomationCommandKind.SetShowAllCaptureOptions:
                {
                    var enabled = RequireBool(payload, "enabled");
                    await _viewModel.SetShowAllCaptureOptionsAsync(enabled, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Show-all capture options {(enabled ? "enable" : "disable")} requested.");
                }

                case AutomationCommandKind.SetPreviewVolume:
                {
                    var previewVolumePercent = RequireDouble(payload, "previewVolumePercent");
                    await _viewModel.SetPreviewVolumeAsync(previewVolumePercent, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Preview volume change requested: {previewVolumePercent:0.###}%.");
                }

                case AutomationCommandKind.SetStatsVisible:
                {
                    var visible = RequireBool(payload, "visible");
                    await _viewModel.SetStatsVisibleAsync(visible, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Stats visibility {(visible ? "show" : "hide")} requested.");
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

                case AutomationCommandKind.SetSettingsVisible:
                {
                    var visible = RequireBool(payload, "visible");
                    await _viewModel.SetSettingsVisibleAsync(visible, cancellationToken).ConfigureAwait(false);
                    return CreateAcknowledgedResponse(correlationId, $"Settings panel {(visible ? "shown" : "hidden")}.");
                }

                case AutomationCommandKind.FlashbackAction:
                {
                    var action = ParseFlashbackAction(payload);
                    var positionMs = action switch
                    {
                        AutomationFlashbackAction.Play => GetDouble(payload, "positionMs"),
                        AutomationFlashbackAction.Seek => GetDouble(payload, "positionMs") ?? 0,
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
                        throw new InvalidOperationException("Flashback is not active.");
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
                    var outputPath = RequireString(payload, "outputPath");
                    var useSelectionRange = GetBool(payload, "useSelectionRange") ?? false;
                    var exportResult = await _viewModel.ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, cancellationToken).ConfigureAwait(false);
                    return CreateResponse(
                        correlationId,
                        exportResult.StatusMessage ?? (exportResult.Succeeded ? "Export complete." : "Export failed."),
                        data: new
                        {
                            exportResult.Succeeded,
                            exportResult.OutputPath,
                            exportResult.StatusMessage,
                            FileSizeBytes = File.Exists(exportResult.OutputPath) ? new FileInfo(exportResult.OutputPath).Length : 0L
                        },
                        errorCode: exportResult.Succeeded ? null : "export-failed",
                        success: exportResult.Succeeded,
                        status: exportResult.Succeeded ? "ok" : "error");
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
                    var filePath = RequireString(payload, "filePath");
                    var verifyStartedAt = Stopwatch.GetTimestamp();
                    var verification = await _diagnosticsHub.VerifyFileAsync(filePath, cancellationToken).ConfigureAwait(false);
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
                        status: verification.Succeeded ? "ok" : "error",
                        elapsedMs: elapsedMs);
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
                    var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(CancellationToken.None).ConfigureAwait(false);
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
                                status: "error");
                        }

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
                        status: met ? "ok" : "error",
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
                        status: verification.Succeeded ? "ok" : "error",
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
                        status: passed ? "ok" : "error",
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
                    var outputPath = GetString(payload, "outputPath")
                        ?? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
                    var result = await _viewModel.CapturePreviewFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
                    return CreateResponse(
                        correlationId,
                        result.Message,
                        data: result,
                        success: result.Succeeded,
                        status: result.Succeeded ? "ok" : "error",
                        errorCode: result.Succeeded ? null : "capture-failed");
                }

                case AutomationCommandKind.CaptureWindowScreenshot:
                {
                    var outputPath = GetString(payload, "outputPath")
                        ?? Path.Combine(Path.GetTempPath(), $"window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
                    var result = await _windowControl.CaptureWindowScreenshotAsync(outputPath, cancellationToken).ConfigureAwait(false);
                    return CreateResponse(
                        correlationId,
                        result.Message,
                        data: result,
                        success: result.Succeeded,
                        status: result.Succeeded ? "ok" : "error",
                        errorCode: result.Succeeded ? null : "capture-failed");
                }

                case AutomationCommandKind.GetPerformanceTimeline:
                {
                    var maxEntries = GetInt(payload, "maxEntries") ?? 240;
                    var timeline = _diagnosticsHub.GetPerformanceTimeline(maxEntries);
                    return CreateResponse(correlationId, "Performance timeline retrieved.", data: timeline);
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
                        status: "error");
            }
        }
        catch (OperationCanceledException)
        {
            return CreateResponse(
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

            return CreateResponse(
                correlationId,
                message,
                errorCode: "command-failed",
                success: false,
                status: "error",
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
        string status = "ok",
        string? commandLifecycle = null,
        int? retryAfterMs = null,
        long? elapsedMs = null,
        AutomationSnapshot? snapshot = null)
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
            AutomationCommandKind.SetVideoFormat => true,
            AutomationCommandKind.SetPreset => true,
            AutomationCommandKind.SetSplitEncodeMode => true,
            AutomationCommandKind.SetMjpegDecoderCount => true,
            AutomationCommandKind.SetRecordingFormat => true,
            AutomationCommandKind.SetQuality => true,
            AutomationCommandKind.SetCustomBitrate => true,
            AutomationCommandKind.SetHdrEnabled => true,
            AutomationCommandKind.SetAudioEnabled => true,
            AutomationCommandKind.SetAudioPreviewEnabled => true,
            AutomationCommandKind.SetPreviewEnabled => true,
            AutomationCommandKind.SetRecordingEnabled => true,
            AutomationCommandKind.SetAnalogAudioGain => true,
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
            $"Invalid flashback action: '{raw}'. Expected play, pause, go-live, seek, set-in-point, set-out-point, or clear-in-out-points.");
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

    private async Task ExecuteWindowActionAsync(AutomationWindowAction action, CancellationToken cancellationToken, JsonElement payload = default)
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
                await _windowControl.CloseAsync(CancellationToken.None).ConfigureAwait(false);
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
