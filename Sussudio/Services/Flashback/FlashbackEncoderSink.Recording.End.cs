using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)
    {
        var wasRecording = Interlocked.Exchange(ref _recordingActive, 0) != 0;
        if (!wasRecording)
        {
            const string message = "Flashback recording was not active.";
            Logger.Log($"FLASHBACK_RECORDING_END_REJECTED reason='{message}'");
            return new FinalizeResult
            {
                Succeeded = false,
                OutputPath = _recordingOutputPath ?? string.Empty,
                StatusMessage = message,
                PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Give encoding loop time to drain remaining queued frames.
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Check if the encoding loop crashed during the recording
            var failure = _encodingFailure;
            if (failure != null)
            {
                Logger.Log($"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'");
                return new FinalizeResult
                {
                    Succeeded = false,
                    OutputPath = _recordingOutputPath ?? string.Empty,
                    StatusMessage = $"Flashback recording failed: {failure.Message}",
                    PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
                };
            }

            // Capture end PTS BEFORE resuming eviction. When an outer pause is held
            // (FinalizeFlashbackRecordingAsync), ResumeEviction won't reach count=0 and
            // therefore won't snapshot the end PTS. Even if count does reach 0, the
            // stored _recordingEndPts may be stale from a previous recording. Always
            // use the live LatestPts as the authoritative recording end time.
            var endPts = _bufferManager.LatestPts;
            LastRecordingEndPts = endPts;

            return new FinalizeResult
            {
                Succeeded = true,
                OutputPath = _recordingOutputPath ?? string.Empty,
                StatusMessage = "Flashback recording ready (single .ts file)",
                PreservedArtifacts = _tsFilePath != null ? new[] { _tsFilePath } : Array.Empty<string>()
            };
        }
        finally
        {
            if (wasRecording)
            {
                var (startPts, _) = ResumeEvictionBestEffort(_bufferManager, "recording_end");
                LastRecordingStartPts = startPts;
                if (LastRecordingEndPts < LastRecordingStartPts)
                {
                    LastRecordingEndPts = _bufferManager.LatestPts;
                    if (LastRecordingEndPts < LastRecordingStartPts)
                    {
                        LastRecordingEndPts = LastRecordingStartPts;
                    }
                }

                Logger.Log(
                    $"FLASHBACK_RECORDING_READY output='{_recordingOutputPath}' " +
                    $"start_pts_ms={(long)LastRecordingStartPts.TotalMilliseconds} " +
                    $"end_pts_ms={(long)LastRecordingEndPts.TotalMilliseconds} " +
                    $"duration_s={NonNegativeDuration(LastRecordingEndPts, LastRecordingStartPts).TotalSeconds:F1}");
            }
        }
    }
}
