using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns automation-driven capture setting mutations and active-preview reinitialization.
    /// </summary>
    private sealed class MainViewModelCaptureSettingsAutomationController
    {
        private readonly MainViewModel _viewModel;
        private readonly SemaphoreSlim _captureModeGate = new(1, 1);

        public MainViewModelCaptureSettingsAutomationController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
        {
            return SetAutomationCaptureModeAsync("resolution", () =>
            {
                var matched = _viewModel.AvailableResolutions.FirstOrDefault(r =>
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

                _viewModel.SelectedResolution = matched.Value;
            }, cancellationToken);
        }

        public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
        {
            return SetAutomationCaptureModeAsync("frame rate", () =>
            {
                if (_viewModel.AvailableFrameRates.Count == 0)
                {
                    throw new InvalidOperationException("No frame rates are available.");
                }

                var enabledRates = _viewModel.AvailableFrameRates
                    .Where(rate => rate.IsEnabled)
                    .ToList();
                if (enabledRates.Count == 0)
                {
                    throw new InvalidOperationException("No enabled frame rates are available for the current selection.");
                }

                if (FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate))
                {
                    var autoRate = enabledRates.FirstOrDefault(rate => FrameRateTimingPolicy.IsAutoFrameRateValue(rate.Value));
                    if (autoRate == null)
                    {
                        throw new InvalidOperationException("Auto frame rate is not available for the current selection.");
                    }

                    _viewModel.SelectAutoFrameRate();
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

                if (friendlyMatches.Count == 0 && !FrameRateTimingPolicy.IsFrameRateMatch(matched.Value, frameRate))
                {
                    throw new InvalidOperationException(
                        $"Frame rate '{frameRate:0.###}' is not available for {_viewModel.SelectedResolution ?? "the current resolution"}.");
                }

                _viewModel.SelectedFrameRate = matched.Value;
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

                var match = _viewModel.AvailableVideoFormats.FirstOrDefault(
                    format => string.Equals(format, videoFormat, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    throw new InvalidOperationException($"Video format '{videoFormat}' is not available.");
                }

                _viewModel.SelectedVideoFormat = match;
            }, cancellationToken);
        }

        public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
        {
            return SetAutomationCaptureModeAsync("mjpeg decoder count", () =>
            {
                _viewModel.MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
            }, cancellationToken);
        }

        private async Task SetAutomationCaptureModeAsync(
            string reason,
            Action apply,
            CancellationToken cancellationToken)
        {
            await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var shouldReinitialize = await _viewModel.InvokeOnUiThreadAsync(() =>
                {
                    var wasPreviewing = _viewModel.IsPreviewing && _viewModel.IsInitialized && _viewModel.SelectedDevice != null;
                    _viewModel._suppressFormatChangeReinitialize = true;
                    try
                    {
                        apply();
                    }
                    finally
                    {
                        _viewModel._suppressFormatChangeReinitialize = false;
                    }

                    return wasPreviewing && _viewModel.SelectedFormat != null;
                }, cancellationToken).ConfigureAwait(false);

                if (shouldReinitialize)
                {
                    await _viewModel.InvokeOnUiThreadAsync(
                            () => _viewModel.ReinitializeDeviceAsync($"automation {reason}"),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _captureModeGate.Release();
            }
        }
    }
}
