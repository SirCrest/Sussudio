using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    public static string FormatSnapshot(JsonElement snapshotResponse)
    {
        if (snapshotResponse.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot response was not a JSON object.";
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(snapshotResponse, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        AppendSnapshotStateSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureSettingsSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotAudioSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotVideoPipelineSection(builder, snapshot);
        AppendSnapshotThreadHealthSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotRecordingSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotFlashbackSection(builder, snapshot);
        AppendSnapshotDiagnosticLanesSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotPerformanceSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotMemorySection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureCadenceSection(builder, snapshot);
        AppendSnapshotMjpegTimingSection(builder, snapshot);
        AppendSnapshotAvSyncSection(builder, snapshot);
        AppendSnapshotPreviewSection(builder, snapshot);
        AppendSnapshotSourceSection(builder, snapshot);

        return builder.ToString().TrimEnd();
    }
}
