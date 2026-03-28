using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ElgatoCapture;

public static class RuntimePaths
{
    private const string LogRootEnvVar = "ELGATOCAPTURE_LOG_ROOT";
    private static readonly Lazy<string> RepoRoot = new(ResolveRepoRoot, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoTempRoot = new(
        () => EnsureDirectory(Path.Combine(RepoRoot.Value, "temp")),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<string> RepoLogRoot = new(
        () => EnsureDirectory(ResolveLogRoot()),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static string GetRepoRoot() => RepoRoot.Value;
    public static string GetRepoTempRoot() => RepoTempRoot.Value;
    public static string GetRepoLogRoot() => RepoLogRoot.Value;
    public static string GetRepoTempFile(string fileName) => Path.Combine(GetRepoTempRoot(), fileName);
    public static string GetRepoLogFile(string fileName) => Path.Combine(GetRepoLogRoot(), fileName);

    private static string ResolveLogRoot()
    {
        var envOverride = Environment.GetEnvironmentVariable(LogRootEnvVar);
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            try
            {
                return EnsureDirectory(Path.GetFullPath(envOverride));
            }
            catch
            {
                /* Best-effort: env var path may be malformed or inaccessible — fall back to default */
            }
        }

        // Prefer repo-local logs when we can identify a repo root (development scenario).
        var repoRoot = RepoRoot.Value;
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            try
            {
                return EnsureDirectory(Path.Combine(repoRoot, "temp", "logs"));
            }
            catch
            {
                /* Best-effort: repo-local log dir creation may fail (permissions) — fall back to %LOCALAPPDATA% */
            }
        }

        // Non-repo scenario: keep logs in a stable per-user location.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return EnsureDirectory(Path.Combine(localAppData, "ElgatoCapture", "logs"));
    }

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
            /* Best-effort: latest-build parent resolution may fail — fall back to cwd */
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
            /* Best-effort: path may be invalid or inaccessible — treat as no repo root found */
            return null;
        }

        while (current != null)
        {
            try
            {
                var full = current.FullName;
                if (Directory.Exists(Path.Combine(full, ".git")) ||
                    File.Exists(Path.Combine(full, ".git")) ||
                    Directory.Exists(Path.Combine(full, ".claude")) ||
                    File.Exists(Path.Combine(full, "AGENTS.md")))
                {
                    return full;
                }

                if (File.Exists(Path.Combine(full, "ElgatoCapture.slnx")) ||
                    File.Exists(Path.Combine(full, "ElgatoCapture.sln")))
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
                /* Best-effort: directory may be inaccessible (permissions/junction) — skip and continue walking parents */
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
            /* Best-effort: candidate path may be malformed — skip it */
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
