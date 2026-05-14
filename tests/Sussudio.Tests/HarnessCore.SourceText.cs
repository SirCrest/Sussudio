static partial class Program
{
    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        while (index < sourceText.Length)
        {
            var getIdx = sourceText.IndexOf("Get(snapshot,", index, StringComparison.Ordinal);
            if (getIdx < 0)
                break;

            var afterComma = getIdx + "Get(snapshot,".Length;
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

        return fields;
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
        var files = new[]
        {
            "Sussudio/Models/Automation/AutomationSnapshot.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.UserSettings.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Hdr.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.AudioIngest.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Recording.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.CaptureFormat.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SourceTelemetry.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Preview.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Mjpeg.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SystemHealth.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Flashback.cs"
        };

        var parts = new List<string>();
        foreach (var file in files)
        {
            parts.Add(ReadRepoFile(file).Replace("\r\n", "\n"));
        }

        return string.Join("\n", parts);
    }
}
