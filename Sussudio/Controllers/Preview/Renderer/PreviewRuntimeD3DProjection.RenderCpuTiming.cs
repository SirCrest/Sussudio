namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public int D3DCpuTimingSampleCount { get; private set; }
    public double D3DInputUploadCpuAvgMs { get; private set; }
    public double D3DInputUploadCpuP95Ms { get; private set; }
    public double D3DInputUploadCpuP99Ms { get; private set; }
    public double D3DInputUploadCpuMaxMs { get; private set; }
    public double D3DRenderSubmitCpuAvgMs { get; private set; }
    public double D3DRenderSubmitCpuP95Ms { get; private set; }
    public double D3DRenderSubmitCpuP99Ms { get; private set; }
    public double D3DRenderSubmitCpuMaxMs { get; private set; }
    public double D3DPresentCallAvgMs { get; private set; }
    public double D3DPresentCallP95Ms { get; private set; }
    public double D3DPresentCallP99Ms { get; private set; }
    public double D3DPresentCallMaxMs { get; private set; }
    public double D3DTotalFrameCpuAvgMs { get; private set; }
    public double D3DTotalFrameCpuP95Ms { get; private set; }
    public double D3DTotalFrameCpuP99Ms { get; private set; }
    public double D3DTotalFrameCpuMaxMs { get; private set; }

    private void ApplyRenderCpuTiming(PreviewRuntimeD3DRenderCpuTiming renderCpuTiming)
    {
        D3DCpuTimingSampleCount = renderCpuTiming.SampleCount;
        D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs;
        D3DInputUploadCpuP95Ms = renderCpuTiming.InputUploadP95Ms;
        D3DInputUploadCpuP99Ms = renderCpuTiming.InputUploadP99Ms;
        D3DInputUploadCpuMaxMs = renderCpuTiming.InputUploadMaxMs;
        D3DRenderSubmitCpuAvgMs = renderCpuTiming.RenderSubmitAverageMs;
        D3DRenderSubmitCpuP95Ms = renderCpuTiming.RenderSubmitP95Ms;
        D3DRenderSubmitCpuP99Ms = renderCpuTiming.RenderSubmitP99Ms;
        D3DRenderSubmitCpuMaxMs = renderCpuTiming.RenderSubmitMaxMs;
        D3DPresentCallAvgMs = renderCpuTiming.PresentCallAverageMs;
        D3DPresentCallP95Ms = renderCpuTiming.PresentCallP95Ms;
        D3DPresentCallP99Ms = renderCpuTiming.PresentCallP99Ms;
        D3DPresentCallMaxMs = renderCpuTiming.PresentCallMaxMs;
        D3DTotalFrameCpuAvgMs = renderCpuTiming.TotalFrameAverageMs;
        D3DTotalFrameCpuP95Ms = renderCpuTiming.TotalFrameP95Ms;
        D3DTotalFrameCpuP99Ms = renderCpuTiming.TotalFrameP99Ms;
        D3DTotalFrameCpuMaxMs = renderCpuTiming.TotalFrameMaxMs;
    }
}
