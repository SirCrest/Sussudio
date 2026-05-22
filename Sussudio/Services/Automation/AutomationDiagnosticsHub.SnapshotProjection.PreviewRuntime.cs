using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(
        PreviewRuntimeSnapshot previewRuntime,
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),
            Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),
            Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),
            Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),
            Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)
        };

    private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),
            Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),
            Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),
            Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),
            Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)
        };

    private readonly record struct PreviewRuntimeProjection
    {
        public PreviewRuntimeFrameProjection Frame { get; init; }
        public PreviewRuntimeCadenceProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceProjection Surface { get; init; }
        public PreviewRuntimeStartupProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorProjection Color { get; init; }
    }

    private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            FramesArrived = previewRuntime.FramesArrived,
            FramesDisplayed = previewRuntime.FramesDisplayed,
            FramesDropped = previewRuntime.FramesDropped,
            EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs
        };

    private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(
        PreviewRuntimeFrameProjection frame)
        => new()
        {
            FramesArrived = frame.FramesArrived,
            FramesDisplayed = frame.FramesDisplayed,
            FramesDropped = frame.FramesDropped,
            EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs
        };

    private readonly record struct PreviewRuntimeFrameProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private readonly record struct PreviewRuntimeFrameFlattenedProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            HdrInputDetected = previewHdrState.InputDetected,
            ToneMapMode = previewHdrState.ToneMapMode,
            ColorContext = captureRuntime.NegotiatedPixelFormat,
            AdapterColorMetadata = captureRuntime.PreviewColorMetadata
        };

    private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(
        PreviewRuntimeColorProjection color)
        => new()
        {
            HdrInputDetected = color.HdrInputDetected,
            ToneMapMode = color.ToneMapMode,
            ColorContext = color.ColorContext,
            AdapterColorMetadata = color.AdapterColorMetadata
        };

    private readonly record struct PreviewRuntimeColorProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private readonly record struct PreviewRuntimeColorFlattenedProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            GpuActive = previewRuntime.GpuActive,
            PlaceholderVisible = previewRuntime.PlaceholderVisible,
            GpuElementVisible = previewRuntime.GpuElementVisible,
            CpuElementVisible = previewRuntime.CpuElementVisible,
            RendererAttached = previewRuntime.RendererAttached
        };

    private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(
        PreviewRuntimeSurfaceProjection surface)
        => new()
        {
            GpuActive = surface.GpuActive,
            PlaceholderVisible = surface.PlaceholderVisible,
            GpuElementVisible = surface.GpuElementVisible,
            CpuElementVisible = surface.CpuElementVisible,
            RendererAttached = surface.RendererAttached
        };

    private readonly record struct PreviewRuntimeSurfaceProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }

    private readonly record struct PreviewRuntimeSurfaceFlattenedProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }

    private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            PlaybackState = previewRuntime.GpuPlaybackState,
            NaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            NaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            PositionMs = previewRuntime.GpuPositionMs,
            PositionEventCount = previewRuntime.GpuPositionEventCount
        };

    private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(
        PreviewRuntimeGpuPlaybackProjection gpuPlayback)
        => new()
        {
            PlaybackState = gpuPlayback.PlaybackState,
            NaturalVideoWidth = gpuPlayback.NaturalVideoWidth,
            NaturalVideoHeight = gpuPlayback.NaturalVideoHeight,
            PositionMs = gpuPlayback.PositionMs,
            PositionEventCount = gpuPlayback.PositionEventCount
        };

    private readonly record struct PreviewRuntimeGpuPlaybackProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeGpuPlaybackFlattenedProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeFlattenedProjection
    {
        public PreviewRuntimeFrameFlattenedProjection Frame { get; init; }
        public PreviewRuntimeCadenceFlattenedProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceFlattenedProjection Surface { get; init; }
        public PreviewRuntimeStartupFlattenedProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackFlattenedProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorFlattenedProjection Color { get; init; }
    }
}
