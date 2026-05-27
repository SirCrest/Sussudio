using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

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
    /// Seeks to the nearest keyframe at or before <paramref name="target"/>.
    /// Fast seek suitable for scrubbing.
    /// </summary>
    public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();

        var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);
        var result = ffmpeg.av_seek_frame(
            _formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
        cancellationToken.ThrowIfCancellationRequested();

        if (result < 0)
        {
            var streamSeekResult = result;
            var timestampUs = ToAvTimeBaseTimestamp(target);
            result = ffmpeg.av_seek_frame(
                _formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);
            cancellationToken.ThrowIfCancellationRequested();

            if (result < 0)
            {
                Logger.Log(
                    $"FLASHBACK_DECODER_SEEK_WARN keyframe_seek_failed code={result} " +
                    $"stream_code={streamSeekResult} target_ms={(long)target.TotalMilliseconds} stream_ts={streamTimestamp}");
                return false;
            }

            Logger.Log(
                $"FLASHBACK_DECODER_SEEK_FALLBACK_OK target_ms={(long)target.TotalMilliseconds} " +
                $"stream_ts={streamTimestamp} us_ts={timestampUs}");
        }

        if (_videoCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
        }

        if (_audioCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_audioCodecCtx);
        }

        // Clear any stashed pending frame - it's from before the seek point.
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "seek_keyframe_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        _suppressRecoverableSeekLogsForNextVideoFrame = true;

        Logger.Log(
            $"FLASHBACK_DECODER_SEEK_OK target_ms={(long)target.TotalMilliseconds} " +
            $"stream_index={_videoStreamIndex} stream_ts={streamTimestamp}");
        return true;
    }

    /// <summary>
    /// Seeks to the exact frame at <paramref name="target"/> by first seeking to the
    /// nearest preceding keyframe, then decoding forward until the target PTS is reached.
    /// </summary>
    public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();
        _lastSeekHitForwardDecodeCap = false;

        if (!SeekToKeyframe(target, cancellationToken))
        {
            return false;
        }

        // Decode forward until we reach (or pass) the target PTS.
        // Stash the target frame so the next TryDecodeNextVideoFrame() returns it
        // instead of skipping past it (fixes off-by-one on seek).
        // Cap at 960 frames (8s at 120fps) to prevent CPU saturation on scrub.
        const int maxForwardFrames = 960;
        var targetTicks = target.Ticks;
        DecodedVideoFrame? bestFrame = null;
        var bestFrameTransferred = false;
        try
        {
            for (var i = 0; i < maxForwardFrames; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))
                {
                    // Reached EOF before target - return best frame if we have one.
                    if (bestFrame != null)
                    {
                        _currentPosition = bestFrame.Value.Pts;
                        _pendingVideoFrame = bestFrame.Value;
                        _hasPendingVideoFrame = true;
                        bestFrameTransferred = true;
                        return true;
                    }

                    return false;
                }

                if (frame.Pts.Ticks >= targetTicks)
                {
                    _currentPosition = frame.Pts;
                    _pendingVideoFrame = frame;
                    _hasPendingVideoFrame = true;
                    if (bestFrame != null)
                    {
                        ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_replace_best");
                        bestFrame = null;
                    }

                    return true;
                }

                // Keep the closest frame in case we hit the limit.
                if (bestFrame != null) ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_superseded");
                bestFrame = frame;
            }

            // Hit frame limit - return the closest frame we decoded.
            if (bestFrame != null)
            {
                var bestMs = (long)bestFrame.Value.Pts.TotalMilliseconds;
                var targetMs = (long)target.TotalMilliseconds;
                var gapMs = targetMs - bestMs;
                // One frame interval in ms (guard against zero/negative frame rate)
                var frameIntervalMs = _frameRate > 0.0 ? (long)(1000.0 / _frameRate) : 0L;
                if (gapMs > frameIntervalMs)
                {
                    _lastSeekHitForwardDecodeCap = true;
                    Interlocked.Increment(ref _seekToCapHits);
                    Logger.Log($"FLASHBACK_DECODER_SEEK_CAP_HIT target_ms={targetMs} best_ms={bestMs} gap_ms={gapMs} frames_decoded={maxForwardFrames}");
                }
                else
                {
                    Logger.Log($"FLASHBACK_DECODER_SEEK_FRAME_LIMIT target_ms={targetMs} best_ms={bestMs} frames={maxForwardFrames}");
                }

                _currentPosition = bestFrame.Value.Pts;
                _pendingVideoFrame = bestFrame.Value;
                _hasPendingVideoFrame = true;
                bestFrameTransferred = true;
                return true;
            }

            return false;
        }
        finally
        {
            if (!bestFrameTransferred && bestFrame != null)
            {
                ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_abandoned");
            }
        }
    }

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

    private void InitializeAudioDecoder()
    {
        var audioStream = _formatCtx->streams[_audioStreamIndex];
        _audioTimeBase = audioStream->time_base;

        var codecPar = audioStream->codecpar;

        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN no decoder for codec_id={codecPar->codec_id}, audio disabled");
            _audioStreamIndex = -1;
            return;
        }

        _audioCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audioCodecCtx == null)
        {
            throw CreateException("Failed to allocate audio codec context.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_to_context(_audioCodecCtx, codecPar),
            "avcodec_parameters_to_context(audio)");

        ThrowIfError(
            ffmpeg.avcodec_open2(_audioCodecCtx, codec, null),
            "avcodec_open2(audio)");

        _audioFrame = ffmpeg.av_frame_alloc();
        if (_audioFrame == null)
        {
            throw CreateException("Failed to allocate audio frame.");
        }

        InitializeAudioResampler();

        var audioCodecName = codec->name != null ? Marshal.PtrToStringAnsi((IntPtr)codec->name) : "?";
        Logger.Log($"FLASHBACK_DECODER_AUDIO codec={audioCodecName} " +
                   $"sample_rate={_audioCodecCtx->sample_rate} sample_fmt={_audioCodecCtx->sample_fmt} " +
                   $"channels={_audioCodecCtx->ch_layout.nb_channels}");
    }

    private void InitializeAudioResampler()
    {
        AVChannelLayout outputLayout = default;
        ffmpeg.av_channel_layout_default(&outputLayout, OutputAudioChannels);

        var swrCtx = _swrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &outputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                OutputAudioSampleRate,
                &_audioCodecCtx->ch_layout,
                _audioCodecCtx->sample_fmt,
                _audioCodecCtx->sample_rate,
                0,
                null);
            _swrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(decode)");

            if (_swrCtx == null)
            {
                throw CreateException("Failed to allocate audio resampler.");
            }

            ThrowIfError(ffmpeg.swr_init(_swrCtx), "swr_init(decode)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&outputLayout);
        }
    }

    /// <summary>
    /// Sends an audio packet to the decoder and delivers any resulting chunks
    /// via <see cref="AudioChunkCallback"/>. If no callback is set, audio is
    /// silently decoded so audio and video codec state advance together.
    /// </summary>
    private void DecodeAndDeliverAudioPacket(AVPacket* packet)
    {
        var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, packet);
        if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            return;
        }

        while (ffmpeg.avcodec_receive_frame(_audioCodecCtx, _audioFrame) == 0)
        {
            var callback = AudioChunkCallback;
            if (callback == null)
            {
                ffmpeg.av_frame_unref(_audioFrame);
                continue; // Codec advanced, but no delivery during seek/scrub
            }

            var chunk = ConvertAndOutputAudioFrame();
            if (chunk.ValidLength > 0)
            {
                try
                {
                    callback(chunk);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_DECODE_AUDIO_CALLBACK_FAIL type={ex.GetType().Name} msg={ex.Message}");
                    if (chunk.Samples != null)
                    {
                        ArrayPool<byte>.Shared.Return(chunk.Samples);
                    }
                }
            }
            else if (chunk.Samples != null && chunk.Samples.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(chunk.Samples);
            }
        }
    }

    private DecodedAudioChunk ConvertAndOutputAudioFrame()
    {
        var inputSamples = _audioFrame->nb_samples;
        var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);
        byte[]? result = null;
        var returnResultToPool = true;

        try
        {
            if (inputSamples <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            var maxOutputSamples = ffmpeg.swr_get_out_samples(_swrCtx, inputSamples);
            if (maxOutputSamples < 0)
            {
                maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);
            }

            if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size input_samples={inputSamples} max_output_samples={maxOutputSamples}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            result = ArrayPool<byte>.Shared.Rent(outputBytesNeeded);

            int outputSamplesProduced;
            fixed (byte* outputPtr = result)
            {
                var outputPlanes = stackalloc byte*[1];
                outputPlanes[0] = outputPtr;

                outputSamplesProduced = ffmpeg.swr_convert(
                    _swrCtx,
                    outputPlanes, maxOutputSamples,
                    _audioFrame->extended_data, inputSamples);
            }

            if (outputSamplesProduced <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size output_samples={outputSamplesProduced} buffer_bytes={result.Length}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            returnResultToPool = false;
            return new DecodedAudioChunk
            {
                Samples = result,
                ValidLength = validBytes,
                Pts = pts
            };
        }
        finally
        {
            ffmpeg.av_frame_unref(_audioFrame);
            if (returnResultToPool && result is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(result);
            }
        }
    }

    private static int ToBoundedAudioSampleCount(long sampleCount)
    {
        var maxSamples = MaxDecodedAudioFrameBytes / (OutputAudioChannels * sizeof(float));
        if (sampleCount <= 0)
        {
            return 0;
        }

        if (sampleCount > maxSamples)
        {
            return maxSamples;
        }

        return (int)sampleCount;
    }

    private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)
    {
        bytes = 0;
        if (sampleCount <= 0)
        {
            return false;
        }

        var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);
        if (calculated <= 0 || calculated > MaxDecodedAudioFrameBytes || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
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

    private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()
    {
        if (!_suppressRecoverableSeekLogsForNextVideoFrame)
        {
            return null;
        }

        _suppressRecoverableSeekLogsForNextVideoFrame = false;
        return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();
    }

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        var microseconds = value.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)microseconds;
    }

    private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)
    {
        if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return 0;
        }

        var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;
        if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)timestamp;
    }
}
