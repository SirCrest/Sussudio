using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation command entry points for device-native audio mode and gain.
/// </summary>
public partial class MainViewModel
{
    public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var normalizedMode = NormalizeDeviceAudioMode(mode);
            WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);
            var applied = await ApplyDeviceAudioModeAsync(
                "automation device audio mode",
                normalizedMode,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Device audio mode change failed ({normalizedMode}).");
            }
        }, cancellationToken);
    }

    public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var clampedGain = Math.Clamp(gainPercent, 0.0, 100.0);
            WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);
            var applied = await ApplyAnalogAudioGainAsync(
                "automation analog audio gain",
                clampedGain,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Analog audio gain change failed ({clampedGain:0}%).");
            }
        }, cancellationToken);
    }
}
