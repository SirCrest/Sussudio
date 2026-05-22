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
