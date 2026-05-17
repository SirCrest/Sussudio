using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
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
}
