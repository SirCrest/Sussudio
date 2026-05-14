using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    private static readonly Regex MarkdownCodeSpanRegex = new(
        "`([^`]+)`",
        RegexOptions.CultureInvariant);

    private static Task ArchitectureAgentMap_FileReferencesResolve()
    {
        var repoRoot = GetRepoRoot();
        var agentMapPath = Path.Combine(repoRoot, "docs", "architecture", "AGENT_MAP.md");
        var agentMapText = File.ReadAllText(agentMapPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        foreach (var token in EnumerateAgentMapPathTokens(agentMapText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesAgentMapToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md references missing files or folders: " + string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }

    private static Task ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve()
    {
        var repoRoot = GetRepoRoot();
        var agentMapPath = Path.Combine(repoRoot, "docs", "architecture", "AGENT_MAP.md");
        var agentMapText = File.ReadAllText(agentMapPath);
        var failures = new List<string>();

        foreach (var line in agentMapText.Split('\n'))
        {
            var normalizedLine = line.TrimEnd('\r');
            if (normalizedLine.Contains("ests/Sussudio.Tests/", StringComparison.Ordinal) &&
                !MarkdownCodeSpanRegex.IsMatch(normalizedLine))
            {
                failures.Add(normalizedLine.Trim());
                continue;
            }

            if (Regex.IsMatch(
                normalizedLine,
                @"^\s*-\s+tests/Sussudio\.Tests/[^`]+ owns\b",
                RegexOptions.CultureInvariant))
            {
                failures.Add(normalizedLine.Trim());
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md test-owner paths must be complete and wrapped in code spans: " +
                string.Join(" | ", failures));
        }

        return ArchitectureAgentMap_FileReferencesResolve();
    }

    private static Task ArchitectureAgentMap_CoversAutomationConsumerChecklist()
    {
        var readmeText = ReadRepoFile("README.md")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var consumers = ExtractReadmeAutomationConsumers(readmeText).ToArray();
        var missing = new List<string>();

        AssertEqual(9, consumers.Length, "README automation consumer checklist count");

        foreach (var consumer in consumers)
        {
            if (AutomationConsumerIsCoveredByAgentMap(consumer, agentMapText))
            {
                continue;
            }

            missing.Add(consumer);
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing automation consumer ownership for: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

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
                agentMapText.Contains("`Sussudio/Services/Automation/AutomationCommandDispatcher.cs`", StringComparison.Ordinal) &&
                agentMapText.Contains("`Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs`", StringComparison.Ordinal),
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
}
