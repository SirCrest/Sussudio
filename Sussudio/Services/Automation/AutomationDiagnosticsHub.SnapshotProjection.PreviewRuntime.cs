using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(
        PreviewRuntimeSnapshot previewRuntime,
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
    {
        var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);
        var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);

        return new()
        {
            FramesArrived = previewRuntime.FramesArrived,
            FramesDisplayed = previewRuntime.FramesDisplayed,
            FramesDropped = previewRuntime.FramesDropped,
            EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,
            Cadence = cadence,
            GpuActive = previewRuntime.GpuActive,
            PlaceholderVisible = previewRuntime.PlaceholderVisible,
            GpuElementVisible = previewRuntime.GpuElementVisible,
            CpuElementVisible = previewRuntime.CpuElementVisible,
            RendererAttached = previewRuntime.RendererAttached,
            Startup = startup,
            GpuPlaybackState = previewRuntime.GpuPlaybackState,
            GpuNaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            GpuNaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            GpuPositionMs = previewRuntime.GpuPositionMs,
            GpuPositionEventCount = previewRuntime.GpuPositionEventCount,
            HdrInputDetected = previewHdrState.InputDetected,
            ToneMapMode = previewHdrState.ToneMapMode,
            ColorContext = captureRuntime.NegotiatedPixelFormat,
            AdapterColorMetadata = captureRuntime.PreviewColorMetadata
        };
    }

    private readonly record struct PreviewRuntimeProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
        public PreviewRuntimeCadenceProjection Cadence { get; init; }
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
        public PreviewRuntimeStartupProjection Startup { get; init; }
        public string GpuPlaybackState { get; init; }
        public int GpuNaturalVideoWidth { get; init; }
        public int GpuNaturalVideoHeight { get; init; }
        public double GpuPositionMs { get; init; }
        public long GpuPositionEventCount { get; init; }
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }
}
