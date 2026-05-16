using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D CPU timing: input/upload avg={Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
    }
}
