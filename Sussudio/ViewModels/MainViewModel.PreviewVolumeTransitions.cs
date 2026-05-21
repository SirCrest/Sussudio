using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Preview-volume persistence and ramp adapters around the transition controller.
/// </summary>
public partial class MainViewModel
{
    internal bool SuppressVolumeSave
    {
        get => _previewAudioVolumeTransitionController.SuppressVolumeSave;
        set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;
    }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current animation-transient property value. Set during preview volume
    /// fade-in/out to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride
    {
        get => _previewAudioVolumeTransitionController.VolumeSaveOverride;
        set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;
    }

    partial void OnPreviewVolumeChanged(double value)
        => _previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);

    private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)
        => await _previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken);

    private async Task RampPreviewVolumeDownForAudioTransitionAsync(
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => await _previewAudioVolumeTransitionController.RampDownForAudioTransitionAsync(
            reason,
            cancellationToken,
            traceSession);

    private double PrimePreviewVolumeForAudioTransition(string reason)
        => _previewAudioVolumeTransitionController.PrimeForAudioTransition(reason);

    private async Task RampPreviewVolumeUpForAudioTransitionAsync(
        double volumeTarget,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => await _previewAudioVolumeTransitionController.RampUpForAudioTransitionAsync(
            volumeTarget,
            reason,
            cancellationToken,
            traceSession);

    private void RestorePreviewVolumeAfterUnavailableAudio(double volumeTarget, string reason)
        => _previewAudioVolumeTransitionController.RestoreAfterUnavailableAudio(volumeTarget, reason);

    internal void SavePreviewVolume() => SaveSettings();
}
