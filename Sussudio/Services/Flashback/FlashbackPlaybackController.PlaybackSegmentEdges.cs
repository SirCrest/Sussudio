using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Continuous playback segment-edge handling ---

    private bool HandleEndOfSegment(
        FlashbackDecoder decoder,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        // Use absolute PTS to measure distance from live edge.
        // PlaybackPosition uses frozenValidStart (captured at scrub time) while
        // BufferedDuration uses the current ValidStartPts (moves as segments are
        // evicted). Mixing these coordinate systems causes a permanently negative
        // gap once eviction advances ValidStartPts past frozenValidStart.
        var latestAbsPts = _bufferManager.LatestPts;
        var lastFrameAbsPts = TimeSpan.FromTicks(Interlocked.Read(ref _lastVideoPtsTicks));
        // Fallback: if no frame was decoded yet, estimate from PlaybackPosition
        if (lastFrameAbsPts == TimeSpan.Zero)
            lastFrameAbsPts = SaturatingAdd(PlaybackPosition, frozenValidStart);
        var gapFromLive = SaturatingSubtract(latestAbsPts, lastFrameAbsPts).TotalMilliseconds;
        var pos = PlaybackPosition;
        var currentOpenFilePath = _currentOpenFilePath;

        if (IsActiveFmp4Segment(currentOpenFilePath) &&
            CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false))
        {
            pacingStopwatch.Restart();
            return false;
        }

        if (gapFromLive > 2000)
        {
            var nextFile = currentOpenFilePath != null
                ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
                : null;
            if (nextFile != null && !IsSamePlaybackPath(nextFile, currentOpenFilePath))
            {
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
                        return false;
                    }
                    RestoreAudioCallback(decoder, audioGate);
                    ResetPlaybackPtsCadenceBaseline();
                    pacingStopwatch.Restart();
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
                    return false;
                }
            }

            // Active fMP4 segment: the demuxer cached the file structure at open
            // time and won't see new fragments without re-opening. Close and re-open
            // the same file, then seek to where playback left off.
            // Only for fMP4; .ts handles appended data via eof_reached reset.
            if (IsActiveFmp4Segment(currentOpenFilePath) && currentOpenFilePath != null)
            {
                return HandleActiveFmp4ReopenAtSegmentEdge(
                    decoder,
                    pacingStopwatch,
                    currentOpenFilePath,
                    pos,
                    lastFrameAbsPts,
                    ref fileOpen,
                    cancellationToken);
            }
        }

        if (commandChannel.Reader.TryPeek(out _) || _disposedFlag != 0)
        {
            pacingStopwatch.Restart();
            return true;
        }

        Interlocked.Increment(ref _playbackWriteHeadWaits);
        Interlocked.Exchange(ref _lastWriteHeadWaitGapMs, Math.Max(0, (long)gapFromLive));
        Logger.Log($"FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT gapFromLive_ms={gapFromLive:F0} pos_ms={(long)pos.TotalMilliseconds} lastFrameAbsPts_ms={(long)lastFrameAbsPts.TotalMilliseconds} latestPts_ms={(long)latestAbsPts.TotalMilliseconds}");
        if (cancellationToken.WaitHandle.WaitOne(50))
        {
            return false;
        }

        pacingStopwatch.Restart();
        return true;
    }
}
