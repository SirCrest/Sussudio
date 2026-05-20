using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackSection(StringBuilder builder, JsonElement snapshot)
    {
        var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, "FlashbackActive", "false");
        var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed", "false");
        if (!flashbackActive.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !flashbackFailed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine("== Flashback ==");
        AppendSnapshotFlashbackEncodingSection(builder, snapshot);
        AppendSnapshotFlashbackPlaybackStatusSection(builder, snapshot);
        AppendSnapshotFlashbackExportSection(builder, snapshot);
        AppendSnapshotFlashbackPlaybackMetricsSection(builder, snapshot);
        builder.AppendLine();
    }
}
