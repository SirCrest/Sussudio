using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
    private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)
    {
        metrics.InputUploadCpuMaxMsObserved = Math.Max(
            metrics.InputUploadCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DInputUploadCpuMaxMs"));
        metrics.RenderSubmitCpuMaxMsObserved = Math.Max(
            metrics.RenderSubmitCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DRenderSubmitCpuMaxMs"));
        metrics.PresentCallMaxMsObserved = Math.Max(
            metrics.PresentCallMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DPresentCallMaxMs"));
        metrics.TotalFrameCpuMaxMsObserved = Math.Max(
            metrics.TotalFrameCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DTotalFrameCpuMaxMs"));
    }
}
