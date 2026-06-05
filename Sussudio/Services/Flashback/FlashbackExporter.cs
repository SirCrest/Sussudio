using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.Win32.SafeHandles;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports Flashback time ranges by remuxing finalized segment artifacts to .mp4.
/// No re-encoding - just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe class FlashbackExporter : IDisposable
{
    // Export reads finalized segment artifacts only. Live capture continues via
    // FlashbackEncoderSink while this class remuxes packets into the target MP4.
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int MaxSupportedInputStreams = 64;
    private const int ProgressHeartbeatIntervalMs = 1_000;
    private const int ExportLockWaitTimeoutSeconds = 30;
    private const int TempOutputCreationAttempts = 16;
    private const int ErrorFileNotFound = 2;
    private const int ErrorPathNotFound = 3;
    private const int ErrorSharingViolation = 32;
    private const int ErrorFileExists = 80;
    private const int ErrorAlreadyExists = 183;
    private const uint NativeFileReadAttributes = 0x00000080;
    private const uint NativeDeleteAccess = 0x00010000;
    private const uint NativeFileShareRead = 0x00000001;
    private const uint NativeFileShareWrite = 0x00000002;
    private const uint NativeOpenExisting = 3;
    private const uint NativeFileAttributeNormal = 0x00000080;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly object _lifetimeSync = new();
    private CancellationTokenSource? _disposeCts = new();
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out ByHandleFileInformation fileInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file,
        FileInformationClass fileInformationClass,
        IntPtr fileInformation,
        uint bufferSize);

    private enum FileInformationClass
    {
        FileRenameInfo = 3,
        FileDispositionInfo = 4
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    private readonly record struct TempFileIdentity(
        uint VolumeSerialNumber,
        uint FileIndexHigh,
        uint FileIndexLow)
    {
        public static TempFileIdentity From(in ByHandleFileInformation info)
            => new(info.VolumeSerialNumber, info.FileIndexHigh, info.FileIndexLow);
    }

    private sealed class TempOutputLease : IDisposable
    {
        private FileStream? _reservationStream;

        public TempOutputLease(string path, TempFileIdentity identity, FileStream reservationStream)
        {
            Path = path;
            Identity = identity;
            _reservationStream = reservationStream;
        }

        public string Path { get; }
        public TempFileIdentity Identity { get; }

        public void ReleaseReservation()
        {
            var stream = _reservationStream;
            if (stream == null)
            {
                return;
            }

            _reservationStream = null;
            stream.Dispose();
        }

        public void Dispose()
            => ReleaseReservation();
    }

    public void Dispose()
    {
        CancellationTokenSource? disposeCts;
        lock (_lifetimeSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            disposeCts = _disposeCts;
        }

        Logger.Log("FLASHBACK_EXPORT_DISPOSE");

        // Signal any running export to cancel. ExportCore/ExportSegmentsCore will exit
        // via OperationCanceledException, clean up native state, and release _exportLock.
        try { disposeCts?.Cancel(); }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }

        // Wait for the export task to release the lock. The CTS is cancelled so
        // the task should exit promptly. Timeout prevents app hang if FFmpeg is stuck.
        var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));
        if (!lockAcquired)
        {
            Logger.Log("FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock (10s)");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT cleanup_invoked=false");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
            DisposeLinkedCtsBestEffort(disposeCts, "dispose_timeout");
            ClearDisposeCtsReference(disposeCts);
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            CleanupNativeState();
        }
        finally
        {
            if (lockAcquired)
            {
                ReleaseExportLockBestEffort("dispose");
            }
        }

        DisposeExportLockBestEffort();
        DisposeLinkedCtsBestEffort(disposeCts, "dispose");
        ClearDisposeCtsReference(disposeCts);
        GC.SuppressFinalize(this);
    }

    private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var disposeCts = _disposeCts ?? throw new ObjectDisposedException(nameof(FlashbackExporter));
            return CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token);
        }
    }

    private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)
    {
        lock (_lifetimeSync)
        {
            if (ReferenceEquals(_disposeCts, disposeCts))
            {
                _disposeCts = null;
            }
        }
    }

    private void EnsureNotDisposed()
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    private static FinalizeResult CreateCancelledExportResult(string outputPath)
    {
        const string message = "Flashback export cancelled.";
        Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
        return FinalizeResult.Failure(outputPath, message);
    }

    private static FinalizeResult CreateDisposedExportResult(string outputPath)
    {
        const string message = "Flashback exporter is disposed.";
        Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
        return FinalizeResult.Failure(outputPath, message);
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)
    {
        try
        {
            if (!_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct))
            {
                var message = $"Flashback export lock timed out after {ExportLockWaitTimeoutSeconds}s.";
                Logger.Log($"FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT timeout_s={ExportLockWaitTimeoutSeconds}");
                cancellationResult = FinalizeResult.Failure(outputPath, message);
                return false;
            }

            cancellationResult = null!;
            return true;
        }
        catch (OperationCanceledException)
        {
            const string message = "Flashback export cancelled.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            cancellationResult = FinalizeResult.Failure(outputPath, message);
            return false;
        }
        catch (ObjectDisposedException)
        {
            cancellationResult = CreateDisposedExportResult(outputPath);
            return false;
        }
    }

    private void ReleaseExportLockBestEffort(string operation)
    {
        try
        {
            _exportLock.Release();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_RELEASE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DisposeExportLockBestEffort()
    {
        try
        {
            _exportLock.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CloseActiveInput()
    {
        if (_activeInputContext == null)
        {
            return;
        }

        var inputContext = _activeInputContext;
        ffmpeg.avformat_close_input(&inputContext);
        _activeInputContext = null;
    }

    private void CloseOutputIo()
    {
        if (_activeOutputContext == null || _activeOutputContext->pb == null)
        {
            return;
        }

        var closeResult = ffmpeg.avio_closep(&_activeOutputContext->pb);
        if (closeResult < 0)
        {
            Logger.Log(
                $"FLASHBACK_EXPORT_WARN reason='avio_closep_failed' code={closeResult} msg='{GetErrorString(closeResult)}'");
        }
    }

    private void CleanupNativeState()
    {
        try
        {
            CloseActiveInput();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=close_input type={ex.GetType().Name} msg='{ex.Message}'");
            _activeInputContext = null;
        }

        try
        {
            CloseOutputIo();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=close_output_io type={ex.GetType().Name} msg='{ex.Message}'");
        }

        if (_activeOutputContext != null)
        {
            try
            {
                ffmpeg.avformat_free_context(_activeOutputContext);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=free_output_context type={ex.GetType().Name} msg='{ex.Message}'");
            }
            finally
            {
                _activeOutputContext = null;
            }
        }
    }

    // --- FFmpeg stream and input/output context setup ---
    /// <summary>
    /// Copies stream templates from input to output, skipping streams with invalid codec parameters
    /// (e.g., audio with 0 channels). Returns a mapping array: streamMap[inputIndex] = outputIndex, or -1 if skipped.
    /// </summary>
    private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)
    {
        var streamMap = new int[inputStreamCount];

        for (var streamIndex = 0; streamIndex < inputStreamCount; streamIndex++)
        {
            var inStream = inputContext->streams[streamIndex];
            var codecType = inStream->codecpar->codec_type;

            // Skip streams with incomplete codec params.
            if (!TemplateStreamContributesToTemplateScore(
                    codecType,
                    inStream->codecpar->width,
                    inStream->codecpar->height,
                    inStream->codecpar->sample_rate,
                    inStream->codecpar->ch_layout.nb_channels))
            {
                if (codecType == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_audio_params' channels={inStream->codecpar->ch_layout.nb_channels} sample_rate={inStream->codecpar->sample_rate}");
                }
                else
                {
                    Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_video_params' width={inStream->codecpar->width} height={inStream->codecpar->height}");
                }
                streamMap[streamIndex] = -1;
                continue;
            }

            var outStream = ffmpeg.avformat_new_stream(outputContext, null);
            if (outStream == null)
            {
                throw new InvalidOperationException("FLASHBACK_EXPORT_ERROR operation=avformat_new_stream msg='Stream allocation returned null.'");
            }

            ThrowIfError(ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar), "avcodec_parameters_copy");
            outStream->codecpar->codec_tag = 0;
            outStream->time_base = inStream->time_base;
            outStream->avg_frame_rate = inStream->avg_frame_rate;
            outStream->sample_aspect_ratio = inStream->sample_aspect_ratio;

            streamMap[streamIndex] = outStream->index;
        }

        return streamMap;
    }

    private static int CountUsableTemplateStreams(AVFormatContext* inputContext, int inputStreamCount)
    {
        var usableStreamCount = 0;

        for (var streamIndex = 0; streamIndex < inputStreamCount; streamIndex++)
        {
            var inStream = inputContext->streams[streamIndex];
            var codecType = inStream->codecpar->codec_type;

            if (!TemplateStreamContributesToTemplateScore(
                    codecType,
                    inStream->codecpar->width,
                    inStream->codecpar->height,
                    inStream->codecpar->sample_rate,
                    inStream->codecpar->ch_layout.nb_channels))
            {
                continue;
            }

            usableStreamCount++;
        }

        return usableStreamCount;
    }

    private static bool TemplateStreamContributesToTemplateScore(
        AVMediaType codecType,
        int width,
        int height,
        int sampleRate,
        int channelCount)
    {
        return codecType switch
        {
            AVMediaType.AVMEDIA_TYPE_AUDIO => channelCount > 0 && sampleRate > 0,
            AVMediaType.AVMEDIA_TYPE_VIDEO => width > 0 && height > 0,
            _ => true
        };
    }

    private static string? FindSegmentStreamLayoutMismatch(
        AVFormatContext* inputContext,
        AVFormatContext* outputContext,
        int[] streamMap,
        int inputStreamCount)
    {
        if (inputContext == null || outputContext == null)
        {
            return "missing_context";
        }

        var comparableStreamCount = Math.Min(inputStreamCount, streamMap.Length);
        for (var streamIndex = 0; streamIndex < comparableStreamCount; streamIndex++)
        {
            var outputIndex = streamMap[streamIndex];
            if (outputIndex < 0)
            {
                continue;
            }

            if (outputIndex >= outputContext->nb_streams)
            {
                return $"stream={streamIndex} output_index_out_of_range output={outputIndex} output_count={outputContext->nb_streams}";
            }

            var inputStream = inputContext->streams[streamIndex];
            var outputStream = outputContext->streams[outputIndex];
            if (inputStream == null || outputStream == null || inputStream->codecpar == null || outputStream->codecpar == null)
            {
                return $"stream={streamIndex} missing_codec_params";
            }

            var inputCodec = inputStream->codecpar;
            var templateCodec = outputStream->codecpar;
            if (inputCodec->codec_type != templateCodec->codec_type)
            {
                return $"stream={streamIndex} codec_type expected={templateCodec->codec_type} actual={inputCodec->codec_type}";
            }

            if (inputCodec->codec_id != templateCodec->codec_id)
            {
                return $"stream={streamIndex} codec_id expected={templateCodec->codec_id} actual={inputCodec->codec_id}";
            }

            if (inputCodec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                if (!VideoDimensionsMatchOrCanUseTemplate(inputCodec, templateCodec))
                {
                    return $"stream={streamIndex} video_size expected={templateCodec->width}x{templateCodec->height} actual={inputCodec->width}x{inputCodec->height}";
                }
            }
            else if (inputCodec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                if (!AudioParamsMatchOrCanUseTemplate(inputCodec, templateCodec))
                {
                    return $"stream={streamIndex} audio_params expected={templateCodec->sample_rate}Hz/{templateCodec->ch_layout.nb_channels}ch/{templateCodec->format} actual={inputCodec->sample_rate}Hz/{inputCodec->ch_layout.nb_channels}ch/{inputCodec->format}";
                }
            }
        }

        return null;
    }

    private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)
    {
        if (inputCodec->width == templateCodec->width && inputCodec->height == templateCodec->height)
        {
            return true;
        }

        var inputHasCompleteDimensions = inputCodec->width > 0 && inputCodec->height > 0;
        var templateHasCompleteDimensions = templateCodec->width > 0 && templateCodec->height > 0;
        return !inputHasCompleteDimensions && templateHasCompleteDimensions;
    }

    private static bool AudioParamsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)
    {
        return AudioParamsMatchOrCanUseTemplate(
            inputCodec->sample_rate,
            inputCodec->ch_layout.nb_channels,
            inputCodec->format,
            templateCodec->sample_rate,
            templateCodec->ch_layout.nb_channels,
            templateCodec->format);
    }

    private static bool AudioParamsMatchOrCanUseTemplate(
        int inputSampleRate,
        int inputChannelCount,
        int inputSampleFormat,
        int templateSampleRate,
        int templateChannelCount,
        int templateSampleFormat)
    {
        var inputAudioParamsIncomplete = inputSampleRate <= 0 || inputChannelCount <= 0;
        var templateHasCompleteAudioParams = templateSampleRate > 0 && templateChannelCount > 0;
        if (inputAudioParamsIncomplete)
        {
            return templateHasCompleteAudioParams;
        }

        if (inputSampleRate != templateSampleRate)
        {
            return false;
        }

        if (inputChannelCount != templateChannelCount)
        {
            return false;
        }

        return inputSampleFormat == templateSampleFormat;
    }

    private static int FindVideoStreamIndex(AVFormatContext* inputContext)
    {
        return ffmpeg.av_find_best_stream(inputContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
    }

    private static bool TryGetInputStreamCount(
        AVFormatContext* inputContext,
        string operation,
        out int streamCount,
        out string failureMessage)
    {
        streamCount = 0;
        if (inputContext == null)
        {
            failureMessage = $"Flashback export failed: input context was not available during {operation}.";
            return false;
        }

        var nativeStreamCount = inputContext->nb_streams;
        if (nativeStreamCount == 0)
        {
            failureMessage = $"Flashback export failed: input had no streams during {operation}.";
            return false;
        }

        if (nativeStreamCount > MaxSupportedInputStreams)
        {
            failureMessage = $"Flashback export failed: input stream count {nativeStreamCount} exceeds supported maximum {MaxSupportedInputStreams} during {operation}.";
            return false;
        }

        streamCount = (int)nativeStreamCount;
        failureMessage = string.Empty;
        return true;
    }

    private static void LogInputStreams(AVFormatContext* inputContext, int inputStreamCount)
    {
        for (var si = 0; si < inputStreamCount; si++)
        {
            var inStr = inputContext->streams[si];
            var codecId = inStr->codecpar->codec_id;
            var codecType = inStr->codecpar->codec_type;
            Logger.Log($"FLASHBACK_EXPORT_INPUT_STREAM idx={si} type={codecType} codec_id={codecId} " +
                $"w={inStr->codecpar->width} h={inStr->codecpar->height} " +
                $"extradata_size={inStr->codecpar->extradata_size} " +
                $"sample_rate={inStr->codecpar->sample_rate} channels={inStr->codecpar->ch_layout.nb_channels}");
        }
    }

    private static void LogTimestampBaseDrift(long[] timestampBasesUs, bool[] hasTimestampBase)
    {
        // All values are already in microseconds - find min/max to detect drift.
        long? minUs = null;
        long? maxUs = null;

        for (var i = 0; i < timestampBasesUs.Length; i++)
        {
            if (!hasTimestampBase[i])
            {
                continue;
            }

            var baseUs = timestampBasesUs[i];
            if (minUs == null || baseUs < minUs.Value) minUs = baseUs;
            if (maxUs == null || baseUs > maxUs.Value) maxUs = baseUs;
        }

        if (minUs == null || maxUs == null || minUs.Value == maxUs.Value)
        {
            return;
        }

        var driftUs = maxUs.Value - minUs.Value;
        if (driftUs > 100_000) // 100ms threshold
        {
            Logger.Log($"FLASHBACK_EXPORT_WARN reason='stream_base_drift' drift_us={driftUs}");
        }
    }

    private void OpenInput(string inputPath)
    {
        CloseActiveInput();

        AVFormatContext* inputContext = null;
        try
        {
            ThrowIfError(ffmpeg.avformat_open_input(&inputContext, inputPath, null, null), "avformat_open_input");

            // Increase probe size for TS segments that may start mid-stream.
            // H.264 TS segments from RotateOutput may not have SPS/PPS at the very start
            // (NVENC pipeline latency can push the first IDR several frames in).
            // Default probesize (5MB) may not be enough for 4K@120fps H.264 - increase
            // to 20MB so avformat_find_stream_info can find the first IDR and extract
            // video dimensions and extradata.
            inputContext->probesize = 20 * 1024 * 1024;
            inputContext->max_analyze_duration = 5 * ffmpeg.AV_TIME_BASE; // 5 seconds
        }
        catch
        {
            /* Cleanup must not throw - close partially-opened input before re-throwing */
            if (inputContext != null)
            {
                ffmpeg.avformat_close_input(&inputContext);
            }

            throw;
        }

        _activeInputContext = inputContext;
    }

    private void CreateOutputContext(string tmpPath, bool fastStart)
    {
        if (_activeOutputContext != null)
        {
            return;
        }

        AVFormatContext* outputContext = null;
        ThrowIfError(ffmpeg.avformat_alloc_output_context2(&outputContext, null, "mp4", tmpPath), "avformat_alloc_output_context2");
        if (outputContext == null)
        {
            throw new InvalidOperationException("FLASHBACK_EXPORT_ERROR operation=avformat_alloc_output_context2 msg='Output context allocation returned null.'");
        }

        _activeOutputContext = outputContext;
        _activeTempPath = tmpPath;

        if (fastStart)
        {
            Logger.Log($"FLASHBACK_EXPORT_MUX mode='faststart' path='{tmpPath}'");
        }
    }

    private static void OpenOutputIoAndWriteHeader(AVFormatContext* outputContext, string tmpPath, bool fastStart)
    {
        ThrowIfError(ffmpeg.avio_open2(&outputContext->pb, tmpPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2");

        AVDictionary* muxerOptions = null;
        try
        {
            if (fastStart)
            {
                ThrowIfError(ffmpeg.av_dict_set(&muxerOptions, "movflags", "+faststart", 0), "av_dict_set(movflags)");
            }

            ThrowIfError(ffmpeg.avformat_write_header(outputContext, &muxerOptions), "avformat_write_header");
        }
        finally
        {
            ffmpeg.av_dict_free(&muxerOptions);
        }
    }

    private const int ExportWriterYieldPacketInterval = 256;
    private const int ExportWriterThrottlePacketInterval = 4096;
    private const int ExportWriterThrottleSleepMs = 1;
    private const int ExportWriterAdaptiveThrottlePacketInterval = 4;
    private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;
    private static readonly AVRational SingleFilePacketUsTimeBase = new() { num = 1, den = 1_000_000 };
    private const int SingleFileMaxBufferedPackets = 600;

    [ThreadStatic]
    private static Func<int>? s_adaptiveThrottleDelayMsProvider;

    private readonly object _adaptiveThrottleSync = new();
    private Func<int>? _nextAdaptiveThrottleDelayMsProvider;

    /// <summary>
    /// Exports a flashback range to .mp4 based on the request parameters.
    /// Uses multi-segment export when <see cref="FlashbackExportRequest.Segments"/> or
    /// <see cref="FlashbackExportRequest.SegmentPaths"/> is set,
    /// otherwise falls back to single-file export from <see cref="FlashbackExportRequest.InputTsPath"/>.
    /// </summary>
    public Task<FinalizeResult> ExportAsync(
        FlashbackExportRequest request,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (request == null)
        {
            return Task.FromResult(FinalizeResult.Failure(
                string.Empty,
                "Flashback export failed: request is required."));
        }

        lock (_lifetimeSync)
        {
            if (_disposed)
            {
                return Task.FromResult(CreateDisposedExportResult(request.OutputPath));
            }
        }

        if (request.Segments is { Count: > 0 })
        {
            SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
            return ExportSegmentsAsync(request.Segments, request.InPoint, request.OutPoint,
                request.OutputPath, request.FastStart, request.Force, progress, ct);
        }

        if (request.SegmentPaths is { Count: > 0 })
        {
            SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
            return ExportSegmentsAsync(
                request.SegmentPaths.Select(path => new FlashbackExportSegment { Path = path }).ToArray(),
                request.InPoint,
                request.OutPoint,
                request.OutputPath,
                request.FastStart,
                request.Force,
                progress,
                ct);
        }

        SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
        return ExportSingleAsync(request.InputTsPath!, request.InPoint, request.OutPoint,
            request.OutputPath, request.FastStart, request.Force, progress, ct);
    }

    /// <summary>
    /// Exports a time range from the flashback .ts file to an .mp4 file.
    /// Seeks to the nearest keyframe before <paramref name="inPoint"/> and copies packets
    /// until <paramref name="outPoint"/> is reached.
    /// </summary>
    private Task<FinalizeResult> ExportSingleAsync(
        string inputTsPath,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        bool allowOverwrite,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        CancellationTokenSource linkedCts;
        try
        {
            linkedCts = CreateExportCancellationSource(ct);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(CreateDisposedExportResult(outputPath));
        }

        var adaptiveThrottleDelayMsProvider = ConsumeNextAdaptiveThrottleDelayProvider();
        return Task.Run(() =>
        {
            return RunWithBackgroundPriority(
                () => RunWithAdaptiveThrottle(
                    adaptiveThrottleDelayMsProvider,
                    () => ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),
                () => DisposeLinkedCtsBestEffort(linkedCts, "single_export"));
        });
    }

    private FinalizeResult ExportCore(
        string inputTsPath,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        bool allowOverwrite,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return CreateCancelledExportResult(outputPath);
        }

        if (string.IsNullOrWhiteSpace(inputTsPath) || !File.Exists(inputTsPath))
        {
            var message = $"Flashback export failed: input file not found '{inputTsPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{rangeFailure}'");
            return FinalizeResult.Failure(outputPath, rangeFailure);
        }

        if (!TryValidateOutputPath(outputPath, out var normalizedOutputPath, out var outputPathFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'");
            return FinalizeResult.Failure(outputPath, outputPathFailure);
        }
        outputPath = normalizedOutputPath;

        if (IsSamePath(inputTsPath, outputPath))
        {
            var message = $"Flashback export failed: output path must not overwrite source segment '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryValidateDestinationDoesNotExist(outputPath, out var destinationFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{destinationFailure}'");
            return FinalizeResult.Failure(outputPath, destinationFailure);
        }

        if (!TryWaitForExportLock(outputPath, ct, out var cancellationResult))
        {
            return cancellationResult;
        }

        try
        {
            TempOutputLease? tempLease = null;

            try
            {
                if (!TryCreateUniqueTempOutputPath(outputPath, out tempLease, out var tempOutputFailure))
                {
                    Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{tempOutputFailure}'");
                    return FinalizeResult.Failure(outputPath, tempOutputFailure);
                }

                _activeTempPath = tempLease.Path;
                if (IsSamePath(inputTsPath, tempLease.Path))
                {
                    var message = $"Flashback export failed: temporary output path must not overwrite source segment '{tempLease.Path}'.";
                    Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                    return FinalizeResult.Failure(outputPath, message);
                }

                LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

                Logger.Log($"FLASHBACK_EXPORT_START input='{inputTsPath}' in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");
                ReportProgress(progress, new ExportProgress(0, 1, 0), "single_start");

                OpenInput(inputTsPath);
                ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
                if (!TryGetInputStreamCount(_activeInputContext, "single_export", out var streamCount, out var streamCountFailure))
                {
                    Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'");
                    return FinalizeResult.Failure(outputPath, streamCountFailure);
                }

                if (inPoint > TimeSpan.Zero)
                {
                    var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);
                    var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                    if (seekResult < 0)
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                    }
                }

                CreateOutputContext(tempLease.Path, fastStart);
                var videoStreamIndex = FindVideoStreamIndex(_activeInputContext);
                var streamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount);
                OpenOutputIoAndWriteHeader(_activeOutputContext, tempLease.Path, fastStart);

                var packetWriteResult = WriteSingleFilePacketsToActiveOutput(
                    streamCount,
                    videoStreamIndex,
                    streamMap,
                    outPoint,
                    outputPath,
                    progress,
                    ct);
                if (packetWriteResult.Failure != null)
                {
                    return packetWriteResult.Failure;
                }

                var totalPackets = packetWriteResult.TotalPackets;
                if (!TryFinalizeActiveOutputFile(tempLease, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
                {
                    return FinalizeResult.Failure(outputPath, outputFailure);
                }

                Logger.Log(
                    $"FLASHBACK_EXPORT_OK output='{outputPath}' packets={totalPackets} bytes={outputBytes}");
                ReportProgress(progress, new ExportProgress(1, 1, 100.0), "single_complete");
                return FinalizeResult.Success(outputPath, $"Exported {totalPackets} packets from .ts");
            }
            catch (OperationCanceledException)
            {
                const string message = "Flashback export cancelled.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }
            catch (Exception ex)
            {
                var message = $"Flashback export failed: {ex.Message}";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }
            finally
            {
                CleanupNativeState();
                if (tempLease != null)
                {
                    DeleteTempFileIfPresent(tempLease);
                }
                _activeTempPath = null;
            }
        }
        finally
        {
            ReleaseExportLockBestEffort("single_export");
        }
    }

    private static SingleFilePacketWriteState CreateSingleFilePacketWriteState(int streamCount)
        => new(
            new long[streamCount],
            new bool[streamCount],
            new long[streamCount],
            new List<IntPtr>(),
            new List<int>());

    private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        TimeSpan outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var packetState = CreateSingleFilePacketWriteState(streamCount);
        WriteSingleFilePacketReadLoop(
            streamCount,
            videoStreamIndex,
            streamMap,
            ToAvTimeBaseTimestampOrMax(outPoint),
            progress,
            ct,
            ref packetState);

        LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);

        if (videoStreamIndex >= 0 && videoStreamIndex < streamCount && packetState.PacketCounts[videoStreamIndex] == 0)
        {
            const string message = "Flashback export failed: no video packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        for (var i = 0; i < streamCount; i++)
        {
            if (i != videoStreamIndex && packetState.HasTimestampBase[i] && packetState.PacketCounts[i] == 0)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN stream={i} reason='no_packets_written' (non-video stream)");
            }
        }

        if (packetState.TotalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        return new SingleFilePacketWriteResult(null, packetState.TotalPackets);
    }

    private void WriteSingleFilePacketReadLoop(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        long outPtsLimit,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        ref SingleFilePacketWriteState packetState)
    {
        var packet = ffmpeg.av_packet_alloc();
        if (packet == null)
        {
            throw new InvalidOperationException("Failed to allocate AVPacket.");
        }

        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);
                if (readResult == ffmpeg.AVERROR_EOF)
                {
                    break;
                }

                ThrowIfError(readResult, "av_read_frame");
                ReportSingleFileProgressHeartbeat(progress, ref packetState);

                try
                {
                    var streamIndex = packet->stream_index;
                    if (streamIndex < 0 || streamIndex >= streamCount)
                    {
                        continue;
                    }

                    var outputIndex = streamMap[streamIndex];
                    if (outputIndex < 0)
                    {
                        continue;
                    }

                    var inputStream = _activeInputContext->streams[streamIndex];
                    var outputStream = _activeOutputContext->streams[outputIndex];
                    var pastOutPoint = PacketPtsExceedsSingleFileOutPoint(packet, inputStream, outPtsLimit);
                    if (pastOutPoint && streamIndex == videoStreamIndex)
                    {
                        break;
                    }

                    if (pastOutPoint)
                    {
                        continue;
                    }

                    ffmpeg.av_packet_rescale_ts(packet, inputStream->time_base, outputStream->time_base);

                    if (!packetState.HasTimestampBase[streamIndex] &&
                        !TryRecordSingleFileTimestampBase(ref packetState, packet, streamIndex, outputStream))
                    {
                        continue;
                    }

                    if (!packetState.AllBasesDiscovered)
                    {
                        BufferSingleFilePacketOrFlushReady(streamCount, streamMap, streamIndex, ref packetState, packet);
                        continue;
                    }

                    WriteSingleFilePacket(packet, streamIndex, outputStream, ref packetState);
                }
                finally
                {
                    ffmpeg.av_packet_unref(packet);
                }
            }

            FlushSingleFileBufferedPacketsAtEof(streamCount, streamMap, ref packetState);
        }
        finally
        {
            FreeBufferedPackets(packetState.BufferedPackets, packetState.BufferedStreamIndices);
            var packetToFree = packet;
            ffmpeg.av_packet_free(&packetToFree);
        }
    }

    private static bool TryRecordSingleFileTimestampBase(
        ref SingleFilePacketWriteState state,
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream)
    {
        if (!TryResolveTimestampBase(packet, out var timestampBase))
        {
            return false;
        }

        var baseUs = ffmpeg.av_rescale_q(timestampBase, outputStream->time_base, SingleFilePacketUsTimeBase);
        state.TimestampBasesUs[streamIndex] = baseUs;
        state.HasTimestampBase[streamIndex] = true;
        if (state.GlobalMinBaseUs == null || baseUs < state.GlobalMinBaseUs.Value)
        {
            state.GlobalMinBaseUs = baseUs;
        }

        return true;
    }

    private void BufferSingleFilePacketOrFlushReady(
        int streamCount,
        int[] streamMap,
        int streamIndex,
        ref SingleFilePacketWriteState state,
        AVPacket* packet)
    {
        var clone = ClonePacketOrThrow(packet, "single_buffer");
        state.BufferedPackets.Add((IntPtr)clone);
        state.BufferedStreamIndices.Add(streamIndex);

        state.AllBasesDiscovered = HasDiscoveredAllMappedSingleFileBases(in state, streamCount, streamMap);
        if (!state.AllBasesDiscovered && state.BufferedPackets.Count >= SingleFileMaxBufferedPackets)
        {
            state.AllBasesDiscovered = true;
            state.GlobalMinBaseUs ??= 0;
            Logger.Log(
                $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH buffered={state.BufferedPackets.Count} streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount}");
        }

        if (state.AllBasesDiscovered)
        {
            state.TotalPackets += FlushBufferedPackets(
                state.BufferedPackets,
                state.BufferedStreamIndices,
                streamMap,
                state.GlobalMinBaseUs!.Value,
                SingleFilePacketUsTimeBase,
                state.PacketCounts);
        }
    }

    private void FlushSingleFileBufferedPacketsAtEof(
        int streamCount,
        int[] streamMap,
        ref SingleFilePacketWriteState state)
    {
        if (state.AllBasesDiscovered || state.BufferedPackets.Count == 0)
        {
            return;
        }

        state.GlobalMinBaseUs ??= 0;
        Logger.Log(
            $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount} buffered={state.BufferedPackets.Count}");
        state.TotalPackets += FlushBufferedPackets(
            state.BufferedPackets,
            state.BufferedStreamIndices,
            streamMap,
            state.GlobalMinBaseUs!.Value,
            SingleFilePacketUsTimeBase,
            state.PacketCounts);
    }

    private void WriteSingleFilePacket(
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream,
        ref SingleFilePacketWriteState state)
    {
        var baseTs = ffmpeg.av_rescale_q(state.GlobalMinBaseUs!.Value, SingleFilePacketUsTimeBase, outputStream->time_base);
        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->pts -= baseTs;
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->dts -= baseTs;
        }

        NormalizePacketTimestampsBeforeWrite(packet);
        packet->pos = -1;
        packet->stream_index = outputStream->index;

        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
        state.PacketCounts[streamIndex]++;
        state.TotalPackets++;
        ThrottleExportWriterIfNeeded(state.TotalPackets);
    }

    private static bool PacketPtsExceedsSingleFileOutPoint(
        AVPacket* packet,
        AVStream* inputStream,
        long outPtsLimit)
    {
        if (packet->pts == ffmpeg.AV_NOPTS_VALUE || outPtsLimit >= long.MaxValue)
        {
            return false;
        }

        var ptsUs = ffmpeg.av_rescale_q(packet->pts, inputStream->time_base, SingleFilePacketUsTimeBase);
        return ptsUs > outPtsLimit;
    }

    private static void ReportSingleFileProgressHeartbeat(
        IProgress<ExportProgress>? progress,
        ref SingleFilePacketWriteState state)
    {
        if (ShouldReportProgressHeartbeat(ref state.LastProgressHeartbeatTick))
        {
            ReportProgress(progress, new ExportProgress(0, 1, 0), "single_heartbeat");
        }
    }

    private static bool HasDiscoveredAllMappedSingleFileBases(
        in SingleFilePacketWriteState state,
        int streamCount,
        int[] streamMap)
    {
        for (var i = 0; i < streamCount; i++)
        {
            if (streamMap[i] >= 0 && !state.HasTimestampBase[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int CountDiscoveredSingleFileBases(in SingleFilePacketWriteState state, int streamCount)
    {
        var discoveredCount = 0;
        for (var i = 0; i < streamCount; i++)
        {
            if (state.HasTimestampBase[i])
            {
                discoveredCount++;
            }
        }

        return discoveredCount;
    }

    private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);

    private struct SingleFilePacketWriteState
    {
        public SingleFilePacketWriteState(
            long[] timestampBasesUs,
            bool[] hasTimestampBase,
            long[] packetCounts,
            List<IntPtr> bufferedPackets,
            List<int> bufferedStreamIndices)
        {
            TimestampBasesUs = timestampBasesUs;
            HasTimestampBase = hasTimestampBase;
            PacketCounts = packetCounts;
            BufferedPackets = bufferedPackets;
            BufferedStreamIndices = bufferedStreamIndices;
        }

        public long[] TimestampBasesUs { get; }
        public bool[] HasTimestampBase { get; }
        public long[] PacketCounts { get; }
        public List<IntPtr> BufferedPackets { get; }
        public List<int> BufferedStreamIndices { get; }
        public long? GlobalMinBaseUs { get; set; }
        public bool AllBasesDiscovered { get; set; }
        public long TotalPackets { get; set; }
        public long LastProgressHeartbeatTick;
    }

    /// <summary>
    /// Exports a time range spanning multiple .ts segment files to a single .mp4 file.
    /// Opens segments sequentially, remapping PTS for continuous output.
    /// </summary>
    private Task<FinalizeResult> ExportSegmentsAsync(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        bool allowOverwrite,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var segmentSnapshot = SnapshotSegments(segments);
        CancellationTokenSource linkedCts;
        try
        {
            linkedCts = CreateExportCancellationSource(ct);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(CreateDisposedExportResult(outputPath));
        }

        var adaptiveThrottleDelayMsProvider = ConsumeNextAdaptiveThrottleDelayProvider();
        return Task.Run(() =>
        {
            return RunWithBackgroundPriority(
                () => RunWithAdaptiveThrottle(
                    adaptiveThrottleDelayMsProvider,
                    () => ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),
                () => DisposeLinkedCtsBestEffort(linkedCts, "segment_export"));
        });
    }

    private static FinalizeResult RunWithBackgroundPriority(Func<FinalizeResult> exportWork, Action cleanup)
    {
        var thread = Thread.CurrentThread;
        var previousPriority = thread.Priority;
        try
        {
            thread.Priority = ThreadPriority.BelowNormal;
            return exportWork();
        }
        finally
        {
            try
            {
                thread.Priority = previousPriority;
            }
            catch
            {
                // Best effort: thread-pool priority restore should not mask export cleanup.
            }

            cleanup();
        }
    }

    private static IReadOnlyList<FlashbackExportSegment> SnapshotSegments(IReadOnlyList<FlashbackExportSegment>? segments)
    {
        if (segments == null || segments.Count == 0)
        {
            return Array.Empty<FlashbackExportSegment>();
        }

        var snapshot = new FlashbackExportSegment[segments.Count];
        for (var i = 0; i < snapshot.Length; i++)
        {
            var segment = segments[i];
            snapshot[i] = segment == null
                ? new FlashbackExportSegment { Path = string.Empty }
                : segment with { };
        }

        return snapshot;
    }

    private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)
    {
        value = NormalizeExportProgress(value, stage);
        try
        {
            progress?.Report(value);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_PROGRESS_WARN stage={stage} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static ExportProgress NormalizeExportProgress(ExportProgress value, string stage)
    {
        var totalSegments = Math.Max(0, value.TotalSegments);
        var segmentsProcessed = Math.Max(0, value.SegmentsProcessed);
        if (totalSegments > 0 && segmentsProcessed > totalSegments)
        {
            segmentsProcessed = totalSegments;
        }

        var percent = double.IsFinite(value.Percent)
            ? Math.Clamp(value.Percent, 0.0, 100.0)
            : 0.0;

        if (segmentsProcessed != value.SegmentsProcessed ||
            totalSegments != value.TotalSegments ||
            percent != value.Percent)
        {
            Logger.Log(
                $"FLASHBACK_EXPORT_PROGRESS_NORMALIZED stage={stage} " +
                $"raw_segments={value.SegmentsProcessed}/{value.TotalSegments} " +
                $"segments={segmentsProcessed}/{totalSegments} " +
                $"raw_percent={value.Percent:0.###} percent={percent:0.###}");
        }

        return new ExportProgress(segmentsProcessed, totalSegments, percent);
    }

    private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)
    {
        var now = Stopwatch.GetTimestamp();
        var last = lastHeartbeatTick;
        if (last != 0 &&
            (now - last) * 1000.0 / Stopwatch.Frequency < ProgressHeartbeatIntervalMs)
        {
            return false;
        }

        lastHeartbeatTick = now;
        return true;
    }

    private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)
    {
        lock (_adaptiveThrottleSync)
        {
            _nextAdaptiveThrottleDelayMsProvider = adaptiveThrottleDelayMsProvider;
        }
    }

    private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()
    {
        lock (_adaptiveThrottleSync)
        {
            var provider = _nextAdaptiveThrottleDelayMsProvider;
            _nextAdaptiveThrottleDelayMsProvider = null;
            return provider;
        }
    }

    private static FinalizeResult RunWithAdaptiveThrottle(
        Func<int>? adaptiveThrottleDelayMsProvider,
        Func<FinalizeResult> exportWork)
    {
        var previousProvider = s_adaptiveThrottleDelayMsProvider;
        try
        {
            s_adaptiveThrottleDelayMsProvider = adaptiveThrottleDelayMsProvider;
            return exportWork();
        }
        finally
        {
            s_adaptiveThrottleDelayMsProvider = previousProvider;
        }
    }

    private static void ThrottleExportWriterIfNeeded(long packetsWritten)
    {
        if (packetsWritten <= 0)
        {
            return;
        }

        var adaptiveThrottleDelayMsProvider = s_adaptiveThrottleDelayMsProvider;
        if (adaptiveThrottleDelayMsProvider != null &&
            packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0)
        {
            var adaptiveDelayMs = Math.Clamp(
                adaptiveThrottleDelayMsProvider(),
                0,
                ExportWriterMaxAdaptiveThrottleSleepMs);
            if (adaptiveDelayMs > 0)
            {
                Thread.Sleep(adaptiveDelayMs);
                return;
            }
        }

        if (packetsWritten % ExportWriterThrottlePacketInterval == 0)
        {
            Thread.Sleep(ExportWriterThrottleSleepMs);
            return;
        }

        if (packetsWritten % ExportWriterYieldPacketInterval == 0)
        {
            Thread.Yield();
        }
    }

    private static void DeleteTempFileIfPresent(string tmpPath)
    {
        const int MaxRetries = 3;
        const int RetryDelayMs = 200;
        const int SharingViolationHResult = 32;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                }
                return;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == SharingViolationHResult && attempt < MaxRetries)
            {
                // Sharing violation (file locked by another process / AV scanner). Retry after back-off.
                Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{tmpPath}' type={ex.GetType().Name} msg='{ex.Message}'");
                return;
            }
        }

        Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed_sharing_violation' path='{tmpPath}'");
    }

    private static void DeleteTempFileIfPresent(TempOutputLease tempLease)
    {
        const int MaxRetries = 3;
        const int RetryDelayMs = 200;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (!File.Exists(tempLease.Path))
            {
                tempLease.Dispose();
                return;
            }

            if (TryDeleteTempLeaseFile(tempLease, out var failureMessage, out var lastError))
            {
                Logger.Log($"FLASHBACK_EXPORT_TMP_DELETE path='{tempLease.Path}'");
                return;
            }

            if (lastError == ErrorSharingViolation && attempt < MaxRetries)
            {
                Thread.Sleep(RetryDelayMs);
                continue;
            }

            Logger.Log(
                $"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{tempLease.Path}' " +
                $"win32={lastError} msg='{failureMessage}'");
            return;
        }

        Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed_sharing_violation' path='{tempLease.Path}'");
    }

    private static bool TryCreateUniqueTempOutputPath(string outputPath, out TempOutputLease tempLease, out string failureMessage)
    {
        tempLease = null!;
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            failureMessage = $"Flashback export failed: output directory does not exist for '{outputPath}'.";
            return false;
        }

        var outputBaseName = Path.GetFileNameWithoutExtension(outputPath);
        if (string.IsNullOrWhiteSpace(outputBaseName))
        {
            outputBaseName = "flashback_export";
        }

        if (outputBaseName.Length > 80)
        {
            outputBaseName = outputBaseName[..80];
        }

        for (var attempt = 0; attempt < TempOutputCreationAttempts; attempt++)
        {
            var candidate = Path.Combine(
                outputDirectory,
                $"{outputBaseName}.{Guid.NewGuid():N}.mp4.tmp");
            if (Directory.Exists(candidate))
            {
                continue;
            }

            try
            {
                var reservationStream = new FileStream(
                    candidate,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                if (!TryGetFileIdentity(reservationStream.SafeFileHandle, out var identity, out var identityFailure, out var identityError))
                {
                    reservationStream.Dispose();
                    failureMessage = $"Flashback export failed: could not identify temporary output file before writing '{outputPath}'.";
                    Logger.Log($"FLASHBACK_EXPORT_TMP_IDENTITY_WARN path='{candidate}' win32={identityError} msg='{identityFailure}'");
                    return false;
                }

                tempLease = new TempOutputLease(candidate, identity, reservationStream);
                failureMessage = string.Empty;
                return true;
            }
            catch (IOException ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_TMP_CREATE_RETRY path='{candidate}' attempt={attempt + 1} type={ex.GetType().Name} msg='{ex.Message}'");
            }
            catch (Exception ex)
            {
                failureMessage = $"Flashback export failed: could not create temporary output file before writing '{outputPath}'.";
                Logger.Log($"FLASHBACK_EXPORT_TMP_CREATE_WARN path='{candidate}' type={ex.GetType().Name} msg='{ex.Message}'");
                return false;
            }
        }

        failureMessage = $"Flashback export failed: could not create a unique temporary output file before writing '{outputPath}'.";
        return false;
    }

    private static bool TryOpenExistingTempOutputLease(string tmpPath, out TempOutputLease tempLease, out string failureMessage)
    {
        tempLease = null!;

        FileStream reservationStream;
        try
        {
            reservationStream = new FileStream(
                tmpPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }
        catch (Exception ex)
        {
            failureMessage = $"Flashback export failed: temporary output file was not created: '{tmpPath}' ({ex.GetType().Name}: {ex.Message}).";
            return false;
        }

        if (!TryGetFileIdentity(reservationStream.SafeFileHandle, out var identity, out var identityFailure, out var identityError))
        {
            reservationStream.Dispose();
            failureMessage = $"Flashback export failed: could not identify temporary output file '{tmpPath}' (Win32 error {identityError}: {identityFailure}).";
            return false;
        }

        tempLease = new TempOutputLease(tmpPath, identity, reservationStream);
        failureMessage = string.Empty;
        return true;
    }

    private static bool TryOpenVerifiedTempLeaseHandle(
        TempOutputLease tempLease,
        out SafeFileHandle? handle,
        out string failureMessage,
        out int lastError)
    {
        tempLease.ReleaseReservation();
        handle = CreateFile(
            tempLease.Path,
            NativeDeleteAccess | NativeFileReadAttributes,
            NativeFileShareRead | NativeFileShareWrite,
            IntPtr.Zero,
            NativeOpenExisting,
            NativeFileAttributeNormal,
            IntPtr.Zero);
        if (handle.IsInvalid)
        {
            lastError = Marshal.GetLastWin32Error();
            handle.Dispose();
            handle = null;
            failureMessage = IsPathMissingError(lastError)
                ? string.Empty
                : $"Flashback export failed: could not lock temporary output file '{tempLease.Path}' safely (Win32 error {lastError}).";
            return false;
        }

        if (!TryGetFileIdentity(handle, out var currentIdentity, out var identityFailure, out var identityError))
        {
            lastError = identityError;
            handle.Dispose();
            handle = null;
            failureMessage = $"Flashback export failed: could not verify temporary output file identity for '{tempLease.Path}' ({identityFailure}).";
            return false;
        }

        if (currentIdentity != tempLease.Identity)
        {
            lastError = 0;
            handle.Dispose();
            handle = null;
            failureMessage = $"Flashback export failed: temporary output path was replaced before finalizing '{tempLease.Path}'.";
            return false;
        }

        lastError = 0;
        failureMessage = string.Empty;
        return true;
    }

    private static bool TryGetFileIdentity(
        SafeFileHandle handle,
        out TempFileIdentity identity,
        out string failureMessage,
        out int lastError)
    {
        identity = default;
        if (handle.IsInvalid || handle.IsClosed)
        {
            lastError = 0;
            failureMessage = "invalid file handle";
            return false;
        }

        if (!GetFileInformationByHandle(handle, out var info))
        {
            lastError = Marshal.GetLastWin32Error();
            failureMessage = $"GetFileInformationByHandle failed with Win32 error {lastError}";
            return false;
        }

        identity = TempFileIdentity.From(in info);
        lastError = 0;
        failureMessage = string.Empty;
        return true;
    }

    private static bool TryDeleteTempLeaseFile(TempOutputLease tempLease, out string failureMessage, out int lastError)
    {
        if (!TryOpenVerifiedTempLeaseHandle(tempLease, out var handle, out failureMessage, out lastError))
        {
            return IsPathMissingError(lastError);
        }

        using (handle)
        {
            var disposition = Marshal.AllocHGlobal(1);
            try
            {
                Marshal.WriteByte(disposition, 1);
                if (SetFileInformationByHandle(handle!, FileInformationClass.FileDispositionInfo, disposition, 1))
                {
                    failureMessage = string.Empty;
                    lastError = 0;
                    return true;
                }

                lastError = Marshal.GetLastWin32Error();
                failureMessage = $"SetFileInformationByHandle(FileDispositionInfo) failed with Win32 error {lastError}";
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(disposition);
            }
        }
    }

    private static bool IsPathMissingError(int lastError)
        => lastError == ErrorFileNotFound || lastError == ErrorPathNotFound;

    private static bool IsDestinationExistsError(int lastError)
        => lastError == ErrorFileExists || lastError == ErrorAlreadyExists;

    internal static void CleanupOrphanedTempFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        try
        {
            var nowUtc = DateTime.UtcNow;
            foreach (var tmpFile in Directory.EnumerateFiles(directory, "*.mp4.tmp"))
            {
                try
                {
                    if (!CanDeleteOrphanedTempFile(tmpFile, nowUtc))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP_SKIP file='{Path.GetFileName(tmpFile)}' reason=active_or_recent");
                        continue;
                    }

                    File.Delete(tmpFile);
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP deleted='{Path.GetFileName(tmpFile)}'");
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP_FAIL path='{Path.GetFileName(tmpFile)}' type={ex.GetType().Name} msg='{ex.Message}'");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_ORPHAN_SCAN_FAIL dir='{directory}' type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static bool CanDeleteOrphanedTempFile(string tmpFile, DateTime nowUtc)
    {
        var lastWriteUtc = File.GetLastWriteTimeUtc(tmpFile);
        if (lastWriteUtc == DateTime.MinValue || nowUtc - lastWriteUtc < OrphanTempFileMinimumAge)
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void MoveTempFileToOutputPath(TempOutputLease tempLease, string outputPath)
    {
        if (!TryMoveTempFileToOutputPath(tempLease, outputPath, out var failureMessage))
        {
            DeleteTempFileIfPresent(tempLease);
            throw new IOException(failureMessage);
        }
    }

    private static bool TryMoveTempFileToOutputPath(
        TempOutputLease tempLease,
        string outputPath,
        out string failureMessage)
    {
        if (!File.Exists(tempLease.Path))
        {
            failureMessage = $"Temporary export file was not created: '{tempLease.Path}'.";
            return false;
        }

        if (!TryOpenVerifiedTempLeaseHandle(tempLease, out var handle, out var ownershipFailure, out var lastError))
        {
            failureMessage = string.IsNullOrWhiteSpace(ownershipFailure)
                ? $"Temporary export file was not created: '{tempLease.Path}'."
                : ownershipFailure;
            return false;
        }

        using (handle)
        {
            if (File.Exists(outputPath) || Directory.Exists(outputPath))
            {
                failureMessage = CreateDestinationExistsMessage(outputPath);
                return false;
            }

            if (TryRenameTempHandle(handle!, outputPath, out var renameFailure, out lastError))
            {
                failureMessage = string.Empty;
                return true;
            }

            failureMessage =
                IsDestinationExistsError(lastError) || File.Exists(outputPath) || Directory.Exists(outputPath)
                    ? CreateDestinationExistsMessage(outputPath)
                    : $"Flashback export failed: could not move temporary output file to '{outputPath}' safely ({renameFailure}).";
            return false;
        }
    }

    private static bool TryRenameTempHandle(
        SafeFileHandle handle,
        string outputPath,
        out string failureMessage,
        out int lastError)
    {
        var renameInfo = CreateFileRenameInfo(outputPath, replaceIfExists: false, out var renameInfoSize);
        try
        {
            if (SetFileInformationByHandle(handle, FileInformationClass.FileRenameInfo, renameInfo, (uint)renameInfoSize))
            {
                failureMessage = string.Empty;
                lastError = 0;
                return true;
            }

            lastError = Marshal.GetLastWin32Error();
            failureMessage = $"SetFileInformationByHandle(FileRenameInfo) failed with Win32 error {lastError}";
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(renameInfo);
        }
    }

    private static IntPtr CreateFileRenameInfo(string outputPath, bool replaceIfExists, out int bufferSize)
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputPathBytes = Encoding.Unicode.GetBytes(fullOutputPath);
        var rootDirectoryOffset = IntPtr.Size == 8 ? 8 : 4;
        var fileNameLengthOffset = rootDirectoryOffset + IntPtr.Size;
        var fileNameOffset = fileNameLengthOffset + sizeof(int);
        bufferSize = fileNameOffset + outputPathBytes.Length + sizeof(char);

        var buffer = Marshal.AllocHGlobal(bufferSize);
        for (var i = 0; i < bufferSize; i++)
        {
            Marshal.WriteByte(buffer, i, 0);
        }

        Marshal.WriteInt32(buffer, 0, replaceIfExists ? 1 : 0);
        Marshal.WriteIntPtr(buffer, rootDirectoryOffset, IntPtr.Zero);
        Marshal.WriteInt32(buffer, fileNameLengthOffset, outputPathBytes.Length);
        Marshal.Copy(outputPathBytes, 0, IntPtr.Add(buffer, fileNameOffset), outputPathBytes.Length);
        return buffer;
    }

    private static bool TryFinalizeTempOutputFile(
        string tmpPath,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage)
        => TryFinalizeTempOutputFileCore(
            tmpPath,
            outputPath,
            allowOverwrite,
            out outputBytes,
            out failureMessage,
            TryValidateCompletedOutputFile);

    private bool TryFinalizeActiveOutputFile(
        TempOutputLease tempLease,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage)
    {
        ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
        CloseOutputIo();

        if (!TryFinalizeTempOutputLeaseFile(tempLease, outputPath, allowOverwrite, out outputBytes, out failureMessage))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{failureMessage}'");
            return false;
        }

        _activeTempPath = null;
        return true;
    }

    private static bool TryFinalizeTempOutputFileCore(
        string tmpPath,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage,
        CompletedOutputValidator validateOutput)
    {
        if (!TryOpenExistingTempOutputLease(tmpPath, out var tempLease, out failureMessage))
        {
            outputBytes = 0;
            return false;
        }

        using (tempLease)
        {
            return TryFinalizeTempOutputLeaseFileCore(
                tempLease,
                outputPath,
                allowOverwrite,
                out outputBytes,
                out failureMessage,
                validateOutput);
        }
    }

    private static bool TryFinalizeTempOutputLeaseFile(
        TempOutputLease tempLease,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage)
        => TryFinalizeTempOutputLeaseFileCore(
            tempLease,
            outputPath,
            allowOverwrite,
            out outputBytes,
            out failureMessage,
            TryValidateCompletedOutputFile);

    private static bool TryFinalizeTempOutputLeaseFileCore(
        TempOutputLease tempLease,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage,
        CompletedOutputValidator validateOutput)
    {
        _ = allowOverwrite;

        if (!validateOutput(tempLease.Path, out outputBytes, out _))
        {
            failureMessage = outputBytes == 0
                ? $"Flashback export failed: temporary output file is empty before replacing '{outputPath}'."
                : $"Flashback export failed: temporary output file length unavailable before replacing '{outputPath}'.";
            DeleteTempFileIfPresent(tempLease);
            return false;
        }

        try
        {
            MoveTempFileToOutputPath(tempLease, outputPath);
        }
        catch (IOException ex)
        {
            failureMessage = ex.Message;
            return false;
        }

        if (!validateOutput(outputPath, out outputBytes, out failureMessage))
        {
            Logger.Log($"FLASHBACK_EXPORT_FINAL_OUTPUT_VALIDATE_WARN path='{outputPath}' reason='{failureMessage}'");
            return false;
        }

        return true;
    }

    private static bool TryValidateSegmentExportInputs(
        IReadOnlyList<FlashbackExportSegment>? segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        out string normalizedOutputPath,
        out FinalizeResult? failure)
    {
        normalizedOutputPath = outputPath;
        failure = null;

        if (segments == null || segments.Count == 0)
        {
            const string message = "Flashback export failed: no segment paths provided.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            failure = FinalizeResult.Failure(outputPath, message);
            return false;
        }

        if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{rangeFailure}'");
            failure = FinalizeResult.Failure(outputPath, rangeFailure);
            return false;
        }

        var invalidSegmentIndex = FindInvalidSegmentPathIndex(segments);
        if (invalidSegmentIndex >= 0)
        {
            var message = $"Flashback export failed: segment path at index {invalidSegmentIndex} is empty.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            failure = FinalizeResult.Failure(outputPath, message);
            return false;
        }

        var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);
        if (duplicateSegmentIndex >= 0)
        {
            var message = $"Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            failure = FinalizeResult.Failure(outputPath, message);
            return false;
        }

        if (!TryValidateOutputPath(outputPath, out normalizedOutputPath, out var outputPathFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'");
            failure = FinalizeResult.Failure(outputPath, outputPathFailure);
            return false;
        }

        var fullOutputPath = normalizedOutputPath;
        if (segments.Any(segment => IsSamePath(segment.Path, fullOutputPath)))
        {
            var message = $"Flashback export failed: output path must not overwrite source segment '{fullOutputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            failure = FinalizeResult.Failure(fullOutputPath, message);
            return false;
        }

        if (!TryValidateDestinationDoesNotExist(fullOutputPath, out var destinationFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{destinationFailure}'");
            failure = FinalizeResult.Failure(fullOutputPath, destinationFailure);
            return false;
        }

        return true;
    }

    private static bool TryValidateDestinationDoesNotExist(string outputPath, out string failureMessage)
    {
        if (File.Exists(outputPath) || Directory.Exists(outputPath))
        {
            failureMessage = CreateDestinationExistsMessage(outputPath);
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static string CreateDestinationExistsMessage(string outputPath)
        => $"Flashback export failed: destination file already exists at '{outputPath}'. Choose a path that does not exist; Flashback export does not overwrite existing files.";

    private static bool TryEstimateSegmentExportReadableBytes(
        IReadOnlyList<FlashbackExportSegment> segments,
        string outputPath,
        out long totalEstimatedBytes,
        out FinalizeResult? failure)
    {
        totalEstimatedBytes = 0;
        failure = null;
        var readableSegmentCount = 0;

        foreach (var segment in segments)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(segment.Path) && File.Exists(segment.Path))
                {
                    var segmentLength = new FileInfo(segment.Path).Length;
                    readableSegmentCount++;
                    totalEstimatedBytes = AddNonNegativeSaturated(totalEstimatedBytes, segmentLength);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN path='{segment.Path}' type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }

        if (readableSegmentCount > 0)
        {
            return true;
        }

        var message = $"Flashback export failed: no readable segment files were available from {segments.Count} planned segments.";
        Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
        failure = FinalizeResult.Failure(outputPath, message);
        return false;
    }

    private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null || string.IsNullOrWhiteSpace(segments[i].Path))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)
    {
        for (var i = 1; i < segments.Count; i++)
        {
            for (var previous = 0; previous < i; previous++)
            {
                if (IsSamePath(segments[previous].Path, segments[i].Path))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static long GetFileLengthBestEffort(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_WARN reason='output_length_unavailable' path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return -1;
        }
    }

    private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)
    {
        outputBytes = GetFileLengthBestEffort(outputPath);
        if (outputBytes > 0)
        {
            failureMessage = string.Empty;
            return true;
        }

        failureMessage = outputBytes == 0
            ? $"Flashback export failed: output file is empty '{outputPath}'."
            : $"Flashback export failed: output file length unavailable '{outputPath}'.";
        return false;
    }

    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)
    {
        fullOutputPath = string.Empty;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            failureMessage = "Flashback export failed: output path is required.";
            return false;
        }

        try
        {
            fullOutputPath = Path.GetFullPath(outputPath);
        }
        catch (Exception ex)
        {
            failureMessage = $"Flashback export failed: output path is invalid '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_PATH_VALIDATE_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }

        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            failureMessage = $"Flashback export failed: output directory does not exist for '{outputPath}'.";
            return false;
        }

        if (Directory.Exists(fullOutputPath))
        {
            failureMessage = $"Flashback export failed: output path is a directory '{outputPath}'.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool SegmentOverlapsExportRange(
        FlashbackExportSegment segment,
        TimeSpan inPoint,
        TimeSpan outPoint)
    {
        if (!segment.StartPts.HasValue || !segment.EndPts.HasValue)
        {
            return true;
        }

        var segmentStart = segment.StartPts.Value;
        var segmentEnd = segment.EndPts.Value;
        if (segmentEnd < segmentStart)
        {
            segmentEnd = segmentStart;
        }

        return segmentEnd > inPoint && segmentStart < outPoint;
    }

    private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)
    {
        if (inPoint < TimeSpan.Zero)
        {
            failureMessage = "Flashback export failed: in point must not be negative.";
            return false;
        }

        if (outPoint != TimeSpan.MaxValue && outPoint <= inPoint)
        {
            failureMessage = "Flashback export failed: export range is empty or invalid.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    // Segment export packet remuxing: template initialization, per-segment loop
    // orchestration, and skip validation.
    private FinalizeResult ExportSegmentsCore(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        bool allowOverwrite,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return CreateCancelledExportResult(outputPath);
        }

        if (!TryValidateSegmentExportInputs(
                segments,
                inPoint,
                outPoint,
                outputPath,
                out var normalizedOutputPath,
                out var validationFailure))
        {
            return validationFailure!;
        }
        outputPath = normalizedOutputPath;

        if (!TryEstimateSegmentExportReadableBytes(
                segments,
                outputPath,
                out var totalEstimatedBytes,
                out var estimateFailure))
        {
            return estimateFailure!;
        }

        if (!TryWaitForExportLock(outputPath, ct, out var cancellationResult))
        {
            return cancellationResult;
        }

        try
        {
            TempOutputLease? tempLease = null;

            try
            {
                if (!TryCreateUniqueTempOutputPath(outputPath, out tempLease, out var tempOutputFailure))
                {
                    Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{tempOutputFailure}'");
                    return FinalizeResult.Failure(outputPath, tempOutputFailure);
                }

                _activeTempPath = tempLease.Path;
                if (segments.Any(segment => IsSamePath(segment.Path, tempLease.Path)))
                {
                    var message = $"Flashback export failed: temporary output path must not overwrite source segment '{tempLease.Path}'.";
                    Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                    return FinalizeResult.Failure(outputPath, message);
                }

                LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

                Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_START segments={segments.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");
                ReportProgress(progress, new ExportProgress(0, segments.Count, 0), "segments_start");

                var packetWriteResult = WriteSegmentPacketsToActiveOutput(
                    segments,
                    inPoint,
                    outPoint,
                    tempLease.Path,
                    outputPath,
                    fastStart,
                    totalEstimatedBytes,
                    progress,
                    ct);
                if (packetWriteResult.Failure != null)
                {
                    return packetWriteResult.Failure;
                }

                var totalPackets = packetWriteResult.TotalPackets;
                if (!TryFinalizeActiveOutputFile(tempLease, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
                {
                    return FinalizeResult.Failure(outputPath, outputFailure);
                }

                Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_OK output='{outputPath}' segments={segments.Count} packets={totalPackets} bytes={outputBytes}");
                ReportProgress(progress, new ExportProgress(segments.Count, segments.Count, 100.0), "segments_complete");
                return FinalizeResult.Success(outputPath, $"Exported {totalPackets} packets from {segments.Count} segments");
            }
            catch (OperationCanceledException)
            {
                const string message = "Flashback export cancelled.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }
            catch (Exception ex)
            {
                var message = $"Flashback export failed: {ex.Message}";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }
            finally
            {
                CleanupNativeState();
                if (tempLease != null)
                {
                    DeleteTempFileIfPresent(tempLease);
                }
                _activeTempPath = null;
            }
        }
        finally
        {
            ReleaseExportLockBestEffort("segment_export");
        }
    }

    private bool TryInitializeSegmentOutputTemplate(
        IReadOnlyList<FlashbackExportSegment> segments,
        string tmpPath,
        bool fastStart,
        CancellationToken ct,
        out int selectedStreamCount,
        out int selectedVideoStreamIndex,
        out int[] selectedStreamMap,
        out string failureMessage)
    {
        selectedStreamCount = 0;
        selectedVideoStreamIndex = -1;
        selectedStreamMap = Array.Empty<int>();
        failureMessage = "Flashback export failed: no usable segment template was found.";

        var bestTemplateSegIdx = -1;
        string? bestTemplatePath = null;
        var bestMappedStreamCount = -1;

        for (var templateSegIdx = 0; templateSegIdx < segments.Count; templateSegIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var templatePath = segments[templateSegIdx].Path;
            if (!File.Exists(templatePath))
            {
                continue;
            }

            OpenInput(templatePath);
            try
            {
                ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
                if (!TryGetInputStreamCount(_activeInputContext, "segment_template", out var candidateStreamCount, out var streamCountFailure))
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP path='{Path.GetFileName(templatePath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
                    continue;
                }

                var candidateVideoStreamIndex = FindVideoStreamIndex(_activeInputContext);
                LogInputStreams(_activeInputContext, candidateStreamCount);
                if (candidateVideoStreamIndex < 0)
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing' seg={templateSegIdx} trying_next_segment={templateSegIdx < segments.Count - 1}");
                    failureMessage = "Flashback export failed: no usable video stream was found in any segment.";
                    continue;
                }

                var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];
                var videoWidth = videoStream->codecpar->width;
                var videoHeight = videoStream->codecpar->height;
                var videoExtradataSize = videoStream->codecpar->extradata_size;
                var videoHasValidParams = videoWidth > 0 && videoHeight > 0;

                if (!videoHasValidParams)
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_params_incomplete' seg={templateSegIdx} " +
                        $"w={videoWidth} " +
                        $"h={videoHeight} " +
                        $"extradata={videoExtradataSize} " +
                        $"trying_next_segment={templateSegIdx < segments.Count - 1}");
                    failureMessage = "Flashback export failed: no segment had complete video parameters.";
                    continue;
                }

                var candidateMappedStreamCount = CountUsableTemplateStreams(_activeInputContext, candidateStreamCount);
                Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_CANDIDATE seg={templateSegIdx} path='{Path.GetFileName(templatePath)}' mapped_streams={candidateMappedStreamCount} video_idx={candidateVideoStreamIndex}");
                if (candidateMappedStreamCount > bestMappedStreamCount)
                {
                    bestTemplateSegIdx = templateSegIdx;
                    bestTemplatePath = templatePath;
                    bestMappedStreamCount = candidateMappedStreamCount;
                }
            }
            finally
            {
                CloseActiveInput();
            }
        }

        if (bestTemplateSegIdx < 0 || string.IsNullOrWhiteSpace(bestTemplatePath))
        {
            return false;
        }

        ct.ThrowIfCancellationRequested();
        OpenInput(bestTemplatePath);
        try
        {
            ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
            if (!TryGetInputStreamCount(_activeInputContext, "segment_template", out var candidateStreamCount, out var streamCountFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP path='{Path.GetFileName(bestTemplatePath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
                failureMessage = streamCountFailure;
                return false;
            }

            var candidateVideoStreamIndex = FindVideoStreamIndex(_activeInputContext);
            LogInputStreams(_activeInputContext, candidateStreamCount);
            if (candidateVideoStreamIndex < 0)
            {
                Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing' seg={bestTemplateSegIdx} trying_next_segment=False");
                failureMessage = "Flashback export failed: no usable video stream was found in any segment.";
                return false;
            }

            var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];
            var videoWidth = videoStream->codecpar->width;
            var videoHeight = videoStream->codecpar->height;
            var videoExtradataSize = videoStream->codecpar->extradata_size;
            var videoHasValidParams = videoWidth > 0 && videoHeight > 0;
            if (!videoHasValidParams)
            {
                Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_params_incomplete' seg={bestTemplateSegIdx} " +
                    $"w={videoWidth} " +
                    $"h={videoHeight} " +
                    $"extradata={videoExtradataSize} " +
                    "trying_next_segment=False");
                failureMessage = "Flashback export failed: no segment had complete video parameters.";
                return false;
            }

            CreateOutputContext(tmpPath, fastStart);
            selectedStreamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount);
            Logger.Log($"FLASHBACK_EXPORT_STREAM_MAP video_idx={candidateVideoStreamIndex} map=[{string.Join(",", selectedStreamMap)}]");
            OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);
            selectedStreamCount = candidateStreamCount;
            selectedVideoStreamIndex = candidateVideoStreamIndex;
            Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SELECTED seg={bestTemplateSegIdx} path='{Path.GetFileName(bestTemplatePath)}' mapped_streams={bestMappedStreamCount}");
            return true;
        }
        finally
        {
            CloseActiveInput();
        }
    }

    private bool TryOpenSegmentInputForExport(
        FlashbackExportSegment segment,
        string segmentPath,
        int templateStreamCount,
        int[] streamMap,
        ref RequestedSegmentSkipTracker requestedSegmentSkips,
        out int currentStreamCount)
    {
        currentStreamCount = 0;

        if (!File.Exists(segmentPath))
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='not_found'");
            requestedSegmentSkips.Track(segment, "not_found");
            return false;
        }

        OpenInput(segmentPath);
        ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");

        if (!TryGetInputStreamCount(_activeInputContext, "segment_export", out currentStreamCount, out var streamCountFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
            requestedSegmentSkips.Track(segment, "invalid_stream_count");
            CloseActiveInput();
            return false;
        }

        if (currentStreamCount != templateStreamCount)
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='stream_count_mismatch' expected={templateStreamCount} actual={currentStreamCount}");
            requestedSegmentSkips.Track(segment, "stream_count_mismatch");
            CloseActiveInput();
            return false;
        }

        var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(
            _activeInputContext,
            _activeOutputContext,
            streamMap,
            currentStreamCount);
        if (streamLayoutMismatch != null)
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
            requestedSegmentSkips.Track(segment, "stream_layout_mismatch");
            CloseActiveInput();
            return false;
        }

        return true;
    }

    private struct RequestedSegmentSkipTracker
    {
        private readonly TimeSpan _inPoint;
        private readonly TimeSpan _outPoint;
        private int _count;
        private string? _firstReason;

        public RequestedSegmentSkipTracker(TimeSpan inPoint, TimeSpan outPoint)
        {
            _inPoint = inPoint;
            _outPoint = outPoint;
            _count = 0;
            _firstReason = null;
        }

        public void Track(FlashbackExportSegment segment, string reason)
        {
            if (!SegmentOverlapsExportRange(segment, _inPoint, _outPoint))
            {
                return;
            }

            _count++;
            _firstReason ??= reason;
        }

        public bool TryCreateFailureMessage(out string message)
        {
            if (_count <= 0)
            {
                message = string.Empty;
                return false;
            }

            message = $"Flashback export failed: {_count} requested segment(s) were skipped; first reason: {_firstReason}.";
            return true;
        }
    }

    private readonly record struct SegmentExportWindow(
        bool UseSegmentTimeline,
        long SegmentInOffsetUs,
        long SegmentOutOffsetUs,
        bool SkipBecauseEmpty);

    private static SegmentExportWindow ProjectSegmentExportWindow(
        FlashbackExportSegment segment,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long outPtsLimitUs)
    {
        var useSegmentTimeline = segment.StartPts.HasValue;
        if (!useSegmentTimeline)
        {
            return new SegmentExportWindow(
                UseSegmentTimeline: false,
                SegmentInOffsetUs: 0,
                SegmentOutOffsetUs: outPtsLimitUs,
                SkipBecauseEmpty: false);
        }

        var segmentInOffsetUs = ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value));
        var segmentOutDelta = SaturatingSubtract(
            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,
            segment.StartPts!.Value);
        var segmentOutOffsetUs = ToMicrosecondsSaturated(segmentOutDelta);

        return new SegmentExportWindow(
            UseSegmentTimeline: true,
            SegmentInOffsetUs: segmentInOffsetUs,
            SegmentOutOffsetUs: segmentOutOffsetUs,
            SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero);
    }

    private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string tmpPath,
        string outputPath,
        bool fastStart,
        long totalEstimatedBytes,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);

        // Output state - initialized from first segment
        int streamCount = 0;
        int videoStreamIndex = -1;
        int[] streamMap = Array.Empty<int>();
        long totalPackets = 0;
        long bytesProcessed = 0;
        var requestedSegmentSkips = new RequestedSegmentSkipTracker(inPoint, outPoint);

        // Cross-segment PTS tracking (in microseconds)
        long outputPtsOffsetUs = 0; // accumulated offset for output continuity

        // Per-stream last DTS tracking for monotonicity enforcement
        var lastDtsPerStream = new long[64]; // indexed by OUTPUT stream index
        for (int i = 0; i < lastDtsPerStream.Length; i++) lastDtsPerStream[i] = long.MinValue;

        if (!TryInitializeSegmentOutputTemplate(segments, tmpPath, fastStart, ct, out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{templateFailure}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, templateFailure), 0);
        }

        var packet = ffmpeg.av_packet_alloc();
        if (packet == null)
            throw new InvalidOperationException("Failed to allocate AVPacket.");

        try
        {
            for (var segIdx = 0; segIdx < segments.Count; segIdx++)
            {
                ct.ThrowIfCancellationRequested();
                var segment = segments[segIdx];
                var segPath = segment.Path;
                var segmentExportWindow = ProjectSegmentExportWindow(segment, inPoint, outPoint, outPtsLimitUs);
                if (segmentExportWindow.SkipBecauseEmpty)
                {
                    continue;
                }
                var useSegmentTimeline = segmentExportWindow.UseSegmentTimeline;
                var segmentInOffsetUs = segmentExportWindow.SegmentInOffsetUs;
                var segmentOutOffsetUs = segmentExportWindow.SegmentOutOffsetUs;

                if (!TryOpenSegmentInputForExport(
                        segment,
                        segPath,
                        streamCount,
                        streamMap,
                        ref requestedSegmentSkips,
                        out var currentStreamCount))
                {
                    continue;
                }

                // Seek to inPoint in first segment
                if (segIdx == 0 && inPoint > TimeSpan.Zero && !useSegmentTimeline)
                {
                    var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);
                    var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                    if (seekResult < 0)
                        Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                }

                WriteSegmentPacketReadLoop(
                    segIdx,
                    segments.Count,
                    streamCount,
                    videoStreamIndex,
                    currentStreamCount,
                    streamMap,
                    lastDtsPerStream,
                    totalEstimatedBytes,
                    bytesProcessed,
                    outputPtsOffsetUs,
                    useSegmentTimeline,
                    segmentInOffsetUs,
                    segmentOutOffsetUs,
                    packet,
                    progress,
                    ct,
                    ref totalPackets,
                    out var segmentPacketState);

                // Update cross-segment offset: next segment's PTS starts after this segment's max + one frame
                if (segmentPacketState.MaxPtsUs > outputPtsOffsetUs)
                {
                    var videoStream = videoStreamIndex >= 0 ? _activeInputContext->streams[videoStreamIndex] : null;
                    long frameDurUs = ResolveFrameDurationUs(videoStream);
                    outputPtsOffsetUs = segmentPacketState.MaxPtsUs + frameDurUs;
                }

                // Track bytes for progress
                try { if (File.Exists(segPath)) bytesProcessed = AddNonNegativeSaturated(bytesProcessed, new FileInfo(segPath).Length); }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UPDATE_WARN path='{segPath}' type={ex.GetType().Name} msg='{ex.Message}'");
                }

                // Close this segment's input
                CloseActiveInput();

                ReportProgress(
                    progress,
                    new ExportProgress(
                        segIdx + 1,
                        segments.Count,
                        totalEstimatedBytes > 0 ? 100.0 * bytesProcessed / totalEstimatedBytes : 100.0 * (segIdx + 1) / segments.Count),
                    "segment_complete");

                Logger.Log($"FLASHBACK_EXPORT_SEGMENT_OK seg={segIdx}/{segments.Count} path='{Path.GetFileName(segPath)}' packets={totalPackets} seg_max_pts_us={segmentPacketState.MaxPtsUs} seg_abs_max_pts_us={segmentPacketState.AbsMaxPtsUs} local_in_us={segmentInOffsetUs} local_out_us={segmentOutOffsetUs} bases_discovered={segmentPacketState.AllBasesDiscovered}");

                // If outPoint was hit, stop processing more segments
                // Use absolute PTS (not rebased) since outPtsLimitUs is in absolute encoder time
                if (outPtsLimitUs < long.MaxValue && segmentPacketState.AbsMaxPtsUs >= outPtsLimitUs)
                    break;
            }
        }
        finally
        {
            var packetToFree = packet;
            ffmpeg.av_packet_free(&packetToFree);
        }

        if (requestedSegmentSkips.TryCreateFailureMessage(out var skippedSegmentFailureMessage))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{skippedSegmentFailureMessage}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, skippedSegmentFailureMessage), 0);
        }

        if (totalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written from any segment.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SegmentPacketWriteResult(FinalizeResult.Failure(outputPath, message), 0);
        }

        return new SegmentPacketWriteResult(null, totalPackets);
    }

    private readonly record struct SegmentPacketWriteResult(FinalizeResult? Failure, long TotalPackets);

    // Per-segment packet read loop: read frames from the active input, discover
    // timestamp bases, buffer early packets, and write rebased packets.
    private static readonly AVRational SegmentPacketUsTimeBase = new() { num = 1, den = 1_000_000 };

    private static SegmentPacketWriteState CreateSegmentPacketWriteState(
        int segmentIndex,
        int streamCount,
        bool useSegmentTimeline,
        long segmentInOffsetUs,
        long segmentOutOffsetUs,
        long outputPtsOffsetUs,
        int videoStreamIndex,
        long videoFrameDurationUs)
        => new(
            segmentIndex,
            useSegmentTimeline,
            segmentInOffsetUs,
            segmentOutOffsetUs,
            outputPtsOffsetUs,
            videoStreamIndex,
            videoFrameDurationUs,
            new long[streamCount],
            new bool[streamCount],
            new List<IntPtr>(),
            new List<int>());

    private static bool TryRecordSegmentTimestampBase(
        ref SegmentPacketWriteState state,
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream)
    {
        if (!TryResolveTimestampBase(packet, out var timestampBase))
        {
            return false;
        }

        var baseUs = ffmpeg.av_rescale_q(timestampBase, outputStream->time_base, SegmentPacketUsTimeBase);
        state.TimestampBasesUs[streamIndex] = baseUs;
        state.HasTimestampBase[streamIndex] = true;
        if (state.MinBaseUs == null || baseUs < state.MinBaseUs.Value)
        {
            state.MinBaseUs = baseUs;
        }

        return true;
    }

    private static bool HasDiscoveredAllMappedSegmentBases(
        in SegmentPacketWriteState state,
        int streamCount,
        int[] streamMap)
    {
        for (var i = 0; i < streamCount; i++)
        {
            if (streamMap[i] >= 0 && !state.HasTimestampBase[i])
            {
                return false;
            }
        }

        return true;
    }

    private int FlushSegmentBufferedPackets(
        ref SegmentPacketWriteState state,
        int[] streamMap,
        long[] lastDtsPerOutputStream,
        out bool stopFlushing)
    {
        var written = 0;
        stopFlushing = false;
        try
        {
            for (var bufferedIndex = 0; bufferedIndex < state.BufferedPackets.Count; bufferedIndex++)
            {
                var bufferedPacket = (AVPacket*)state.BufferedPackets[bufferedIndex];
                var sourceStreamIndex = state.BufferedStreamIndices[bufferedIndex];
                var outputStreamIndex = streamMap[sourceStreamIndex];
                var outputStream = _activeOutputContext->streams[outputStreamIndex];
                var writeOutcome = WriteRebasedSegmentPacket(
                    ref state,
                    bufferedPacket,
                    sourceStreamIndex,
                    outputStreamIndex,
                    outputStream,
                    lastDtsPerOutputStream);
                if (writeOutcome == SegmentPacketWriteOutcome.StopAtVideoOutPoint)
                {
                    stopFlushing = true;
                }
                else if (writeOutcome == SegmentPacketWriteOutcome.Written)
                {
                    written++;
                    ThrottleExportWriterIfNeeded(written);
                }

                ffmpeg.av_packet_free(&bufferedPacket);
                state.BufferedPackets[bufferedIndex] = IntPtr.Zero;
            }
        }
        finally
        {
            FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);
        }

        return written;
    }

    private void WriteSegmentPacketReadLoop(
        int segIdx,
        int segmentCount,
        int streamCount,
        int videoStreamIndex,
        int currentStreamCount,
        int[] streamMap,
        long[] lastDtsPerStream,
        long totalEstimatedBytes,
        long bytesProcessed,
        long outputPtsOffsetUs,
        bool useSegmentTimeline,
        long segmentInOffsetUs,
        long segmentOutOffsetUs,
        AVPacket* packet,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        ref long totalPackets,
        out SegmentPacketWriteState segmentPacketState)
    {
        var lastProgressHeartbeatTick = 0L;
        var segmentVideoFrameDurUs = 33333L;
        if (useSegmentTimeline &&
            videoStreamIndex >= 0 &&
            videoStreamIndex < currentStreamCount)
        {
            segmentVideoFrameDurUs = ResolveFrameDurationUs(_activeInputContext->streams[videoStreamIndex]);
        }
        segmentPacketState = CreateSegmentPacketWriteState(
            segIdx,
            streamCount,
            useSegmentTimeline,
            segmentInOffsetUs,
            segmentOutOffsetUs,
            outputPtsOffsetUs,
            videoStreamIndex,
            segmentVideoFrameDurUs);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);
            if (readResult == ffmpeg.AVERROR_EOF)
                break;
            ThrowIfError(readResult, "av_read_frame");
            if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))
            {
                ReportProgress(
                    progress,
                    new ExportProgress(
                        segIdx,
                        segmentCount,
                        totalEstimatedBytes > 0
                            ? 100.0 * bytesProcessed / totalEstimatedBytes
                            : 100.0 * segIdx / segmentCount),
                    "segment_heartbeat");
            }

            try
            {
                var streamIndex = packet->stream_index;
                if (streamIndex < 0 || streamIndex >= streamCount)
                    continue;

                // Skip streams filtered out by CopyTemplateStreams
                var mappedIndex = streamMap[streamIndex];
                if (mappedIndex < 0)
                    continue;

                var inStream = _activeInputContext->streams[streamIndex];
                var outStream = _activeOutputContext->streams[mappedIndex];

                // Rescale to output time base
                ffmpeg.av_packet_rescale_ts(packet, inStream->time_base, outStream->time_base);

                // Discover per-stream base
                if (!segmentPacketState.HasTimestampBase[streamIndex])
                {
                    if (!TryRecordSegmentTimestampBase(ref segmentPacketState, packet, streamIndex, outStream))
                    {
                        continue;
                    }
                }

                // Phase 1: buffer until all bases known
                const int MaxBufferedPackets = 600;
                if (!segmentPacketState.AllBasesDiscovered)
                {
                    var clone = ClonePacketOrThrow(packet, "segment_buffer");
                    segmentPacketState.BufferedPackets.Add((IntPtr)clone);
                    segmentPacketState.BufferedStreamIndices.Add(streamIndex);

                    segmentPacketState.AllBasesDiscovered = HasDiscoveredAllMappedSegmentBases(
                        in segmentPacketState,
                        streamCount,
                        streamMap);
                    if (!segmentPacketState.AllBasesDiscovered &&
                        segmentPacketState.BufferedPackets.Count >= MaxBufferedPackets)
                    {
                        segmentPacketState.MinBaseUs ??= 0; // Silent streams never set a base - default to 0
                        segmentPacketState.AllBasesDiscovered = true;
                    }

                    if (segmentPacketState.AllBasesDiscovered)
                    {
                        totalPackets += FlushSegmentBufferedPackets(
                            ref segmentPacketState,
                            streamMap,
                            lastDtsPerStream,
                            out var stopFlushing);
                        if (stopFlushing)
                            break;
                    }
                    continue;
                }

                var writeOutcome = WriteRebasedSegmentPacket(
                    ref segmentPacketState,
                    packet,
                    streamIndex,
                    mappedIndex,
                    outStream,
                    lastDtsPerStream);
                if (writeOutcome == SegmentPacketWriteOutcome.StopAtVideoOutPoint)
                {
                    break;
                }
                if (writeOutcome == SegmentPacketWriteOutcome.Written)
                {
                    totalPackets++;
                    ThrottleExportWriterIfNeeded(totalPackets);
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(packet);
            }
        }

        // EOF: if Phase 1 never completed (some configured stream, typically a
        // silent mic, never produced packets and the buffer never reached the
        // 600-packet cap), flush whatever we have using a fallback base of 0.
        // Without this, every video packet in a short segment would be silently
        // discarded by the FreeBufferedPackets path that used to live here.
        if (!segmentPacketState.AllBasesDiscovered && segmentPacketState.BufferedPackets.Count > 0)
        {
            segmentPacketState.MinBaseUs ??= 0;
            segmentPacketState.AllBasesDiscovered = true;
            var discoveredCount = 0;
            for (var i = 0; i < streamCount; i++) { if (segmentPacketState.HasTimestampBase[i]) discoveredCount++; }
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx} buffered={segmentPacketState.BufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
            totalPackets += FlushSegmentBufferedPackets(
                ref segmentPacketState,
                streamMap,
                lastDtsPerStream,
                out _);
        }
        else
        {
            // Either Phase 1 completed inline (nothing to flush) or buffer is empty.
            // FreeBufferedPackets is a no-op on an empty list; safe in both cases.
            FreeBufferedPackets(segmentPacketState.BufferedPackets, segmentPacketState.BufferedStreamIndices);
        }
    }

    private SegmentPacketWriteOutcome WriteRebasedSegmentPacket(
        ref SegmentPacketWriteState state,
        AVPacket* packet,
        int sourceStreamIndex,
        int outputStreamIndex,
        AVStream* outputStream,
        long[] lastDtsPerOutputStream)
    {
        // Check outPoint against absolute PTS before remapping. At this point
        // packet->pts is in the output time base but still absolute encoder PTS.
        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            var absolutePtsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
            var comparePtsUs = state.UseSegmentTimeline
                ? absolutePtsUs - state.MinBaseUs!.Value
                : absolutePtsUs;
            if (sourceStreamIndex == state.VideoStreamIndex && absolutePtsUs > state.AbsMaxPtsUs)
            {
                state.AbsMaxPtsUs = absolutePtsUs;
            }

            if (state.UseSegmentTimeline && comparePtsUs < state.SegmentInOffsetUs)
            {
                return SegmentPacketWriteOutcome.Skipped;
            }

            if (state.SegmentOutOffsetUs < long.MaxValue && comparePtsUs > state.SegmentOutOffsetUs)
            {
                return sourceStreamIndex == state.VideoStreamIndex
                    ? SegmentPacketWriteOutcome.StopAtVideoOutPoint
                    : SegmentPacketWriteOutcome.Skipped;
            }
        }

        // Remap: subtract segment base, add cross-segment output offset.
        var segmentBaseTs = ffmpeg.av_rescale_q(
            state.MinBaseUs!.Value,
            SegmentPacketUsTimeBase,
            outputStream->time_base);
        var offsetTs = ffmpeg.av_rescale_q(
            state.OutputPtsOffsetUs,
            SegmentPacketUsTimeBase,
            outputStream->time_base);

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->pts = packet->pts - segmentBaseTs + offsetTs;
            var ptsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
            if (state.UseSegmentTimeline && sourceStreamIndex == state.VideoStreamIndex)
            {
                var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                    ptsUs,
                    state.OutputPtsOffsetUs,
                    state.VideoFrameDurationUs,
                    state.VideoPacketsSeen,
                    state.VideoTimestampRepairUs);
                if (repairUs > 0)
                {
                    state.VideoTimestampRepairUs += repairUs;
                    Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={state.SegmentIndex} stream={sourceStreamIndex} repair_us={repairUs} total_repair_us={state.VideoTimestampRepairUs}");
                }

                if (state.VideoTimestampRepairUs > 0)
                {
                    var repairTs = ffmpeg.av_rescale_q(
                        state.VideoTimestampRepairUs,
                        SegmentPacketUsTimeBase,
                        outputStream->time_base);
                    packet->pts -= repairTs;
                    ptsUs = ffmpeg.av_rescale_q(packet->pts, outputStream->time_base, SegmentPacketUsTimeBase);
                }

                state.VideoPacketsSeen++;
            }

            if (ptsUs > state.MaxPtsUs)
            {
                state.MaxPtsUs = ptsUs;
            }
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            packet->dts = packet->dts - segmentBaseTs + offsetTs;
            if (state.UseSegmentTimeline &&
                sourceStreamIndex == state.VideoStreamIndex &&
                state.VideoTimestampRepairUs > 0)
            {
                var repairTs = ffmpeg.av_rescale_q(
                    state.VideoTimestampRepairUs,
                    SegmentPacketUsTimeBase,
                    outputStream->time_base);
                packet->dts -= repairTs;
            }

            // mp4 muxing rejects non-monotonic DTS; preserve the existing per-output-stream clamp.
            if (outputStreamIndex < lastDtsPerOutputStream.Length &&
                lastDtsPerOutputStream[outputStreamIndex] != long.MinValue &&
                packet->dts <= lastDtsPerOutputStream[outputStreamIndex])
            {
                packet->dts = lastDtsPerOutputStream[outputStreamIndex] + 1;
            }
        }

        if (outputStreamIndex < lastDtsPerOutputStream.Length &&
            packet->dts != ffmpeg.AV_NOPTS_VALUE)
        {
            lastDtsPerOutputStream[outputStreamIndex] = packet->dts;
        }

        NormalizePacketTimestampsBeforeWrite(packet);
        packet->pos = -1;
        packet->stream_index = outputStreamIndex;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
        return SegmentPacketWriteOutcome.Written;
    }

    private enum SegmentPacketWriteOutcome
    {
        Skipped,
        Written,
        StopAtVideoOutPoint,
    }

    private struct SegmentPacketWriteState
    {
        public SegmentPacketWriteState(
            int segmentIndex,
            bool useSegmentTimeline,
            long segmentInOffsetUs,
            long segmentOutOffsetUs,
            long outputPtsOffsetUs,
            int videoStreamIndex,
            long videoFrameDurationUs,
            long[] timestampBasesUs,
            bool[] hasTimestampBase,
            List<IntPtr> bufferedPackets,
            List<int> bufferedStreamIndices)
        {
            SegmentIndex = segmentIndex;
            UseSegmentTimeline = useSegmentTimeline;
            SegmentInOffsetUs = segmentInOffsetUs;
            SegmentOutOffsetUs = segmentOutOffsetUs;
            OutputPtsOffsetUs = outputPtsOffsetUs;
            VideoStreamIndex = videoStreamIndex;
            VideoFrameDurationUs = videoFrameDurationUs;
            TimestampBasesUs = timestampBasesUs;
            HasTimestampBase = hasTimestampBase;
            BufferedPackets = bufferedPackets;
            BufferedStreamIndices = bufferedStreamIndices;
        }

        public int SegmentIndex { get; }
        public bool UseSegmentTimeline { get; }
        public long SegmentInOffsetUs { get; }
        public long SegmentOutOffsetUs { get; }
        public long OutputPtsOffsetUs { get; }
        public int VideoStreamIndex { get; }
        public long VideoFrameDurationUs { get; }
        public long[] TimestampBasesUs { get; }
        public bool[] HasTimestampBase { get; }
        public List<IntPtr> BufferedPackets { get; }
        public List<int> BufferedStreamIndices { get; }
        public long? MinBaseUs { get; set; }
        public bool AllBasesDiscovered { get; set; }
        public long MaxPtsUs { get; set; }
        public long AbsMaxPtsUs { get; set; }
        public long VideoTimestampRepairUs { get; set; }
        public int VideoPacketsSeen { get; set; }
    }

    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    private static long ToAvTimeBaseTimestampOrMax(TimeSpan value)
        => value == TimeSpan.MaxValue ? long.MaxValue : ToAvTimeBaseTimestamp(value);

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
        => ToMicrosecondsSaturated(value);

    private static long ToMicrosecondsSaturated(TimeSpan value)
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

    private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)
    {
        if (left <= right)
        {
            return TimeSpan.Zero;
        }

        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(leftTicks - rightTicks);
    }

    private static long ResolveFrameDurationUs(AVStream* videoStream)
    {
        if (videoStream != null && IsValidPositiveRational(videoStream->avg_frame_rate))
        {
            return Math.Max(1, 1_000_000L * videoStream->avg_frame_rate.den / videoStream->avg_frame_rate.num);
        }

        if (videoStream != null && IsValidPositiveRational(videoStream->r_frame_rate))
        {
            return Math.Max(1, 1_000_000L * videoStream->r_frame_rate.den / videoStream->r_frame_rate.num);
        }

        return 33333; // fallback ~30fps
    }

    private static bool IsValidPositiveRational(AVRational value)
        => value.num > 0 && value.den > 0;

    private static long ResolveSegmentBoundaryTimestampRepairUs(
        long ptsUs,
        long outputPtsOffsetUs,
        long frameDurUs,
        int segmentVideoPacketsSeen,
        long existingRepairUs)
    {
        if (outputPtsOffsetUs <= 0 ||
            frameDurUs <= 0 ||
            segmentVideoPacketsSeen <= 0 ||
            segmentVideoPacketsSeen > 12)
        {
            return 0;
        }

        var expectedPtsUs = outputPtsOffsetUs + segmentVideoPacketsSeen * frameDurUs;
        var repairedPtsUs = ptsUs - existingRepairUs;
        var gapUs = repairedPtsUs - expectedPtsUs;
        var thresholdUs = frameDurUs + frameDurUs / 2;
        if (gapUs <= thresholdUs)
        {
            return 0;
        }

        return gapUs;
    }

    private static bool TryResolveTimestampBase(AVPacket* packet, out long timestampBase)
    {
        timestampBase = 0;

        var hasPts = packet->pts != ffmpeg.AV_NOPTS_VALUE;
        var hasDts = packet->dts != ffmpeg.AV_NOPTS_VALUE;
        if (!hasPts && !hasDts)
        {
            return false;
        }

        if (hasPts && hasDts)
        {
            timestampBase = Math.Min(packet->pts, packet->dts);
            return true;
        }

        timestampBase = hasPts ? packet->pts : packet->dts;
        return true;
    }

    private static void NormalizePacketTimestampsBeforeWrite(AVPacket* packet)
    {
        if (packet == null)
        {
            return;
        }

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE && packet->pts < 0)
        {
            packet->pts = 0;
        }

        if (packet->dts != ffmpeg.AV_NOPTS_VALUE && packet->dts < 0)
        {
            packet->dts = 0;
        }

        if (packet->pts != ffmpeg.AV_NOPTS_VALUE &&
            packet->dts != ffmpeg.AV_NOPTS_VALUE &&
            packet->pts < packet->dts)
        {
            packet->pts = packet->dts;
        }
    }

    /// <summary>
    /// Flushes all buffered packets by subtracting <paramref name="globalMinBaseUs"/> from PTS/DTS,
    /// clamping negative values to zero, and writing each packet to the active output context.
    /// Frees all packet clones and clears both lists. Returns the number of packets written.
    /// </summary>
    private long FlushBufferedPackets(
        List<IntPtr> bufferedPackets,
        List<int> bufferedStreamIndices,
        int[] streamMap,
        long globalMinBaseUs,
        AVRational usTimeBase,
        long[] packetCounts)
    {
        long flushed = 0;
        try
        {
            for (int bi = 0; bi < bufferedPackets.Count; bi++)
            {
                var buffPkt = (AVPacket*)bufferedPackets[bi];
                var si = bufferedStreamIndices[bi];
                var oi = streamMap[si];
                var outStr = _activeOutputContext->streams[oi];
                var bTs = ffmpeg.av_rescale_q(globalMinBaseUs, usTimeBase, outStr->time_base);
                if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                {
                    buffPkt->pts -= bTs;
                }

                if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                {
                    buffPkt->dts -= bTs;
                }

                NormalizePacketTimestampsBeforeWrite(buffPkt);
                buffPkt->pos = -1;
                buffPkt->stream_index = oi;
                ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                packetCounts[si]++;
                flushed++;
                ffmpeg.av_packet_free(&buffPkt);
                bufferedPackets[bi] = IntPtr.Zero;
            }
        }
        finally
        {
            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);
        }

        return flushed;
    }

    private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)
    {
        foreach (var pktPtr in bufferedPackets)
        {
            if (pktPtr != IntPtr.Zero)
            {
                var p = (AVPacket*)pktPtr;
                ffmpeg.av_packet_free(&p);
            }
        }

        bufferedPackets.Clear();
        bufferedStreamIndices?.Clear();
    }

    private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)
    {
        var clone = ffmpeg.av_packet_clone(packet);
        if (clone != null)
        {
            return clone;
        }

        Logger.Log($"FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        throw new InvalidOperationException($"FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
    }
}
