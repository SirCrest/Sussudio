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
    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler> UiSettingsHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler>
        {
            [AutomationCommandKind.SetShowAllCaptureOptions] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetShowAllCaptureOptionsAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetPreviewVolume] = AutomationCommandHandler.Double(
                (vm, v, ct) => vm.SetPreviewVolumeAsync(v, ct), "previewVolumePercent"),
            [AutomationCommandKind.SetStatsVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetStatsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetSettingsVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetSettingsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFrameTimeOverlayVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetFrameTimeOverlayVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFlashbackTimelineVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetFlashbackTimelineVisibleAsync(v, ct), "visible"),
        };

    private async Task<AutomationCommandResponse?> TryExecuteUiSettingsCommandAsync(
        AutomationCommandKind command,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (UiSettingsHandlers.TryGetValue(command, out var handler))
        {
            await handler.InvokeAsync(_viewModel, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, handler.AcknowledgeMessage(command, payload));
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
        await _viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, $"Stats section '{section}' {(visible ? "expanded" : "collapsed")}.");
    }
}
