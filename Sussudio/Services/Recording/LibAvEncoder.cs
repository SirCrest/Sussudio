using System;
using System.Runtime.InteropServices;
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

    private void EnsureOpen()
    {
        if (!_isOpen || _formatCtx == null || _videoCodecCtx == null || _videoStream == null || _videoFrame == null || _packet == null)
        {
            throw new InvalidOperationException("LibAvEncoder is not initialized.");
        }
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateLibAvException(string message)
    {
        Logger.Log(message);
        return new InvalidOperationException(message);
    }

    /// <summary>
    /// Checks ID3D11Device::GetDeviceRemovedReason (vtable slot 39) to detect TDR.
    /// CopySubresourceRegion is void-return, so after a device-removed event all
    /// context calls silently no-op. This proactive check surfaces the error before
    /// NVENC encodes from stale/garbage textures, allowing the caller to finalize
    /// the recording and preserve already-encoded data.
    /// </summary>
    private static void CheckDeviceRemoved(IntPtr d3d11Device)
    {
        if (d3d11Device == IntPtr.Zero)
            return;

        var deviceVtable = *(IntPtr*)d3d11Device;
        // ID3D11Device vtable layout: IUnknown (0-2) + ID3D11Device methods (3+).
        // CreateTexture2D = slot 5 (validated elsewhere in this file).
        // GetDeviceRemovedReason = slot 39 (3 IUnknown + 36 ID3D11Device methods before it).
        var getDeviceRemovedReason =
            (delegate* unmanaged[Stdcall]<IntPtr, int>)*(IntPtr*)(deviceVtable + 39 * IntPtr.Size);
        var hr = getDeviceRemovedReason(d3d11Device);

        if (hr < 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_DEVICE_REMOVED hr=0x{unchecked((uint)hr):X8} " +
                "msg=GPU device was removed (TDR). Recording will be finalized with frames encoded so far.");
        }
    }

}

internal sealed record LibAvEncoderOptions
{
    public required string OutputPath { get; init; }
    public string ContainerFormat { get; init; } = "mp4";
    public required string CodecName { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required double FrameRate { get; init; }
    public int? FrameRateNumerator { get; init; }
    public int? FrameRateDenominator { get; init; }
    public required uint BitRate { get; init; }
    public required bool IsP010 { get; init; }
    public string? NvencPreset { get; init; }
    public string SplitEncodeMode { get; init; } = "Auto";
    public int GopSize { get; init; } = -1;
    /// <summary>
    /// Use frag_keyframe+empty_moov instead of faststart for MP4.
    /// Required for flashback segments that are read while still being written.
    /// </summary>
    public bool FragmentedMp4 { get; init; }
    public bool AudioEnabled { get; init; }
    public int AudioSampleRate { get; init; } = 48_000;
    public int AudioChannels { get; init; } = 2;
    public int AudioBitRate { get; init; } = 320_000;
    public bool MicrophoneEnabled { get; init; }
    public int MicrophoneSampleRate { get; init; } = 48_000;
    public int MicrophoneChannels { get; init; } = 2;
    public int MicrophoneBitRate { get; init; } = 320_000;
    public bool HdrEnabled { get; init; }
    public bool IsFullRangeInput { get; init; }
    public string? HdrMasterDisplayMetadata { get; init; }
    public int HdrMaxCll { get; init; }
    public int HdrMaxFall { get; init; }
    public IntPtr D3D11DevicePtr { get; init; }
    public IntPtr D3D11DeviceContextPtr { get; init; }
    public IntPtr CudaHwDeviceCtxPtr { get; init; }
    public IntPtr CudaHwFramesCtxPtr { get; init; }
}

internal readonly record struct RotateOutputResult(string PreviousPath, long PreviousEncodedFrames, long PreviousTotalBytes);
