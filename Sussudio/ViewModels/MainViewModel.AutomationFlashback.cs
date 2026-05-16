using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation Flashback enablement and restart mutators.
/// </summary>
public partial class MainViewModel
{
    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken);

    public async Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken).ConfigureAwait(false);
        await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false);
        await InvokeOnUiThreadAsync(
            () =>
            {
                _flashbackBitrateSamples.Clear();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }
}
