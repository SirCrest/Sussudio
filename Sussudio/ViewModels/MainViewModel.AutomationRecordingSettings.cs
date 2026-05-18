using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutators for recording, encoder, and output settings.
/// </summary>
public partial class MainViewModel
{
    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken);

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetQualityAsync(quality, cancellationToken);

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetSplitEncodeModeAsync(splitEncodeMode, cancellationToken);

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetCustomBitrateAsync(bitrateMbps, cancellationToken);

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetPresetAsync(preset, cancellationToken);

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetOutputPathAsync(outputPath, cancellationToken);
}
