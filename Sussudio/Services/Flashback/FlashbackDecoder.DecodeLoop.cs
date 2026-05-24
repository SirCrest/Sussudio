using System.Diagnostics;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;

    public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;

    public readonly record struct PlaybackDecodePhaseTimings(
        double ReceiveMs,
        double FeedMs,
        double ReadMs,
        double SendMs,
        double AudioMs,
        double ConvertMs);

    /// <summary>
    /// Decodes the next video frame.
    /// For D3D11VA: returns a <see cref="DecodedVideoFrame"/> with <see cref="DecodedVideoFrame.IsD3D11Texture"/> = true.
    /// For software: returns raw NV12/P010 data in <see cref="DecodedVideoFrame.Data"/>.
    /// </summary>
    public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)
    {
        frame = default;
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();
        _lastDecodePhaseTimings = default;

        // Return stashed frame from SeekTo() before decoding new ones.
        if (_hasPendingVideoFrame)
        {
            frame = _pendingVideoFrame;
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
            return true;
        }

        using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // First try to receive a frame from the decoder (may have buffered frames).
            var receiveStart = Stopwatch.GetTimestamp();
            var receiveResult = ffmpeg.avcodec_receive_frame(_videoCodecCtx, _videoFrame);
            AddLastDecodeReceiveMs(ElapsedMsSince(receiveStart));
            if (receiveResult == 0)
            {
                // Got a decoded frame: convert and return.
                var convertStart = Stopwatch.GetTimestamp();
                frame = ConvertAndOutputVideoFrame();
                AddLastDecodeConvertMs(ElapsedMsSince(convertStart));
                if (frame.Width <= 0)
                    return false; // clone failed, treat as decode failure
                return true;
            }

            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                // Decoder needs more packets.
                var feedStart = Stopwatch.GetTimestamp();
                if (!FeedNextVideoPacket(cancellationToken))
                {
                    // Temporary EOF on live fMP4: do not enter drain mode.
                    // The encoder is still appending; drain mode is permanent and
                    // would prevent decoding any future frames.
                    AddLastDecodeFeedMs(ElapsedMsSince(feedStart));
                    return false;
                }

                AddLastDecodeFeedMs(ElapsedMsSince(feedStart));
                continue;
            }

            if (receiveResult == ffmpeg.AVERROR_EOF)
            {
                // Decoder was previously drained; reset so it can accept new packets.
                ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
                return false;
            }

            // Unexpected error.
            Logger.Log($"FLASHBACK_DECODER_VIDEO_ERROR receive_frame code={receiveResult}");
            return false;
        }
    }

    /// <summary>
    /// Reads packets until a video packet is sent to the decoder.
    /// Audio packets are decoded inline via AudioChunkCallback.
    /// </summary>
    private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ffmpeg.av_packet_unref(_packet);
            var readStart = Stopwatch.GetTimestamp();
            var readResult = ffmpeg.av_read_frame(_formatCtx, _packet);
            AddLastDecodeReadMs(ElapsedMsSince(readStart));
            if (readResult < 0)
            {
                // Clear AVIO EOF flag so subsequent reads can see newly appended data.
                // Without this, C stdio's fread EOF is cached and av_read_frame keeps
                // returning EOF even after the encoder writes more to the file.
                if (_formatCtx->pb != null)
                    _formatCtx->pb->eof_reached = 0;
                return false;
            }

            if (_packet->stream_index == _videoStreamIndex)
            {
                var sendStart = Stopwatch.GetTimestamp();
                var sendResult = ffmpeg.avcodec_send_packet(_videoCodecCtx, _packet);
                AddLastDecodeSendMs(ElapsedMsSince(sendStart));
                ffmpeg.av_packet_unref(_packet);
                if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN send_packet code={sendResult}");
                    continue;
                }

                return true;
            }

            // Decode audio inline; keeps A/V naturally interleaved.
            try
            {
                if (_packet->stream_index == _audioStreamIndex && _audioCodecCtx != null)
                {
                    var audioStart = Stopwatch.GetTimestamp();
                    DecodeAndDeliverAudioPacket(_packet);
                    AddLastDecodeAudioMs(ElapsedMsSince(audioStart));
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void AddLastDecodeReceiveMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReceiveMs = _lastDecodePhaseTimings.ReceiveMs + elapsedMs };

    private void AddLastDecodeFeedMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { FeedMs = _lastDecodePhaseTimings.FeedMs + elapsedMs };

    private void AddLastDecodeReadMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReadMs = _lastDecodePhaseTimings.ReadMs + elapsedMs };

    private void AddLastDecodeSendMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { SendMs = _lastDecodePhaseTimings.SendMs + elapsedMs };

    private void AddLastDecodeAudioMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { AudioMs = _lastDecodePhaseTimings.AudioMs + elapsedMs };

    private void AddLastDecodeConvertMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ConvertMs = _lastDecodePhaseTimings.ConvertMs + elapsedMs };

    private static double ElapsedMsSince(long startTimestamp)
        => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
}
