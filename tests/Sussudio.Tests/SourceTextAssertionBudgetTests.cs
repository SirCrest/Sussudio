using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{
    /// <summary>
    /// Ceiling on source-text assertions.
    ///
    /// An assertion whose value was read from repository source cannot fail
    /// when production behavior breaks: it only fails when the text moves. A
    /// suite built from them reports green while proving nothing, and it makes
    /// every legitimate refactor expensive. This test keeps the population
    /// from growing so new coverage has to be behavioral.
    ///
    /// The counts live in docs/architecture/source-text-assertions.json. When a
    /// slice removes source-text assertions, lower the ceiling for that file in
    /// the same change so the reduction is locked in.
    ///
    /// Regenerate the ceilings with:
    ///   $env:SUSSUDIO_WRITE_SOURCE_TEXT_BASELINE = "1"
    ///   dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --filter FullyQualifiedName~SourceTextAssertionBudget
    /// </summary>
    public sealed class SourceTextAssertionBudgetTests
    {
        [Fact]
        public Task SourceTextAssertionsDoNotGrow()
            => global::Program.SourceTextAssertions_DoNotGrow();
    }
}

static partial class Program
{
    private const string SourceTextBaselineRelativePath = "docs/architecture/source-text-assertions.json";
    private const string SourceTextBaselineWriteEnvVar = "SUSSUDIO_WRITE_SOURCE_TEXT_BASELINE";

    // Locals assigned from a repository read. Assertions on these assert on
    // source text, not on behavior.
    private static readonly Regex SourceTextAssignmentPattern = new(
        @"(\w+)\s*=\s*(?:ReadRepoFile|GetRepoFileText|(?:System\.IO\.)?File\.ReadAllText)\(",
        RegexOptions.Compiled);

    private static readonly Regex AssertionCallPattern = new(
        @"Assert(?:DoesNot)?Contains\(\s*([A-Za-z_]\w*)",
        RegexOptions.Compiled);

    private static readonly Regex DirectSourceAssertionPattern = new(
        @"Assert(?:DoesNot)?Contains\(\s*ReadRepoFile\(",
        RegexOptions.Compiled);

    internal static Task SourceTextAssertions_DoNotGrow()
    {
        var repoRoot = GetRepoRoot();
        var testDirectory = Path.Combine(repoRoot, "tests", "Sussudio.Tests");

        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(testDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var name = Path.GetFileName(path);
            if (name == "SourceTextAssertionBudgetTests.cs")
            {
                // This file necessarily contains the patterns it searches for.
                continue;
            }

            var text = File.ReadAllText(path);
            var sourceVariables = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in SourceTextAssignmentPattern.Matches(text))
            {
                sourceVariables.Add(match.Groups[1].Value);
            }

            var count = 0;
            foreach (Match match in AssertionCallPattern.Matches(text))
            {
                if (sourceVariables.Contains(match.Groups[1].Value))
                {
                    count++;
                }
            }
            count += DirectSourceAssertionPattern.Matches(text).Count;

            if (count > 0)
            {
                counts[name] = count;
            }
        }

        if (Environment.GetEnvironmentVariable(SourceTextBaselineWriteEnvVar) == "1")
        {
            var regenerated = new
            {
                schemaVersion = 1,
                description =
                    "Ceiling on source-text assertions per test file. " +
                    "An assertion on repository source text cannot fail when behavior breaks; " +
                    "lower a ceiling when a slice removes some. The only reason to raise one is " +
                    "restoring coverage that a previous slice deleted by mistake.",
                totalCeiling = counts.Values.Sum(),
                fileCeilings = counts
            };
            File.WriteAllText(
                Path.Combine(repoRoot, SourceTextBaselineRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                JsonSerializer.Serialize(regenerated, new JsonSerializerOptions { WriteIndented = true }));
            return Task.CompletedTask;
        }

        var baselinePath = Path.Combine(
            repoRoot,
            SourceTextBaselineRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(baselinePath))
        {
            throw new InvalidOperationException(
                $"{SourceTextBaselineRelativePath} is missing. Regenerate it with " +
                $"{SourceTextBaselineWriteEnvVar}=1.");
        }

        var ceilings = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var baseline = JsonDocument.Parse(File.ReadAllText(baselinePath)))
        {
            foreach (var entry in baseline.RootElement.GetProperty("fileCeilings").EnumerateObject())
            {
                ceilings[entry.Name] = entry.Value.GetInt32();
            }
        }

        var increases = new List<string>();
        var unlisted = new List<string>();
        foreach (var pair in counts)
        {
            if (!ceilings.TryGetValue(pair.Key, out var ceiling))
            {
                unlisted.Add($"{pair.Key}={pair.Value}");
            }
            else if (pair.Value > ceiling)
            {
                increases.Add($"{pair.Key}: {ceiling} -> {pair.Value}");
            }
        }

        var currentTotal = counts.Values.Sum();
        var ceilingTotal = ceilings.Values.Sum();
        var messages = new List<string>();
        if (increases.Count > 0)
        {
            messages.Add(
                "source-text assertions grew (add behavioral coverage instead, or lower " +
                $"the ceiling if you removed some): {string.Join("; ", increases)}");
        }
        if (unlisted.Count > 0)
        {
            messages.Add(
                $"file(s) with source-text assertions but no ceiling entry: {string.Join("; ", unlisted)}");
        }

        AssertEqual(0, messages.Count,
            string.Join(" | ", messages) + $" [total {currentTotal} vs ceiling {ceilingTotal}]");

        return Task.CompletedTask;
    }
}
