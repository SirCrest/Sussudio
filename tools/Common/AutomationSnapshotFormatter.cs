using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

// Converts broad automation snapshots into terse console text. The formatter is
// intentionally tolerant of missing JSON properties so old/new app builds can
// still be inspected during live investigations.
internal static partial class AutomationSnapshotFormatter
{
    internal static string FormatSnapshot(JsonElement snapshotResponse, bool includeFlashback = false)
    {
        if (snapshotResponse.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot response was not a JSON object.";
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return Get(snapshotResponse, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        AppendStateSection(builder, snapshot);
        AppendCaptureSettingsSection(builder, snapshot);
        AppendAudioSection(builder, snapshot);
        AppendVideoPipelineSection(builder, snapshot);
        AppendRecordingSection(builder, snapshot);
        if (includeFlashback)
        {
            AppendFlashbackSection(builder, snapshot);
        }

        AppendDiagnosticsSection(builder, snapshot);
        AppendPerformanceSection(builder, snapshot);
        AppendMemorySection(builder, snapshot);
        AppendCaptureCadenceSection(builder, snapshot);
        return builder.ToString().TrimEnd();
    }
}
