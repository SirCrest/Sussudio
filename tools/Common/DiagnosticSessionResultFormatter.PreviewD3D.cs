using System.Text;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPreviewD3DPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D Perf: " +
            $"onePercentLowFpsEnd={result.PreviewCadenceOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##} " +
            $"missedRefreshDelta={result.PreviewD3DFrameStatsMissedRefreshDelta} " +
            $"statsFailureDelta={result.PreviewD3DFrameStatsFailureDelta} " +
            $"maxRecentSlowFrames={result.PreviewD3DMaxRecentSlowFramesObserved} " +
            $"latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)} " +
            $"overBudgetMs={result.PreviewD3DLatestSlowFrameOverBudgetMs:0.##} " +
            $"presentIntervalMs={result.PreviewD3DLatestSlowFramePresentIntervalMs:0.##} " +
            $"totalFrameCpuMs={result.PreviewD3DLatestSlowFrameTotalFrameCpuMs:0.##} " +
            $"presentCallMs={result.PreviewD3DLatestSlowFramePresentCallMs:0.##} " +
            $"pending={result.PreviewD3DLatestSlowFramePendingFrameCount}");
    }

    private static void AppendPreviewD3DCpuTiming(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D CPU Timing: " +
            $"inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##} " +
            $"inputUploadMaxObserved={result.PreviewD3DInputUploadCpuMaxMsObserved:0.##} " +
            $"renderSubmitP99End={result.PreviewD3DRenderSubmitCpuP99MsAtEnd:0.##} " +
            $"renderSubmitMaxObserved={result.PreviewD3DRenderSubmitCpuMaxMsObserved:0.##} " +
            $"presentCallP99End={result.PreviewD3DPresentCallP99MsAtEnd:0.##} " +
            $"presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##} " +
            $"totalFrameP99End={result.PreviewD3DTotalFrameCpuP99MsAtEnd:0.##} " +
            $"totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
    }
}
