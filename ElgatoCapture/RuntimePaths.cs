using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            catch (Exception ex)
            {
                Trace.TraceWarning($"RuntimePaths: env var '{LogRootEnvVar}' path resolution failed, falling back to default: {ex.Message}");
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
            catch (Exception ex)
            {
                Trace.TraceWarning($"RuntimePaths: repo-local log dir creation failed, falling back to %%LOCALAPPDATA%%: {ex.Message}");
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
        catch (Exception ex)
        {
            Trace.TraceWarning($"RuntimePaths: latest-build parent resolution failed, falling back to cwd: {ex.Message}");
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
        catch (Exception ex)
        {
            Trace.TraceWarning($"RuntimePaths: path '{startPath}' is invalid or inaccessible: {ex.Message}");
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
            catch (Exception ex)
            {
                Trace.TraceWarning($"RuntimePaths: directory inaccessible during repo root search, skipping: {ex.Message}");
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
        catch (Exception ex)
        {
            Trace.TraceWarning($"RuntimePaths: candidate path '{candidate}' is malformed, skipping: {ex.Message}");
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
