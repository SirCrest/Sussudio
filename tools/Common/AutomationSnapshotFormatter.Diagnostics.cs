using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendDiagnosticsSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Diagnostics ==");
        builder.AppendLine($"Health: {Get(snapshot, "DiagnosticHealthStatus")} | Stage: {Get(snapshot, "DiagnosticLikelyStage")}");
        builder.AppendLine($"Summary: {Get(snapshot, "DiagnosticSummary")}");
        builder.AppendLine($"Evidence: {Get(snapshot, "DiagnosticEvidence")}");
        builder.AppendLine("Frame Lanes:");
        builder.AppendLine($"  Source: {Get(snapshot, "DiagnosticSourceLane")}");
        builder.AppendLine($"  Decode: {Get(snapshot, "DiagnosticDecodeLane")}");
        builder.AppendLine($"  Preview: {Get(snapshot, "DiagnosticPreviewLane")}");
        builder.AppendLine($"  Render: {Get(snapshot, "DiagnosticRenderLane")}");
        builder.AppendLine($"  Present: {Get(snapshot, "DiagnosticPresentLane")}");
        builder.AppendLine($"  Recording: {Get(snapshot, "DiagnosticRecordingLane")}");
        builder.AppendLine($"  Audio: {Get(snapshot, "DiagnosticAudioLane")}");
        builder.AppendLine();
    }
}
