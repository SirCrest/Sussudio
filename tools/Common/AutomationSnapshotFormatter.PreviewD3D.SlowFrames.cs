using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var slowFrames) ||
            slowFrames.ValueKind != JsonValueKind.Array ||
            slowFrames.GetArrayLength() <= 0)
        {
            return;
        }

        var lines = new List<string>();
        foreach (var frame in slowFrames.EnumerateArray())
        {
            if (frame.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            lines.Add(
                $"present={Get(frame, "PreviewPresentId")} srcSeq={Get(frame, "SourceSequenceNumber")} " +
                $"reason={Get(frame, "SlowReason")} target={FormatDiagnosticMs(frame, "ExpectedIntervalMs")} " +
                $"over={FormatDiagnosticMs(frame, "WorstOverBudgetMs")} interval={FormatDiagnosticMs(frame, "PresentIntervalMs")} total={FormatDiagnosticMs(frame, "TotalFrameCpuMs")} " +
                $"upload={FormatDiagnosticMs(frame, "InputUploadCpuMs")} render={FormatDiagnosticMs(frame, "RenderSubmitCpuMs")} " +
                $"presentCall={FormatDiagnosticMs(frame, "PresentCallMs")} sched={FormatDiagnosticMs(frame, "SchedulerToPresentMs")} pipeline={FormatDiagnosticMs(frame, "PipelineLatencyMs")} " +
                $"pending={Get(frame, "PendingFrameCount")} dxgiDelta={Get(frame, "DxgiPresentDelta")}/{Get(frame, "DxgiPresentRefreshDelta")}/{Get(frame, "DxgiSyncRefreshDelta")}");
            if (lines.Count >= 3)
            {
                break;
            }
        }

        if (lines.Count > 0)
        {
            builder.AppendLine($"D3D Slow Frames: {string.Join(" | ", lines)}");
        }
    }

    private static string FormatDiagnosticMs(JsonElement element, string propertyName)
    {
        var value = GetDouble(element, propertyName, double.NaN);
        return double.IsFinite(value) ? $"{FormatNumber(value, "0.00")}ms" : "N/A";
    }
}
