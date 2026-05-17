using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for NVENC split-encode mode selection.
/// </summary>
public partial class MainViewModel
{
    public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableSplitEncodeModes.FirstOrDefault(value =>
                string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedSplitEncodeMode = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset, SplitEncodeMode: SelectedSplitEncodeMode);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                splitEncodeMode: settings.SplitEncodeMode,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
