using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutators that change capture mode selection and reinitialize preview
/// when an active session needs to renegotiate.
/// </summary>
public partial class MainViewModel
{
    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("resolution", () =>
        {
            var matched = AvailableResolutions.FirstOrDefault(r =>
                string.Equals(r.Value, resolution, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Resolution '{resolution}' is not available.");
            }
            if (!matched.IsEnabled)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(matched.DisableReason)
                        ? $"Resolution '{resolution}' is currently disabled."
                        : matched.DisableReason);
            }

            SelectedResolution = matched.Value;
        }, cancellationToken);
    }

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("frame rate", () =>
        {
            if (AvailableFrameRates.Count == 0)
            {
                throw new InvalidOperationException("No frame rates are available.");
            }

            var enabledRates = AvailableFrameRates
                .Where(rate => rate.IsEnabled)
                .ToList();
            if (enabledRates.Count == 0)
            {
                throw new InvalidOperationException("No enabled frame rates are available for the current selection.");
            }

            if (IsAutoFrameRateValue(frameRate))
            {
                var autoRate = enabledRates.FirstOrDefault(rate => IsAutoFrameRateValue(rate.Value));
                if (autoRate == null)
                {
                    throw new InvalidOperationException("Auto frame rate is not available for the current selection.");
                }

                SelectAutoFrameRate();
                return;
            }

            var requestedFriendly = Math.Round(frameRate);
            var friendlyMatches = enabledRates
                .Where(rate => Math.Round(rate.FriendlyValue) == requestedFriendly)
                .OrderBy(rate => Math.Abs(rate.FriendlyValue - frameRate))
                .ThenBy(rate => Math.Abs(rate.Value - frameRate))
                .ToList();

            var matched = (friendlyMatches.Count > 0 ? friendlyMatches : enabledRates)
                .OrderBy(rate => Math.Abs(rate.Value - frameRate))
                .First();

            if (friendlyMatches.Count == 0 && !IsFrameRateMatch(matched.Value, frameRate))
            {
                throw new InvalidOperationException(
                    $"Frame rate '{frameRate:0.###}' is not available for {SelectedResolution ?? "the current resolution"}.");
            }

            SelectedFrameRate = matched.Value;
        }, cancellationToken);
    }

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("video format", () =>
        {
            if (string.IsNullOrWhiteSpace(videoFormat))
            {
                throw new ArgumentException("Video format is required.", nameof(videoFormat));
            }

            var match = AvailableVideoFormats.FirstOrDefault(
                format => string.Equals(format, videoFormat, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new InvalidOperationException($"Video format '{videoFormat}' is not available.");
            }

            SelectedVideoFormat = match;
        }, cancellationToken);
    }

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("mjpeg decoder count", () =>
        {
            MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
        }, cancellationToken);
    }

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
