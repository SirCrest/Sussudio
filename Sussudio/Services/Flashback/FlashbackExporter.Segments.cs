using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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

        if (!TryValidateSegmentExportInputs(
                segments,
                inPoint,
                outPoint,
                outputPath,
                out var normalizedOutputPath,
                out var validationFailure))
        {
            return validationFailure!;
        }
        outputPath = normalizedOutputPath;

        var tmpPath = outputPath + ".tmp";

        if (!TryEstimateSegmentExportReadableBytes(
                segments,
                outputPath,
                out var totalEstimatedBytes,
                out var estimateFailure))
        {
            return estimateFailure!;
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

            var packetWriteResult = WriteSegmentPacketsToActiveOutput(
                segments,
                inPoint,
                outPoint,
                tmpPath,
                outputPath,
                fastStart,
                totalEstimatedBytes,
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
