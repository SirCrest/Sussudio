using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

static partial class Program
{
    internal static Task ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve()
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

    internal static Task ArchitectureAgentMap_FileReferencesResolve()
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

    internal static Task ArchitectureAgentMap_CoversArchitectureDocsTestFamily()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateArchitectureDocsTestFiles(repoRoot)
            .Where(file => !AgentMapContainsExactCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing ArchitectureDocs test-family owner entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureAgentMap_CoversAutomationConsumerChecklist()
    {
        var readmeText = ReadRepoFile("README.md")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var consumers = ExtractReadmeAutomationConsumers(readmeText).ToArray();
        var missing = new List<string>();

        AssertEqual(8, consumers.Length, "README automation consumer checklist count");

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

    internal static Task ArchitectureAgentMap_CoversUiPresentationOwnershipFiles()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateUiPresentationOwnershipFiles(repoRoot)
            .Where(file => !AgentMapContainsRequiredUiPresentationCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing UI/presentation ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureAgentMap_CoversCaptureRuntimeOwnershipFiles()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateCaptureRuntimeOwnershipFiles(repoRoot)
            .Where(file => !AgentMapContainsExactCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing CaptureService ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureAgentMap_MapsFlashbackPreviewStartupToResourceOwner()
    {
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var previewBackendEntry = ExtractTextBetween(
            agentMapText,
            "- `CaptureService.FlashbackControls.cs` owns Flashback public state",
            "- `CaptureService.FlashbackControls.cs` owns buffer-cycle");

        AssertContains(previewBackendEntry, "transition coordination");
        AssertContains(previewBackendEntry, "AV1 encoder support probing");
        AssertContains(previewBackendEntry, "video/audio readiness");
        AssertContains(previewBackendEntry, "resource-owner request construction");
        AssertContains(previewBackendEntry, "deferred cleanup handoff");
        AssertContains(previewBackendEntry, "preview backend disposal request construction");
        AssertContains(previewBackendEntry, "`FlashbackBackendResources.cs` owns startup construction");
        AssertContains(previewBackendEntry, "producer attach/detach request");
        AssertContains(previewBackendEntry, "feed wiring");
        AssertContains(previewBackendEntry, "`FlashbackBackendResources.cs`");
        AssertContains(previewBackendEntry, "rollback cleanup");
        AssertDoesNotContain(previewBackendEntry, "`FlashbackBackendResources.Startup.cs`");
        AssertDoesNotContain(previewBackendEntry, "`FlashbackBackendResources.Teardown.cs`");
        AssertDoesNotContain(previewBackendEntry, "startup: buffer manager, encoder sink, exporter, playback controller, and\n  producer attachment");

        return Task.CompletedTask;
    }

    internal static Task ArchitectureAgentMap_CoversToolAutomationPartialFamiliesWithExactPaths()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateToolAutomationPartialFamilyFiles(repoRoot)
            .Where(file => !agentMapText.Contains($"`{file}`", StringComparison.Ordinal))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing exact tool automation ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureAgentMap_ToolsCommonOwnershipEntriesAreUnique()
    {
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var ownerBulletRegex = new Regex(
            @"^\s*-\s+`(?<path>tools/Common/[^`]+\.cs)`\s+(?:also\s+owns|owns|is)\b",
            RegexOptions.CultureInvariant);
        var firstLineByPath = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        var lines = agentMapText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var match = ownerBulletRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var path = match.Groups["path"].Value;
            var lineNumber = i + 1;
            if (firstLineByPath.TryGetValue(path, out var firstLineNumber))
            {
                duplicates.Add($"{path} first={firstLineNumber} duplicate={lineNumber}");
                continue;
            }

            firstLineByPath[path] = lineNumber;
        }

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md has duplicate tools/Common ownership bullets: " +
                string.Join(" | ", duplicates));
        }

        return Task.CompletedTask;
    }

    internal static Task TestProject_DoesNotKeepEmptyPartialMarkerShells()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        var emptyMarkerShells = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var normalized = string.Join(
                    "\n",
                    File.ReadAllLines(path)
                        .Select(line => line.Trim())
                        .Where(line =>
                            line.Length > 0 &&
                            !line.StartsWith("//", StringComparison.Ordinal) &&
                            !line.StartsWith("using ", StringComparison.Ordinal)));
                return normalized == "static partial class Program\n{\n}";
            })
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (emptyMarkerShells.Length > 0)
        {
            throw new InvalidOperationException(
                "Empty test partial marker shells add navigation cost without ownership: " +
                string.Join(", ", emptyMarkerShells));
        }

        return Task.CompletedTask;
    }

    internal static Task TestProject_TestOwnerFilesDoNotReopenProgramWithinSameFile()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        var duplicateProgramBodies = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => new
            {
                Path = Path.GetRelativePath(repoRoot, path).Replace('\\', '/'),
                Count = Regex.Matches(
                    File.ReadAllText(path),
                    @"(?m)^static partial class Program\s*$").Count
            })
            .Where(candidate => candidate.Count > 1)
            .OrderBy(candidate => candidate.Path, StringComparer.Ordinal)
            .Select(candidate => $"{candidate.Path} ({candidate.Count})")
            .ToArray();

        if (duplicateProgramBodies.Length > 0)
        {
            throw new InvalidOperationException(
                "Test owner files should keep one Program body instead of same-file partial shells: " +
                string.Join(", ", duplicateProgramBodies));
        }

        return Task.CompletedTask;
    }

    internal static Task TestProject_DoesNotKeepStaleSingleFilePartials()
    {
        var repoRoot = GetRepoRoot();
        var declarations = new[]
            {
                Path.Combine(repoRoot, "Sussudio"),
                Path.Combine(repoRoot, "tests", "Sussudio.Tests")
            }
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Select(item => new
            {
                item.path,
                item.index,
                Match = Regex.Match(
                    item.line,
                    @"^\s*(?:public|internal|private|protected|static|sealed|abstract|partial|\s)+\bpartial\s+(?:class|record|struct)\s+(?<type>[A-Za-z_][A-Za-z0-9_]*)")
            })
            .Where(item => item.Match.Success)
            .Select(item => new
            {
                Type = item.Match.Groups["type"].Value,
                Path = Path.GetRelativePath(repoRoot, item.path).Replace('\\', '/'),
                Line = item.index + 1
            })
            .ToArray();

        var stalePartials = declarations
            .GroupBy(item => item.Type, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .Select(group => group.Single())
            .Where(item => !IsAllowedSingleFilePartial(item.Type, item.Path))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .Select(item => $"{item.Path}:{item.Line} ({item.Type})")
            .ToArray();

        if (stalePartials.Length > 0)
        {
            throw new InvalidOperationException(
                "Single-file partial declarations should be generated/XAML/source-generator companions, not stale ownership markers: " +
                string.Join(", ", stalePartials));
        }

        return Task.CompletedTask;
    }

    private static bool IsAllowedSingleFilePartial(string typeName, string relativePath)
        => relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) ||
           typeName.EndsWith("JsonContext", StringComparison.Ordinal);

    internal static Task ArchitectureDocs_ReadRepoFileLiteralPathsResolve()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        var pathPattern = new Regex(
            @"(?:RuntimeContractSource\.)?ReadRepoFile\(\s*""(?<path>[^""]+)""",
            RegexOptions.Compiled);
        var failures = new List<string>();

        foreach (var file in Directory.GetFiles(testRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in pathPattern.Matches(text))
            {
                var repoRelativePath = match.Groups["path"].Value;
                var candidate = Path.Combine(
                    repoRoot,
                    repoRelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                {
                    var relativeTestPath = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
                    failures.Add($"{relativeTestPath}: {repoRelativePath}");
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Literal ReadRepoFile paths must resolve to live repo files or directories:\n" +
                string.Join("\n", failures.OrderBy(path => path, StringComparer.Ordinal)));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureCleanupPlan_FileReferencesResolve()
    {
        var repoRoot = GetRepoRoot();
        var cleanupPlanPath = Path.Combine(repoRoot, "docs", "architecture", "cleanup-plan.md");
        var cleanupPlanText = File.ReadAllText(cleanupPlanPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        foreach (var token in EnumerateCleanupPlanPathTokens(cleanupPlanText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesCleanupPlanToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "cleanup-plan.md references missing repo files or folders: " + string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureCleanupPlan_CoversArchitectureDocsTestFamily()
    {
        var repoRoot = GetRepoRoot();
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var missing = EnumerateArchitectureDocsTestFiles(repoRoot)
            .Where(file => !CleanupPlanContainsExactCodeSpan(cleanupPlanText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "cleanup-plan.md is missing ArchitectureDocs test-family owner entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    internal static Task ArchitectureCleanupPlan_DefinesSmallFileHygiene()
    {
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(cleanupPlanText, "Small-file hygiene applies to every slice below");
        AssertContains(cleanupPlanText, "do not create or keep sub-100-line files");
        AssertContains(cleanupPlanText, "owning a stable contract, hot-path lifetime, XAML adapter surface, shared tool");
        AssertContains(cleanupPlanText, "fold it back into that owner and update the source-shape tests");

        return Task.CompletedTask;
    }

    internal static Task ArchitectureDefragBaseline_TracksCheckpointCountsAndLoc()
    {
        var repoRoot = GetRepoRoot();
        var scriptText = ReadRepoFile("scripts/architecture/Capture-SussudioDefragBaseline.ps1");
        var baselineText = ReadRepoFile("docs/architecture/Sussudio-Defragmentation-Baseline.generated.md");
        var coreFiles = EnumerateSourceFiles(
                Path.Combine(repoRoot, "Sussudio"),
                SearchOption.AllDirectories)
            .ToArray();
        var sussudioTestFiles = EnumerateSourceFiles(
                Path.Combine(repoRoot, "tests", "Sussudio.Tests"),
                SearchOption.AllDirectories)
            .ToArray();
        var coreNonBlankLines = coreFiles.Sum(CountNonBlankSourceLines);
        var sussudioTestNonBlankLines = sussudioTestFiles.Sum(CountNonBlankSourceLines);

        AssertContains(scriptText, "Get-NonBlankLineCount");
        AssertContains(scriptText, "Core app .cs files (Sussudio/)");
        AssertContains(scriptText, "Sussudio.Tests nonblank LoC");
        AssertContains(baselineText, $"| Core app .cs files (Sussudio/) | {coreFiles.Length} |");
        AssertContains(baselineText, $"| Core app nonblank LoC (Sussudio/) | {coreNonBlankLines} |");
        AssertContains(baselineText, $"| Sussudio.Tests .cs files | {sussudioTestFiles.Length} |");
        AssertContains(baselineText, $"| Sussudio.Tests nonblank LoC | {sussudioTestNonBlankLines} |");

        return Task.CompletedTask;
    }

    internal static Task TestMigrationPlan_FileReferencesResolveAndNamesValidationCommands()
    {
        var repoRoot = GetRepoRoot();
        var migrationPath = Path.Combine(repoRoot, "tests", "Sussudio.Tests", "MIGRATION.md");
        var migrationText = File.ReadAllText(migrationPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var token in EnumerateMigrationPlanPathTokens(migrationText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesMigrationPlanToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (!migrationText.Contains("dotnet test tests/Sussudio.Tests/Sussudio.Tests.csproj --no-restore", StringComparison.Ordinal))
        {
            failures.Add("missing dotnet test validation command");
        }

        if (!migrationText.Contains("dotnet exec tests\\Sussudio.Tests\\bin\\Debug\\net8.0\\Sussudio.Tests.dll", StringComparison.Ordinal))
        {
            failures.Add("missing dotnet exec legacy harness validation command");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "MIGRATION.md references or validation commands are stale: " +
                string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }

    internal static Task TestMigrationPlan_CoversXUnitInventory()
    {
        var repoRoot = GetRepoRoot();
        var migrationText = ReadRepoFile("tests/Sussudio.Tests/MIGRATION.md");
        var missing = EnumerateXUnitTestFiles(repoRoot)
            .Where(file => !MarkdownContainsExactCodeSpan(migrationText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "MIGRATION.md is missing xUnit test inventory entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    private static readonly Regex MarkdownCodeSpanRegex = new(
        "`([^`]+)`",
        RegexOptions.CultureInvariant);

    private static int CountNonBlankSourceLines(string path)
        => File.ReadLines(path).Count(line => line.Trim().Length > 0);

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
            "Sussudio.Automation.Contracts/AutomationCommandCatalog.cs" =>
                agentMapText.Contains("Primary owner: `Sussudio.Automation.Contracts/`", StringComparison.Ordinal) &&
                agentMapText.Contains("`AutomationCommandCatalog.cs` owns numeric command IDs", StringComparison.Ordinal) &&
                agentMapText.Contains("command lookup", StringComparison.Ordinal) &&
                agentMapText.Contains("command metadata table", StringComparison.Ordinal) &&
                agentMapText.Contains("registration orchestration", StringComparison.Ordinal),
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
                agentMapText.Contains("`tests/Sussudio.Tests/HarnessCore.cs`", StringComparison.Ordinal) &&
                agentMapText.Contains("focused `tests/Sussudio.Tests/XUnit.*.cs` slices", StringComparison.Ordinal) &&
                agentMapText.Contains("xUnit slices", StringComparison.Ordinal) &&
                agentMapText.Contains("focused contract tests", StringComparison.Ordinal),
            _ => agentMapText.Contains($"`{consumer}`", StringComparison.Ordinal)
        };

    private static bool AgentMapCoversEveryAutomationCommandDispatcherFile(string agentMapText)
        => EnumerateAutomationCommandDispatcherFamilyFiles()
            .All(file => agentMapText.Contains($"`{file}`", StringComparison.Ordinal));

    private static IEnumerable<string> EnumerateAgentMapPathTokens(string markdown)
        => EnumerateMarkdownPathTokens(markdown, IsAgentMapPathToken);

    private static IEnumerable<string> EnumerateCleanupPlanPathTokens(string markdown)
        => EnumerateMarkdownPathTokens(markdown, IsCleanupPlanPathToken);

    private static IEnumerable<string> EnumerateMigrationPlanPathTokens(string markdown)
        => EnumerateMarkdownPathTokens(markdown, IsMigrationPlanPathToken);

    private static IEnumerable<string> EnumerateMarkdownPathTokens(
        string markdown,
        Func<string, bool> isPathToken)
    {
        foreach (Match match in MarkdownCodeSpanRegex.Matches(markdown))
        {
            var token = NormalizeProjectInclude(match.Groups[1].Value.Trim());
            if (isPathToken(token))
            {
                yield return token;
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

    private static bool IsCleanupPlanPathToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "/", StringComparison.Ordinal))
        {
            return false;
        }

        if (token.StartsWith("Sussudio/", StringComparison.Ordinal) ||
            token.StartsWith("tests/", StringComparison.Ordinal) ||
            token.StartsWith("tools/", StringComparison.Ordinal) ||
            token.StartsWith("docs/", StringComparison.Ordinal))
        {
            return true;
        }

        return token.StartsWith("MainWindow.", StringComparison.Ordinal) &&
            token.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMigrationPlanPathToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Contains(" ", StringComparison.Ordinal) ||
            token.Contains("<", StringComparison.Ordinal) ||
            token.Contains(">", StringComparison.Ordinal) ||
            string.Equals(token, "/", StringComparison.Ordinal))
        {
            return false;
        }

        return token.EndsWith("/", StringComparison.Ordinal) ||
            token.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            token.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolvesAgentMapToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
        => ResolvesMarkdownPathToken(
            token,
            files,
            directories,
            allowDirectoryPathWithoutTrailingSlash: false);

    private static bool ResolvesCleanupPlanToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
        => ResolvesMarkdownPathToken(
            token,
            files,
            directories,
            allowDirectoryPathWithoutTrailingSlash: true);

    private static bool ResolvesMigrationPlanToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
        => ResolvesMarkdownPathToken(
            token,
            files,
            directories,
            allowDirectoryPathWithoutTrailingSlash: true);

    private static bool ResolvesMarkdownPathToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories,
        bool allowDirectoryPathWithoutTrailingSlash)
    {
        if (token.EndsWith("/", StringComparison.Ordinal))
        {
            return directories.Contains(token.TrimEnd('/'));
        }

        if (token.Contains('*', StringComparison.Ordinal))
        {
            return MarkdownWildcardMatches(token, files);
        }

        var normalized = token.TrimEnd('/');
        if (token.Contains('/', StringComparison.Ordinal))
        {
            return files.Contains(normalized, StringComparer.OrdinalIgnoreCase) ||
                (allowDirectoryPathWithoutTrailingSlash && directories.Contains(normalized));
        }

        return files.Any(file => string.Equals(Path.GetFileName(file), token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MarkdownWildcardMatches(string token, IEnumerable<string> files)
    {
        var wildcard = "^" + Regex.Escape(token).Replace("\\*", ".*") + "$";
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        return token.Contains('/', StringComparison.Ordinal)
            ? files.Any(file => Regex.IsMatch(file, wildcard, options))
            : files.Any(file => Regex.IsMatch(Path.GetFileName(file), wildcard, options));
    }

    private static string NormalizeRepoRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');

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
             fileName.StartsWith("PreviewAudioTransitionControllers", StringComparison.Ordinal) ||
             string.Equals(fileName, "ViewModelBuilders.cs", StringComparison.Ordinal));
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
                 fileName.StartsWith("PreviewAudioTransitionControllers", StringComparison.Ordinal) ||
                 fileName.StartsWith("StatsPresentationBuilder", StringComparison.Ordinal) ||
                 fileName.StartsWith("StatsSnapshot", StringComparison.Ordinal) ||
                 string.Equals(fileName, "ViewModelSelectionPolicies.cs", StringComparison.Ordinal) ||
                 string.Equals(fileName, "ViewModelBuilders.cs", StringComparison.Ordinal))) ||
            ((string.Equals(directory, "Sussudio/Controllers", StringComparison.OrdinalIgnoreCase) ||
              directory.StartsWith("Sussudio/Controllers/", StringComparison.OrdinalIgnoreCase)) &&
                fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }
}

namespace Sussudio.Tests
{
    public sealed class ArchitectureDocsAgentMapOwnershipTests
    {
        [Fact]
        public Task AgentMapFileReferencesResolve()
            => global::Program.ArchitectureAgentMap_FileReferencesResolve();

        [Fact]
        public Task AgentMapTestOwnerPathsUseCodeSpansAndResolve()
            => global::Program.ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve();

        [Fact]
        public Task AgentMapCoversArchitectureDocsTestFamily()
            => global::Program.ArchitectureAgentMap_CoversArchitectureDocsTestFamily();

        [Fact]
        public Task AgentMapHasUniqueToolsCommonOwnershipEntries()
            => global::Program.ArchitectureAgentMap_ToolsCommonOwnershipEntriesAreUnique();

        [Fact]
        public Task TestProjectDoesNotKeepEmptyPartialMarkerShells()
            => global::Program.TestProject_DoesNotKeepEmptyPartialMarkerShells();

        [Fact]
        public Task TestProjectTestOwnerFilesDoNotReopenProgramWithinSameFile()
            => global::Program.TestProject_TestOwnerFilesDoNotReopenProgramWithinSameFile();

        [Fact]
        public Task TestProjectDoesNotKeepStaleSingleFilePartials()
            => global::Program.TestProject_DoesNotKeepStaleSingleFilePartials();

        [Fact]
        public Task AgentMapCoversAutomationConsumerChecklist()
            => global::Program.ArchitectureAgentMap_CoversAutomationConsumerChecklist();

        [Fact]
        public Task AgentMapCoversUiPresentationOwnershipFiles()
            => global::Program.ArchitectureAgentMap_CoversUiPresentationOwnershipFiles();

        [Fact]
        public Task AgentMapCoversCaptureRuntimeOwnershipFiles()
            => global::Program.ArchitectureAgentMap_CoversCaptureRuntimeOwnershipFiles();

        [Fact]
        public Task AgentMapMapsFlashbackPreviewStartupToResourceOwner()
            => global::Program.ArchitectureAgentMap_MapsFlashbackPreviewStartupToResourceOwner();

        [Fact]
        public Task AgentMapCoversToolAutomationPartialFamiliesWithExactPaths()
            => global::Program.ArchitectureAgentMap_CoversToolAutomationPartialFamiliesWithExactPaths();
    }

    public sealed class ArchitectureDocsReferenceIntegrityTests
    {
        [Fact]
        public Task ReadRepoFileLiteralPathsResolve()
            => global::Program.ArchitectureDocs_ReadRepoFileLiteralPathsResolve();

        [Fact]
        public Task CleanupPlanFileReferencesResolve()
            => global::Program.ArchitectureCleanupPlan_FileReferencesResolve();

        [Fact]
        public Task CleanupPlanCoversArchitectureDocsTestFamily()
            => global::Program.ArchitectureCleanupPlan_CoversArchitectureDocsTestFamily();

        [Fact]
        public Task CleanupPlanDefinesSmallFileHygiene()
            => global::Program.ArchitectureCleanupPlan_DefinesSmallFileHygiene();

        [Fact]
        public Task DefragBaselineTracksCheckpointCountsAndLoc()
            => global::Program.ArchitectureDefragBaseline_TracksCheckpointCountsAndLoc();

        [Fact]
        public Task TestMigrationPlanFileReferencesResolveAndNamesValidationCommands()
            => global::Program.TestMigrationPlan_FileReferencesResolveAndNamesValidationCommands();

        [Fact]
        public Task TestMigrationPlanCoversXUnitInventory()
            => global::Program.TestMigrationPlan_CoversXUnitInventory();
    }
}
