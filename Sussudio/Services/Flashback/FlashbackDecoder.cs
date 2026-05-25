using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

/// <summary>
/// A decoded video frame. Either a D3D11 texture (GPU-direct) or raw NV12/P010 data.
/// For D3D11: <see cref="DecodedVideoFrame.TexturePtr"/> and <see cref="DecodedVideoFrame.SubresourceIndex"/> are valid.
/// For software: <see cref="DecodedVideoFrame.Data"/> is valid until the next decode call.
/// </summary>
internal readonly struct DecodedVideoFrame
{
    public IntPtr Data { get; init; }
    public int DataLength { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsHdr { get; init; }
    public TimeSpan Pts { get; init; }
    public IntPtr TexturePtr { get; init; }
    public int SubresourceIndex { get; init; }
    public bool IsD3D11Texture { get; init; }
    public IntPtr HeldFrame { get; init; } // AVFrame* that must be freed after texture is consumed
}

/// <summary>
/// A decoded audio chunk in f32le interleaved stereo 48kHz format.
/// <see cref="DecodedAudioChunk.Samples"/> is rented from <see cref="ArrayPool{T}"/> and should be returned when done.
/// </summary>
internal readonly struct DecodedAudioChunk
{
    public byte[] Samples { get; init; }
    public int ValidLength { get; init; }
    public TimeSpan Pts { get; init; }
}

/// <summary>
/// Video+audio decoder for Flashback .ts files.
/// Decodes HEVC/H.264 video via D3D11VA (GPU-direct) or software fallback to NV12/P010,
/// and AAC audio to f32le interleaved stereo 48kHz.
/// This type is NOT thread-safe — all calls must come from the playback controller's thread.
/// </summary>
internal sealed unsafe partial class FlashbackDecoder : IDisposable
{
    private const int OutputAudioSampleRate = 48000;
    private const int OutputAudioChannels = 2;
    private const int VideoFrameBufferCount = 2;
    private const int MaxSupportedInputStreams = 64;
    private const int MaxDecodedVideoDimension = 8192;
    private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;
    private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;
    private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;
    private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;

    private AVFormatContext* _formatCtx;
    private AVCodecContext* _videoCodecCtx;
    private AVCodecContext* _audioCodecCtx;
    private SwrContext* _swrCtx;
    private AVFrame* _videoFrame;
    private AVFrame* _audioFrame;
    private AVPacket* _packet;

    private int _videoStreamIndex = -1;
    private int _audioStreamIndex = -1;
    private AVRational _videoTimeBase;
    private AVRational _audioTimeBase;

    // Double-buffered video output: alternate between two managed buffers
    // so the renderer can consume one while we decode the next.
    // Only used for software decode path.
    private byte[]?[] _videoFrameBuffers = new byte[VideoFrameBufferCount][];
    private GCHandle[] _videoFrameHandles = new GCHandle[VideoFrameBufferCount];
    private int _currentVideoBufferIndex;

    // Pending frame stash: SeekTo() decodes forward to the target frame but must
    // not discard it — stash it so the next TryDecodeNextVideoFrame() returns it.
    private DecodedVideoFrame _pendingVideoFrame;
    private bool _hasPendingVideoFrame;
    private bool _suppressRecoverableSeekLogsForNextVideoFrame;

    // SeekTo forward-decode cap observability
    private long _seekToCapHits;
    private bool _lastSeekHitForwardDecodeCap;

    private bool _isOpen;
    private bool _disposed;
    private bool _initialized;
    private string? _currentFilePath;

    /// <summary>
    /// When set, audio packets encountered during video reads are decoded and
    /// delivered here immediately — no stashing, no separate audio loop.
    /// This keeps audio naturally interleaved with video, like any video player.
    /// </summary>
    public Action<DecodedAudioChunk>? AudioChunkCallback { get; set; }

    // Video info (populated after OpenFile)
    private int _videoWidth;
    private int _videoHeight;
    private bool _isHdr;
    private double _frameRate;
    private double _metadataFrameRate;
    private int _ptsCalibrationCount;
    private long _firstCalibrationPtsTicks;
    private long _lastCalibrationPtsTicks;
    private const int PtsCalibrationFrames = 10;
    private TimeSpan _currentPosition;
    private AVPixelFormat _decodedPixelFormat;
    private bool _needsConvert;

    // D3D11VA hardware decode state (persistent across file opens)
    private AVBufferRef* _d3d11HwDeviceCtx;
    private bool _isD3D11HwAccelerated;
    private IntPtr _d3dDevicePtr;
    private IntPtr _d3dContextPtr;

    public bool IsOpen => _isOpen;
    public int VideoWidth => _videoWidth;
    public int VideoHeight => _videoHeight;
    public bool IsHdr => _isHdr;
    public double FrameRate => _frameRate;
    public TimeSpan CurrentPosition => _currentPosition;
    public bool IsD3D11HwAccelerated => _isD3D11HwAccelerated;

    /// <summary>
    /// Total number of times SeekTo() hit the forward-decode cap AND the best frame
    /// was more than one frame interval behind the seek target (i.e., a missed seek).
    /// </summary>
    public long SeekToForwardDecodeCapHits => Interlocked.Read(ref _seekToCapHits);

    /// <summary>
    /// True if the most recent SeekTo() call hit the forward-decode cap and the
    /// returned frame's PTS was more than one frame interval behind the target.
    /// Reset to false on each SeekTo() entry.
    /// </summary>
    public bool LastSeekHitForwardDecodeCap => _lastSeekHitForwardDecodeCap;

    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("FlashbackDecoder has not been initialized. Call Initialize() first.");
        }
    }

    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();
        if (!_isOpen)
        {
            throw new InvalidOperationException("No file is open. Call OpenFile() first.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException(
            $"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateException(string message)
    {
        Logger.Log($"FLASHBACK_DECODER_ERROR {message}");
        return new InvalidOperationException($"FLASHBACK_DECODER_ERROR {message}");
    }

    /// <summary>
    /// Opens a .ts or .mp4 file for decoding.
    /// </summary>
    public void OpenFile(string filePath)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        if (_isOpen)
        {
            CloseFile();
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        _currentFilePath = filePath;

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                Logger.Log($"FLASHBACK_DECODER_OPEN_FAIL reason=file_not_found path='{filePath}'");
                throw new System.IO.FileNotFoundException($"File not found: '{filePath}'", filePath);
            }

            // Open input
            AVFormatContext* formatCtx = null;
            ThrowIfError(
                ffmpeg.avformat_open_input(&formatCtx, filePath, null, null),
                "avformat_open_input");
            _formatCtx = formatCtx;
            _formatCtx->flags |= ffmpeg.AVFMT_FLAG_GENPTS;

            // Rotated MPEG-TS segments can start mid-stream before the next IDR/SPS.
            // Use the same larger probe window as the exporter so playback can recover
            // dimensions and extradata from high-bitrate 4K120 flashback segments.
            _formatCtx->probesize = MaxMpegTsProbeSizeBytes;
            _formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;

            ThrowIfError(
                ffmpeg.avformat_find_stream_info(_formatCtx, null),
                "avformat_find_stream_info");
            if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))
            {
                throw CreateException(streamCountFailure);
            }

            // Find video stream
            _videoStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            if (!IsValidStreamIndex(_videoStreamIndex, streamCount))
            {
                throw CreateException("No video stream found in file.");
            }

            // Find audio stream (optional)
            _audioStreamIndex = ffmpeg.av_find_best_stream(
                _formatCtx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);
            if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index index={_audioStreamIndex} stream_count={streamCount}; audio disabled");
                _audioStreamIndex = -1;
            }

            // Set up video decoder
            InitializeVideoDecoder();

            // Set up audio decoder (if present)
            if (_audioStreamIndex >= 0)
            {
                InitializeAudioDecoder();
            }

            // Allocate shared packet
            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw CreateException("Failed to allocate AVPacket.");
            }

            _currentPosition = TimeSpan.Zero;
            _isOpen = true;

            Logger.Log($"FLASHBACK_DECODER_OPEN path='{filePath}' " +
                       $"video={_videoWidth}x{_videoHeight} fps={_frameRate:F2} hdr={_isHdr} " +
                       $"hw_accel={((_isD3D11HwAccelerated ? "D3D11VA" : "Software"))} " +
                       $"audio={(_audioStreamIndex >= 0 ? "yes" : "no")}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_OPEN_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            CloseFileCore();
            throw;
        }
    }

    /// <summary>
    /// Closes the currently open file and frees per-file decoder resources.
    /// The D3D11VA device context is NOT freed (persistent across files).
    /// </summary>
    public void CloseFile()
    {
        if (!_isOpen)
        {
            return;
        }

        var closedPath = _currentFilePath;
        CloseFileCore();
        Logger.Log($"FLASHBACK_DECODER_CLOSE path='{closedPath}'");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AudioChunkCallback = null;
        CloseFileCore();

        // Free persistent D3D11VA device context
        if (_d3d11HwDeviceCtx != null)
        {
            var h = _d3d11HwDeviceCtx;
            ffmpeg.av_buffer_unref(&h);
            _d3d11HwDeviceCtx = null;
        }

        Logger.Log("FLASHBACK_DECODER_DISPOSED");
    }

    private void CloseFileCore()
    {
        Logger.Log($"FLASHBACK_DECODER_CLOSE_CORE path='{_currentFilePath}' had_swr={_swrCtx != null} had_video={_videoCodecCtx != null} had_audio={_audioCodecCtx != null}");
        _isOpen = false;
        _suppressRecoverableSeekLogsForNextVideoFrame = false;

        // Clear any stashed pending frame (free held D3D11VA surface if present).
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "close_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        // Free pinned handles used by the software decode path.
        for (var i = 0; i < VideoFrameBufferCount; i++)
        {
            if (_videoFrameHandles[i].IsAllocated)
            {
                _videoFrameHandles[i].Free();
            }

            if (_videoFrameBuffers[i] != null)
            {
                ArrayPool<byte>.Shared.Return(_videoFrameBuffers[i]!);
                _videoFrameBuffers[i] = null;
            }
        }

        if (_packet != null)
        {
            var pkt = _packet;
            ffmpeg.av_packet_free(&pkt);
            _packet = null;
        }

        if (_videoFrame != null)
        {
            var frame = _videoFrame;
            ffmpeg.av_frame_free(&frame);
            _videoFrame = null;
        }

        if (_audioFrame != null)
        {
            var frame = _audioFrame;
            ffmpeg.av_frame_free(&frame);
            _audioFrame = null;
        }

        // _d3d11HwDeviceCtx is persistent across files. _isD3D11HwAccelerated
        // is reset per file in InitializeVideoDecoder.
        if (_swrCtx != null)
        {
            ffmpeg.swr_convert(_swrCtx, null, 0, null, 0);
            var swr = _swrCtx;
            ffmpeg.swr_free(&swr);
            _swrCtx = null;
        }

        if (_videoCodecCtx != null)
        {
            var ctx = _videoCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _videoCodecCtx = null;
        }

        if (_audioCodecCtx != null)
        {
            var ctx = _audioCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _audioCodecCtx = null;
        }

        if (_formatCtx != null)
        {
            var fmt = _formatCtx;
            ffmpeg.avformat_close_input(&fmt);
            _formatCtx = null;
        }

        _videoStreamIndex = -1;
        _audioStreamIndex = -1;
        _videoWidth = 0;
        _videoHeight = 0;
        _isHdr = false;
        _frameRate = 0;
        _metadataFrameRate = 0;
        _ptsCalibrationCount = 0;
        _firstCalibrationPtsTicks = 0;
        _lastCalibrationPtsTicks = 0;
        _currentPosition = TimeSpan.Zero;
        _currentFilePath = null;
        _needsConvert = false;
        _currentVideoBufferIndex = 0;
    }

    /// <summary>
    /// Frees the cloned AVFrame held by a D3D11VA <see cref="DecodedVideoFrame"/>,
    /// releasing the D3D11VA surface reference back to the decoder pool.
    /// Safe to call on software frames (no-op when <see cref="DecodedVideoFrame.HeldFrame"/> is zero).
    /// </summary>
    internal static void ReleaseHeldFrame(DecodedVideoFrame frame)
    {
        if (frame.HeldFrame != IntPtr.Zero)
        {
            var heldFrame = (AVFrame*)frame.HeldFrame;
            ffmpeg.av_frame_free(&heldFrame);
        }
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
