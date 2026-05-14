using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Source ==");
        var sourceFrameRate = Get(snapshot, "DetectedSourceFrameRate", string.Empty);
        var sourceFrameRateArg = Get(snapshot, "DetectedSourceFrameRateArg", string.Empty);
        var sourceFpsSummary = !string.IsNullOrWhiteSpace(sourceFrameRateArg)
            ? $"{sourceFrameRate}fps ({sourceFrameRateArg})"
            : !string.IsNullOrWhiteSpace(sourceFrameRate)
                ? $"{sourceFrameRate}fps"
                : "N/A";
        builder.AppendLine($"Source: {Get(snapshot, "SourceWidth")} x {Get(snapshot, "SourceHeight")} @ {sourceFpsSummary} HDR={Get(snapshot, "SourceIsHdr")}");
        builder.AppendLine($"Telemetry: {Get(snapshot, "SourceTelemetryAvailability")} ({Get(snapshot, "SourceTelemetryConfidence")})");
    }
}
