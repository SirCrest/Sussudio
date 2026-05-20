using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D DXGI stats: ok={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
    }

    private static void AppendSnapshotPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Ownership: submitted present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDropReason")} dropPts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
    }
}
