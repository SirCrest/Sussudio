using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private bool TryReopenCurrentFmp4BeforeSegmentSwitch(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string currentOpenFilePath,
        TimeSpan playbackPosition,
        TimeSpan lastFrameAbsPts,
        TimeSpan nextSegmentStart,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _playbackFmp4Reopens);
        Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH pos_ms={(long)playbackPosition.TotalMilliseconds} resumePts_ms={(long)lastFrameAbsPts.TotalMilliseconds} nextStart_ms={(long)nextSegmentStart.TotalMilliseconds}");
        try
        {
            ReopenDecoderPlaybackFile(
                decoder,
                currentOpenFilePath,
                ref fileOpen,
                updateCurrentOpenPath: false,
                closeOnlyWhenOpen: false);
            var preReopenLastAudioPts = SuppressAudioForFmp4Reopen(decoder);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, lastFrameAbsPts, "fmp4_reopen_before_segment_switch", cancellationToken))
            {
                RestoreAudioAfterFmp4Reopen(decoder, lastFrameAbsPts, preReopenLastAudioPts);
                pacingStopwatch.Restart();
                return true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
        }

        return false;
    }

    private bool HandleActiveFmp4ReopenAtSegmentEdge(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string currentOpenFilePath,
        TimeSpan playbackPosition,
        TimeSpan lastFrameAbsPts,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _playbackFmp4Reopens);
        Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var resumeTarget = lastFrameAbsPts;
        var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);
        if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)
            resumeTarget = currentSegmentStart.Value;
        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN pos_ms={(long)playbackPosition.TotalMilliseconds} resumePts_ms={(long)resumeTarget.TotalMilliseconds}");
        try
        {
            ReopenDecoderPlaybackFile(
                decoder,
                currentOpenFilePath,
                ref fileOpen,
                updateCurrentOpenPath: false,
                closeOnlyWhenOpen: false);
            var preReopenLastAudioPts = SuppressAudioForFmp4Reopen(decoder);
            cancellationToken.ThrowIfCancellationRequested();
            if (!SeekToWithCapTelemetry(decoder, resumeTarget, "fmp4_reopen", cancellationToken))
            {
                SetReopenFailure("fmp4_reopen", "seek_failed", resumeTarget);
                Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL path='{currentOpenFilePath}' offset_ms={(long)resumeTarget.TotalMilliseconds}");
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "fmp4_reopen_seek_failed");
                return false;
            }
            RestoreAudioAfterFmp4Reopen(decoder, resumeTarget, preReopenLastAudioPts);
            pacingStopwatch.Restart();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return false;
        }
    }

    private long SuppressAudioForFmp4Reopen(FlashbackDecoder decoder)
    {
        var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
        Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
        decoder.AudioChunkCallback = null;
        return preReopenLastAudioPts;
    }

    private void RestoreAudioAfterFmp4Reopen(
        FlashbackDecoder decoder,
        TimeSpan resumeTarget,
        long preReopenLastAudioPts)
    {
        // Gate audio at the post-seek video PTS (seek target), not at
        // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
        // using it suppresses audio if the seek lands earlier, or creates
        // a gap if it lands later, causing WASAPI underruns and A/V desync.
        var audioGateTicks = resumeTarget.Ticks;
        Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)resumeTarget.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)resumeTarget.TotalMilliseconds}");
        RestoreAudioCallback(decoder, audioGateTicks);
    }
}
