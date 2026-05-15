using System.Globalization;
using System.Text;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static string NormalizeHeader(string value)
        => value.Trim().TrimStart('\uFEFF');

    private static double? ReadMetric(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        params string[] names)
    {
        var found = false;
        var fieldIndex = -1;
        foreach (var name in names)
        {
            if (index.TryGetValue(name, out fieldIndex))
            {
                found = true;
                break;
            }
        }

        if (!found || fieldIndex >= fields.Count)
        {
            return null;
        }

        var field = fields[fieldIndex].Trim();
        if (field.Length == 0 || string.Equals(field, "NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !double.IsNaN(value) &&
            !double.IsInfinity(value))
        {
            return value;
        }

        return null;
    }

    private static string ReadField(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> index,
        string name)
    {
        if (!index.TryGetValue(name, out var fieldIndex) || fieldIndex >= fields.Count)
        {
            return string.Empty;
        }

        return fields[fieldIndex].Trim();
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(ch);
            }
        }

        result.Add(builder.ToString());
        return result;
    }
}
