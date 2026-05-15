using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackExportSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Export: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportActive")} status={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportStatus")} id={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportId")} lastResultId={AutomationSnapshotFormatter.Get(snapshot, "LastExportId")} kind={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportFailureKind", "None")} progress={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportPercent")}% segments={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportSegmentsProcessed")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportTotalSegments")} elapsed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={AutomationSnapshotFormatter.FormatBytes((long)AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportInPointMs")}ms out={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} forceRotateFallbacks={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportForceRotateFallbacks")} lastForceRotateFallbackSegments={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastForceRotateFallbackSegments")} lastForceRotateFallbackUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastForceRotateFallbackUtcUnixMs")} path={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutputPath")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportMessage", "")}");
    }
}
