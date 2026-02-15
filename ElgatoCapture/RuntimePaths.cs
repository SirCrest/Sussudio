using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ElgatoCapture;

public static class RuntimePaths
{
    private static readonly Lazy<string> RepoRoot = new(ResolveRepoRoot, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoTempRoot = new(
        () => EnsureDirectory(Path.Combine(RepoRoot.Value, "temp")),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoLogRoot = new(
        () => EnsureDirectory(Path.Combine(RepoTempRoot.Value, "logs")),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static string GetRepoRoot() => RepoRoot.Value;
    public static string GetRepoTempRoot() => RepoTempRoot.Value;
    public static string GetRepoLogRoot() => RepoLogRoot.Value;
    public static string GetRepoTempFile(string fileName) => Path.Combine(GetRepoTempRoot(), fileName);
    public static string GetRepoLogFile(string fileName) => Path.Combine(GetRepoLogRoot(), fileName);

    private static string ResolveRepoRoot()
    {
        var searchStarts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddPathIfPresent(searchStarts, AppContext.BaseDirectory);
        AddPathIfPresent(searchStarts, Directory.GetCurrentDirectory());

        foreach (var start in searchStarts)
        {
            var found = FindRepoRoot(start);
            if (!string.IsNullOrWhiteSpace(found))
            {
                return found;
            }
        }

        // If running from repo\latest-build, use parent as repo root.
        try
        {
            var baseDir = Path.GetFullPath(AppContext.BaseDirectory);
            var baseName = Path.GetFileName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.Equals(baseName, "latest-build", StringComparison.OrdinalIgnoreCase))
            {
                var parent = Directory.GetParent(baseDir);
                if (parent != null)
                {
                    return parent.FullName;
                }
            }
        }
        catch
        {
            // Best effort fallback below.
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindRepoRoot(string startPath)
    {
        DirectoryInfo? current;
        try
        {
            current = new DirectoryInfo(startPath);
        }
        catch
        {
            return null;
        }

        while (current != null)
        {
            try
            {
                var full = current.FullName;
                if (Directory.Exists(Path.Combine(full, ".git")) ||
                    Directory.Exists(Path.Combine(full, ".claude")) ||
                    File.Exists(Path.Combine(full, "AGENTS.md")))
                {
                    return full;
                }

                if (File.Exists(Path.Combine(full, "ElgatoCapture.csproj")))
                {
                    return current.Parent?.FullName ?? full;
                }
            }
            catch
            {
                // Continue walking parents.
            }

            current = current.Parent;
        }

        return null;
    }

    private static void AddPathIfPresent(ISet<string> paths, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(candidate);
            paths.Add(full);
        }
        catch
        {
            // Ignore malformed paths.
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
