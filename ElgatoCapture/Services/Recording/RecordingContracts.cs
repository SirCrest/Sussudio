using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using FFmpeg.AutoGen;
namespace ElgatoCapture.Services;

public readonly record struct GpuPipelineHandles(
    IntPtr D3D11DevicePtr,
    IntPtr D3D11DeviceContextPtr,
    IntPtr CudaHwDeviceCtxPtr,
    IntPtr CudaHwFramesCtxPtr)
{
    public static GpuPipelineHandles None => default;
}

public sealed class RecordingContextRequest
{
    public required CaptureSettings Settings { get; init; }
    public bool UsePostMuxAudio { get; init; }
    public string? AudioDeviceName { get; init; }
    public string? MicrophoneDeviceName { get; init; }
    public double EffectiveFrameRate { get; init; }
    public string FrameRateArg { get; init; } = "30";
    public uint EffectiveWidth { get; init; }
    public uint EffectiveHeight { get; init; }
    public string VideoInputPixelFormat { get; init; } = "nv12";
    public bool IsFullRangeInput { get; init; }
    public GpuPipelineHandles GpuHandles { get; init; }
}

public sealed class RecordingContext
{
    public RecordingContext(
        RecordingContextRequest request,
        string videoOutputPath,
        string finalOutputPath,
        string? audioTempPath,
        bool hdrPipelineActive)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        VideoOutputPath = videoOutputPath;
        FinalOutputPath = finalOutputPath;
        AudioTempPath = audioTempPath;
        HdrPipelineActive = hdrPipelineActive;
    }

    /// <summary>The original request that produced this context.</summary>
    public RecordingContextRequest Request { get; }

    // ── Extra fields not present on the request ─────────────────────
    public string VideoOutputPath { get; }
    public string FinalOutputPath { get; }
    public string? AudioTempPath { get; }
    public bool HdrPipelineActive { get; }

    // ── Forwarding properties from Request ──────────────────────────
    public CaptureSettings Settings => Request.Settings;
    public bool UsePostMuxAudio => Request.UsePostMuxAudio;
    public string? AudioDeviceName => Request.AudioDeviceName;
    public string? MicrophoneDeviceName => Request.MicrophoneDeviceName;
    public double EffectiveFrameRate => Request.EffectiveFrameRate;
    public string FrameRateArg => Request.FrameRateArg;
    public uint EffectiveWidth => Request.EffectiveWidth;
    public uint EffectiveHeight => Request.EffectiveHeight;
    public string VideoInputPixelFormat => Request.VideoInputPixelFormat;
    public bool IsFullRangeInput => Request.IsFullRangeInput;
    public GpuPipelineHandles GpuHandles => Request.GpuHandles;

    // Convenience accessors for existing consumers.
    public IntPtr D3D11DevicePtr => GpuHandles.D3D11DevicePtr;
    public IntPtr D3D11DeviceContextPtr => GpuHandles.D3D11DeviceContextPtr;
    public IntPtr CudaHwDeviceCtxPtr => GpuHandles.CudaHwDeviceCtxPtr;
    public IntPtr CudaHwFramesCtxPtr => GpuHandles.CudaHwFramesCtxPtr;
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
    Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default);
    Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default);
}

public interface IRecordingVerifier
{
    Task<RecordingVerificationResult> VerifyAsync(
        string? outputPath,
        CaptureRuntimeSnapshot runtimeSnapshot,
        CancellationToken cancellationToken = default);
}
