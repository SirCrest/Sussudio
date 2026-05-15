using System.Text.RegularExpressions;

static partial class Program
{
    private static readonly Regex MarkdownCodeSpanRegex = new(
        "`([^`]+)`",
        RegexOptions.CultureInvariant);

    private static IEnumerable<string> ExtractReadmeAutomationConsumers(string readmeText)
    {
        const string marker = "Then keep these consumers in sync:";
        var markerIndex = readmeText.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException("README.md automation consumer checklist marker was not found.");
        }

        var checklistStart = markerIndex + marker.Length;
        var checklistText = readmeText.Substring(checklistStart);
        var started = false;
        foreach (var line in checklistText.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (started)
                {
                    yield break;
                }

                continue;
            }

            var match = Regex.Match(line, @"^\s*-\s+`([^`]+)`", RegexOptions.CultureInvariant);
            if (match.Success)
            {
                started = true;
                yield return match.Groups[1].Value.Trim();
            }
        }
    }

    private static bool AutomationConsumerIsCoveredByAgentMap(string consumer, string agentMapText)
        => consumer switch
        {
            "Sussudio/Services/Automation/AutomationCommandDispatcher*.cs" =>
                AgentMapCoversEveryAutomationCommandDispatcherFile(agentMapText),
            "Sussudio.Automation.Contracts/AutomationCommandKind.cs" =>
                agentMapText.Contains("Primary owner: `Sussudio.Automation.Contracts/`", StringComparison.Ordinal) &&
                agentMapText.Contains("`AutomationCommandKind.cs` owns numeric command IDs.", StringComparison.Ordinal),
            "Sussudio.Automation.Contracts/AutomationCommandCatalog.cs" =>
                agentMapText.Contains("Primary owner: `Sussudio.Automation.Contracts/`", StringComparison.Ordinal) &&
                agentMapText.Contains("`AutomationCommandCatalog.cs` owns command metadata", StringComparison.Ordinal),
            "Sussudio.Automation.Contracts/AutomationPipeProtocol.cs" =>
                agentMapText.Contains("Primary owner: `Sussudio.Automation.Contracts/`", StringComparison.Ordinal) &&
                agentMapText.Contains("`AutomationPipeProtocol.cs` owns pipe names", StringComparison.Ordinal),
            "tools/ssctl/" =>
                agentMapText.Contains("`tools/ssctl/` for the preferred CLI.", StringComparison.Ordinal),
            "tools/McpServer/" =>
                agentMapText.Contains("`tools/McpServer/` for MCP bridge tools.", StringComparison.Ordinal),
            "tools/AutomationClient/" =>
                agentMapText.Contains("`tools/AutomationClient/Program.cs` owns the low-level pipe client entry", StringComparison.Ordinal) &&
                agentMapText.Contains("`tools/AutomationClient/README.md` owns AutomationClient usage notes.", StringComparison.Ordinal),
            "tools/send-automation-command.ps1" =>
                agentMapText.Contains("`tools/send-automation-command.ps1` owns the PowerShell helper wrapper", StringComparison.Ordinal),
            "tests/Sussudio.Tests/" =>
                agentMapText.Contains("`tests/Sussudio.Tests/Program.cs`", StringComparison.Ordinal) &&
                agentMapText.Contains("`tests/Sussudio.Tests/HarnessCheckCatalog*.cs`", StringComparison.Ordinal),
            _ => agentMapText.Contains($"`{consumer}`", StringComparison.Ordinal)
        };

    private static bool AgentMapCoversEveryAutomationCommandDispatcherFile(string agentMapText)
        => EnumerateAutomationCommandDispatcherFamilyFiles()
            .All(file => agentMapText.Contains($"`{file}`", StringComparison.Ordinal));

    private static IEnumerable<string> EnumerateAgentMapPathTokens(string markdown)
    {
        foreach (Match match in MarkdownCodeSpanRegex.Matches(markdown))
        {
            var token = match.Groups[1].Value.Trim();
            if (IsAgentMapPathToken(token))
            {
                yield return NormalizeProjectInclude(token);
            }
        }
    }

    private static bool IsAgentMapPathToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "/", StringComparison.Ordinal))
        {
            return false;
        }

        return token.EndsWith("/", StringComparison.Ordinal) ||
            token.Contains('*', StringComparison.Ordinal) ||
            token.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolvesAgentMapToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
    {
        if (token.EndsWith("/", StringComparison.Ordinal))
        {
            return directories.Contains(token.TrimEnd('/'));
        }

        if (token.Contains('*', StringComparison.Ordinal))
        {
            return AgentMapWildcardMatches(token, files);
        }

        if (token.Contains('/', StringComparison.Ordinal))
        {
            return files.Contains(token, StringComparer.OrdinalIgnoreCase);
        }

        return files.Any(file => string.Equals(Path.GetFileName(file), token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool AgentMapWildcardMatches(string token, IEnumerable<string> files)
    {
        var wildcard = "^" + Regex.Escape(token).Replace("\\*", ".*") + "$";
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        return token.Contains('/', StringComparison.Ordinal)
            ? files.Any(file => Regex.IsMatch(file, wildcard, options))
            : files.Any(file => Regex.IsMatch(Path.GetFileName(file), wildcard, options));
    }

    private static string NormalizeRepoRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');

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
            "AutomationSnapshotFormatter",
            "DiagnosticSessionFlashbackExportScenarios",
            "DiagnosticSessionFlashbackMetrics",
        };

        return EnumerateSourceFiles(commonDirectory, SearchOption.TopDirectoryOnly)
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .Where(file => familyPrefixes.Any(prefix =>
                GetRepoFileName(file).StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
    }

    private static bool AgentMapContainsExactCodeSpan(string agentMapText, string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var fileName = GetRepoFileName(normalizedPath);

        return agentMapText.Contains($"`{normalizedPath}`", StringComparison.Ordinal) ||
            agentMapText.Contains($"`{fileName}`", StringComparison.Ordinal);
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
             string.Equals(fileName, "CaptureModeOptionsBuilder.cs", StringComparison.Ordinal) ||
             string.Equals(fileName, "LiveSignalTextPresentationBuilder.cs", StringComparison.Ordinal) ||
             string.Equals(fileName, "SourceTelemetryPresentationBuilder.cs", StringComparison.Ordinal));
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
                 fileName.StartsWith("StatsPresentationBuilder", StringComparison.Ordinal) ||
                 fileName.StartsWith("StatsSnapshot", StringComparison.Ordinal) ||
                 string.Equals(fileName, "StatsPresentationModels.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "CaptureModeOptionsBuilder.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "DeviceFormatProbeRetargetPolicy.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "DeviceAudioGainMapper.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "LiveSignalTextPresentationBuilder.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "RecordingFormatSelectionPolicy.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "SourceTelemetryPresentationBuilder.cs", StringComparison.Ordinal))) ||
            (string.Equals(directory, "Sussudio/Controllers", StringComparison.OrdinalIgnoreCase) &&
                fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRepoDirectory(string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var slashIndex = normalizedPath.LastIndexOf('/');
        return slashIndex < 0 ? string.Empty : normalizedPath.Substring(0, slashIndex);
    }

    private static string GetRepoFileName(string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var slashIndex = normalizedPath.LastIndexOf('/');
        return slashIndex < 0 ? normalizedPath : normalizedPath.Substring(slashIndex + 1);
    }
}
