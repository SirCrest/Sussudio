using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

/// <summary>
/// Exports a time range from a single .ts flashback file by remuxing to .mp4.
/// No re-encoding — just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe class FlashbackExporter : IDisposable
{
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

    /// <summary>
    /// Exports a time range from the flashback .ts file to an .mp4 file.
    /// Seeks to the nearest keyframe before <paramref name="inPoint"/> and copies packets
    /// until <paramref name="outPoint"/> is reached.
    /// </summary>
    public Task<FinalizeResult> ExportAsync(
        string inputTsPath,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        EnsureNotDisposed();
        var result = ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, progress, ct);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Exports all content from the flashback .ts file to an .mp4 file (full remux).
    /// </summary>
    public Task<FinalizeResult> ExportFullAsync(
        string inputTsPath,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        return ExportAsync(inputTsPath, TimeSpan.Zero, TimeSpan.MaxValue, outputPath, fastStart, progress, ct);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Logger.Log("FLASHBACK_EXPORT_DISPOSE");
        CleanupNativeState();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private FinalizeResult ExportCore(
        string inputTsPath,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(inputTsPath) || !File.Exists(inputTsPath))
        {
            var message = $"Flashback export failed: input file not found '{inputTsPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            const string message = "Flashback export failed: output path is required.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            var message = $"Flashback export failed: output directory does not exist for '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        var tmpPath = outputPath + ".tmp";
        _activeTempPath = tmpPath;

        try
        {
            DeleteTempFileIfPresent(tmpPath);
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            Logger.Log($"FLASHBACK_EXPORT_START input='{inputTsPath}' in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");

            // Open input .ts file
            OpenInput(inputTsPath);
            ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");

            // Seek to inPoint
            if (inPoint > TimeSpan.Zero)
            {
                var seekTimestamp = (long)(inPoint.TotalSeconds * ffmpeg.AV_TIME_BASE);
                var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                if (seekResult < 0)
                {
                    Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                }
            }

            // Create output .mp4 context
            CreateOutputContext(tmpPath, fastStart);
            var videoStreamIndex = FindVideoStreamIndex(_activeInputContext);
            CopyTemplateStreams(_activeInputContext, _activeOutputContext);
            OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);

            var streamCount = checked((int)_activeOutputContext->nb_streams);
            var timestampBases = new long[streamCount]; // per-stream base in output time_base ticks
            var timestampBasesUs = new long[streamCount]; // per-stream base in microseconds
            var hasTimestampBase = new bool[streamCount];
            long? globalMinBaseUs = null; // global minimum base in microseconds
            var packetCounts = new long[streamCount];
            long totalPackets = 0;
            var outPtsLimit = outPoint == TimeSpan.MaxValue ? long.MaxValue : (long)(outPoint.TotalSeconds * ffmpeg.AV_TIME_BASE);
            var usTimeBase = new AVRational { num = 1, den = 1_000_000 };

            // Read and remux packets — two-phase approach:
            // Phase 1: buffer packets until all stream timestamp bases are discovered (globalMinBaseUs is final)
            // Phase 2: flush buffer then process remaining packets inline with stable base
            var bufferedPackets = new List<IntPtr>();
            var bufferedStreamIndices = new List<int>();
            var allBasesDiscovered = false;

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

                    try
                    {
                        var streamIndex = packet->stream_index;
                        if (streamIndex < 0 || streamIndex >= streamCount)
                        {
                            continue;
                        }

                        var inStream = _activeInputContext->streams[streamIndex];
                        var outStream = _activeOutputContext->streams[streamIndex];

                        // Check if we've passed outPoint (video stream boundary only)
                        if (streamIndex == videoStreamIndex &&
                            packet->pts != ffmpeg.AV_NOPTS_VALUE && outPtsLimit < long.MaxValue)
                        {
                            var ptsUs = ffmpeg.av_rescale_q(packet->pts, inStream->time_base,
                                new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE });
                            if (ptsUs > outPtsLimit)
                            {
                                break;
                            }
                        }

                        // Rescale timestamps
                        ffmpeg.av_packet_rescale_ts(packet, inStream->time_base, outStream->time_base);

                        // Discover per-stream timestamp base and track global minimum (in microseconds)
                        if (!hasTimestampBase[streamIndex])
                        {
                            if (TryResolveTimestampBase(packet, out var tsBase))
                            {
                                timestampBases[streamIndex] = tsBase;
                                var baseUs = ffmpeg.av_rescale_q(tsBase, outStream->time_base, usTimeBase);
                                timestampBasesUs[streamIndex] = baseUs;
                                hasTimestampBase[streamIndex] = true;

                                if (globalMinBaseUs == null || baseUs < globalMinBaseUs.Value)
                                {
                                    globalMinBaseUs = baseUs;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        // Phase 1: buffer until all stream bases are known so globalMinBaseUs is final
                        if (!allBasesDiscovered)
                        {
                            var clone = ffmpeg.av_packet_clone(packet);
                            if (clone != null)
                            {
                                bufferedPackets.Add((IntPtr)clone);
                                bufferedStreamIndices.Add(streamIndex);
                            }

                            // Check if all streams now have bases
                            allBasesDiscovered = true;
                            for (int i = 0; i < streamCount; i++)
                            {
                                if (!hasTimestampBase[i]) { allBasesDiscovered = false; break; }
                            }

                            if (allBasesDiscovered)
                            {
                                // Flush buffer with final globalMinBaseUs
                                for (int bi = 0; bi < bufferedPackets.Count; bi++)
                                {
                                    var buffPkt = (AVPacket*)bufferedPackets[bi];
                                    var si = bufferedStreamIndices[bi];
                                    var outStr = _activeOutputContext->streams[si];
                                    var bTs = ffmpeg.av_rescale_q(globalMinBaseUs!.Value, usTimeBase, outStr->time_base);
                                    if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                                        buffPkt->pts -= bTs;
                                    if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                        buffPkt->dts -= bTs;
                                    buffPkt->pos = -1;
                                    buffPkt->stream_index = outStr->index;
                                    ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                                    packetCounts[si]++;
                                    totalPackets++;
                                    // Free clone struct (data already unreffed by write)
                                    ffmpeg.av_packet_free(&buffPkt);
                                    bufferedPackets[bi] = IntPtr.Zero;
                                }
                                bufferedPackets.Clear();
                                bufferedStreamIndices.Clear();
                            }
                            continue;
                        }

                        // Phase 2: inline write with stable globalMinBaseUs
                        var baseTs = ffmpeg.av_rescale_q(globalMinBaseUs!.Value, usTimeBase, outStream->time_base);
                        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            packet->pts -= baseTs;
                        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                            packet->dts -= baseTs;

                        packet->pos = -1;
                        packet->stream_index = outStream->index;

                        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
                        packetCounts[streamIndex]++;
                        totalPackets++;
                    }
                    finally
                    {
                        ffmpeg.av_packet_unref(packet);
                    }
                }

                // Phase 1 EOF: flush buffered packets with best-known base if not all streams discovered
                if (!allBasesDiscovered && globalMinBaseUs.HasValue && bufferedPackets.Count > 0)
                {
                    var discoveredCount = 0;
                    for (int i = 0; i < streamCount; i++) { if (hasTimestampBase[i]) discoveredCount++; }
                    Logger.Log($"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={discoveredCount}/{streamCount} buffered={bufferedPackets.Count}");
                    for (int bi = 0; bi < bufferedPackets.Count; bi++)
                    {
                        var buffPkt = (AVPacket*)bufferedPackets[bi];
                        var si = bufferedStreamIndices[bi];
                        var outStr = _activeOutputContext->streams[si];
                        var bTs = ffmpeg.av_rescale_q(globalMinBaseUs!.Value, usTimeBase, outStr->time_base);
                        if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                            buffPkt->pts -= bTs;
                        if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                            buffPkt->dts -= bTs;
                        buffPkt->pos = -1;
                        buffPkt->stream_index = outStr->index;
                        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                        packetCounts[si]++;
                        totalPackets++;
                        ffmpeg.av_packet_free(&buffPkt);
                        bufferedPackets[bi] = IntPtr.Zero;
                    }
                    bufferedPackets.Clear();
                    bufferedStreamIndices.Clear();
                }
            }
            finally
            {
                // Free any buffered packets not yet flushed (early EOF, error, or cancellation)
                foreach (var pktPtr in bufferedPackets)
                {
                    if (pktPtr != IntPtr.Zero)
                    {
                        var p = (AVPacket*)pktPtr;
                        ffmpeg.av_packet_free(&p);
                    }
                }
                var packetToFree = packet;
                ffmpeg.av_packet_free(&packetToFree);
            }

            // Log per-stream base drift warning if bases differ by more than 100ms
            LogTimestampBaseDrift(timestampBasesUs, hasTimestampBase);

            // Validate per-stream packet counts
            if (videoStreamIndex >= 0 && videoStreamIndex < streamCount && packetCounts[videoStreamIndex] == 0)
            {
                const string message = "Flashback export failed: no video packets were written.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            for (var i = 0; i < streamCount; i++)
            {
                if (i != videoStreamIndex && hasTimestampBase[i] && packetCounts[i] == 0)
                {
                    Logger.Log($"FLASHBACK_EXPORT_WARN stream={i} reason='no_packets_written' (non-video stream)");
                }
            }

            if (totalPackets == 0)
            {
                const string message = "Flashback export failed: no packets were written.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
            CloseOutputIo();

            AtomicMoveTempFile(tmpPath, outputPath);
            _activeTempPath = null;

            var outputBytes = new FileInfo(outputPath).Length;
            Logger.Log(
                $"FLASHBACK_EXPORT_DONE output='{outputPath}' packets={totalPackets} bytes={outputBytes}");
            progress?.Report(new ExportProgress(1, 1, 100.0));
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
            DeleteTempFileIfPresent(tmpPath);
            _activeTempPath = null;
        }
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

    private static int FindVideoStreamIndex(AVFormatContext* inputContext)
    {
        return ffmpeg.av_find_best_stream(inputContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
    }

    private static void LogTimestampBaseDrift(long[] timestampBasesUs, bool[] hasTimestampBase)
    {
        // All values are already in microseconds — find min/max to detect drift
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
        }
        catch
        {
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

    private static void CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext)
    {
        var streamCount = checked((int)inputContext->nb_streams);
        for (var streamIndex = 0; streamIndex < streamCount; streamIndex++)
        {
            var inStream = inputContext->streams[streamIndex];
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

    private static void AtomicMoveTempFile(string tmpPath, string outputPath)
    {
        if (!File.Exists(tmpPath))
        {
            throw new IOException($"Temporary export file was not created: '{tmpPath}'.");
        }

        File.Move(tmpPath, outputPath, overwrite: true);
    }

    private static void DeleteTempFileIfPresent(string tmpPath)
    {
        try
        {
            if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{tmpPath}' msg='{ex.Message}'");
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
        CloseActiveInput();
        CloseOutputIo();

        if (_activeOutputContext != null)
        {
            ffmpeg.avformat_free_context(_activeOutputContext);
            _activeOutputContext = null;
        }
    }

    private void EnsureNotDisposed()
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
        Logger.Log($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }
}
