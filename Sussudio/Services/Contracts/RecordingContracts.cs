using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Contracts;

public readonly record struct GpuPipelineHandles(
    IntPtr D3D11DevicePtr,
    IntPtr D3D11DeviceContextPtr,
    IntPtr CudaHwDeviceCtxPtr,
    IntPtr CudaHwFramesCtxPtr)
{
    public static GpuPipelineHandles None => default;
}

// Caller-built input describing the recording the user has asked for. Path
// resolution and HDR-pipeline negotiation happen downstream and produce a
// RecordingContext. Kept distinct so input shape and resolved-execution shape
// don't leak into each other.
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
    public RecordingFormat? FileNameFormatOverride { get; init; }
}

// Resolved recording-execution context. Flat record: every field is held
// directly rather than forwarded through a wrapped Request, so consumers see
// one boundary type with no duplicated surface.
public sealed record RecordingContext
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
    public RecordingFormat? FileNameFormatOverride { get; init; }

    public required string VideoOutputPath { get; init; }
    public required string FinalOutputPath { get; init; }
    public string? AudioTempPath { get; init; }
    public bool HdrPipelineActive { get; init; }

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

/// <summary>
/// Accepts D3D11 texture references for GPU-resident NVENC encoding.
/// Callee does AddRef on the texture; caller may release after return.
/// </summary>
public interface IGpuVideoFrameEncoder
{
    void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex);
}

public interface IGpuVideoFrameTryEncoder
{
    bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex);
}

public interface IRawVideoFrameEncoder
{
    void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize);
}

public interface IRawVideoFrameTryEncoder
{
    bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize);
}

internal interface IRawVideoFrameLeaseEncoder
{
    void EnqueueRawVideoFrame(PooledVideoFrameLease frame);
}

internal interface IRawVideoFrameLeaseTryEncoder
{
    bool TryEnqueueRawVideoFrame(PooledVideoFrameLease frame);
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

    /// <summary>
    /// Hot WASAPI callback write. Implementations must copy or enqueue
    /// synchronously, must not do blocking/async work, and must return a
    /// completed task. The Task return preserves the existing contract shape.
    /// </summary>
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
