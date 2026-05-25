using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
}
