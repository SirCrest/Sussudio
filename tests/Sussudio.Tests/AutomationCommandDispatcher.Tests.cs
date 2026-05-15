static partial class Program
{
    private static string ReadAutomationCommandDispatcherFamilyText()
    {
        var files = EnumerateAutomationCommandDispatcherFamilyFiles();

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }

    private static string[] EnumerateAutomationCommandDispatcherFamilyFiles()
    {
        var repoRoot = GetRepoRoot();
        var automationDirectory = Path.Combine(repoRoot, "Sussudio", "Services", "Automation");
        return EnumerateSourceFiles(automationDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("AutomationCommandDispatcher", StringComparison.Ordinal))
            .OrderBy(file => AutomationCommandDispatcherFamilySortKey(file), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string AutomationCommandDispatcherFamilySortKey(string relativePath)
    {
        var fileName = GetRepoFileName(relativePath);
        return string.Equals(fileName, "AutomationCommandDispatcher.cs", StringComparison.Ordinal)
            ? "0"
            : "1" + fileName;
    }
}
