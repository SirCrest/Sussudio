using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        AppendSourceReaderThreadHealthLine(builder, snapshot);
        AppendWasapiCaptureThreadHealthLine(builder, snapshot);
        AppendWasapiPlaybackThreadHealthLine(builder, snapshot);
    }
}
