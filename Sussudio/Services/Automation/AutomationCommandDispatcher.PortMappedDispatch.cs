using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
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
}
