using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackSection(StringBuilder builder, JsonElement snapshot)
    {
        var flashbackActive = Get(snapshot, "FlashbackActive", "false");
        var flashbackFailed = Get(snapshot, "FlashbackEncodingFailed", "false");
        if (!flashbackActive.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !flashbackFailed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine("== Flashback ==");
        AppendFlashbackEncodingSection(builder, snapshot);
        AppendFlashbackPlaybackStatusSection(builder, snapshot);
        AppendFlashbackExportSection(builder, snapshot);
        AppendFlashbackPlaybackMetricsSection(builder, snapshot);
        builder.AppendLine();
    }
}
