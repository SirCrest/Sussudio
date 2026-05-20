using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DCpuTimingSampleCount,
            InputUploadAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            InputUploadP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            InputUploadMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            RenderSubmitAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            RenderSubmitP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            RenderSubmitP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            RenderSubmitMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            TotalFrameAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            TotalFrameP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            TotalFrameP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            TotalFrameMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadAvgMs { get; init; }
        public double InputUploadP95Ms { get; init; }
        public double InputUploadP99Ms { get; init; }
        public double InputUploadMaxMs { get; init; }
        public double RenderSubmitAvgMs { get; init; }
        public double RenderSubmitP95Ms { get; init; }
        public double RenderSubmitP99Ms { get; init; }
        public double RenderSubmitMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameAvgMs { get; init; }
        public double TotalFrameP95Ms { get; init; }
        public double TotalFrameP99Ms { get; init; }
        public double TotalFrameMaxMs { get; init; }
    }
}
