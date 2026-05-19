using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DRenderCpuTiming(
    int SampleCount,
    double InputUploadAverageMs,
    double InputUploadP95Ms,
    double InputUploadP99Ms,
    double InputUploadMaxMs,
    double RenderSubmitAverageMs,
    double RenderSubmitP95Ms,
    double RenderSubmitP99Ms,
    double RenderSubmitMaxMs,
    double PresentCallAverageMs,
    double PresentCallP95Ms,
    double PresentCallP99Ms,
    double PresentCallMaxMs,
    double TotalFrameAverageMs,
    double TotalFrameP95Ms,
    double TotalFrameP99Ms,
    double TotalFrameMaxMs);

internal static class PreviewRuntimeD3DRenderCpuTimingPolicy
{
    public static PreviewRuntimeD3DRenderCpuTiming Evaluate(D3D11PreviewRenderer? d3d)
    {
        var renderCpuTiming = d3d?.GetRenderCpuTimingMetrics();

        return new PreviewRuntimeD3DRenderCpuTiming(
            SampleCount: renderCpuTiming?.TotalFrame.SampleCount ?? 0,
            InputUploadAverageMs: renderCpuTiming?.InputUpload.AverageMs ?? 0,
            InputUploadP95Ms: renderCpuTiming?.InputUpload.P95Ms ?? 0,
            InputUploadP99Ms: renderCpuTiming?.InputUpload.P99Ms ?? 0,
            InputUploadMaxMs: renderCpuTiming?.InputUpload.MaxMs ?? 0,
            RenderSubmitAverageMs: renderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            RenderSubmitP95Ms: renderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            RenderSubmitP99Ms: renderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            RenderSubmitMaxMs: renderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            PresentCallAverageMs: renderCpuTiming?.PresentCall.AverageMs ?? 0,
            PresentCallP95Ms: renderCpuTiming?.PresentCall.P95Ms ?? 0,
            PresentCallP99Ms: renderCpuTiming?.PresentCall.P99Ms ?? 0,
            PresentCallMaxMs: renderCpuTiming?.PresentCall.MaxMs ?? 0,
            TotalFrameAverageMs: renderCpuTiming?.TotalFrame.AverageMs ?? 0,
            TotalFrameP95Ms: renderCpuTiming?.TotalFrame.P95Ms ?? 0,
            TotalFrameP99Ms: renderCpuTiming?.TotalFrame.P99Ms ?? 0,
            TotalFrameMaxMs: renderCpuTiming?.TotalFrame.MaxMs ?? 0);
    }
}
