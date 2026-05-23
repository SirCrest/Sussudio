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
