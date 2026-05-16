using System.Text.Json;

namespace Sussudio.Tools;

// Shared JSON formatting options for command-line tools.
internal static class ToolJsonOptions
{
    internal static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}
