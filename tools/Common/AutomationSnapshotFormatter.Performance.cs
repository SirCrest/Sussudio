using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {Get(snapshot, "EstimatedPipelineLatencyMs")}ms (app receive -> estimated visible)");
        builder.AppendLine();
    }
}
