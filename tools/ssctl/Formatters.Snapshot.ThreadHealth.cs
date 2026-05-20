using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        AppendSnapshotSourceReaderThreadHealthLine(builder, snapshot);
        AppendSnapshotWasapiCaptureThreadHealthLine(builder, snapshot);
        AppendSnapshotWasapiPlaybackThreadHealthLine(builder, snapshot);
    }
}
