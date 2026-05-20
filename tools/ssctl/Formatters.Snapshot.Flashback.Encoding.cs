using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)
    {
        AppendSnapshotFlashbackEncodingStatusSection(builder, snapshot);
        AppendSnapshotFlashbackEncodingHealthSection(builder, snapshot);
    }
}
