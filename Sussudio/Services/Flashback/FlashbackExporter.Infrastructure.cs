using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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
