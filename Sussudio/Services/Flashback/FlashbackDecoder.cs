using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

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

    /// <summary>
    /// Initializes the decoder with D3D11 device pointers for GPU-direct decode.
    /// Must be called before <see cref="OpenFile"/>.
    /// </summary>
    public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            return;
        }

        LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

        _d3dDevicePtr = d3dDevicePtr;
        _d3dContextPtr = d3dContextPtr;

        // Create persistent D3D11VA hw device context (reused across all file opens)
        if (d3dDevicePtr != IntPtr.Zero && d3dContextPtr != IntPtr.Zero)
        {
            try
            {
                var hwDeviceCtx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
                if (hwDeviceCtx != null)
                {
                    var hwCtx = (AVHWDeviceContext*)hwDeviceCtx->data;
                    var d3d11vaCtx = (AVD3D11VADeviceContext*)hwCtx->hwctx;
                    d3d11vaCtx->device = (FFmpeg.AutoGen.ID3D11Device*)d3dDevicePtr;
                    d3d11vaCtx->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)d3dContextPtr;

                    var initResult = ffmpeg.av_hwdevice_ctx_init(hwDeviceCtx);
                    if (initResult >= 0)
                    {
                        _d3d11HwDeviceCtx = hwDeviceCtx;
                        Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=true device=0x{d3dDevicePtr:X}");
                    }
                    else
                    {
                        ffmpeg.av_buffer_unref(&hwDeviceCtx);
                        Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=false reason=init_fail code={initResult}");
                    }
                }
                else
                {
                    Logger.Log("FLASHBACK_DECODER_INIT d3d11va=false reason=alloc_fail");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }
        else
        {
            Logger.Log("FLASHBACK_DECODER_INIT d3d11va=false reason=no_device");
        }

        _initialized = true;
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


}
