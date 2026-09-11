using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{
    /// <summary>
    /// Enforces the architecture denial policy.
    ///
    /// The policy lives in docs/architecture/architecture.policy.json rather
    /// than as hundreds of per-file assertions scattered through the contract
    /// suites, so the whole set of forbiddens can be read, reviewed, and
    /// amended in one place. Add an entry there instead of writing a new
    /// File.Exists assertion next to the test that motivated it.
    ///
    /// Guards whose asserted path is a directory combined with a loop variable
    /// stay in their owning suites; they are already table-driven and cannot be
    /// flattened into a static list.
    /// </summary>
    public sealed class ArchitecturePolicyTests
    {
        [Fact]
        public Task ForbiddenArchitecturePathsDoNotExist()
            => global::Program.ArchitecturePolicy_ForbiddenPathsDoNotExist();
    }
}

static partial class Program
{
    internal static Task ArchitecturePolicy_ForbiddenPathsDoNotExist()
    {
        const string PolicyRelativePath = "docs/architecture/architecture.policy.json";

        var policyText = ReadRepoFile(PolicyRelativePath);
        using var policy = JsonDocument.Parse(policyText);
        if (!policy.RootElement.TryGetProperty("forbiddenPaths", out var forbiddenPaths) ||
            forbiddenPaths.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"{PolicyRelativePath} must contain a 'forbiddenPaths' array.");
        }

        var repoRoot = GetRepoRoot();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();
        var violations = new List<string>();
        var entryCount = 0;

        foreach (var entry in forbiddenPaths.EnumerateArray())
        {
            var relativePath = entry.TryGetProperty("path", out var pathElement)
                ? pathElement.GetString()
                : null;
            var reason = entry.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(reason))
            {
                violations.Add("entry with a missing or empty 'path' or 'reason'");
                continue;
            }

            entryCount++;
            if (!seenPaths.Add(relativePath))
            {
                duplicates.Add(relativePath);
                continue;
            }

            var fullPath = Path.Combine(
                repoRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                violations.Add($"{relativePath} exists but is forbidden: {reason}");
            }
        }

        AssertEqual(0, duplicates.Count,
            $"architecture policy has duplicate path entries: {string.Join(", ", duplicates)}");
        AssertEqual(0, violations.Count,
            $"architecture policy violations: {string.Join(" | ", violations)}");
        AssertEqual(true, entryCount > 0,
            "architecture policy is empty; it should list every forbidden split path");

        return Task.CompletedTask;
    }
}
