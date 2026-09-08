using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Pure Flashback export decisions over snapshots supplied by CaptureService.
/// The service retains synchronization, live sampling, file checks, diagnostics, and dispatch.
/// </summary>
internal static class FlashbackExportPlanner
{
    internal static FlashbackExportRangeResolution ResolveRange(
        FlashbackExportRangeSelection selection,
        FlashbackExportBufferTiming timing)
    {
        var validStart = timing.ValidStartPts;
        if (selection.InPointFilePts.HasValue || selection.OutPointFilePts.HasValue)
        {
            var absoluteInPoint = selection.InPointFilePts ?? validStart;
            var absoluteOutPoint = selection.OutPointFilePts ?? TimeSpan.MaxValue;
            if (absoluteInPoint < validStart)
            {
                return FlashbackExportRangeResolution.Failure(
                    absoluteInPoint,
                    absoluteOutPoint,
                    "Flashback export in point has been evicted from the buffer.");
            }

            if (absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= validStart)
            {
                return FlashbackExportRangeResolution.Failure(
                    absoluteInPoint,
                    absoluteOutPoint,
                    "Flashback export out point has been evicted from the buffer.");
            }

            return absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= absoluteInPoint
                ? FlashbackExportRangeResolution.Failure(
                    absoluteInPoint,
                    absoluteOutPoint,
                    "Flashback export range is empty or invalid.")
                : FlashbackExportRangeResolution.Success(absoluteInPoint, absoluteOutPoint);
        }

        var bufferInPoint = ClampBufferPosition(selection.InPoint ?? TimeSpan.Zero, timing.BufferedDuration);
        var bufferOutPoint = selection.OutPoint.HasValue
            ? ClampBufferPosition(selection.OutPoint.Value, timing.BufferedDuration)
            : TimeSpan.MaxValue;
        var fileInPoint = AddPtsOffsetOrMax(bufferInPoint, validStart);
        var fileOutPoint = AddPtsOffsetOrMax(bufferOutPoint, validStart);
        return fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint
            ? FlashbackExportRangeResolution.Failure(
                fileInPoint,
                fileOutPoint,
                "Flashback export range is empty or invalid.")
            : FlashbackExportRangeResolution.Success(fileInPoint, fileOutPoint);
    }

    internal static FlashbackExportRangeResolution ResolveLastNRange(
        double seconds,
        FlashbackExportBufferTiming timing)
    {
        var rangeStart = timing.BufferedDuration.TotalSeconds > seconds
            ? TimeSpan.FromSeconds(timing.BufferedDuration.TotalSeconds - seconds)
            : TimeSpan.Zero;
        return FlashbackExportRangeResolution.Success(
            AddPtsOffsetOrMax(rangeStart, timing.ValidStartPts),
            TimeSpan.MaxValue);
    }

    internal static bool NeedsStableSegmentPaths(FlashbackForceRotateResult? forceRotateResult)
        => forceRotateResult != null &&
           (forceRotateResult.Status is FlashbackForceRotateStatus.Failed or FlashbackForceRotateStatus.CommittedPending ||
            forceRotateResult.SegmentPaths.Count == 0);

    internal static FlashbackExportLiveEdgePlan PlanLiveEdge(
        FlashbackForceRotateResult? forceRotateResult,
        bool requireCompleteLiveEdge,
        IReadOnlyList<string>? stableSegmentPaths,
        IReadOnlyList<string> preservedArtifacts)
    {
        if (forceRotateResult?.Status == FlashbackForceRotateStatus.Failed)
        {
            return FlashbackExportLiveEdgePlan.Failure(
                "Flashback export failed: live-edge segment rotation failed.",
                preservedArtifacts,
                FlashbackExportPlanFailureKind.ForceRotateFailed);
        }

        if (forceRotateResult?.Status == FlashbackForceRotateStatus.CommittedPending)
        {
            return FlashbackExportLiveEdgePlan.Failure(
                requireCompleteLiveEdge
                    ? "Flashback recording finalize failed: live-edge segment was not closed before timeout."
                    : "Flashback export failed: live-edge segment rotation committed but did not complete before timeout.",
                preservedArtifacts,
                FlashbackExportPlanFailureKind.ForceRotateCommittedPending);
        }

        var segmentPaths = forceRotateResult == null
            ? null
            : forceRotateResult.SegmentPaths;
        if (segmentPaths is { Count: > 0 })
        {
            return FlashbackExportLiveEdgePlan.Ready(
                segmentPaths,
                forceRotateFallbackUsed: false);
        }

        if (forceRotateResult != null && requireCompleteLiveEdge)
        {
            return FlashbackExportLiveEdgePlan.Failure(
                "Flashback recording finalize failed: live-edge segment was not closed before timeout.",
                preservedArtifacts,
                FlashbackExportPlanFailureKind.IncompleteLiveEdge);
        }

        return FlashbackExportLiveEdgePlan.Ready(
            stableSegmentPaths,
            forceRotateFallbackUsed: stableSegmentPaths is { Count: > 0 });
    }

    internal static FlashbackExportRequestPlan CreateRequest(
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool force,
        IReadOnlyList<FlashbackExportPathSnapshot>? segmentPaths,
        IReadOnlyList<FlashbackExportSegmentMetadata> completedSegments,
        string? activeFilePath)
    {
        if (segmentPaths is not { Count: > 0 })
        {
            if (string.IsNullOrWhiteSpace(activeFilePath))
            {
                return FlashbackExportRequestPlan.Failure("Flashback buffer has no active file");
            }

            return FlashbackExportRequestPlan.Ready(
                new FlashbackExportRequest
                {
                    InputPath = activeFilePath,
                    InPoint = inPoint,
                    OutPoint = outPoint,
                    OutputPath = outputPath,
                    FastStart = false,
                    Force = force
                },
                usesActiveFileFallback: true);
        }

        return FlashbackExportRequestPlan.Ready(
            new FlashbackExportRequest
            {
                Segments = BuildSegments(completedSegments, segmentPaths),
                InPoint = inPoint,
                OutPoint = outPoint,
                OutputPath = outputPath,
                FastStart = false,
                Force = force
            },
            usesActiveFileFallback: false);
    }

    private static IReadOnlyList<FlashbackExportSegment> BuildSegments(
        IReadOnlyList<FlashbackExportSegmentMetadata> completedSegments,
        IReadOnlyList<FlashbackExportPathSnapshot> segmentPaths)
    {
        var metadataByPath = completedSegments
            .Where(segment => !string.IsNullOrWhiteSpace(segment.NormalizedPath))
            .GroupBy(segment => segment.NormalizedPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var segments = new List<FlashbackExportSegment>(segmentPaths.Count);
        foreach (var path in segmentPaths)
        {
            if (path.NormalizedPath != null && metadataByPath.TryGetValue(path.NormalizedPath, out var metadata))
            {
                var startPts = FromSegmentMilliseconds(metadata.StartPtsMs);
                var endPts = FromSegmentMilliseconds(metadata.EndPtsMs);
                if (endPts < startPts)
                {
                    endPts = startPts;
                }

                segments.Add(new FlashbackExportSegment
                {
                    Path = path.Path,
                    StartPts = startPts,
                    EndPts = endPts
                });
            }
            else
            {
                segments.Add(new FlashbackExportSegment { Path = path.Path });
            }
        }

        return segments;
    }

    private static TimeSpan ClampBufferPosition(TimeSpan position, TimeSpan bufferedDuration)
    {
        if (bufferedDuration <= TimeSpan.Zero || position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > bufferedDuration ? bufferedDuration : position;
    }

    private static TimeSpan AddPtsOffsetOrMax(TimeSpan position, TimeSpan offset)
    {
        if (position == TimeSpan.MaxValue || offset == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (offset <= TimeSpan.Zero)
        {
            return position;
        }

        return position > TimeSpan.MaxValue - offset ? TimeSpan.MaxValue : position + offset;
    }

    private static TimeSpan FromSegmentMilliseconds(long milliseconds)
        => milliseconds <= 0
            ? TimeSpan.Zero
            : milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
                ? TimeSpan.MaxValue
                : TimeSpan.FromMilliseconds(milliseconds);
}

internal readonly record struct FlashbackExportRangeSelection(
    TimeSpan? InPoint,
    TimeSpan? OutPoint,
    TimeSpan? InPointFilePts,
    TimeSpan? OutPointFilePts);

internal readonly record struct FlashbackExportBufferTiming(
    TimeSpan ValidStartPts,
    TimeSpan BufferedDuration);

internal readonly record struct FlashbackExportRangeResolution(
    bool Succeeded,
    TimeSpan InPoint,
    TimeSpan OutPoint,
    string? FailureMessage)
{
    public static FlashbackExportRangeResolution Success(TimeSpan inPoint, TimeSpan outPoint)
        => new(true, inPoint, outPoint, null);

    public static FlashbackExportRangeResolution Failure(
        TimeSpan inPoint,
        TimeSpan outPoint,
        string failureMessage)
        => new(false, inPoint, outPoint, failureMessage);
}

internal readonly record struct FlashbackExportPathSnapshot(string Path, string? NormalizedPath);

internal readonly record struct FlashbackExportSegmentMetadata(
    string NormalizedPath,
    long StartPtsMs,
    long EndPtsMs);

internal enum FlashbackExportPlanFailureKind
{
    None,
    ForceRotateFailed,
    ForceRotateCommittedPending,
    IncompleteLiveEdge
}

internal sealed record FlashbackExportLiveEdgePlan(
    IReadOnlyList<string>? SegmentPaths,
    string? FailureMessage,
    IReadOnlyList<string> PreservedArtifacts,
    FlashbackExportPlanFailureKind FailureKind,
    bool ForceRotateFallbackUsed)
{
    public static FlashbackExportLiveEdgePlan Ready(
        IReadOnlyList<string>? segmentPaths,
        bool forceRotateFallbackUsed)
        => new(segmentPaths, null, Array.Empty<string>(), FlashbackExportPlanFailureKind.None, forceRotateFallbackUsed);

    public static FlashbackExportLiveEdgePlan Failure(
        string failureMessage,
        IReadOnlyList<string> preservedArtifacts,
        FlashbackExportPlanFailureKind failureKind)
        => new(null, failureMessage, preservedArtifacts, failureKind, false);
}

internal sealed record FlashbackExportRequestPlan(
    FlashbackExportRequest? Request,
    string? FailureMessage,
    bool UsesActiveFileFallback)
{
    public static FlashbackExportRequestPlan Ready(FlashbackExportRequest request, bool usesActiveFileFallback)
        => new(request, null, usesActiveFileFallback);

    public static FlashbackExportRequestPlan Failure(string failureMessage)
        => new(null, failureMessage, false);
}
