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
    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private CancellationTokenSource? _disposeCts = new();
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

    /// <summary>
    /// Exports a flashback range to .mp4 based on the request parameters.
    /// Uses multi-segment export when <see cref="FlashbackExportRequest.SegmentPaths"/> is set,
    /// otherwise falls back to single-file export from <see cref="FlashbackExportRequest.InputTsPath"/>.
    /// </summary>
    public Task<FinalizeResult> ExportAsync(
        FlashbackExportRequest request,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (request.SegmentPaths is { Count: > 0 })
            return ExportSegmentsAsync(request.SegmentPaths, request.InPoint, request.OutPoint,
                request.OutputPath, request.FastStart, progress, ct);

        return ExportSingleAsync(request.InputTsPath!, request.InPoint, request.OutPoint,
            request.OutputPath, request.FastStart, progress, ct);
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
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        EnsureNotDisposed();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts!.Token);
        return Task.Run(() =>
        {
            try
            {
                return ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, progress, linkedCts.Token);
            }
            finally
            {
                linkedCts.Dispose();
            }
        }, linkedCts.Token);
    }

    /// <summary>
    /// Exports a time range spanning multiple .ts segment files to a single .mp4 file.
    /// Opens segments sequentially, remapping PTS for continuous output.
    /// </summary>
    private Task<FinalizeResult> ExportSegmentsAsync(
        IReadOnlyList<string> segmentPaths,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        EnsureNotDisposed();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts!.Token);
        return Task.Run(() =>
        {
            try
            {
                return ExportSegmentsCore(segmentPaths, inPoint, outPoint, outputPath, fastStart, progress, linkedCts.Token);
            }
            finally
            {
                linkedCts.Dispose();
            }
        }, linkedCts.Token);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Logger.Log("FLASHBACK_EXPORT_DISPOSE");

        // Signal any running export to cancel — ExportCore/ExportSegmentsCore will exit
        // via OperationCanceledException, clean up native state in their own finally block,
        // and release _exportLock before we acquire it.
        try { _disposeCts?.Cancel(); } catch (ObjectDisposedException) { /* Best-effort: CTS may already be disposed if Dispose races */ }

        // Wait for the export task to release the lock. The CTS is cancelled so
        // the task must exit — waiting indefinitely is safe and avoids use-after-free.
        _exportLock.Wait();
        try
        {
            CleanupNativeState();
        }
        finally
        {
            _exportLock.Release();
        }

        _exportLock.Dispose();
        _disposeCts?.Dispose();
        _disposeCts = null;
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

        _exportLock.Wait(ct);
        try
        {
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
            var streamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext);
            OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);

            var streamCount = checked((int)_activeInputContext->nb_streams);
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

                        // Skip streams that were filtered out (invalid codec params)
                        var outputIndex = streamMap[streamIndex];
                        if (outputIndex < 0)
                            continue;

                        var inStream = _activeInputContext->streams[streamIndex];
                        var outStream = _activeOutputContext->streams[outputIndex];

                        // Check if we've passed outPoint (applies to all streams)
                        if (packet->pts != ffmpeg.AV_NOPTS_VALUE && outPtsLimit < long.MaxValue)
                        {
                            var ptsUs = ffmpeg.av_rescale_q(packet->pts, inStream->time_base,
                                new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE });
                            if (ptsUs > outPtsLimit)
                            {
                                if (streamIndex == videoStreamIndex)
                                    break; // Video past out-point — stop reading entirely
                                continue; // Non-video past out-point — skip but keep reading for video
                            }
                        }

                        // Rescale timestamps
                        ffmpeg.av_packet_rescale_ts(packet, inStream->time_base, outStream->time_base);

                        // Discover per-stream timestamp base and track global minimum (in microseconds)
                        if (!hasTimestampBase[streamIndex])
                        {
                            if (TryResolveTimestampBase(packet, out var tsBase))
                            {
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

                        // Phase 1: buffer until all active stream bases are known so globalMinBaseUs is final.
                        // Cap buffered packets to avoid unbounded memory if a configured stream never
                        // produces packets (e.g., microphone enabled but silent).
                        const int MaxBufferedPackets = 600;
                        if (!allBasesDiscovered)
                        {
                            var clone = ffmpeg.av_packet_clone(packet);
                            if (clone != null)
                            {
                                bufferedPackets.Add((IntPtr)clone);
                                bufferedStreamIndices.Add(streamIndex);
                            }

                            // Check if all mapped streams have bases discovered,
                            // OR we've buffered enough packets that missing streams are assumed empty
                            allBasesDiscovered = true;
                            for (int i = 0; i < streamCount; i++)
                            {
                                if (streamMap[i] >= 0 && !hasTimestampBase[i]) { allBasesDiscovered = false; break; }
                            }
                            if (!allBasesDiscovered && bufferedPackets.Count >= MaxBufferedPackets)
                            {
                                allBasesDiscovered = true;
                                globalMinBaseUs ??= 0; // Silent streams never set a base — default to 0
                                var discoveredCount = 0;
                                for (var i = 0; i < streamCount; i++) { if (hasTimestampBase[i]) discoveredCount++; }
                                Logger.Log($"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH buffered={bufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
                            }

                            if (allBasesDiscovered)
                            {
                                totalPackets += FlushBufferedPackets(
                                    bufferedPackets, bufferedStreamIndices, streamMap,
                                    globalMinBaseUs!.Value, usTimeBase, packetCounts);
                            }
                            continue;
                        }

                        // Phase 2: inline write with stable globalMinBaseUs
                        var baseTs = ffmpeg.av_rescale_q(globalMinBaseUs!.Value, usTimeBase, outStream->time_base);
                        if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            packet->pts -= baseTs;
                        if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                            packet->dts -= baseTs;
                        if (packet->pts < 0) packet->pts = 0;
                        if (packet->dts < 0) packet->dts = 0;

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
                if (!allBasesDiscovered && bufferedPackets.Count > 0)
                {
                    globalMinBaseUs ??= 0; // No stream ever produced a base — default to 0
                }
                if (!allBasesDiscovered && globalMinBaseUs.HasValue && bufferedPackets.Count > 0)
                {
                    var discoveredCount = 0;
                    for (int i = 0; i < streamCount; i++) { if (hasTimestampBase[i]) discoveredCount++; }
                    Logger.Log($"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={discoveredCount}/{streamCount} buffered={bufferedPackets.Count}");
                    totalPackets += FlushBufferedPackets(
                        bufferedPackets, bufferedStreamIndices, streamMap,
                        globalMinBaseUs!.Value, usTimeBase, packetCounts);
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
        finally
        {
            try { _exportLock.Release(); } catch (ObjectDisposedException) { }
        }
    }

    private FinalizeResult ExportSegmentsCore(
        IReadOnlyList<string> segmentPaths,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (segmentPaths == null || segmentPaths.Count == 0)
        {
            const string message = "Flashback export failed: no segment paths provided.";
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

        // Estimate total bytes for progress
        long totalEstimatedBytes = 0;
        foreach (var segPath in segmentPaths)
        {
            try { if (File.Exists(segPath)) totalEstimatedBytes += new FileInfo(segPath).Length; }
            catch { /* Best-effort: segment may be deleted mid-scan; progress estimate is non-critical */ }
        }

        _exportLock.Wait(ct);
        try
        {
        var tmpPath = outputPath + ".tmp";
        _activeTempPath = tmpPath;

        try
        {
            DeleteTempFileIfPresent(tmpPath);
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_START segments={segmentPaths.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");

            var usTimeBase = new AVRational { num = 1, den = 1_000_000 };
            var outPtsLimitUs = outPoint == TimeSpan.MaxValue ? long.MaxValue : (long)(outPoint.TotalSeconds * ffmpeg.AV_TIME_BASE);

            // Output state — initialized from first segment
            int streamCount = 0;
            int videoStreamIndex = -1;
            int[] streamMap = Array.Empty<int>();
            long totalPackets = 0;
            long bytesProcessed = 0;

            // Cross-segment PTS tracking (in microseconds)
            long outputPtsOffsetUs = 0; // accumulated offset for output continuity

            // Per-stream last DTS tracking for monotonicity enforcement
            var lastDtsPerStream = new long[64]; // indexed by OUTPUT stream index
            for (int i = 0; i < lastDtsPerStream.Length; i++) lastDtsPerStream[i] = long.MinValue;

            var packet = ffmpeg.av_packet_alloc();
            if (packet == null)
                throw new InvalidOperationException("Failed to allocate AVPacket.");

            try
            {
                for (var segIdx = 0; segIdx < segmentPaths.Count; segIdx++)
                {
                    ct.ThrowIfCancellationRequested();
                    var segPath = segmentPaths[segIdx];

                    if (!File.Exists(segPath))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='not_found'");
                        continue;
                    }

                    var isFirst = segIdx == 0 || _activeOutputContext == null;
                    var isLast = segIdx == segmentPaths.Count - 1;

                    // Open this segment
                    OpenInput(segPath);
                    ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");

                    if (isFirst)
                    {
                        // Create output from first segment's streams
                        videoStreamIndex = FindVideoStreamIndex(_activeInputContext);
                        CreateOutputContext(tmpPath, fastStart);
                        streamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext);
                        OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);
                        streamCount = checked((int)_activeInputContext->nb_streams);
                    }
                    else
                    {
                        // Validate that this segment's stream layout matches the first segment's.
                        // Mismatched layouts (e.g. microphone toggled mid-capture) would cause
                        // packet->stream_index to map incorrectly, producing corrupt output.
                        var segNbStreams = checked((int)_activeInputContext->nb_streams);
                        if (segNbStreams != streamCount)
                        {
                            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='stream_count_mismatch' expected={streamCount} actual={segNbStreams}");
                            CloseActiveInput();
                            continue;
                        }
                    }

                    // Seek to inPoint in first segment
                    if (isFirst && inPoint > TimeSpan.Zero)
                    {
                        var seekTimestamp = (long)(inPoint.TotalSeconds * ffmpeg.AV_TIME_BASE);
                        var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                        if (seekResult < 0)
                            Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                    }

                    // Per-segment timestamp discovery (two-phase like single-file export)
                    var segTimestampBasesUs = new long[streamCount];
                    var segHasTimestampBase = new bool[streamCount];
                    long? segMinBaseUs = null;
                    var segBufferedPackets = new List<IntPtr>();
                    var segBufferedStreamIndices = new List<int>();
                    var segAllBasesDiscovered = false;
                    long segMaxPtsUs = 0; // track highest rebased PTS in this segment for offset calculation
                    long segAbsMaxPtsUs = 0; // track highest absolute PTS for outPoint check

                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        var readResult = ffmpeg.av_read_frame(_activeInputContext, packet);
                        if (readResult == ffmpeg.AVERROR_EOF)
                            break;
                        ThrowIfError(readResult, "av_read_frame");

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
                            if (!segHasTimestampBase[streamIndex])
                            {
                                if (TryResolveTimestampBase(packet, out var tsBase))
                                {
                                    var baseUs = ffmpeg.av_rescale_q(tsBase, outStream->time_base, usTimeBase);
                                    segTimestampBasesUs[streamIndex] = baseUs;
                                    segHasTimestampBase[streamIndex] = true;
                                    if (segMinBaseUs == null || baseUs < segMinBaseUs.Value)
                                        segMinBaseUs = baseUs;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            // Phase 1: buffer until all bases known
                            const int MaxBufferedPackets = 600;
                            if (!segAllBasesDiscovered)
                            {
                                var clone = ffmpeg.av_packet_clone(packet);
                                if (clone != null)
                                {
                                    segBufferedPackets.Add((IntPtr)clone);
                                    segBufferedStreamIndices.Add(streamIndex);
                                }

                                segAllBasesDiscovered = true;
                                for (int i = 0; i < streamCount; i++)
                                {
                                    if (streamMap[i] >= 0 && !segHasTimestampBase[i]) { segAllBasesDiscovered = false; break; }
                                }
                                if (!segAllBasesDiscovered && segBufferedPackets.Count >= MaxBufferedPackets)
                                {
                                    segMinBaseUs ??= 0; // Silent streams never set a base — default to 0
                                    segAllBasesDiscovered = true;
                                }

                                if (segAllBasesDiscovered)
                                {
                                    // Flush buffer
                                    var stopFlushing = false;
                                    for (int bi = 0; bi < segBufferedPackets.Count; bi++)
                                    {
                                        var buffPkt = (AVPacket*)segBufferedPackets[bi];
                                        var si = segBufferedStreamIndices[bi];
                                        var oi = streamMap[si];
                                        var outStr = _activeOutputContext->streams[oi];

                                        // Check outPoint against absolute PTS BEFORE remapping
                                        // At this point buffPkt->pts is in outStr->time_base but still absolute encoder PTS
                                        if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                                        {
                                            var absPtsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                            if (si == videoStreamIndex && absPtsUs > segAbsMaxPtsUs)
                                                segAbsMaxPtsUs = absPtsUs;
                                            if (outPtsLimitUs < long.MaxValue && si == videoStreamIndex && absPtsUs > outPtsLimitUs)
                                            {
                                                ffmpeg.av_packet_free(&buffPkt);
                                                segBufferedPackets[bi] = IntPtr.Zero;
                                                stopFlushing = true;
                                                continue;
                                            }
                                        }

                                        // Remap: subtract segment base, add cross-segment offset
                                        var segBaseTs = ffmpeg.av_rescale_q(segMinBaseUs!.Value, usTimeBase, outStr->time_base);
                                        var offsetTs = ffmpeg.av_rescale_q(outputPtsOffsetUs, usTimeBase, outStr->time_base);

                                        if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                                        {
                                            buffPkt->pts = buffPkt->pts - segBaseTs + offsetTs;
                                            // Track max PTS for offset calculation
                                            var ptsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                            if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                                        }
                                        if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                        {
                                            buffPkt->dts = buffPkt->dts - segBaseTs + offsetTs;
                                            if (oi < lastDtsPerStream.Length && lastDtsPerStream[oi] != long.MinValue && buffPkt->dts <= lastDtsPerStream[oi])
                                                buffPkt->dts = lastDtsPerStream[oi] + 1;
                                        }
                                        if (oi < lastDtsPerStream.Length && buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                            lastDtsPerStream[oi] = buffPkt->dts;

                                        buffPkt->pos = -1;
                                        buffPkt->stream_index = oi;
                                        ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                                        totalPackets++;
                                        ffmpeg.av_packet_free(&buffPkt);
                                        segBufferedPackets[bi] = IntPtr.Zero;
                                    }
                                    // Free remaining unflushed clones
                                    for (int bi = 0; bi < segBufferedPackets.Count; bi++)
                                    {
                                        var ptr = segBufferedPackets[bi];
                                        if (ptr != IntPtr.Zero)
                                        {
                                            var p = (AVPacket*)ptr;
                                            ffmpeg.av_packet_free(&p);
                                        }
                                    }
                                    segBufferedPackets.Clear();
                                    segBufferedStreamIndices.Clear();

                                    if (stopFlushing)
                                        break;
                                }
                                continue;
                            }

                            // Phase 2: inline write
                            var outStream2 = _activeOutputContext->streams[mappedIndex];

                            // Check outPoint against absolute PTS BEFORE remapping
                            // At this point packet->pts is in outStream2->time_base but still absolute encoder PTS
                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE && streamIndex == videoStreamIndex)
                            {
                                var absPtsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                if (absPtsUs > segAbsMaxPtsUs)
                                    segAbsMaxPtsUs = absPtsUs;
                                if (outPtsLimitUs < long.MaxValue && absPtsUs > outPtsLimitUs)
                                    break;
                            }

                            var segBase = ffmpeg.av_rescale_q(segMinBaseUs!.Value, usTimeBase, outStream2->time_base);
                            var offset = ffmpeg.av_rescale_q(outputPtsOffsetUs, usTimeBase, outStream2->time_base);

                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->pts = packet->pts - segBase + offset;
                                var ptsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                            }
                            if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->dts = packet->dts - segBase + offset;
                                // Enforce DTS monotonicity — mp4 muxer rejects non-monotonic DTS
                                if (mappedIndex < lastDtsPerStream.Length && lastDtsPerStream[mappedIndex] != long.MinValue && packet->dts <= lastDtsPerStream[mappedIndex])
                                    packet->dts = lastDtsPerStream[mappedIndex] + 1;
                            }
                            if (mappedIndex < lastDtsPerStream.Length && packet->dts != ffmpeg.AV_NOPTS_VALUE)
                                lastDtsPerStream[mappedIndex] = packet->dts;

                            packet->pos = -1;
                            packet->stream_index = mappedIndex;
                            ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, packet), "av_interleaved_write_frame");
                            totalPackets++;
                        }
                        finally
                        {
                            ffmpeg.av_packet_unref(packet);
                        }
                    }

                    // Free any remaining buffered packets (EOF before all bases discovered)
                    foreach (var pktPtr in segBufferedPackets)
                    {
                        if (pktPtr != IntPtr.Zero)
                        {
                            var p = (AVPacket*)pktPtr;
                            ffmpeg.av_packet_free(&p);
                        }
                    }

                    // Update cross-segment offset: next segment's PTS starts after this segment's max + one frame
                    if (segMaxPtsUs > outputPtsOffsetUs)
                    {
                        var videoStream = videoStreamIndex >= 0 ? _activeInputContext->streams[videoStreamIndex] : null;
                        long frameDurUs = (videoStream != null && videoStream->avg_frame_rate.num > 0)
                            ? 1_000_000L * videoStream->avg_frame_rate.den / videoStream->avg_frame_rate.num
                            : 33333; // fallback ~30fps
                        outputPtsOffsetUs = segMaxPtsUs + frameDurUs;
                    }

                    // Track bytes for progress
                    try { if (File.Exists(segPath)) bytesProcessed += new FileInfo(segPath).Length; }
                    catch { /* Best-effort: segment may be deleted mid-export; progress tracking is non-critical */ }

                    // Close this segment's input
                    CloseActiveInput();

                    progress?.Report(new ExportProgress(segIdx + 1, segmentPaths.Count,
                        totalEstimatedBytes > 0 ? 100.0 * bytesProcessed / totalEstimatedBytes : 100.0 * (segIdx + 1) / segmentPaths.Count));

                    Logger.Log($"FLASHBACK_EXPORT_SEGMENT_DONE seg={segIdx}/{segmentPaths.Count} path='{Path.GetFileName(segPath)}' packets={totalPackets}");

                    // If outPoint was hit, stop processing more segments
                    // Use absolute PTS (not rebased) since outPtsLimitUs is in absolute encoder time
                    if (outPtsLimitUs < long.MaxValue && segAbsMaxPtsUs >= outPtsLimitUs)
                        break;
                }
            }
            finally
            {
                var packetToFree = packet;
                ffmpeg.av_packet_free(&packetToFree);
            }

            if (totalPackets == 0)
            {
                const string message = "Flashback export failed: no packets were written from any segment.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
            CloseOutputIo();

            AtomicMoveTempFile(tmpPath, outputPath);
            _activeTempPath = null;

            var outputBytes = new FileInfo(outputPath).Length;
            Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_DONE output='{outputPath}' segments={segmentPaths.Count} packets={totalPackets} bytes={outputBytes}");
            progress?.Report(new ExportProgress(segmentPaths.Count, segmentPaths.Count, 100.0));
            return FinalizeResult.Success(outputPath, $"Exported {totalPackets} packets from {segmentPaths.Count} segments");
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
        finally
        {
            try { _exportLock.Release(); } catch (ObjectDisposedException) { }
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
        for (int bi = 0; bi < bufferedPackets.Count; bi++)
        {
            var buffPkt = (AVPacket*)bufferedPackets[bi];
            var si = bufferedStreamIndices[bi];
            var oi = streamMap[si];
            var outStr = _activeOutputContext->streams[oi];
            var bTs = ffmpeg.av_rescale_q(globalMinBaseUs, usTimeBase, outStr->time_base);
            if (buffPkt->pts != ffmpeg.AV_NOPTS_VALUE)
                buffPkt->pts -= bTs;
            if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                buffPkt->dts -= bTs;
            if (buffPkt->pts < 0) buffPkt->pts = 0;
            if (buffPkt->dts < 0) buffPkt->dts = 0;
            buffPkt->pos = -1;
            buffPkt->stream_index = oi;
            ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
            packetCounts[si]++;
            flushed++;
            ffmpeg.av_packet_free(&buffPkt);
            bufferedPackets[bi] = IntPtr.Zero;
        }
        bufferedPackets.Clear();
        bufferedStreamIndices.Clear();
        return flushed;
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
            /* Cleanup must not throw — close partially-opened input before re-throwing */
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

    /// <summary>
    /// Copies stream templates from input to output, skipping streams with invalid codec parameters
    /// (e.g., audio with 0 channels). Returns a mapping array: streamMap[inputIndex] = outputIndex, or -1 if skipped.
    /// </summary>
    private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext)
    {
        var inputStreamCount = checked((int)inputContext->nb_streams);
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

    internal static void CleanupOrphanedTempFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        try
        {
            foreach (var tmpFile in Directory.EnumerateFiles(directory, "*.mp4.tmp"))
            {
                try
                {
                    File.Delete(tmpFile);
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP deleted='{Path.GetFileName(tmpFile)}'");
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP_FAIL path='{Path.GetFileName(tmpFile)}' msg='{ex.Message}'");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_ORPHAN_SCAN_FAIL dir='{directory}' msg='{ex.Message}'");
        }
    }
}
