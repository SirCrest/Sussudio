using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for custom encoder bitrate selection.
/// </summary>
public partial class MainViewModel
{
    public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                CustomBitrateMbps = Math.Clamp(bitrateMbps, 1, 300);
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
