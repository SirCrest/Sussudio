using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;

static partial class Program
{
    private static readonly Dictionary<string, Assembly> ToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Assembly> IsolatedToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, AssemblyLoadContext> IsolatedToolAssemblyContexts = new(StringComparer.OrdinalIgnoreCase);

    private static Assembly LoadToolAssembly(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        if (ToolAssemblyCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
        var assemblyDirectory = Path.GetDirectoryName(fullPath)
                                ?? throw new InvalidOperationException($"Tool assembly directory not found for '{fullPath}'.");

        Assembly? ResolveToolAssemblyDependency(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var dependencyPath = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(dependencyPath)
                ? context.LoadFromAssemblyPath(dependencyPath)
                : null;
        }

        AssemblyLoadContext.Default.Resolving += ResolveToolAssemblyDependency;
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            ToolAssemblyCache[fullPath] = assembly;
            return assembly;
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= ResolveToolAssemblyDependency;
        }
    }

    private static Assembly LoadToolAssemblyIsolated(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        if (IsolatedToolAssemblyCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
        var loadContext = new ToolAssemblyLoadContext(fullPath);
        var assembly = loadContext.LoadFromAssemblyPath(fullPath);
        IsolatedToolAssemblyCache[fullPath] = assembly;
        IsolatedToolAssemblyContexts[fullPath] = loadContext;
        return assembly;
    }

    private sealed class ToolAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ToolAssemblyLoadContext(string mainAssemblyToLoadPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }

    private static void RequireFreshToolAssembly(string relativeAssemblyPath, string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Required tool assembly was not found: {relativeAssemblyPath}. Build it first with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }

        var assemblyWriteTime = File.GetLastWriteTimeUtc(fullPath);
        var newestInputWriteTime = GetNewestToolInputWriteTimeUtc(relativeAssemblyPath);
        if (newestInputWriteTime > assemblyWriteTime)
        {
            throw new InvalidOperationException(
                $"Required tool assembly is stale: {relativeAssemblyPath}. Build it again with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }
    }

    private static DateTime GetNewestToolInputWriteTimeUtc(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var projectDirectory = GetToolProjectDirectory(relativeAssemblyPath);
        var inputDirectories = EnumerateToolInputDirectories(projectDirectory)
            .Concat(UsesCommonToolSources(projectDirectory)
                ? EnumerateToolInputDirectories(Path.Combine(root, "tools", "Common"))
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inputFiles = inputDirectories
            .SelectMany(Directory.EnumerateFiles)
            .Concat(EnumerateToolProjectCompileIncludes(projectDirectory))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Sussudio.Automation.Contracts"), "*.cs"))
            .Where(file => File.Exists(file) && IsToolInputFile(file))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var newest = DateTime.MinValue;
        foreach (var file in inputFiles)
        {
            var writeTime = File.GetLastWriteTimeUtc(file);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        foreach (var directory in inputDirectories)
        {
            var writeTime = Directory.GetLastWriteTimeUtc(directory);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        return newest;
    }

    private static IEnumerable<string> EnumerateToolProjectCompileIncludes(string projectDirectory)
    {
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            XDocument project;
            try
            {
                project = XDocument.Load(projectFile);
            }
            catch
            {
                continue;
            }

            var projectFileDirectory = Path.GetDirectoryName(projectFile)
                                       ?? throw new InvalidOperationException($"Project directory not found for '{projectFile}'.");
            foreach (var include in project.Descendants()
                         .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                         .Select(element => element.Attribute("Include")?.Value)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var expanded = include!.Replace('\\', Path.DirectorySeparatorChar);
                if (expanded.Contains('*'))
                {
                    continue;
                }

                yield return Path.GetFullPath(Path.Combine(projectFileDirectory, expanded));
            }
        }
    }

    private static bool UsesCommonToolSources(string projectDirectory)
    {
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            XDocument project;
            try
            {
                project = XDocument.Load(projectFile);
            }
            catch
            {
                continue;
            }

            foreach (var include in project.Descendants()
                         .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                         .Select(element => element.Attribute("Include")?.Value)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var normalized = include!.Replace('\\', '/');
                if (normalized.Contains("../Common/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetToolProjectDirectory(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "ssctl");
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "McpServer");
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "AutomationClient");
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "NativeXuAudioProbe");
        }

        throw new InvalidOperationException($"No tool project mapping is configured for '{relativeAssemblyPath}'.");
    }

    private static string GetToolBuildCommand(string relativeAssemblyPath)
    {
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/ssctl/ssctl.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/McpServer/McpServer.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug --no-restore";
        }

        return "dotnet build";
    }

    private static bool IsToolInputFile(string file)
    {
        var extension = Path.GetExtension(file);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateToolInputDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        yield return directory;
        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(childDirectory);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var nestedDirectory in EnumerateToolInputDirectories(childDirectory))
            {
                yield return nestedDirectory;
            }
        }
    }
}
