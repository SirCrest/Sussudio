using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for capture video format selection.
/// </summary>
public partial class MainViewModel
{
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
}
