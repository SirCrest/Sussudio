using Sussudio.Models;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Authoritative ownership record for the preview-owned Flashback backend.
/// CaptureService remains the transition coordinator; this aggregate keeps the
/// sink, buffer, exporter, playback controller, and settings snapshot together.
/// </summary>
internal sealed partial class FlashbackBackendResources
{
    public FlashbackBufferManager? BufferManager { get; set; }

    public FlashbackEncoderSink? Sink { get; set; }

    public FlashbackExporter? Exporter { get; set; }

    public FlashbackPlaybackController? PlaybackController { get; set; }

    public CaptureSettings? SettingsSnapshot { get; set; }

    public bool PreserveSegmentsAfterFailedRecordingFinalize { get; private set; }

    public bool HasAnyResource =>
        BufferManager != null ||
        Sink != null ||
        Exporter != null ||
        PlaybackController != null;

    public void Install(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink sink,
        FlashbackExporter exporter,
        FlashbackPlaybackController? playbackController,
        CaptureSettings? settingsSnapshot)
    {
        BufferManager = bufferManager;
        Sink = sink;
        Exporter = exporter;
        PlaybackController = playbackController;
        SettingsSnapshot = settingsSnapshot;
    }

    public FlashbackPlaybackController? TakePlaybackController()
    {
        var playbackController = PlaybackController;
        PlaybackController = null;
        return playbackController;
    }

    public void ClearSinkAndSettings()
    {
        Sink = null;
        SettingsSnapshot = null;
    }

    public void Clear()
    {
        BufferManager = null;
        Sink = null;
        Exporter = null;
        PlaybackController = null;
        SettingsSnapshot = null;
    }
}
