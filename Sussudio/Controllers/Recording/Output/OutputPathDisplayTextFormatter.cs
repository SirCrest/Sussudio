namespace Sussudio.Controllers;

internal static class OutputPathDisplayTextFormatter
{
    public static string Format(string path, double availableWidth)
    {
        if (availableWidth <= 0)
        {
            return path;
        }

        // FontSize 12 is about 7px per char, minus internal padding.
        var maxChars = (int)((availableWidth - 20) / 7);
        if (path.Length <= maxChars)
        {
            return path;
        }

        var parts = path.Split('\\', '/');
        if (parts.Length <= 2)
        {
            return path;
        }

        // Progressively truncate: keep root, show as many trailing segments as fit.
        var root = parts[0];
        for (int tailCount = parts.Length - 1; tailCount >= 1; tailCount--)
        {
            var tail = string.Join("\\", parts[^tailCount..]);
            var candidate = $"{root}\\...\\{tail}";
            if (candidate.Length <= maxChars)
            {
                return candidate;
            }
        }

        return $"{root}\\...\\{parts[^1]}";
    }
}
