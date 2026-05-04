using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using FFmpeg.AutoGen;
using Sussudio.Services.Audio;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports a time range from a single .ts flashback file by remuxing to .mp4.
/// No re-encoding — just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe class FlashbackExporter : IDisposable
{
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
            return ExportSegmentsAsync(request.Segments, request.InPoint, request.OutPoint,
                request.OutputPath, request.FastStart, progress, ct);

        if (request.SegmentPaths is { Count: > 0 })
            return ExportSegmentsAsync(
                request.SegmentPaths.Select(path => new FlashbackExportSegment { Path = path }).ToArray(),
                request.InPoint,
                request.OutPoint,
                request.OutputPath,
                request.FastStart,
                progress,
                ct);

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
        CancellationTokenSource linkedCts;
        try
        {
            linkedCts = CreateExportCancellationSource(ct);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(CreateDisposedExportResult(outputPath));
        }

        return Task.Run(() =>
        {
            try
            {
                return ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, progress, linkedCts.Token);
            }
            finally
            {
                DisposeLinkedCtsBestEffort(linkedCts, "single_export");
            }
        });
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

        return Task.Run(() =>
        {
            try
            {
                return ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, progress, linkedCts.Token);
            }
            finally
            {
                DisposeLinkedCtsBestEffort(linkedCts, "segment_export");
            }
        });
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

        // Signal any running export to cancel — ExportCore/ExportSegmentsCore will exit
        // via OperationCanceledException, clean up native state in their own finally block,
        // and release _exportLock before we acquire it.
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
                ReleaseExportLockBestEffort("dispose");
        }

        DisposeExportLockBestEffort();
        DisposeLinkedCtsBestEffort(disposeCts, "dispose");
        ClearDisposeCtsReference(disposeCts);
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
        CleanupOrphanedTempFilesNearOutput(outputPath);

        if (IsSamePath(inputTsPath, outputPath))
        {
            var message = $"Flashback export failed: output path must not overwrite source segment '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        var tmpPath = outputPath + ".tmp";
        if (IsSamePath(inputTsPath, tmpPath))
        {
            var message = $"Flashback export failed: temporary output path must not overwrite source segment '{tmpPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryWaitForExportLock(outputPath, ct, out var cancellationResult))
        {
            return cancellationResult;
        }

        try
        {
        _activeTempPath = tmpPath;

        try
        {
            if (!TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{tempOutputFailure}'");
                return FinalizeResult.Failure(outputPath, tempOutputFailure);
            }

            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            Logger.Log($"FLASHBACK_EXPORT_START input='{inputTsPath}' in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");
            ReportProgress(progress, new ExportProgress(0, 1, 0), "single_start");

            // Open input .ts file
            OpenInput(inputTsPath);
            ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
            if (!TryGetInputStreamCount(_activeInputContext, "single_export", out var streamCount, out var streamCountFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'");
                return FinalizeResult.Failure(outputPath, streamCountFailure);
            }

            // Seek to inPoint
            if (inPoint > TimeSpan.Zero)
            {
                var seekTimestamp = ToAvTimeBaseTimestamp(inPoint);
                var seekResult = ffmpeg.av_seek_frame(_activeInputContext, -1, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                if (seekResult < 0)
                {
                    Logger.Log($"FLASHBACK_EXPORT_SEEK_WARN code={seekResult} target_ms={(long)inPoint.TotalMilliseconds}");
                }
            }

            // Create output .mp4 context
            CreateOutputContext(tmpPath, fastStart);
            var videoStreamIndex = FindVideoStreamIndex(_activeInputContext);
            var streamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount);
            OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);

            var timestampBasesUs = new long[streamCount]; // per-stream base in microseconds
            var hasTimestampBase = new bool[streamCount];
            long? globalMinBaseUs = null; // global minimum base in microseconds
            var packetCounts = new long[streamCount];
            long totalPackets = 0;
            var outPtsLimit = ToAvTimeBaseTimestampOrMax(outPoint);
            var usTimeBase = new AVRational { num = 1, den = 1_000_000 };

            // Read and remux packets — two-phase approach:
            // Phase 1: buffer packets until all stream timestamp bases are discovered (globalMinBaseUs is final)
            // Phase 2: flush buffer then process remaining packets inline with stable base
            var bufferedPackets = new List<IntPtr>();
            var bufferedStreamIndices = new List<int>();
            var allBasesDiscovered = false;
            var lastProgressHeartbeatTick = 0L;

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
                    if (ShouldReportProgressHeartbeat(ref lastProgressHeartbeatTick))
                    {
                        ReportProgress(progress, new ExportProgress(0, 1, 0), "single_heartbeat");
                    }

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
                            var clone = ClonePacketOrThrow(packet, "single_buffer");
                            bufferedPackets.Add((IntPtr)clone);
                            bufferedStreamIndices.Add(streamIndex);

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
                        NormalizePacketTimestampsBeforeWrite(packet);

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
                FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);
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

            if (!TryFinalizeTempOutputFile(tmpPath, outputPath, out var outputBytes, out var outputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputFailure}'");
                return FinalizeResult.Failure(outputPath, outputFailure);
            }
            _activeTempPath = null;

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
            DeleteTempFileIfPresent(tmpPath);
            _activeTempPath = null;
        }
        }
        finally
        {
            ReleaseExportLockBestEffort("single_export");
        }
    }

    private FinalizeResult ExportSegmentsCore(
        IReadOnlyList<FlashbackExportSegment> segments,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool fastStart,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return CreateCancelledExportResult(outputPath);
        }

        if (segments == null || segments.Count == 0)
        {
            const string message = "Flashback export failed: no segment paths provided.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{rangeFailure}'");
            return FinalizeResult.Failure(outputPath, rangeFailure);
        }

        var invalidSegmentIndex = FindInvalidSegmentPathIndex(segments);
        if (invalidSegmentIndex >= 0)
        {
            var message = $"Flashback export failed: segment path at index {invalidSegmentIndex} is empty.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);
        if (duplicateSegmentIndex >= 0)
        {
            var message = $"Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryValidateOutputPath(outputPath, out var normalizedOutputPath, out var outputPathFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'");
            return FinalizeResult.Failure(outputPath, outputPathFailure);
        }
        outputPath = normalizedOutputPath;
        CleanupOrphanedTempFilesNearOutput(outputPath);

        if (segments.Any(segment => IsSamePath(segment.Path, outputPath)))
        {
            var message = $"Flashback export failed: output path must not overwrite source segment '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        var tmpPath = outputPath + ".tmp";
        if (segments.Any(segment => IsSamePath(segment.Path, tmpPath)))
        {
            var message = $"Flashback export failed: temporary output path must not overwrite source segment '{tmpPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        // Estimate total bytes for progress
        long totalEstimatedBytes = 0;
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

        if (readableSegmentCount == 0)
        {
            var message = $"Flashback export failed: no readable segment files were available from {segments.Count} planned segments.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return FinalizeResult.Failure(outputPath, message);
        }

        if (!TryWaitForExportLock(outputPath, ct, out var cancellationResult))
        {
            return cancellationResult;
        }

        try
        {
        _activeTempPath = tmpPath;

        try
        {
            if (!TryPrepareTempOutputFile(tmpPath, outputPath, out var tempOutputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{tempOutputFailure}'");
                return FinalizeResult.Failure(outputPath, tempOutputFailure);
            }

            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            Logger.Log($"FLASHBACK_EXPORT_SEGMENTS_START segments={segments.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)} output='{outputPath}'");
            ReportProgress(progress, new ExportProgress(0, segments.Count, 0), "segments_start");

            var usTimeBase = new AVRational { num = 1, den = 1_000_000 };
            var outPtsLimitUs = ToAvTimeBaseTimestampOrMax(outPoint);

            // Output state — initialized from first segment
            int streamCount = 0;
            int videoStreamIndex = -1;
            int[] streamMap = Array.Empty<int>();
            long totalPackets = 0;
            long bytesProcessed = 0;
            var skippedRequestedSegmentCount = 0;
            string? firstSkippedRequestedSegmentReason = null;

            // Cross-segment PTS tracking (in microseconds)
            long outputPtsOffsetUs = 0; // accumulated offset for output continuity

            // Per-stream last DTS tracking for monotonicity enforcement
            var lastDtsPerStream = new long[64]; // indexed by OUTPUT stream index
            for (int i = 0; i < lastDtsPerStream.Length; i++) lastDtsPerStream[i] = long.MinValue;

            void TrackSkippedRequestedSegment(FlashbackExportSegment segment, string reason)
            {
                if (!SegmentOverlapsExportRange(segment, inPoint, outPoint))
                {
                    return;
                }

                skippedRequestedSegmentCount++;
                firstSkippedRequestedSegmentReason ??= reason;
            }

            bool TryInitializeSegmentOutputTemplate(
                out int selectedStreamCount,
                out int selectedVideoStreamIndex,
                out int[] selectedStreamMap,
                out string failureMessage)
            {
                selectedStreamCount = 0;
                selectedVideoStreamIndex = -1;
                selectedStreamMap = Array.Empty<int>();
                failureMessage = "Flashback export failed: no usable segment template was found.";

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

                        CreateOutputContext(tmpPath, fastStart);
                        selectedStreamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount);
                        Logger.Log($"FLASHBACK_EXPORT_STREAM_MAP video_idx={candidateVideoStreamIndex} map=[{string.Join(",", selectedStreamMap)}]");
                        OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);
                        selectedStreamCount = candidateStreamCount;
                        selectedVideoStreamIndex = candidateVideoStreamIndex;
                        Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SELECTED seg={templateSegIdx} path='{Path.GetFileName(templatePath)}'");
                        return true;
                    }
                    finally
                    {
                        CloseActiveInput();
                    }
                }

                return false;
            }

            if (!TryInitializeSegmentOutputTemplate(out streamCount, out videoStreamIndex, out streamMap, out var templateFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{templateFailure}'");
                return FinalizeResult.Failure(outputPath, templateFailure);
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
                    var useSegmentTimeline = segment.StartPts.HasValue;
                    var segmentInOffsetUs = useSegmentTimeline
                        ? ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value))
                        : 0;
                    var segmentOutDelta = useSegmentTimeline
                        ? SaturatingSubtract(
                            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,
                            segment.StartPts!.Value)
                        : TimeSpan.Zero;
                    var segmentOutOffsetUs = useSegmentTimeline
                        ? ToMicrosecondsSaturated(segmentOutDelta)
                        : outPtsLimitUs;
                    if (useSegmentTimeline && segmentOutDelta <= TimeSpan.Zero)
                    {
                        continue;
                    }

                    if (!File.Exists(segPath))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='not_found'");
                        TrackSkippedRequestedSegment(segment, "not_found");
                        continue;
                    }

                    // Open this segment
                    OpenInput(segPath);
                    ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
                    if (!TryGetInputStreamCount(_activeInputContext, "segment_export", out var currentStreamCount, out var streamCountFailure))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
                        TrackSkippedRequestedSegment(segment, "invalid_stream_count");
                        CloseActiveInput();
                        continue;
                    }

                    // Validate that this segment's stream layout matches the selected template.
                    // Mismatched layouts (e.g. microphone toggled mid-capture) would cause
                    // packet->stream_index to map incorrectly, producing corrupt output.
                    var segNbStreams = currentStreamCount;
                    if (segNbStreams != streamCount)
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='stream_count_mismatch' expected={streamCount} actual={segNbStreams}");
                        TrackSkippedRequestedSegment(segment, "stream_count_mismatch");
                        CloseActiveInput();
                        continue;
                    }

                    var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(
                        _activeInputContext,
                        _activeOutputContext,
                        streamMap,
                        segNbStreams);
                    if (streamLayoutMismatch != null)
                    {
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
                        TrackSkippedRequestedSegment(segment, "stream_layout_mismatch");
                        CloseActiveInput();
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

                    // Per-segment timestamp discovery (two-phase like single-file export)
                    var segTimestampBasesUs = new long[streamCount];
                    var segHasTimestampBase = new bool[streamCount];
                    long? segMinBaseUs = null;
                    var segBufferedPackets = new List<IntPtr>();
                    var segBufferedStreamIndices = new List<int>();
                    var segAllBasesDiscovered = false;
                    var lastProgressHeartbeatTick = 0L;
                    long segMaxPtsUs = 0; // track highest rebased PTS in this segment for offset calculation
                    long segAbsMaxPtsUs = 0; // track highest absolute PTS for outPoint check
                    long segmentVideoTimestampRepairUs = 0;
                    var segmentVideoPacketsSeen = 0;
                    var segmentVideoFrameDurUs = 33333L;
                    if (useSegmentTimeline &&
                        videoStreamIndex >= 0 &&
                        videoStreamIndex < currentStreamCount)
                    {
                        segmentVideoFrameDurUs = ResolveFrameDurationUs(_activeInputContext->streams[videoStreamIndex]);
                    }

                    // Flush segment-buffered packets (Phase 1 → Phase 2 transition or EOF rescue).
                    // Captures per-iteration locals via closure so both the mid-loop trigger and
                    // the EOF rescue path go through the same code.
                    int FlushSegmentBufferedPackets(out bool stopFlushing)
                    {
                        int written = 0;
                        stopFlushing = false;
                        try
                        {
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
                                    var comparePtsUs = useSegmentTimeline
                                        ? absPtsUs - segMinBaseUs!.Value
                                        : absPtsUs;
                                    if (si == videoStreamIndex && absPtsUs > segAbsMaxPtsUs)
                                        segAbsMaxPtsUs = absPtsUs;
                                    if (useSegmentTimeline && comparePtsUs < segmentInOffsetUs)
                                    {
                                        ffmpeg.av_packet_free(&buffPkt);
                                        segBufferedPackets[bi] = IntPtr.Zero;
                                        continue;
                                    }

                                    if (segmentOutOffsetUs < long.MaxValue && comparePtsUs > segmentOutOffsetUs)
                                    {
                                        ffmpeg.av_packet_free(&buffPkt);
                                        segBufferedPackets[bi] = IntPtr.Zero;
                                        if (si == videoStreamIndex)
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
                                    var ptsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                    if (useSegmentTimeline && si == videoStreamIndex)
                                    {
                                        var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                                            ptsUs,
                                            outputPtsOffsetUs,
                                            segmentVideoFrameDurUs,
                                            segmentVideoPacketsSeen,
                                            segmentVideoTimestampRepairUs);
                                        if (repairUs > 0)
                                        {
                                            segmentVideoTimestampRepairUs += repairUs;
                                            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={segIdx} stream={si} repair_us={repairUs} total_repair_us={segmentVideoTimestampRepairUs}");
                                        }

                                        if (segmentVideoTimestampRepairUs > 0)
                                        {
                                            var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStr->time_base);
                                            buffPkt->pts -= repairTs;
                                            ptsUs = ffmpeg.av_rescale_q(buffPkt->pts, outStr->time_base, usTimeBase);
                                        }

                                        segmentVideoPacketsSeen++;
                                    }

                                    // Track max PTS for offset calculation
                                    if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                                }
                                if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                {
                                    buffPkt->dts = buffPkt->dts - segBaseTs + offsetTs;
                                    if (useSegmentTimeline && si == videoStreamIndex && segmentVideoTimestampRepairUs > 0)
                                    {
                                        var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStr->time_base);
                                        buffPkt->dts -= repairTs;
                                    }
                                    if (oi < lastDtsPerStream.Length && lastDtsPerStream[oi] != long.MinValue && buffPkt->dts <= lastDtsPerStream[oi])
                                        buffPkt->dts = lastDtsPerStream[oi] + 1;
                                }
                                if (oi < lastDtsPerStream.Length && buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                                    lastDtsPerStream[oi] = buffPkt->dts;

                                NormalizePacketTimestampsBeforeWrite(buffPkt);
                                buffPkt->pos = -1;
                                buffPkt->stream_index = oi;
                                ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), "av_interleaved_write_frame");
                                written++;
                                ffmpeg.av_packet_free(&buffPkt);
                                segBufferedPackets[bi] = IntPtr.Zero;
                            }
                        }
                        finally
                        {
                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);
                        }

                        return written;
                    }

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
                                    segments.Count,
                                    totalEstimatedBytes > 0
                                        ? 100.0 * bytesProcessed / totalEstimatedBytes
                                        : 100.0 * segIdx / segments.Count),
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
                                var clone = ClonePacketOrThrow(packet, "segment_buffer");
                                segBufferedPackets.Add((IntPtr)clone);
                                segBufferedStreamIndices.Add(streamIndex);

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
                                    totalPackets += FlushSegmentBufferedPackets(out var stopFlushing);
                                    if (stopFlushing)
                                        break;
                                }
                                continue;
                            }

                            // Phase 2: inline write
                            var outStream2 = _activeOutputContext->streams[mappedIndex];

                            // Check outPoint against absolute PTS BEFORE remapping
                            // At this point packet->pts is in outStream2->time_base but still absolute encoder PTS
                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                var absPtsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                var comparePtsUs = useSegmentTimeline
                                    ? absPtsUs - segMinBaseUs!.Value
                                    : absPtsUs;
                                if (streamIndex == videoStreamIndex && absPtsUs > segAbsMaxPtsUs)
                                    segAbsMaxPtsUs = absPtsUs;
                                if (useSegmentTimeline && comparePtsUs < segmentInOffsetUs)
                                    continue;
                                if (segmentOutOffsetUs < long.MaxValue && comparePtsUs > segmentOutOffsetUs)
                                {
                                    if (streamIndex == videoStreamIndex)
                                        break;

                                    continue;
                                }
                            }

                            var segBase = ffmpeg.av_rescale_q(segMinBaseUs!.Value, usTimeBase, outStream2->time_base);
                            var offset = ffmpeg.av_rescale_q(outputPtsOffsetUs, usTimeBase, outStream2->time_base);

                            if (packet->pts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->pts = packet->pts - segBase + offset;
                                var ptsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                if (useSegmentTimeline && streamIndex == videoStreamIndex)
                                {
                                    var repairUs = ResolveSegmentBoundaryTimestampRepairUs(
                                        ptsUs,
                                        outputPtsOffsetUs,
                                        segmentVideoFrameDurUs,
                                        segmentVideoPacketsSeen,
                                        segmentVideoTimestampRepairUs);
                                    if (repairUs > 0)
                                    {
                                        segmentVideoTimestampRepairUs += repairUs;
                                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR seg={segIdx} stream={streamIndex} repair_us={repairUs} total_repair_us={segmentVideoTimestampRepairUs}");
                                    }

                                    if (segmentVideoTimestampRepairUs > 0)
                                    {
                                        var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStream2->time_base);
                                        packet->pts -= repairTs;
                                        ptsUs = ffmpeg.av_rescale_q(packet->pts, outStream2->time_base, usTimeBase);
                                    }

                                    segmentVideoPacketsSeen++;
                                }

                                if (ptsUs > segMaxPtsUs) segMaxPtsUs = ptsUs;
                            }
                            if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                            {
                                packet->dts = packet->dts - segBase + offset;
                                if (useSegmentTimeline && streamIndex == videoStreamIndex && segmentVideoTimestampRepairUs > 0)
                                {
                                    var repairTs = ffmpeg.av_rescale_q(segmentVideoTimestampRepairUs, usTimeBase, outStream2->time_base);
                                    packet->dts -= repairTs;
                                }
                                // Enforce DTS monotonicity — mp4 muxer rejects non-monotonic DTS
                                if (mappedIndex < lastDtsPerStream.Length && lastDtsPerStream[mappedIndex] != long.MinValue && packet->dts <= lastDtsPerStream[mappedIndex])
                                    packet->dts = lastDtsPerStream[mappedIndex] + 1;
                            }
                            if (mappedIndex < lastDtsPerStream.Length && packet->dts != ffmpeg.AV_NOPTS_VALUE)
                                lastDtsPerStream[mappedIndex] = packet->dts;

                            NormalizePacketTimestampsBeforeWrite(packet);
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

                    // EOF: if Phase 1 never completed (some configured stream — typically a
                    // silent mic — never produced packets and the buffer never reached the
                    // 600-packet cap), flush whatever we have using a fallback base of 0.
                    // Without this, every video packet in a short segment would be silently
                    // discarded by the FreeBufferedPackets path that used to live here.
                    if (!segAllBasesDiscovered && segBufferedPackets.Count > 0)
                    {
                        segMinBaseUs ??= 0;
                        segAllBasesDiscovered = true;
                        var discoveredCount = 0;
                        for (var i = 0; i < streamCount; i++) { if (segHasTimestampBase[i]) discoveredCount++; }
                        Logger.Log($"FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx} buffered={segBufferedPackets.Count} streams_discovered={discoveredCount}/{streamCount}");
                        totalPackets += FlushSegmentBufferedPackets(out _);
                    }
                    else
                    {
                        // Either Phase 1 completed inline (nothing to flush) or buffer is empty.
                        // FreeBufferedPackets is a no-op on an empty list; safe in both cases.
                        FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);
                    }

                    // Update cross-segment offset: next segment's PTS starts after this segment's max + one frame
                    if (segMaxPtsUs > outputPtsOffsetUs)
                    {
                        var videoStream = videoStreamIndex >= 0 ? _activeInputContext->streams[videoStreamIndex] : null;
                        long frameDurUs = ResolveFrameDurationUs(videoStream);
                        outputPtsOffsetUs = segMaxPtsUs + frameDurUs;
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

                    Logger.Log($"FLASHBACK_EXPORT_SEGMENT_OK seg={segIdx}/{segments.Count} path='{Path.GetFileName(segPath)}' packets={totalPackets} seg_max_pts_us={segMaxPtsUs} seg_abs_max_pts_us={segAbsMaxPtsUs} local_in_us={segmentInOffsetUs} local_out_us={segmentOutOffsetUs} bases_discovered={segAllBasesDiscovered}");

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

            if (skippedRequestedSegmentCount > 0)
            {
                var message = $"Flashback export failed: {skippedRequestedSegmentCount} requested segment(s) were skipped; first reason: {firstSkippedRequestedSegmentReason}.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            if (totalPackets == 0)
            {
                const string message = "Flashback export failed: no packets were written from any segment.";
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
                return FinalizeResult.Failure(outputPath, message);
            }

            ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
            CloseOutputIo();

            if (!TryFinalizeTempOutputFile(tmpPath, outputPath, out var outputBytes, out var outputFailure))
            {
                Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{outputFailure}'");
                return FinalizeResult.Failure(outputPath, outputFailure);
            }
            _activeTempPath = null;

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
            DeleteTempFileIfPresent(tmpPath);
            _activeTempPath = null;
        }
        }
        finally
        {
            ReleaseExportLockBestEffort("segment_export");
        }
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
                    buffPkt->pts -= bTs;
                if (buffPkt->dts != ffmpeg.AV_NOPTS_VALUE)
                    buffPkt->dts -= bTs;
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

            // Increase probe size for TS segments that may start mid-stream.
            // H.264 TS segments from RotateOutput may not have SPS/PPS at the very start
            // (NVENC pipeline latency can push the first IDR several frames in).
            // Default probesize (5MB) may not be enough for 4K@120fps H.264 — increase
            // to 20MB so avformat_find_stream_info can find the first IDR and extract
            // video dimensions and extradata.
            inputContext->probesize = 20 * 1024 * 1024;
            inputContext->max_analyze_duration = 5 * ffmpeg.AV_TIME_BASE; // 5 seconds
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

    private static bool TryFinalizeTempOutputFile(
        string tmpPath,
        string outputPath,
        out long outputBytes,
        out string failureMessage)
        => TryFinalizeTempOutputFileCore(
            tmpPath,
            outputPath,
            out outputBytes,
            out failureMessage,
            TryValidateCompletedOutputFile);

    private static bool TryFinalizeTempOutputFileCore(
        string tmpPath,
        string outputPath,
        out long outputBytes,
        out string failureMessage,
        CompletedOutputValidator validateOutput)
    {
        if (!validateOutput(tmpPath, out outputBytes, out _))
        {
            failureMessage = outputBytes == 0
                ? $"Flashback export failed: temporary output file is empty before replacing '{outputPath}'."
                : $"Flashback export failed: temporary output file length unavailable before replacing '{outputPath}'.";
            DeleteTempFileIfPresent(tmpPath);
            return false;
        }

        AtomicMoveTempFile(tmpPath, outputPath);

        if (!validateOutput(outputPath, out outputBytes, out failureMessage))
        {
            Logger.Log($"FLASHBACK_EXPORT_FINAL_OUTPUT_VALIDATE_WARN path='{outputPath}' reason='{failureMessage}'");
            DeleteInvalidFinalOutputIfPresent(outputPath, failureMessage);
            return false;
        }

        return true;
    }

    private static void DeleteInvalidFinalOutputIfPresent(string outputPath, string reason)
    {
        try
        {
            if (!File.Exists(outputPath))
            {
                return;
            }

            File.Delete(outputPath);
            Logger.Log($"FLASHBACK_EXPORT_FINAL_OUTPUT_DELETE_INVALID path='{outputPath}' reason='{reason}'");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_FINAL_OUTPUT_DELETE_INVALID_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'");
        }
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

    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
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
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{tmpPath}' type={ex.GetType().Name} msg='{ex.Message}'");
                return;
            }
        }

        // All retries exhausted on sharing violation — log and swallow.
        Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed_sharing_violation' path='{tmpPath}'");
    }

    private static bool TryPrepareTempOutputFile(string tmpPath, string outputPath, out string failureMessage)
    {
        if (Directory.Exists(tmpPath))
        {
            failureMessage = $"Flashback export failed: temporary output path is a directory '{tmpPath}'.";
            return false;
        }

        try
        {
            if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
            }
        }
        catch (Exception ex)
        {
            failureMessage = $"Flashback export failed: could not remove stale temporary output file before replacing '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_TMP_PREPARE_WARN path='{tmpPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }

        if (File.Exists(tmpPath) || Directory.Exists(tmpPath))
        {
            failureMessage = $"Flashback export failed: stale temporary output path could not be cleared '{tmpPath}'.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
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

    private static void CleanupOrphanedTempFilesNearOutput(string outputPath)
    {
        try
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                CleanupOrphanedTempFiles(outputDirectory);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_ORPHAN_OUTPUT_SCAN_FAIL path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'");
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
}
