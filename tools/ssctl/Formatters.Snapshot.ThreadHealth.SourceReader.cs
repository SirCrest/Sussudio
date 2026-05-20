using System;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var sourceReaderLastFrameAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var sourceReaderOutstanding = AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderFrameChannelDepth")}");
    }
}
