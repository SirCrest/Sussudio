using System;
using System.IO;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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

        // All retries exhausted on sharing violation - log and swallow.
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

        // Refuse-on-collision is enforced here before the atomic move so the
        // existing destination is preserved when the caller did not opt in to
        // overwrite. AtomicMoveTempFile throws IOException for the refusal path.
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
}
