using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;

namespace Sussudio.Services.Recording;

// Creates the file paths for a recording attempt. It keeps temp video/audio
// artifacts, final output naming, and HDR-active state in one place so sinks
// only write bytes.
public sealed partial class RecordingArtifactManager
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
}
