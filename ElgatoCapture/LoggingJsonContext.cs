using System.Text.Json.Serialization;
using ElgatoCapture.Models;

namespace ElgatoCapture;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CaptureHealthSnapshot))]
[JsonSerializable(typeof(CaptureDiagnosticsSnapshot))]
internal sealed partial class LoggingJsonContext : JsonSerializerContext
{
}
