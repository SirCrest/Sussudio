using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Recording finalization router: choose the active recording backend and delegate
// backend-specific stop/dispose work to the focused finalization partials.
public partial class CaptureService
{
    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        if (IsFlashbackRecordingBackendActive())
        {
            return await StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken).ConfigureAwait(false);
        }

        return await StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken).ConfigureAwait(false);
    }
}
