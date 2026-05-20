using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)
    {
        AppendFlashbackEncodingStatusSection(builder, snapshot);
        AppendFlashbackEncodingHealthSection(builder, snapshot);
    }
}
