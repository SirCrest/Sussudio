using System.Text.Json.Serialization;
using Sussudio.Models;

namespace Sussudio;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CaptureHealthSnapshot))]
[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]
internal sealed partial class LoggingJsonContext : JsonSerializerContext
{
}
