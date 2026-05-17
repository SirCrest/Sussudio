using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Shared automation gate for capture-mode changes that may reinitialize an active preview.
/// </summary>
public partial class MainViewModel
{
    private async Task SetAutomationCaptureModeAsync(
        string reason,
        Action apply,
        CancellationToken cancellationToken)
    {
        await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var shouldReinitialize = await InvokeOnUiThreadAsync(() =>
            {
                var wasPreviewing = IsPreviewing && IsInitialized && SelectedDevice != null;
                _suppressFormatChangeReinitialize = true;
                try
                {
                    apply();
                }
                finally
                {
                    _suppressFormatChangeReinitialize = false;
                }

                return wasPreviewing && SelectedFormat != null;
            }, cancellationToken).ConfigureAwait(false);

            if (shouldReinitialize)
            {
                await InvokeOnUiThreadAsync(
                        () => ReinitializeDeviceAsync($"automation {reason}"),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _automationCaptureModeGate.Release();
        }
    }
}
