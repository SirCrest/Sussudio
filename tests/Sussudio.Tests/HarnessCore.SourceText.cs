static partial class Program
{
    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var callPrefix in new[]
        {
            "Get(snapshot,",
            "GetInt(snapshot,",
            "GetDouble(snapshot,",
            "GetLong(snapshot,",
            "GetNullableLong(snapshot,",
            "GetBool(snapshot,",
            "GetString(snapshot,",
            "FormatFrameBudgetMs(snapshot,",
            "FormatIntervalMs(snapshot,"
        })
        {
            ExtractSnapshotFieldsFromCalls(sourceText, callPrefix, fields);
        }

        return fields;
    }

    private static void ExtractSnapshotFieldsFromCalls(string sourceText, string callPrefix, HashSet<string> fields)
    {
        var index = 0;
        while (index < sourceText.Length)
        {
            var callIdx = sourceText.IndexOf(callPrefix, index, StringComparison.Ordinal);
            if (callIdx < 0)
                break;

            var afterComma = callIdx + callPrefix.Length;
            var quoteIdx = sourceText.IndexOf('"', afterComma);
            if (quoteIdx < 0 || quoteIdx - afterComma > 10)
            {
                index = afterComma;
                continue;
            }

            var endQuoteIdx = sourceText.IndexOf('"', quoteIdx + 1);
            if (endQuoteIdx < 0)
            {
                index = quoteIdx + 1;
                continue;
            }

            var fieldName = sourceText.Substring(quoteIdx + 1, endQuoteIdx - quoteIdx - 1);
            if (fieldName.Length > 0)
                fields.Add(fieldName);

            index = endQuoteIdx + 1;
        }
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string ReadAutomationSnapshotFamilyText()
    {
        return ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs")
            .Replace("\r\n", "\n");
    }

    private static string ReadAutomationSnapshotFlatteningFamilyText()
        => string.Join(
            "\n",
            ReadAutomationSnapshotFlatteningOrchestrationText(),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"))
            .Replace("\r\n", "\n");

    private static string ReadAutomationSnapshotFlatteningOrchestrationText()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        const string startToken = "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(";
        const string endToken = "private SnapshotStatusProjection BuildSnapshotStatusProjection(";
        var startIndex = snapshotProjectionText.IndexOf(startToken, StringComparison.Ordinal);
        var endIndex = snapshotProjectionText.IndexOf(endToken, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            throw new InvalidOperationException("Unable to locate automation snapshot flattening orchestration in the root snapshot projection file.");
        }

        return snapshotProjectionText[startIndex..endIndex];
    }
}
