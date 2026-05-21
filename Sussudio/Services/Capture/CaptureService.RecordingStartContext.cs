using System;
using System.Threading.Tasks;
using Windows.Storage;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static async Task<StorageFolder> OpenRecordingOutputFolderAsync(CaptureSettings settings)
    {
        try
        {
            return await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
        }
    }

    private async Task<RecordingContext> CreateLibAvRecordingContextAsync(
        CaptureSettings settings,
        StorageFolder outputFolder,
        UnifiedVideoCapture unifiedVideoCapture,
        string? audioDeviceName,
        double recordingFrameRate,
        string videoInputPixelFormat,
        bool isMjpegMode)
    {
        var d3dManager = unifiedVideoCapture.D3DManager;
        var recordingWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
        var recordingHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
        return await _artifactManager.CreateContextAsync(
            outputFolder,
            new RecordingContextRequest
            {
                Settings = settings,
                UsePostMuxAudio = false,
                AudioDeviceName = audioDeviceName,
                MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                EffectiveFrameRate = recordingFrameRate,
                FrameRateArg = ResolveFrameRateArg(settings, recordingFrameRate),
                EffectiveWidth = recordingWidth,
                EffectiveHeight = recordingHeight,
                VideoInputPixelFormat = videoInputPixelFormat,
                IsFullRangeInput = isMjpegMode,
                GpuHandles = new GpuPipelineHandles(
                    isMjpegMode ? IntPtr.Zero : (d3dManager?.Device.NativePointer ?? IntPtr.Zero),
                    isMjpegMode ? IntPtr.Zero : (d3dManager?.ImmediateContext.NativePointer ?? IntPtr.Zero),
                    IntPtr.Zero,
                    IntPtr.Zero)
            }).ConfigureAwait(false);
    }

    private async Task<RecordingContext> CreateFlashbackRecordingContextAsync(
        CaptureSettings settings,
        StorageFolder outputFolder,
        double effectiveFrameRate)
        => await _artifactManager.CreateContextAsync(
            outputFolder,
            new RecordingContextRequest
            {
                Settings = settings,
                UsePostMuxAudio = false,
                AudioDeviceName = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice?.AudioDeviceName))
                    : null,
                MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                EffectiveFrameRate = effectiveFrameRate,
                FrameRateArg = ResolveFrameRateArg(settings, effectiveFrameRate),
                EffectiveWidth = _actualWidth ?? settings.Width,
                EffectiveHeight = _actualHeight ?? settings.Height,
                VideoInputPixelFormat = _unifiedVideoCapture?.IsP010 == true ? "p010le" : "nv12",
                IsFullRangeInput = _unifiedVideoCapture?.IsSoftwareMjpegPipelineActive == true,
                GpuHandles = GpuPipelineHandles.None
            }).ConfigureAwait(false);
}
