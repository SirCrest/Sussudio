using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static string PrettyJson<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions.Pretty);

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }
}
