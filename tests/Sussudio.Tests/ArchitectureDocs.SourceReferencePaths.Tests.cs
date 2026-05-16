using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureDocs_ReadRepoFileLiteralPathsResolve()
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
}
