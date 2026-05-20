using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public string RendererMode { get; private set; } = "None";
    public int D3DPresentSyncInterval { get; private set; }
    public int D3DMaxFrameLatency { get; private set; }
    public int D3DSwapChainBufferCount { get; private set; }
    public string D3DSwapChainAddress { get; private set; } = string.Empty;
    public long D3DRenderThreadFailureCount { get; private set; }
    public string D3DLastRenderThreadFailureType { get; private set; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; private set; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; private set; }
    public int D3DPendingFrameCount { get; private set; }
    public string D3DInputColorSpace { get; private set; } = "None";
    public string D3DOutputColorSpace { get; private set; } = "None";
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; private set; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string GpuPlaybackState { get; private set; } = "None";
    public int GpuNaturalVideoWidth { get; private set; }
    public int GpuNaturalVideoHeight { get; private set; }
    public double GpuPositionMs { get; private set; }

    private void ApplyRendererState(PreviewRuntimeD3DRendererState rendererState)
    {
        RendererMode = rendererState.RendererMode;
        D3DPresentSyncInterval = rendererState.PresentSyncInterval;
        D3DMaxFrameLatency = rendererState.MaxFrameLatency;
        D3DSwapChainBufferCount = rendererState.SwapChainBufferCount;
        D3DSwapChainAddress = rendererState.SwapChainAddress;
        D3DRenderThreadFailureCount = rendererState.RenderThreadFailureCount;
        D3DLastRenderThreadFailureType = rendererState.LastRenderThreadFailureType;
        D3DLastRenderThreadFailureMessage = rendererState.LastRenderThreadFailureMessage;
        D3DLastRenderThreadFailureHResult = rendererState.LastRenderThreadFailureHResult;
        D3DPendingFrameCount = rendererState.PendingFrameCount;
        D3DInputColorSpace = rendererState.InputColorSpace;
        D3DOutputColorSpace = rendererState.OutputColorSpace;
        D3DRecentSlowFrames = rendererState.RecentSlowFrames;
        GpuPlaybackState = rendererState.GpuPlaybackState;
        GpuNaturalVideoWidth = rendererState.NaturalVideoWidth;
        GpuNaturalVideoHeight = rendererState.NaturalVideoHeight;
        GpuPositionMs = rendererState.PositionMs;
    }
}
