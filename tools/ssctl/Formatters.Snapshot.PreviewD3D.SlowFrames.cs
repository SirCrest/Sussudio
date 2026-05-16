using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)
    {
        AutomationSnapshotFormatter.AppendPreviewSlowFrameDiagnostics(builder, snapshot);
    }
}
