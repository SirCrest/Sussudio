using System;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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

}
