using System.Text.Json;

namespace ElgatoCapture.Tools;

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}
