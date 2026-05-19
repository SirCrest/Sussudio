using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sussudio;

public static partial class RuntimePaths
{
    private static string ResolveLogRoot()
    {
        var envOverride = Environment.GetEnvironmentVariable(LogRootEnvVar);
        if (TryEnsureFullPath(envOverride, out var envLogRoot, $"env var '{LogRootEnvVar}' path resolution failed"))
        {
            return envLogRoot;
        }

        // Prefer repo-local logs when we can identify a repo root (development scenario).
        var repoRoot = RepoRoot.Value;
        if (TryEnsureDirectory(Path.Combine(repoRoot, "temp", "logs"), out var repoLogRoot, "repo-local log dir creation failed"))
        {
            return repoLogRoot;
        }

        // Non-repo scenario: keep logs in a stable per-user location.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return EnsureDirectory(Path.Combine(localAppData, "Sussudio", "logs"));
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

        if (TryResolveLatestBuildParent(out var latestBuildParent))
        {
            return latestBuildParent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindRepoRoot(string startPath)
    {
        if (!TryCreateDirectoryInfo(startPath, out var current))
        {
            return null;
        }

        while (current != null)
        {
            if (IsRepoMarkerDirectory(current, out var repoRoot))
            {
                return repoRoot;
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

        if (TryGetFullPath(candidate, out var full, $"candidate path '{candidate}' is malformed"))
        {
            paths.Add(full);
        }
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool TryResolveLatestBuildParent(out string parentPath)
    {
        parentPath = string.Empty;

        if (!TryGetFullPath(AppContext.BaseDirectory, out var baseDir, "latest-build parent resolution failed"))
        {
            return false;
        }

        var baseName = Path.GetFileName(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.Equals(baseName, "latest-build", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parent = Directory.GetParent(baseDir);
        if (parent == null)
        {
            return false;
        }

        parentPath = parent.FullName;
        return true;
    }

    private static bool TryCreateDirectoryInfo(string startPath, out DirectoryInfo? directory)
    {
        try
        {
            directory = new DirectoryInfo(startPath);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback($"path '{startPath}' is invalid or inaccessible", ex);
            directory = null;
            return false;
        }
    }

    private static bool IsRepoMarkerDirectory(DirectoryInfo current, out string repoRoot)
    {
        repoRoot = string.Empty;

        try
        {
            var full = current.FullName;
            if (Directory.Exists(Path.Combine(full, ".git")) ||
                File.Exists(Path.Combine(full, ".git")) ||
                Directory.Exists(Path.Combine(full, ".claude")) ||
                File.Exists(Path.Combine(full, "AGENTS.md")))
            {
                repoRoot = full;
                return true;
            }

            if (File.Exists(Path.Combine(full, "Sussudio.slnx")) ||
                File.Exists(Path.Combine(full, "Sussudio.sln")))
            {
                repoRoot = full;
                return true;
            }

            if (File.Exists(Path.Combine(full, "Sussudio.csproj")))
            {
                repoRoot = current.Parent?.FullName ?? full;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            TraceFallback("directory inaccessible during repo root search", ex);
            return false;
        }
    }

    private static bool TryEnsureFullPath(string? candidate, out string fullPath, string failureContext)
    {
        fullPath = string.Empty;

        if (!TryGetFullPath(candidate, out var normalizedPath, failureContext))
        {
            return false;
        }

        return TryEnsureDirectory(normalizedPath, out fullPath, failureContext);
    }

    private static bool TryEnsureDirectory(string? path, out string ensuredPath, string failureContext)
    {
        ensuredPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            ensuredPath = EnsureDirectory(path);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback(failureContext, ex);
            return false;
        }
    }

    private static bool TryGetFullPath(string? candidate, out string fullPath, string failureContext)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(candidate);
            return true;
        }
        catch (Exception ex)
        {
            TraceFallback(failureContext, ex);
            return false;
        }
    }

    private static void TraceFallback(string context, Exception exception) =>
        Trace.TraceWarning($"RuntimePaths: {context}, falling back: {exception.Message}");
}
