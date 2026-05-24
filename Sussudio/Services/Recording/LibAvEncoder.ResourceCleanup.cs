using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    public void FlushAndClose()
    {
        if (!_isOpen && _formatCtx == null && _videoCodecCtx == null && _audio.CodecCtx == null && _mic.CodecCtx == null)
        {
            return;
        }

        try
        {
            if (_isOpen && !_flushSent)
            {
                try
                {
                    var flushResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, null);
                    if (flushResult != ffmpeg.AVERROR_EOF)
                    {
                        ThrowIfError(flushResult, "avcodec_send_frame(flush)");
                        _flushSent = true;
                    }

                    DrainEncoderPackets();
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_ENCODER_WARNING video_flush_error msg='{ex.Message}'");
                }
            }

            if (_audio.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _audio, "audio_flush",
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_audio.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(audio_flush)");
                }

                DrainStreamEncoderPackets(ref _audio);
            }

            if (_mic.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _mic, "mic_flush",
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_mic.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(mic_flush)");
                }

                DrainStreamEncoderPackets(ref _mic);
            }
        }
        finally
        {
            CleanupResources(writeTrailer: true);
        }
    }

    public void Dispose()
    {
        FlushAndClose();
    }

    private void CleanupResources(bool writeTrailer)
    {
        var outputPath = _options?.OutputPath;
        var normalClose = _isOpen;

        try
        {
            if (writeTrailer && _headerWritten && _formatCtx != null)
            {
                var trailerResult = ffmpeg.av_write_trailer(_formatCtx);
                if (trailerResult < 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_ERROR operation=av_write_trailer code={trailerResult} msg='{GetErrorString(trailerResult)}'");
                }
            }
        }
        finally
        {
            var useCudaHardwareFrames = _useCudaHardwareFrames;
            var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);

            var outputBytes = 0L;
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
            {
                outputBytes = new FileInfo(outputPath).Length;
            }

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (normalClose)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLOSE output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
                else if (_headerWritten || _encodedFrameCount > 0 || outputBytes > 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLEANUP init_failed=true output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
            }
        }
    }

    private long ReleaseNativeResources(bool useCudaHardwareFrames)
    {
        if (_formatCtx != null && _formatCtx->pb != null)
        {
            var closeResult = ffmpeg.avio_closep(&_formatCtx->pb);
            if (closeResult < 0)
            {
                Logger.Log(
                    $"LIBAV_ENCODER_ERROR operation=avio_closep code={closeResult} msg='{GetErrorString(closeResult)}'");
            }
        }

        if (_bsfCtx != null)
        {
            var bsfCtx = _bsfCtx;
            ffmpeg.av_bsf_free(&bsfCtx);
            _bsfCtx = null;
        }

        if (_packet != null)
        {
            var packet = _packet;
            ffmpeg.av_packet_free(&packet);
            _packet = null;
        }

        if (_hwFrame != null)
        {
            var hwFrame = _hwFrame;
            ffmpeg.av_frame_free(&hwFrame);
            _hwFrame = null;
        }

        if (_hwFramesCtx != null)
        {
            var hwFramesCtx = _hwFramesCtx;
            ffmpeg.av_buffer_unref(&hwFramesCtx);
            _hwFramesCtx = null;
        }

        if (_hwDeviceCtx != null)
        {
            var hwDeviceCtx = _hwDeviceCtx;
            ffmpeg.av_buffer_unref(&hwDeviceCtx);
            _hwDeviceCtx = null;
        }

        _useHardwareFrames = false;
        _useCudaHardwareFrames = false;

        if (!useCudaHardwareFrames && _hwPoolTextures != null)
        {
            for (var i = 0; i < _hwPoolTextures.Length; i++)
            {
                if (_hwPoolTextures[i] != IntPtr.Zero)
                {
                    Marshal.Release(_hwPoolTextures[i]);
                    _hwPoolTextures[i] = IntPtr.Zero;
                }
            }

            _hwPoolTextures = null;
        }

        if (_audio.Frame != null)
        {
            var audioFrame = _audio.Frame;
            ffmpeg.av_frame_free(&audioFrame);
            _audio.Frame = null;
        }

        if (_mic.Frame != null)
        {
            var micFrame = _mic.Frame;
            ffmpeg.av_frame_free(&micFrame);
            _mic.Frame = null;
        }

        if (_videoFrame != null)
        {
            var videoFrame = _videoFrame;
            ffmpeg.av_frame_free(&videoFrame);
            _videoFrame = null;
        }

        if (_audio.SwrCtx != null)
        {
            var swrCtx = _audio.SwrCtx;
            ffmpeg.swr_free(&swrCtx);
            _audio.SwrCtx = null;
        }

        if (_mic.SwrCtx != null)
        {
            var micSwrCtx = _mic.SwrCtx;
            ffmpeg.swr_free(&micSwrCtx);
            _mic.SwrCtx = null;
        }

        if (_audio.CodecCtx != null)
        {
            var audioCodecCtx = _audio.CodecCtx;
            ffmpeg.avcodec_free_context(&audioCodecCtx);
            _audio.CodecCtx = null;
        }

        if (_mic.CodecCtx != null)
        {
            var micCodecCtx = _mic.CodecCtx;
            ffmpeg.avcodec_free_context(&micCodecCtx);
            _mic.CodecCtx = null;
        }

        if (_videoCodecCtx != null)
        {
            var videoCodecCtx = _videoCodecCtx;
            ffmpeg.avcodec_free_context(&videoCodecCtx);
            _videoCodecCtx = null;
        }

        if (_audio.ResampleBuffer != null)
        {
            ffmpeg.av_free(_audio.ResampleBuffer);
            _audio.ResampleBuffer = null;
        }

        if (_audio.SampleQueueBuffer != null)
        {
            ffmpeg.av_free(_audio.SampleQueueBuffer);
            _audio.SampleQueueBuffer = null;
        }

        if (_mic.ResampleBuffer != null)
        {
            ffmpeg.av_free(_mic.ResampleBuffer);
            _mic.ResampleBuffer = null;
        }

        if (_mic.SampleQueueBuffer != null)
        {
            ffmpeg.av_free(_mic.SampleQueueBuffer);
            _mic.SampleQueueBuffer = null;
        }

        if (_formatCtx != null)
        {
            ffmpeg.avformat_free_context(_formatCtx);
            _formatCtx = null;
        }

        _videoStream = null;
        _audio.Stream = null;
        _mic.Stream = null;
        _audio.FrameSize = 0;
        _mic.FrameSize = 0;
        _audio.AccumulatorCapacity = 0;
        _audio.SampleQueueCapacity = 0;
        _mic.AccumulatorCapacity = 0;
        _mic.SampleQueueCapacity = 0;
        _audio.AccumulatorBytes = 0;
        _audio.BufferedSamples = 0;
        _mic.AccumulatorBytes = 0;
        _mic.BufferedSamples = 0;
        _nextVideoPts = 0;
        _audio.NextPts = 0;
        _mic.NextPts = 0;
        var finalMicSamplesReceived = _micSamplesReceived;
        _micSamplesReceived = 0;
        _mic.CachedTimeBase = default;
        _isOpen = false;
        _headerWritten = false;
        _flushSent = false;

        return finalMicSamplesReceived;
    }
}
