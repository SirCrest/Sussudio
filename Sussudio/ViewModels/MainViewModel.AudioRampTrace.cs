using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Main view-model adapter for preview-volume audio ramp diagnostics.
/// </summary>
public partial class MainViewModel
{
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
