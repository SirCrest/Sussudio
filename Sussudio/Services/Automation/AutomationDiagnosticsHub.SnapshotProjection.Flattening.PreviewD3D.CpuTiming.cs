namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(
        PreviewD3DCpuTimingProjection cpuTiming)
        => new()
        {
            SampleCount = cpuTiming.SampleCount,
            InputUploadCpuAvgMs = cpuTiming.InputUploadAvgMs,
            InputUploadCpuP95Ms = cpuTiming.InputUploadP95Ms,
            InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,
            InputUploadCpuMaxMs = cpuTiming.InputUploadMaxMs,
            RenderSubmitCpuAvgMs = cpuTiming.RenderSubmitAvgMs,
            RenderSubmitCpuP95Ms = cpuTiming.RenderSubmitP95Ms,
            RenderSubmitCpuP99Ms = cpuTiming.RenderSubmitP99Ms,
            RenderSubmitCpuMaxMs = cpuTiming.RenderSubmitMaxMs,
            PresentCallAvgMs = cpuTiming.PresentCallAvgMs,
            PresentCallP95Ms = cpuTiming.PresentCallP95Ms,
            PresentCallP99Ms = cpuTiming.PresentCallP99Ms,
            PresentCallMaxMs = cpuTiming.PresentCallMaxMs,
            TotalFrameCpuAvgMs = cpuTiming.TotalFrameAvgMs,
            TotalFrameCpuP95Ms = cpuTiming.TotalFrameP95Ms,
            TotalFrameCpuP99Ms = cpuTiming.TotalFrameP99Ms,
            TotalFrameCpuMaxMs = cpuTiming.TotalFrameMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingFlattenedProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadCpuAvgMs { get; init; }
        public double InputUploadCpuP95Ms { get; init; }
        public double InputUploadCpuP99Ms { get; init; }
        public double InputUploadCpuMaxMs { get; init; }
        public double RenderSubmitCpuAvgMs { get; init; }
        public double RenderSubmitCpuP95Ms { get; init; }
        public double RenderSubmitCpuP99Ms { get; init; }
        public double RenderSubmitCpuMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameCpuAvgMs { get; init; }
        public double TotalFrameCpuP95Ms { get; init; }
        public double TotalFrameCpuP99Ms { get; init; }
        public double TotalFrameCpuMaxMs { get; init; }
    }
}
