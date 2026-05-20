using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public string RendererMode { get; init; } = "None";
    public int D3DPresentSyncInterval { get; init; }
    public int D3DMaxFrameLatency { get; init; }
    public int D3DSwapChainBufferCount { get; init; }
    public string D3DSwapChainAddress { get; init; } = string.Empty;
    public long D3DRenderThreadFailureCount { get; init; }
    public string D3DLastRenderThreadFailureType { get; init; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; init; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; init; }
    public int D3DPendingFrameCount { get; init; }
    public string D3DInputColorSpace { get; init; } = "None";
    public string D3DOutputColorSpace { get; init; } = "None";
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string GpuPlaybackState { get; init; } = "None";
    public int GpuNaturalVideoWidth { get; init; }
    public int GpuNaturalVideoHeight { get; init; }
    public double GpuPositionMs { get; init; }
}
