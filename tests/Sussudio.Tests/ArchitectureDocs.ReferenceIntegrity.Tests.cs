using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            "- `CaptureService.FlashbackPreviewBackend.cs` owns Flashback preview backend",
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
}
