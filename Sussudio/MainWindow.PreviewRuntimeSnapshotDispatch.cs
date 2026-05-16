using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio;

// Automation preview snapshot adapter; dispatch/retry policy lives in WindowUiDispatchController.
public sealed partial class MainWindow
{
    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => await WindowUiDispatchController.InvokeWithRetryAsync(
            GetPreviewRuntimeSnapshot,
            "Failed to enqueue preview snapshot operation.",
            cancellationToken).ConfigureAwait(false);
}
