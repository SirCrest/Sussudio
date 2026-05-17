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

            case AutomationCommandKind.SetStatsSectionVisible:
                return await ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken).ConfigureAwait(false);

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

    private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var section = RequireString(payload, "section");
        var visible = RequireBool(payload, "visible");
        await _viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Stats section '{section}' {(visible ? "expanded" : "collapsed")}.");
    }
}
