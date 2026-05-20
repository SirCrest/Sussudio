using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Continuous playback next-segment switch transaction ---

    private bool TrySwitchToNextSegment(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string? currentOpenFilePath,
        TimeSpan lastFrameAbsPts,
        TimeSpan pos,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken,
        out bool playbackContinues)
    {
        playbackContinues = false;
        var nextFile = currentOpenFilePath != null
            ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
            : null;
        if (nextFile == null || IsSamePlaybackPath(nextFile, currentOpenFilePath))
        {
            return false;
        }

        var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);
        if (currentOpenFilePath != null &&
            nextSegmentStart.HasValue &&
            nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250))
        {
            if (TryReopenCurrentFmp4BeforeSegmentSwitch(
                decoder,
                pacingStopwatch,
                currentOpenFilePath,
                pos,
                lastFrameAbsPts,
                nextSegmentStart.Value,
                ref fileOpen,
                cancellationToken))
            {
                playbackContinues = true;
                return true;
            }
        }

        Interlocked.Increment(ref _playbackSegmentSwitches);
        Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} next='{System.IO.Path.GetFileName(nextFile)}'");
        try
        {
            decoder.CloseFile();
            fileOpen = false;
            decoder.OpenFile(nextFile);
            fileOpen = true;
            _currentOpenFilePath = nextFile;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            // Gate audio at last played position, not seek target - audio between
            // the last played sample and the seek point would otherwise be dropped,
            // causing an audible gap at segment boundaries.
            var audioGate = Interlocked.Read(ref _lastAudioPtsTicks);
            decoder.AudioChunkCallback = null;
            var segSwitchTarget = SaturatingAdd(pos, frozenValidStart);
            if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)
                segSwitchTarget = nextSegmentStart.Value;
            cancellationToken.ThrowIfCancellationRequested();
            if (!SeekToWithCapTelemetry(decoder, segSwitchTarget, "segment_switch", cancellationToken))
            {
                SetReopenFailure("segment_switch", "seek_failed", segSwitchTarget);
                Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL path='{nextFile}' offset_ms={(long)segSwitchTarget.TotalMilliseconds}");
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "segment_switch_seek_failed");
                return true;
            }
            RestoreAudioCallback(decoder, audioGate);
            ResetPlaybackPtsCadenceBaseline();
            pacingStopwatch.Restart();
            playbackContinues = true;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'");
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return true;
        }
    }
}
