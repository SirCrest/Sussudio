using System;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var sourceReaderLastFrameAgeMs = ComputeTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var sourceReaderOutstanding = Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={Get(snapshot, "SourceReaderFrameChannelDepth")}");
    }
}
