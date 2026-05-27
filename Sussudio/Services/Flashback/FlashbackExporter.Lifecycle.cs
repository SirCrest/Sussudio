using System;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Models;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports Flashback time ranges by remuxing finalized segment artifacts to .mp4.
/// No re-encoding - just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe partial class FlashbackExporter : IDisposable
{
    // Export reads finalized segment artifacts only. Live capture continues via
    // FlashbackEncoderSink while this class remuxes packets into the target MP4.
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int MaxSupportedInputStreams = 64;
    private const int ProgressHeartbeatIntervalMs = 1_000;
    private const int ExportLockWaitTimeoutSeconds = 30;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly object _lifetimeSync = new();
    private CancellationTokenSource? _disposeCts = new();
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

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

            // Skip audio streams with incomplete codec params (0 channels or 0 sample_rate)
            if (codecType == AVMediaType.AVMEDIA_TYPE_AUDIO &&
                (inStream->codecpar->ch_layout.nb_channels <= 0 || inStream->codecpar->sample_rate <= 0))
            {
                Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_audio_params' channels={inStream->codecpar->ch_layout.nb_channels} sample_rate={inStream->codecpar->sample_rate}");
                streamMap[streamIndex] = -1;
                continue;
            }

            // Skip video streams with incomplete params
            if (codecType == AVMediaType.AVMEDIA_TYPE_VIDEO &&
                (inStream->codecpar->width <= 0 || inStream->codecpar->height <= 0))
            {
                Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_video_params' width={inStream->codecpar->width} height={inStream->codecpar->height}");
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
                if (inputCodec->sample_rate != templateCodec->sample_rate)
                {
                    return $"stream={streamIndex} sample_rate expected={templateCodec->sample_rate} actual={inputCodec->sample_rate}";
                }

                if (inputCodec->ch_layout.nb_channels != templateCodec->ch_layout.nb_channels)
                {
                    return $"stream={streamIndex} channels expected={templateCodec->ch_layout.nb_channels} actual={inputCodec->ch_layout.nb_channels}";
                }

                if (inputCodec->format != templateCodec->format)
                {
                    return $"stream={streamIndex} sample_format expected={templateCodec->format} actual={inputCodec->format}";
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
}
