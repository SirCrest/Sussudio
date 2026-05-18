using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Main view-model adapter for preview-volume audio ramp diagnostics.
/// </summary>
public partial class MainViewModel
{
    private AudioRampTraceRecorder CreateAudioRampTraceRecorder()
    {
        return new AudioRampTraceRecorder(
            new AudioRampTraceRecorderContext
            {
                GetRuntimeSnapshot = () => _captureService.GetRuntimeSnapshot(),
                GetPreviewVolume = () => PreviewVolume,
                GetIsAudioEnabled = () => IsAudioEnabled,
                GetIsAudioPreviewEnabled = () => IsAudioPreviewEnabled,
                GetAudioPeak = () => AudioPeak,
                Log = message => Logger.Log(message),
            });
    }

    private PreviewAudioVolumeTransitionController CreatePreviewAudioVolumeTransitionController()
    {
        return new PreviewAudioVolumeTransitionController(
            new PreviewAudioVolumeTransitionControllerContext
            {
                GetPreviewVolume = () => PreviewVolume,
                SetPreviewVolume = value => PreviewVolume = value,
                SetSessionPreviewVolume = volume => _sessionCoordinator.SetPreviewVolume(volume),
                BeginTraceSession = BeginAudioRampTraceSession,
                CompleteTraceSession = CompleteAudioRampTraceSession,
                RecordTracePoint = RecordAudioRampTracePoint,
                Log = (message, caller) => Logger.Log(message, caller),
            });
    }

    public AudioRampTraceSnapshot GetAudioRampTraceSnapshot(int maxEntries = 512)
        => _audioRampTraceRecorder.GetSnapshot(maxEntries);

    public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync(
        int maxEntries = 512,
        CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(() => GetAudioRampTraceSnapshot(maxEntries), cancellationToken);

    private long BeginAudioRampTraceSession(string reason, double targetVolume)
        => _audioRampTraceRecorder.BeginSession(reason, targetVolume);

    private void CompleteAudioRampTraceSession(long sessionId, string reason)
        => _audioRampTraceRecorder.CompleteSession(sessionId, reason);

    private void RecordAudioRampTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
        => _audioRampTraceRecorder.RecordPoint(kind, reason, targetVolume, note, sessionId);
}
