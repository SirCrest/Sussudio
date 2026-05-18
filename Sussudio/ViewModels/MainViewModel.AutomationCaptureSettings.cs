using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Public automation facade for capture settings that may reinitialize an active preview.
/// </summary>
public partial class MainViewModel
{
    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);
}
