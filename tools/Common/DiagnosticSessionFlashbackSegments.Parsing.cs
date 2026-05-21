using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal readonly record struct FlashbackSegmentProbe(
    int SequenceNumber,
    long StartPtsMs,
    long EndPtsMs,
    bool IsActive);

internal static partial class DiagnosticSessionFlashbackSegments
{
    internal static bool TryGetFlashbackSegments(JsonElement response, out List<FlashbackSegmentProbe> segments)
    {
        segments = new List<FlashbackSegmentProbe>();
        if (!response.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty("Segments", out var segmentsElement) ||
            segmentsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var segment in segmentsElement.EnumerateArray())
        {
            if (segment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            segments.Add(new FlashbackSegmentProbe(
                SequenceNumber: GetInt(segment, "SequenceNumber"),
                StartPtsMs: GetNullableLong(segment, "StartPtsMs") ?? 0,
                EndPtsMs: GetNullableLong(segment, "EndPtsMs") ?? 0,
                IsActive: GetBool(segment, "IsActive")));
        }

        return true;
    }
}
