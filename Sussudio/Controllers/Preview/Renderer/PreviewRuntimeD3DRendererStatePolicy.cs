using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DRendererState(
    string RendererMode,
    int PresentSyncInterval,
    int MaxFrameLatency,
    int SwapChainBufferCount,
    string SwapChainAddress,
    long RenderThreadFailureCount,
    string LastRenderThreadFailureType,
    string LastRenderThreadFailureMessage,
    int LastRenderThreadFailureHResult,
    int PendingFrameCount,
    string InputColorSpace,
    string OutputColorSpace,
    PreviewSlowFrameDiagnostic[] RecentSlowFrames,
    string GpuPlaybackState,
    int NaturalVideoWidth,
    int NaturalVideoHeight,
    double PositionMs);

internal static class PreviewRuntimeD3DRendererStatePolicy
{
    public static PreviewRuntimeD3DRendererState Evaluate(D3D11PreviewRenderer? d3d, bool isPreviewing)
        => new(
            RendererMode: d3d?.RendererMode ?? (isPreviewing ? "CpuSoftwareBitmap" : "None"),
            PresentSyncInterval: d3d?.PresentSyncInterval ?? 0,
            MaxFrameLatency: d3d?.DxgiMaxFrameLatency ?? 0,
            SwapChainBufferCount: d3d?.SwapChainBufferCount ?? 0,
            SwapChainAddress: d3d?.SwapChainAddress ?? string.Empty,
            RenderThreadFailureCount: d3d?.RenderThreadFailureCount ?? 0,
            LastRenderThreadFailureType: d3d?.LastRenderThreadFailureType ?? string.Empty,
            LastRenderThreadFailureMessage: d3d?.LastRenderThreadFailureMessage ?? string.Empty,
            LastRenderThreadFailureHResult: d3d?.LastRenderThreadFailureHResult ?? 0,
            PendingFrameCount: d3d?.PendingFrameCount ?? 0,
            InputColorSpace: d3d?.InputColorSpaceLabel ?? "None",
            OutputColorSpace: d3d?.OutputColorSpaceLabel ?? "None",
            RecentSlowFrames: d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),
            GpuPlaybackState: d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            NaturalVideoWidth: d3d?.NaturalWidth ?? 0,
            NaturalVideoHeight: d3d?.NaturalHeight ?? 0,
            PositionMs: 0);
}
