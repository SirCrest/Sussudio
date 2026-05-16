using System;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

/// <summary>
/// In-process libav encoder for MP4 recording.
/// This type is not thread-safe; all libav calls must be serialized onto one thread.
/// </summary>
internal sealed unsafe partial class LibAvEncoder : IDisposable
{
    /// <summary>Forwards to <see cref="FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs"/>.</summary>
    internal static IDisposable SuppressRecoverableSeekFfmpegLogs()
        => FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs();

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVStream* _videoStream;
    private AVFrame* _videoFrame;
    private AVPacket* _packet;
    private AVBSFContext* _bsfCtx;
    private LibAvEncoderOptions? _options;
    private long _nextVideoPts;
    private long _encodedFrameCount;
    private long _droppedFrameCount;
    private long _videoPacketsWritten;
    private long _totalBytesWritten;
    private bool _isOpen;
    private bool _headerWritten;
    private AVRational _cachedVideoTimeBase;
    private bool _flushSent;
    private AVBufferRef* _hwDeviceCtx;
    private AVBufferRef* _hwFramesCtx;
    private AVFrame* _hwFrame;
    private bool _useHardwareFrames;
    private bool _useCudaHardwareFrames;
    private volatile bool _forceNextKeyframe;
    private IntPtr[]? _hwPoolTextures; // individual ArraySize=1 D3D11 textures for the hw frames pool
    private int _hwPoolIndex; // round-robin index into _hwPoolTextures

    /// <summary>No-op free callback for av_buffer_create; our pool textures outlive individual frames.</summary>
    private static readonly av_buffer_create_free HwPoolTextureFreeDelegate = (opaque, data) => { /* intentional no-op */ };
    private static readonly av_buffer_create_free_func HwPoolTextureFree = HwPoolTextureFreeDelegate;

    public long EncodedFrameCount => _encodedFrameCount;
    public long DroppedFrameCount => _droppedFrameCount;
    public long VideoPacketsWritten => Interlocked.Read(ref _videoPacketsWritten);
    public long TotalBytesWritten => _totalBytesWritten;
    public bool IsEncoding => _isOpen;
    public string VideoCodecName => _options?.CodecName ?? string.Empty;
    public string OutputPath => _options?.OutputPath ?? string.Empty;
    public bool UseHardwareFrames => _useHardwareFrames;
    public bool UseCudaHardwareFrames => _useCudaHardwareFrames;
    public long NextVideoPts => _nextVideoPts;

    /// <summary>
    /// Sets initial PTS counters for video (frame units) and audio (sample units).
    /// Used when continuing encoding after a sink-only cycle so file-level
    /// timestamps continue from the previous session.
    /// Must be called after <see cref="Initialize"/> and before encoding any frames.
    /// </summary>
    public void SetInitialPts(long videoPts, long audioPts)
    {
        Interlocked.Exchange(ref _nextVideoPts, videoPts);
        Interlocked.Exchange(ref _audio.NextPts, audioPts);
        Interlocked.Exchange(ref _mic.NextPts, audioPts);
    }

    public void SkipVideoFrame() { Interlocked.Increment(ref _nextVideoPts); }

    /// <summary>
    /// Encode a D3D11 texture directly on GPU via hardware frames.
    /// The caller must have done AddRef on the texture; this method does NOT AddRef/Release.
    /// The source texture is copied into a pool texture via CopySubresourceRegion on the immediate context.
    /// </summary>
    /// <summary>
    /// Encode a CUDA-resident decoded frame directly.
    /// Zero-copy is preserved when decoder and encoder share the same hw_frames_ctx.
    /// </summary>
    public RotateOutputResult RotateOutput(string newPath)
    {
        EnsureOpen();

        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("New output path is required.", nameof(newPath));
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var previousPath = options.OutputPath;
        var previousEncodedFrames = _encodedFrameCount;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(newPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (_audio.CodecCtx != null)
        {
            // CRITICAL: Do NOT call FlushPendingAudioSamples() here.
            // It uses flushPartialFrame=true, which sends a partial AAC frame
            // (< 1024 samples). The AAC encoder interprets partial frames as
            // "end of stream" and rejects all subsequent frames with EINVAL.
            // Instead, drain only full frames and carry partial samples into
            // the next segment.
            DrainBufferedFrames(ref _audio, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _audio);
        }

        if (_mic.CodecCtx != null)
        {
            DrainBufferedFrames(ref _mic, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _mic);
        }

        DrainEncoderPackets();

        if (_headerWritten && _formatCtx != null)
        {
            ThrowIfError(ffmpeg.av_write_trailer(_formatCtx), "av_write_trailer(rotate)");
        }

        // Capture total bytes AFTER drains and trailer so the count includes
        // all bytes flushed to the completed segment, but BEFORE the reset
        // in ReinitializeOutputContext / ResetSegmentRuntimeState.
        var previousTotalBytes = _totalBytesWritten;

        CloseCurrentOutputIo();
        FreeCurrentOutputContext();
        try
        {
            ReinitializeOutputContext(newPath);
        }
        catch (Exception ex)
        {
            _isOpen = false;
            Logger.Log($"LIBAV_ENCODER_ROTATE_FAILED path='{newPath}' error={ex.Message}");
            throw;
        }

        ResetSegmentRuntimeState();
        _options = options with { OutputPath = newPath };

        Logger.Log(
            $"LIBAV_ENCODER_ROTATE old_output='{previousPath}' new_output='{newPath}' frames={previousEncodedFrames} bytes={previousTotalBytes}");
        return new RotateOutputResult(previousPath, previousEncodedFrames, previousTotalBytes);
    }

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

}
