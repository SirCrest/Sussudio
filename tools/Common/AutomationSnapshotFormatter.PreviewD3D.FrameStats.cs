using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D DXGI stats: ok={Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
    }
}
