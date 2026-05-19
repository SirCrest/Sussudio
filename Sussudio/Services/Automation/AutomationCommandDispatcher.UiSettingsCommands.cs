using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    // UI/settings automation commands live together because callers treat them
    // as ready-independent app-state toggles, even when their payload shapes vary.
    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> UiCaptureSettingsHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>>
        {
            [AutomationCommandKind.SetShowAllCaptureOptions] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Bool(
                (vm, v, ct) => vm.SetShowAllCaptureOptionsAsync(v, ct), "enabled"),
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

    private async Task<AutomationCommandResponse?> TryExecuteUiSettingsCommandAsync(
        AutomationCommandKind command,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (UiCaptureSettingsHandlers.TryGetValue(command, out var captureSettingsHandler))
        {
            await captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, captureSettingsHandler.AcknowledgeMessage(command, payload));
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
