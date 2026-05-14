using System.Text.Json;

namespace Sussudio.Tools;

public sealed class DiagnosticSessionSample
{
    public long OffsetMs { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public JsonElement Snapshot { get; init; }
}
