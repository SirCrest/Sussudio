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
}
