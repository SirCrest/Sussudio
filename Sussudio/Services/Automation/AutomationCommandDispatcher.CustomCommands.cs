using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
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
                return await ExecuteFlashbackActionCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.FlashbackExport:
                return await ExecuteFlashbackExportCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.FlashbackGetSegments:
                return await ExecuteFlashbackGetSegmentsCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

            case AutomationCommandKind.VerifyFile:
                return await ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

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
                return await ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);

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
