using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Storage;

namespace ElgatoCapture.Services;

public sealed class RecordingArtifactManager
{
    public async Task<RecordingContext> CreateContextAsync(
        StorageFolder outputFolder,
        CaptureSettings settings,
        bool usePostMuxAudio,
        string? audioDeviceName,
        string? microphoneDeviceName,
        double effectiveFrameRate,
        string frameRateArg,
        uint effectiveWidth,
        uint effectiveHeight,
        string videoInputPixelFormat,
        bool isFullRangeInput = false,
        IntPtr d3d11DevicePtr = default,
        IntPtr d3d11DeviceContextPtr = default,
        IntPtr cudaHwDeviceCtxPtr = default,
        IntPtr cudaHwFramesCtxPtr = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputFolder);
        ArgumentNullException.ThrowIfNull(settings);

        cancellationToken.ThrowIfCancellationRequested();

        var finalOutputFile = await outputFolder.CreateFileAsync(
            settings.GetOutputFileName(),
            CreationCollisionOption.GenerateUniqueName);

        var hdrPipelineActive = string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);

        if (!usePostMuxAudio)
        {
            return new RecordingContext
            {
                Settings = settings,
                VideoOutputPath = finalOutputFile.Path,
                FinalOutputPath = finalOutputFile.Path,
                AudioTempPath = null,
                UsePostMuxAudio = false,
                AudioDeviceName = audioDeviceName,
                MicrophoneDeviceName = microphoneDeviceName,
                EffectiveFrameRate = effectiveFrameRate,
                FrameRateArg = frameRateArg,
                EffectiveWidth = effectiveWidth,
                EffectiveHeight = effectiveHeight,
                VideoInputPixelFormat = videoInputPixelFormat,
                HdrPipelineActive = hdrPipelineActive,
                IsFullRangeInput = isFullRangeInput,
                D3D11DevicePtr = d3d11DevicePtr,
                D3D11DeviceContextPtr = d3d11DeviceContextPtr,
                CudaHwDeviceCtxPtr = cudaHwDeviceCtxPtr,
                CudaHwFramesCtxPtr = cudaHwFramesCtxPtr
            };
        }

        var baseName = Path.GetFileNameWithoutExtension(finalOutputFile.Name);
        var extension = Path.GetExtension(finalOutputFile.Name);

        var tempVideoFile = await outputFolder.CreateFileAsync(
            $"{baseName}_video{extension}",
            CreationCollisionOption.GenerateUniqueName);

        var tempAudioFile = await outputFolder.CreateFileAsync(
            $"{baseName}_audio.m4a",
            CreationCollisionOption.GenerateUniqueName);

        return new RecordingContext
        {
            Settings = settings,
            VideoOutputPath = tempVideoFile.Path,
            FinalOutputPath = finalOutputFile.Path,
            AudioTempPath = tempAudioFile.Path,
            UsePostMuxAudio = true,
            AudioDeviceName = audioDeviceName,
            MicrophoneDeviceName = microphoneDeviceName,
            EffectiveFrameRate = effectiveFrameRate,
            FrameRateArg = frameRateArg,
            EffectiveWidth = effectiveWidth,
            EffectiveHeight = effectiveHeight,
            VideoInputPixelFormat = videoInputPixelFormat,
            HdrPipelineActive = hdrPipelineActive,
            IsFullRangeInput = isFullRangeInput,
            D3D11DevicePtr = d3d11DevicePtr,
            D3D11DeviceContextPtr = d3d11DeviceContextPtr,
            CudaHwDeviceCtxPtr = cudaHwDeviceCtxPtr,
            CudaHwFramesCtxPtr = cudaHwFramesCtxPtr
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
            return FinalizeResult.Success(context.FinalOutputPath, "Stopped");
        }

        if (muxSucceeded)
        {
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
}
