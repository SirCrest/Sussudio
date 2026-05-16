using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation preview enable/disable mutator.
/// </summary>
public partial class MainViewModel
{
    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (!enabled && IsPreviewReinitializing)
            {
                CancelPendingPreviewRestart();
                if (!IsPreviewing)
                {
                    return;
                }
            }

            if (enabled == IsPreviewing)
            {
                return;
            }

            if (enabled)
            {
                await StartPreviewAsync(userInitiated: true, cancellationToken);
            }
            else
            {
                await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);
            }
        }, cancellationToken);
    }
}
