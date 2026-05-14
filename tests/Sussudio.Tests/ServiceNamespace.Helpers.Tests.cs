using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

static partial class Program
{
    private static readonly Regex RootServicesUsingRegex = new(
        @"(^|\s)using\s+Sussudio\.Services\s*;",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static IEnumerable<string> EnumerateSourceFiles(string root, SearchOption searchOption)
        => Directory.EnumerateFiles(root, "*.cs", searchOption)
            .Where(file => !HasIgnoredPathSegment(root, file));

    private static string[] ReadCompileIncludes(string projectPath)
        => XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => NormalizeProjectInclude(include!))
            .ToArray();

    private static int CountCompileInclude(IEnumerable<string> includes, string include)
        => includes.Count(value => string.Equals(value, NormalizeProjectInclude(include), StringComparison.OrdinalIgnoreCase));

    private static string[] ReadProjectReferences(string projectPath)
        => XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => NormalizeProjectInclude(include!))
            .ToArray();

    private static int CountProjectReference(IEnumerable<string> references, string include)
        => references.Count(value => string.Equals(value, NormalizeProjectInclude(include), StringComparison.OrdinalIgnoreCase));

    private static string NormalizeProjectInclude(string include)
        => include.Trim().Replace('\\', '/');

    private static bool HasIgnoredPathSegment(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(segment, "Generated Files", StringComparison.OrdinalIgnoreCase));
    }

    private static string StripCSharpCommentsAndLiterals(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var current = text[index];
            if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index = StripLineComment(text, index, builder);
                continue;
            }

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = StripBlockComment(text, index, builder);
                continue;
            }

            if (current == '\'')
            {
                index = StripCharacterLiteral(text, index, builder);
                continue;
            }

            if (current == '"')
            {
                var quoteCount = CountQuoteRun(text, index);
                index = quoteCount >= 3
                    ? StripRawStringLiteral(text, index, quoteCount, builder)
                    : StripStringLiteral(text, index, IsVerbatimStringQuote(text, index), builder);
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static int StripLineComment(string text, int start, StringBuilder builder)
    {
        var index = start;
        while (index < text.Length && text[index] != '\r' && text[index] != '\n')
        {
            builder.Append(' ');
            index++;
        }

        return index;
    }

    private static int StripBlockComment(string text, int start, StringBuilder builder)
    {
        var index = start;
        while (index < text.Length)
        {
            if (index + 1 < text.Length && text[index] == '*' && text[index + 1] == '/')
            {
                builder.Append(' ');
                builder.Append(' ');
                return index + 2;
            }

            AppendSpaceOrNewline(builder, text[index]);
            index++;
        }

        return index;
    }

    private static int StripStringLiteral(string text, int start, bool verbatim, StringBuilder builder)
    {
        AppendSpaceOrNewline(builder, text[start]);
        var index = start + 1;
        while (index < text.Length)
        {
            var current = text[index];
            AppendSpaceOrNewline(builder, current);

            if (verbatim)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        AppendSpaceOrNewline(builder, text[index + 1]);
                        index += 2;
                        continue;
                    }

                    return index + 1;
                }

                index++;
                continue;
            }

            if (current == '\\' && index + 1 < text.Length)
            {
                AppendSpaceOrNewline(builder, text[index + 1]);
                index += 2;
                continue;
            }

            if (current == '"')
            {
                return index + 1;
            }

            index++;
        }

        return index;
    }

    private static int StripRawStringLiteral(string text, int start, int quoteCount, StringBuilder builder)
    {
        for (var quoteIndex = 0; quoteIndex < quoteCount; quoteIndex++)
        {
            builder.Append(' ');
        }

        var index = start + quoteCount;
        while (index < text.Length)
        {
            if (CountQuoteRun(text, index) >= quoteCount)
            {
                for (var quoteIndex = 0; quoteIndex < quoteCount; quoteIndex++)
                {
                    builder.Append(' ');
                }

                return index + quoteCount;
            }

            AppendSpaceOrNewline(builder, text[index]);
            index++;
        }

        return index;
    }

    private static int StripCharacterLiteral(string text, int start, StringBuilder builder)
    {
        AppendSpaceOrNewline(builder, text[start]);
        var index = start + 1;
        while (index < text.Length)
        {
            var current = text[index];
            AppendSpaceOrNewline(builder, current);

            if (current == '\\' && index + 1 < text.Length)
            {
                AppendSpaceOrNewline(builder, text[index + 1]);
                index += 2;
                continue;
            }

            if (current == '\'')
            {
                return index + 1;
            }

            index++;
        }

        return index;
    }

    private static bool IsVerbatimStringQuote(string text, int quoteIndex)
    {
        var index = quoteIndex - 1;
        while (index >= 0 && text[index] == '$')
        {
            index--;
        }

        return index >= 0 && text[index] == '@';
    }

    private static int CountQuoteRun(string text, int start)
    {
        var count = 0;
        while (start + count < text.Length && text[start + count] == '"')
        {
            count++;
        }

        return count;
    }

    private static void AppendSpaceOrNewline(StringBuilder builder, char value)
        => builder.Append(value is '\r' or '\n' ? value : ' ');

    private static string StripCSharpCommentsPreserveLiterals(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var current = text[index];
            if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index = StripLineComment(text, index, builder);
                continue;
            }

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = StripBlockComment(text, index, builder);
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }
}
