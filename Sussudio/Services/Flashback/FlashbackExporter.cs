using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports a time range from a single .ts flashback file by remuxing to .mp4.
/// No re-encoding — just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe partial class FlashbackExporter : IDisposable
{
    // Export reads finalized segment artifacts only. Live capture continues via
    // FlashbackEncoderSink while this class remuxes packets into the target MP4.
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int MaxSupportedInputStreams = 64;
    private const int ProgressHeartbeatIntervalMs = 1_000;
    private const int ExportLockWaitTimeoutSeconds = 30;
    private const int ExportWriterYieldPacketInterval = 256;
    private const int ExportWriterThrottlePacketInterval = 4096;
    private const int ExportWriterThrottleSleepMs = 1;
    private const int ExportWriterAdaptiveThrottlePacketInterval = 4;
    private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);
    [ThreadStatic]
    private static Func<int>? s_adaptiveThrottleDelayMsProvider;

    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly object _lifetimeSync = new();
    private readonly object _adaptiveThrottleSync = new();
    private CancellationTokenSource? _disposeCts = new();
    private Func<int>? _nextAdaptiveThrottleDelayMsProvider;
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
                        ThrottleExportWriterIfNeeded(totalPackets);
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

            if (!TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
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
        bool allowOverwrite,
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
                                ThrottleExportWriterIfNeeded(written);
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
                            ThrottleExportWriterIfNeeded(totalPackets);
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

            if (!TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
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

}
