using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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
}
