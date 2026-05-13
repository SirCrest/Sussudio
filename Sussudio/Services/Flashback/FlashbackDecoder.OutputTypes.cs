using System;
using System.Buffers;

namespace Sussudio.Services.Flashback;

/// <summary>
/// A decoded video frame. Either a D3D11 texture (GPU-direct) or raw NV12/P010 data.
/// For D3D11: <see cref="DecodedVideoFrame.TexturePtr"/> and <see cref="DecodedVideoFrame.SubresourceIndex"/> are valid.
/// For software: <see cref="DecodedVideoFrame.Data"/> is valid until the next decode call.
/// </summary>
internal readonly struct DecodedVideoFrame
{
    public IntPtr Data { get; init; }
    public int DataLength { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsHdr { get; init; }
    public TimeSpan Pts { get; init; }
    public IntPtr TexturePtr { get; init; }
    public int SubresourceIndex { get; init; }
    public bool IsD3D11Texture { get; init; }
    public IntPtr HeldFrame { get; init; } // AVFrame* that must be freed after texture is consumed
}

/// <summary>
/// A decoded audio chunk in f32le interleaved stereo 48kHz format.
/// <see cref="DecodedAudioChunk.Samples"/> is rented from <see cref="ArrayPool{T}"/> and should be returned when done.
/// </summary>
internal readonly struct DecodedAudioChunk
{
    public byte[] Samples { get; init; }
    public int ValidLength { get; init; }
    public TimeSpan Pts { get; init; }
}
