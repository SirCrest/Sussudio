using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D DXGI stats: ok={Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
    }

    private static void AppendPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Ownership: submitted present={Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={Get(snapshot, "PreviewD3DLastDropReason")} dropPts={Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
    }
}
