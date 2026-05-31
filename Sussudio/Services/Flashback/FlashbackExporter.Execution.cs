using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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

                CreateOutputContext(tmpPath, fastStart);
                var videoStreamIndex = FindVideoStreamIndex(_activeInputContext);
                var streamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount);
                OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);

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
                if (!TryFinalizeActiveOutputFile(tmpPath, outputPath, allowOverwrite, out var outputBytes, out var outputFailure))
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
                DeleteTempFileIfPresent(tmpPath);
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

    private static void AtomicMoveTempFile(string tmpPath, string outputPath, bool allowOverwrite)
    {
        if (!File.Exists(tmpPath))
        {
            throw new IOException($"Temporary export file was not created: '{tmpPath}'.");
        }

        var destinationExists = File.Exists(outputPath);
        if (destinationExists && !allowOverwrite)
        {
            Logger.Log(
                $"FLASHBACK_EXPORT_REFUSED_DESTINATION_EXISTS path='{outputPath}' " +
                "reason='destination_exists' force=false");
            DeleteTempFileIfPresent(tmpPath);
            throw new IOException(
                $"Flashback export failed: destination file already exists at '{outputPath}'. " +
                "Pass force=true to overwrite an existing export.");
        }

        if (destinationExists)
        {
            Logger.Log($"FLASHBACK_EXPORT_OVERWRITE path='{outputPath}' force=true");
        }

        File.Move(tmpPath, outputPath, overwrite: true);
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
        string tmpPath,
        string outputPath,
        bool allowOverwrite,
        out long outputBytes,
        out string failureMessage)
    {
        ThrowIfError(ffmpeg.av_write_trailer(_activeOutputContext), "av_write_trailer");
        CloseOutputIo();

        if (!TryFinalizeTempOutputFile(tmpPath, outputPath, allowOverwrite, out outputBytes, out failureMessage))
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
        if (!validateOutput(tmpPath, out outputBytes, out _))
        {
            failureMessage = outputBytes == 0
                ? $"Flashback export failed: temporary output file is empty before replacing '{outputPath}'."
                : $"Flashback export failed: temporary output file length unavailable before replacing '{outputPath}'.";
            DeleteTempFileIfPresent(tmpPath);
            return false;
        }

        try
        {
            AtomicMoveTempFile(tmpPath, outputPath, allowOverwrite);
        }
        catch (IOException ex)
        {
            failureMessage = ex.Message;
            return false;
        }

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

        var tempOutputPath = fullOutputPath + ".tmp";
        if (segments.Any(segment => IsSamePath(segment.Path, tempOutputPath)))
        {
            var message = $"Flashback export failed: temporary output path must not overwrite source segment '{tempOutputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            failure = FinalizeResult.Failure(fullOutputPath, message);
            return false;
        }

        return true;
    }

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
}
