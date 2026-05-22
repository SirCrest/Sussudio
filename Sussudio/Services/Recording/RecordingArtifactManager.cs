using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;

namespace Sussudio.Services.Recording;

// Creates the file paths for a recording attempt. It keeps temp video/audio
// artifacts, final output naming, and HDR-active state in one place so sinks
// only write bytes.
public sealed class RecordingArtifactManager
{
    public async Task<RecordingContext> CreateContextAsync(
        StorageFolder outputFolder,
        RecordingContextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputFolder);
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var settings = request.Settings;
        var outputFileName = request.FileNameFormatOverride is { } fileNameFormatOverride
            ? settings.GetOutputFileNameForFormat(fileNameFormatOverride)
            : settings.GetOutputFileName();
        var finalOutputFile = await outputFolder.CreateFileAsync(
            outputFileName,
            CreationCollisionOption.GenerateUniqueName);

        var hdrPipelineActive = string.Equals(request.VideoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);

        if (!request.UsePostMuxAudio)
        {
            return BuildContext(request, finalOutputFile.Path, finalOutputFile.Path, null, hdrPipelineActive);
        }

        var baseName = Path.GetFileNameWithoutExtension(finalOutputFile.Name);
        var extension = Path.GetExtension(finalOutputFile.Name);

        var tempVideoFile = await outputFolder.CreateFileAsync(
            $"{baseName}_video{extension}",
            CreationCollisionOption.GenerateUniqueName);

        var tempAudioFile = await outputFolder.CreateFileAsync(
            $"{baseName}_audio.m4a",
            CreationCollisionOption.GenerateUniqueName);

        return BuildContext(request, tempVideoFile.Path, finalOutputFile.Path, tempAudioFile.Path, hdrPipelineActive);
    }

    private static RecordingContext BuildContext(
        RecordingContextRequest request,
        string videoOutputPath,
        string finalOutputPath,
        string? audioTempPath,
        bool hdrPipelineActive)
    {
        return new RecordingContext
        {
            Settings = request.Settings,
            UsePostMuxAudio = request.UsePostMuxAudio,
            AudioDeviceName = request.AudioDeviceName,
            MicrophoneDeviceName = request.MicrophoneDeviceName,
            EffectiveFrameRate = request.EffectiveFrameRate,
            FrameRateArg = request.FrameRateArg,
            EffectiveWidth = request.EffectiveWidth,
            EffectiveHeight = request.EffectiveHeight,
            VideoInputPixelFormat = request.VideoInputPixelFormat,
            IsFullRangeInput = request.IsFullRangeInput,
            GpuHandles = request.GpuHandles,
            FileNameFormatOverride = request.FileNameFormatOverride,
            VideoOutputPath = videoOutputPath,
            FinalOutputPath = finalOutputPath,
            AudioTempPath = audioTempPath,
            HdrPipelineActive = hdrPipelineActive,
        };
    }

    public FinalizeResult FinalizeContext(
        RecordingContext context,
        bool muxSucceeded,
        string? muxFailureReason = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.UsePostMuxAudio)
        {
            if (!TryValidateFinalOutput(context.FinalOutputPath, out var directOutputFailure))
            {
                return FinalizeResult.Failure(
                    context.FinalOutputPath,
                    $"Stopped (final output invalid: {directOutputFailure})");
            }

            return FinalizeResult.Success(context.FinalOutputPath, "Stopped");
        }

        if (muxSucceeded)
        {
            if (!TryValidateFinalOutput(context.FinalOutputPath, out var muxedOutputFailure))
            {
                return FinalizeResult.Failure(
                    context.FinalOutputPath,
                    $"Stopped (final output invalid: {muxedOutputFailure})",
                    GetExistingTempArtifacts(context));
            }

            TryDelete(context.VideoOutputPath);
            TryDelete(context.AudioTempPath);
            return FinalizeResult.Success(context.FinalOutputPath, "Stopped");
        }

        // When mux fails we preserve the temp artifacts for recovery and remove any
        // empty final placeholder file to avoid surfacing a misleading output.
        TryDeleteIfEmpty(context.FinalOutputPath);

        var preserved = new List<string>();
        if (File.Exists(context.VideoOutputPath))
        {
            preserved.Add(context.VideoOutputPath);
        }
        if (!string.IsNullOrWhiteSpace(context.AudioTempPath) && File.Exists(context.AudioTempPath))
        {
            preserved.Add(context.AudioTempPath);
        }

        var reason = string.IsNullOrWhiteSpace(muxFailureReason) ? "mux failed" : muxFailureReason;
        return FinalizeResult.Failure(
            context.FinalOutputPath,
            $"Stopped (mux failed: {reason})",
            preserved);
    }

    public Task RollbackAsync(RecordingContext? context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context == null)
        {
            return Task.CompletedTask;
        }

        TryDelete(context.VideoOutputPath);

        if (context.UsePostMuxAudio)
        {
            TryDelete(context.AudioTempPath);
            TryDelete(context.FinalOutputPath);
        }

        return Task.CompletedTask;
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to delete file '{path}': {ex.Message}");
        }
    }

    private static void TryDeleteIfEmpty(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            var info = new FileInfo(path);
            if (info.Length == 0)
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to cleanup empty final output '{path}': {ex.Message}");
        }
    }

    private static bool TryValidateFinalOutput(string path, out string failureMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            failureMessage = "output path is empty";
            return false;
        }

        try
        {
            if (!File.Exists(path))
            {
                failureMessage = "output file is missing";
                return false;
            }

            var info = new FileInfo(path);
            if (info.Length <= 0)
            {
                failureMessage = "output file is empty";
                return false;
            }
        }
        catch (Exception ex)
        {
            failureMessage = $"output file length unavailable: {ex.Message}";
            Logger.Log($"Recording final output validation failed for '{path}': {ex.Message}");
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static IReadOnlyList<string> GetExistingTempArtifacts(RecordingContext context)
    {
        var preserved = new List<string>();
        if (File.Exists(context.VideoOutputPath))
        {
            preserved.Add(context.VideoOutputPath);
        }
        if (!string.IsNullOrWhiteSpace(context.AudioTempPath) && File.Exists(context.AudioTempPath))
        {
            preserved.Add(context.AudioTempPath);
        }

        return preserved;
    }
}
