static partial class Program
{
    private static IEnumerable<string> EnumerateUiPresentationOwnershipFiles(string repoRoot)
        => EnumerateSourceFiles(Path.Combine(repoRoot, "Sussudio"), SearchOption.AllDirectories)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(IsUiPresentationOwnershipFile)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateToolAutomationPartialFamilyFiles(string repoRoot)
    {
        var commonDirectory = Path.Combine(repoRoot, "tools", "Common");
        var familyPrefixes = new[]
        {
            "AutomationPipeClient",
            "AutomationSnapshotFormatter",
            "DiagnosticSessionFlashbackExportScenarios",
            "DiagnosticSessionFlashbackMetrics",
        };

        return EnumerateSourceFiles(commonDirectory, SearchOption.AllDirectories)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => familyPrefixes.Any(prefix =>
                GetRepoFileName(file).StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateCaptureRuntimeOwnershipFiles(string repoRoot)
    {
        var captureDirectory = Path.Combine(repoRoot, "Sussudio", "Services", "Capture");
        return EnumerateSourceFiles(captureDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("CaptureService", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateArchitectureDocsTestFiles(string repoRoot)
    {
        var testsDirectory = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        return EnumerateSourceFiles(testsDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("ArchitectureDocs", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateXUnitTestFiles(string repoRoot)
    {
        var testsDirectory = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        return EnumerateSourceFiles(testsDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => GetRepoFileName(file).StartsWith("XUnit.", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
    }

    private static bool AgentMapContainsExactCodeSpan(string agentMapText, string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        if (normalizedPath.StartsWith("tests/", StringComparison.Ordinal))
        {
            return agentMapText.Contains($"`{normalizedPath}`", StringComparison.Ordinal);
        }

        return MarkdownContainsExactCodeSpan(agentMapText, normalizedPath);
    }

    private static bool CleanupPlanContainsExactCodeSpan(string cleanupPlanText, string relativePath)
        => MarkdownContainsExactCodeSpan(cleanupPlanText, relativePath);

    private static bool MarkdownContainsExactCodeSpan(string markdownText, string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var fileName = GetRepoFileName(normalizedPath);

        return markdownText.Contains($"`{normalizedPath}`", StringComparison.Ordinal) ||
            markdownText.Contains($"`{fileName}`", StringComparison.Ordinal);
    }

    private static bool AgentMapContainsRequiredUiPresentationCodeSpan(string agentMapText, string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        if (RequiresExactUiPresentationOwnershipPath(normalizedPath))
        {
            return agentMapText.Contains($"`{normalizedPath}`", StringComparison.Ordinal);
        }

        return AgentMapContainsExactCodeSpan(agentMapText, normalizedPath);
    }

    private static bool RequiresExactUiPresentationOwnershipPath(string normalizedPath)
    {
        var directory = GetRepoDirectory(normalizedPath);
        var fileName = GetRepoFileName(normalizedPath);

        return string.Equals(directory, "Sussudio/ViewModels", StringComparison.OrdinalIgnoreCase) &&
            (fileName.StartsWith("StatsPresentationBuilder", StringComparison.Ordinal) ||
             fileName.StartsWith("StatsSnapshot", StringComparison.Ordinal) ||
             string.Equals(fileName, "StatsPresentationModels.cs", StringComparison.Ordinal) ||
             fileName.StartsWith("AudioRampTraceRecorder", StringComparison.Ordinal) ||
             string.Equals(fileName, "ViewModelPresentationBuilders.cs", StringComparison.Ordinal));
    }

    private static bool IsUiPresentationOwnershipFile(string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var directory = GetRepoDirectory(normalizedPath);
        var fileName = GetRepoFileName(normalizedPath);

        return (string.Equals(directory, "Sussudio", StringComparison.OrdinalIgnoreCase) &&
                fileName.StartsWith("MainWindow", StringComparison.Ordinal)) ||
            (string.Equals(directory, "Sussudio/ViewModels", StringComparison.OrdinalIgnoreCase) &&
                (fileName.StartsWith("MainViewModel", StringComparison.Ordinal) ||
                 fileName.StartsWith("AudioRampTraceRecorder", StringComparison.Ordinal) ||
                 fileName.StartsWith("StatsPresentationBuilder", StringComparison.Ordinal) ||
                 fileName.StartsWith("StatsSnapshot", StringComparison.Ordinal) ||
                 string.Equals(fileName, "StatsPresentationModels.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "DeviceFormatProbeRetargetPolicy.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "ViewModelPresentationBuilders.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "RecordingSettingsSelectionPolicy.cs", StringComparison.Ordinal))) ||
            ((string.Equals(directory, "Sussudio/Controllers", StringComparison.OrdinalIgnoreCase) ||
              directory.StartsWith("Sussudio/Controllers/", StringComparison.OrdinalIgnoreCase)) &&
                fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }
}
