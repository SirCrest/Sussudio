using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

// Recording-internal frame-encoder contracts. The hot-path raw and CUDA
// encoders sit below the IRecordingSink boundary (which lives in
// Sussudio.Services.Contracts) and are consumed only by recording producers
// inside the Sussudio assembly. PooledVideoFrameLease comes in via the
// Contracts namespace.

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
