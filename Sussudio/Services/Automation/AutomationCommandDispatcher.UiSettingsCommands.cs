using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
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
