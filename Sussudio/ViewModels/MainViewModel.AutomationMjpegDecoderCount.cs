using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for MJPEG decoder worker count selection.
/// </summary>
public partial class MainViewModel
{
    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("mjpeg decoder count", () =>
        {
            MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
        }, cancellationToken);
    }
}
