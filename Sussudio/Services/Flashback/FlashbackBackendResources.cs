using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackProducerDetachRequest(
    UnifiedVideoCapture? VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    string WarningToken,
    bool DetachMicrophoneWriter);

/// <summary>
/// Authoritative ownership record for the preview-owned Flashback backend.
/// CaptureService remains the transition coordinator; this aggregate keeps the
/// sink, buffer, exporter, playback controller, and settings snapshot together.
/// </summary>
internal sealed class FlashbackBackendResources
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

    public void ClearRecoveryPreserve()
    {
        PreserveSegmentsAfterFailedRecordingFinalize = false;
    }

    public bool ResolveSegmentPurge(bool requested, string reason)
    {
        if (!requested)
        {
            return false;
        }

        if (!PreserveSegmentsAfterFailedRecordingFinalize)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_SEGMENT_PURGE_BLOCKED reason={reason}");
        return false;
    }

    public void PreserveRecoverySegments(string reason)
    {
        PreserveSegmentsAfterFailedRecordingFinalize = true;
        Logger.Log($"FLASHBACK_RECOVERY_PRESERVE reason={reason}");
        BufferManager?.MarkSessionPreservedForRecovery();
    }

    public async Task<FinalizeResult> FinalizeRecordingAsync(
        string outputPath,
        Action<FlashbackEncoderSink>? captureBoundarySnapshot,
        Func<TimeSpan, TimeSpan, string, CancellationToken, Task<FinalizeResult>> exportRecordingAsync,
        Action<FlashbackBufferManager?, string> resumeEvictionBestEffort,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exportRecordingAsync);
        ArgumentNullException.ThrowIfNull(resumeEvictionBestEffort);

        var flashbackSink = Sink
            ?? throw new InvalidOperationException("Flashback recording backend is not active.");
        var bufferManager = BufferManager;
        var outerPauseApplied = false;
        try
        {
            bufferManager?.PauseEviction();
            outerPauseApplied = bufferManager != null;

            var endResult = await flashbackSink.EndRecordingAsync(cancellationToken).ConfigureAwait(false);
            captureBoundarySnapshot?.Invoke(flashbackSink);
            if (!endResult.Succeeded)
            {
                return endResult;
            }

            var startPts = flashbackSink.LastRecordingStartPts;
            var endPts = flashbackSink.LastRecordingEndPts;
            var exportResult = await exportRecordingAsync(startPts, endPts, outputPath, cancellationToken)
                .ConfigureAwait(false);

            exportResult = PreserveEndArtifactsOnFailure(exportResult, endResult);
            if (exportResult.Succeeded)
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_OK output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_FAIL output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }

            return exportResult;
        }
        finally
        {
            if (outerPauseApplied)
            {
                resumeEvictionBestEffort(bufferManager, "flashback_recording_finalize");
            }
        }
    }

    private static FinalizeResult PreserveEndArtifactsOnFailure(
        FinalizeResult exportResult,
        FinalizeResult endResult)
    {
        if (exportResult.Succeeded || endResult.PreservedArtifacts.Count == 0)
        {
            return exportResult;
        }

        return FinalizeResult.Failure(
            exportResult.OutputPath,
            exportResult.StatusMessage,
            exportResult.PreservedArtifacts.Concat(endResult.PreservedArtifacts));
    }

    public FlashbackPlaybackController? TakePlaybackController()
    {
        var playbackController = PlaybackController;
        PlaybackController = null;
        return playbackController;
    }

    public void DetachProducers(FlashbackProducerDetachRequest request)
    {
        if (request.DetachMicrophoneWriter)
        {
            try { request.MicrophoneCapture?.SetAudioWriter(null); }
            catch (Exception ex) { Logger.Log($"{request.WarningToken} target=microphone type={ex.GetType().Name} msg={ex.Message}"); }
        }

        try { request.AudioCapture?.DetachFlashbackSink(); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=audio type={ex.GetType().Name} msg={ex.Message}"); }

        try { request.VideoCapture?.SetFlashbackSink(null); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=video type={ex.GetType().Name} msg={ex.Message}"); }
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
