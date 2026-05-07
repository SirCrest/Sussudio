using System.Text.Json.Serialization;
using Sussudio.Models;

namespace Sussudio;

// Source-generated JSON metadata for diagnostic snapshots written to the log.
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CaptureHealthSnapshot))]
[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]
internal sealed partial class LoggingJsonContext : JsonSerializerContext
{
}
