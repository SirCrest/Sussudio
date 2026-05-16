using System;
using System.IO;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
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
