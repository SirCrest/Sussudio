using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

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
}
