using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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
}
