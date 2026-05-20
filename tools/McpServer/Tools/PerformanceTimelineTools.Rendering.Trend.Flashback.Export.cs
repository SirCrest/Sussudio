using System.Text;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)
    {
        builder.AppendLine($"Export State:    {FormatOptional(first.FlashbackExportStatus)} -> {FormatOptional(last.FlashbackExportStatus)} active={last.FlashbackExportActive} kind={FormatOptional(last.FlashbackExportFailureKind)}");
        builder.AppendLine($"Export Message:  {FormatOptional(last.FlashbackExportMessage)}");
        builder.AppendLine($"Export Progress: {first.FlashbackExportPercent:F1}% -> {last.FlashbackExportPercent:F1}% segments={last.FlashbackExportSegmentsProcessed}/{last.FlashbackExportTotalSegments}");
        builder.AppendLine($"Export Range:    in={last.FlashbackExportInPointMs}ms out={FormatExportOutPoint(last.FlashbackExportOutPointMs)}");
        builder.AppendLine($"Export Output:   {FormatBytes(first.FlashbackExportOutputBytes)} -> {FormatBytes(last.FlashbackExportOutputBytes)} throughput={FormatBytesPerSecond(last.FlashbackExportThroughputBytesPerSec)} elapsed={last.FlashbackExportElapsedMs}ms lastProgressAge={last.FlashbackExportLastProgressAgeMs}ms");
    }
}
