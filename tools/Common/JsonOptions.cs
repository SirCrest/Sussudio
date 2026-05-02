using System.Text.Json;

namespace Sussudio.Tools;

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}
