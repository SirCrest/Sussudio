using System;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
    private const double StageDominanceRatio = 1.15;

    private static string ResolveDominantD3DStage(
        PreviewPacingClassificationInput input,
        double targetFrameMs)
    {
        var threshold = Math.Max(1.0, targetFrameMs * 0.25);
        var inputUpload = Positive(input.PreviewD3DInputUploadCpuP99Ms);
        var renderSubmit = Positive(input.PreviewD3DRenderSubmitCpuP99Ms);
        var presentCall = Positive(input.PreviewD3DPresentCallP99Ms);
        var wait = Math.Max(
            Positive(input.PreviewD3DFrameLatencyWaitP95Ms),
            Positive(input.PreviewD3DFrameLatencyWaitMaxMs) * 0.50);

        if (input.RecentD3DFrameLatencyWaitTimeoutCount > 0)
        {
            return "PresentBlocked";
        }

        var max = Math.Max(Math.Max(inputUpload, renderSubmit), Math.Max(presentCall, wait));
        if (max < threshold)
        {
            if (input.PreviewD3DTotalFrameCpuP99Ms > targetFrameMs * P99OverBudgetRatio)
            {
                return "RenderSubmit";
            }

            return string.Empty;
        }

        if (inputUpload >= max && IsDominant(inputUpload, renderSubmit, presentCall, wait))
        {
            return "RenderUpload";
        }

        if (renderSubmit >= max && IsDominant(renderSubmit, inputUpload, presentCall, wait))
        {
            return "RenderSubmit";
        }

        if (presentCall >= max && IsDominant(presentCall, inputUpload, renderSubmit, wait) ||
            wait >= max && IsDominant(wait, inputUpload, renderSubmit, presentCall))
        {
            return "PresentBlocked";
        }

        return string.Empty;
    }

    private static bool IsDominant(double candidate, params double[] others)
    {
        foreach (var other in others)
        {
            if (other > 0 && candidate < other * StageDominanceRatio)
            {
                return false;
            }
        }

        return true;
    }

    private static double Positive(double value)
        => double.IsFinite(value) && value > 0 ? value : 0.0;
}
