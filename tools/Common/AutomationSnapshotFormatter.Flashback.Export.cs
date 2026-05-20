using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackExportSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Export: active={Get(snapshot, "FlashbackExportActive")} status={Get(snapshot, "FlashbackExportStatus")} id={Get(snapshot, "FlashbackExportId")} lastResultId={Get(snapshot, "LastExportId")} kind={Get(snapshot, "FlashbackExportFailureKind", "None")} progress={Get(snapshot, "FlashbackExportPercent")}% segments={Get(snapshot, "FlashbackExportSegmentsProcessed")}/{Get(snapshot, "FlashbackExportTotalSegments")} elapsed={Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={FormatBytes(GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={FormatBytes((long)GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={Get(snapshot, "FlashbackExportInPointMs")}ms out={Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} forceRotateFallbacks={Get(snapshot, "FlashbackExportForceRotateFallbacks")} lastForceRotateFallbackSegments={Get(snapshot, "FlashbackExportLastForceRotateFallbackSegments")} lastForceRotateFallbackUtc={Get(snapshot, "FlashbackExportLastForceRotateFallbackUtcUnixMs")} path={Get(snapshot, "FlashbackExportOutputPath")} msg={Get(snapshot, "FlashbackExportMessage", "")}");
    }
}
