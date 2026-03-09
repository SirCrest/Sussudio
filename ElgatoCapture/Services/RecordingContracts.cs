using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using FFmpeg.AutoGen;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

public sealed class RecordingContext
{
    public required CaptureSettings Settings { get; init; }
    public required string VideoOutputPath { get; init; }
    public required string FinalOutputPath { get; init; }
    public string? AudioTempPath { get; init; }
    public bool UsePostMuxAudio { get; init; }
    public string? AudioDeviceName { get; init; }
    public double EffectiveFrameRate { get; init; }
    public string FrameRateArg { get; init; } = "30";
    public uint EffectiveWidth { get; init; }
    public uint EffectiveHeight { get; init; }
    public string VideoInputPixelFormat { get; init; } = "nv12";
    public bool HdrPipelineActive { get; init; }
    public IntPtr D3D11DevicePtr { get; init; }
    public IntPtr D3D11DeviceContextPtr { get; init; }
    public IntPtr CudaHwDeviceCtxPtr { get; init; }
    public IntPtr CudaHwFramesCtxPtr { get; init; }
}

public sealed class FinalizeResult
{
    private static readonly IReadOnlyList<string> EmptyArtifacts = Array.Empty<string>();

    public bool Succeeded { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string StatusMessage { get; init; } = "Stopped";
    public IReadOnlyList<string> PreservedArtifacts { get; init; } = EmptyArtifacts;

    public static FinalizeResult Success(string outputPath, string statusMessage = "Stopped")
    {
        return new FinalizeResult
        {
            Succeeded = true,
            OutputPath = outputPath,
            StatusMessage = statusMessage,
            PreservedArtifacts = EmptyArtifacts
        };
    }

    public static FinalizeResult Failure(
        string outputPath,
        string statusMessage,
        IEnumerable<string>? preservedArtifacts = null)
    {
        var artifacts = preservedArtifacts == null
            ? EmptyArtifacts
            : new ReadOnlyCollection<string>(
                preservedArtifacts
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        return new FinalizeResult
        {
            Succeeded = false,
            OutputPath = outputPath,
            StatusMessage = statusMessage,
            PreservedArtifacts = artifacts
        };
    }
}

public interface IRawVideoFrameEncoder
{
    void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize);
}

/// <summary>
/// Accepts D3D11 texture references for GPU-resident NVENC encoding.
/// Callee does AddRef on the texture; caller may release after return.
/// </summary>
public interface IGpuVideoFrameEncoder
{
    void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex);
}

/// <summary>
/// Accepts decoded CUDA AVFrame references for GPU-resident NVENC encoding.
/// Callee clones the frame; caller retains ownership.
/// </summary>
public unsafe interface ICudaVideoFrameEncoder
{
    void EnqueueCudaVideoFrame(AVFrame* cudaFrame);
}

public interface IRecordingSink : IDisposable, IAsyncDisposable
{
    Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default);
    Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default);
    Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default);
    Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default);
}
