using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// xUnit slice for no-hardware runtime contracts ported from the legacy runner.
public sealed class RuntimeContractsTests
{
    [Fact]
    public void RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot()
    {
        var runtimePathsType = SussudioAssembly.Load().GetType("Sussudio.RuntimePaths", throwOnError: true)!;
        var getRepoLogFile = runtimePathsType.GetMethod(
            "GetRepoLogFile",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        Assert.NotNull(getRepoLogFile);

        var logPath = Assert.IsType<string>(getRepoLogFile!.Invoke(null, new object[] { "test.log" }));

        Assert.Contains("test.log", logPath);
        Assert.True(Path.IsPathRooted(logPath), $"GetRepoLogFile returned non-rooted path: {logPath}");
    }

    [Fact]
    public void RuntimePaths_PathsContainExpectedDirectoryNames()
    {
        var runtimePathsType = SussudioAssembly.Load().GetType("Sussudio.RuntimePaths", throwOnError: true)!;

        var getRepoLogRoot = runtimePathsType.GetMethod("GetRepoLogRoot", BindingFlags.Public | BindingFlags.Static);
        var getRepoTempRoot = runtimePathsType.GetMethod("GetRepoTempRoot", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(getRepoLogRoot);
        Assert.NotNull(getRepoTempRoot);

        var logRoot = Assert.IsType<string>(getRepoLogRoot!.Invoke(null, null));
        var tempRoot = Assert.IsType<string>(getRepoTempRoot!.Invoke(null, null));

        Assert.Contains("logs", logRoot);
        Assert.Contains("temp", tempRoot);
    }

    [Fact]
    public void RuntimePaths_OwnsPublicApiAndResolutionPolicy()
    {
        var rootText = RuntimeContractSource.ReadRepoFile("Sussudio/RuntimePaths.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("public static class RuntimePaths", rootText);
        Assert.DoesNotContain("partial class RuntimePaths", rootText);
        Assert.Contains("public static string GetRepoRoot() => RepoRoot.Value;", rootText);
        Assert.Contains("public static string GetRepoLogFile(string fileName)", rootText);
        Assert.Contains("private static string ResolveRepoRoot()", rootText);
        Assert.Contains("private static string ResolveLogRoot()", rootText);
        Assert.Contains("private static bool TryResolveLatestBuildParent(", rootText);
        Assert.Contains("private static bool IsRepoMarkerDirectory(", rootText);
        Assert.Contains("private static bool TryEnsureDirectory(", rootText);
        Assert.Contains("RuntimePaths: {context}, falling back:", rootText);
    }

    [Fact]
    public void MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint()
    {
        var source = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/MmcssThreadRegistration.cs");

        Assert.Contains("EntryPoint = \"AvSetMmThreadCharacteristicsW\"", source);
        Assert.Contains("MMCSS registered task=", source);
    }

    [Fact]
    public void ProcessSpec_DefaultTimeout_Is30Seconds()
    {
        var asm = SussudioAssembly.Load();
        var specType = asm.GetType("Sussudio.Services.Runtime.ProcessSpec", throwOnError: true)!;
        var resultType = asm.GetType("Sussudio.Services.Runtime.ProcessRunResult", throwOnError: true)!;

        var spec = Activator.CreateInstance(specType)!;

        Assert.Equal(30_000, specType.GetProperty("TimeoutMs")!.GetValue(spec));
        Assert.Equal(string.Empty, specType.GetProperty("Arguments")!.GetValue(spec));
        Assert.Equal(typeof(ProcessPriorityClass?), specType.GetProperty("PriorityClass")!.PropertyType);

        Assert.NotNull(resultType.GetProperty("Started"));
        Assert.NotNull(resultType.GetProperty("TimedOut"));
        Assert.Equal(string.Empty, resultType.GetProperty("StdOut")!.GetValue(Activator.CreateInstance(resultType)!));
        Assert.Equal(string.Empty, resultType.GetProperty("StdErr")!.GetValue(Activator.CreateInstance(resultType)!));

        var sourceText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/ProcessSupervisor.cs");
        Assert.Contains("process.PriorityClass = priorityClass;", sourceText);
    }

    [Fact]
    public void ExternalProcessProbes_UseBoundedProcessSupervisor()
    {
        var ffmpegText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        var hdrText = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Recording/HdrValidationRunner.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("internal static class FfmpegRuntimeLocator", ffmpegText);
        Assert.DoesNotContain("partial class FfmpegRuntimeLocator", ffmpegText);
        Assert.Contains("internal static bool TryResolveNativeRuntimeRoot", ffmpegText);
        Assert.Contains("internal static string FindToolPath", ffmpegText);
        Assert.Contains("private const int ProbeTimeoutMs = 10_000;", ffmpegText);
        Assert.Contains("new ProcessSupervisor().RunAsync", ffmpegText);
        Assert.Contains("TimeoutMs = ProbeTimeoutMs", ffmpegText);
        Assert.Contains("if (!result.Started || result.TimedOut || result.ExitCode != 0)", ffmpegText);
        Assert.Contains("return result.Started && !result.TimedOut && result.ExitCode == 0;", ffmpegText);
        Assert.Contains("private const int ValidationTimeoutMs = 30_000;", hdrText);
        Assert.Contains("new ProcessSupervisor().RunAsync", hdrText);
        Assert.Contains("validator-timeout", hdrText);
    }

    [Fact]
    public void FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ec-ffmpeg-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var localFfmpegDir = Path.Combine(tempRoot, "ffmpeg");
        Directory.CreateDirectory(localFfmpegDir);

        try
        {
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avcodec-62.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avutil-60.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffmpeg.exe"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffprobe.exe"), Array.Empty<byte>());

            var locatorType = SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.FfmpegRuntimeLocator", throwOnError: true)!;
            var resolveRuntime = locatorType.GetMethod(
                "TryResolveNativeRuntimeRoot",
                ReflectionFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string).MakeByRefType() },
                modifiers: null);
            Assert.NotNull(resolveRuntime);

            var runtimeArgs = new object?[] { tempRoot, null };
            var resolved = Assert.IsType<bool>(resolveRuntime!.Invoke(null, runtimeArgs));

            Assert.True(resolved);
            Assert.Equal(localFfmpegDir, runtimeArgs[1]?.ToString());

            var findToolPath = locatorType.GetMethod(
                "FindToolPath",
                ReflectionFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null);
            Assert.NotNull(findToolPath);

            var ffmpegPath = findToolPath!.Invoke(null, new object?[] { "ffmpeg.exe", tempRoot })?.ToString();
            var ffprobePath = findToolPath.Invoke(null, new object?[] { "ffprobe.exe", tempRoot })?.ToString();

            Assert.Equal(Path.Combine(localFfmpegDir, "ffmpeg.exe"), ffmpegPath);
            Assert.Equal(Path.Combine(localFfmpegDir, "ffprobe.exe"), ffprobePath);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

internal static class RuntimeContractSource
{
    public static string ReadRepoFile(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    public static string ReadAutomationPipeClientSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs",
            "tools/Common/AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs",
            "tools/Common/AutomationPipeClient/AutomationPipeClient.Commands.cs",
            "tools/Common/AutomationPipeClient/AutomationCommandTransport.cs"
        });

    public static string ReadAutomationSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationSnapshotFormatter.cs",
            "tools/Common/AutomationSnapshotFormatter.CaptureSettings.cs",
            "tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs",
            "tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs",
            "tools/Common/AutomationSnapshotFormatter.AvSync.cs",
            "tools/Common/AutomationSnapshotFormatter.Source.cs",
            "tools/Common/AutomationSnapshotFormatter.Values.cs",
            "tools/Common/AutomationSnapshotFormatter.DisplayValues.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.cs",
            "tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs",
            "tools/Common/AutomationSnapshotFormatter.Preview.cs",
            "tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs",
            "tools/Common/AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs",
            "tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs"
        });

    public static string ReadSsctlSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
            "tools/ssctl/Formatters.Snapshot.CoreSections.cs",
            "tools/ssctl/Formatters.Snapshot.Audio.cs",
            "tools/ssctl/Formatters.Snapshot.Recording.cs",
            "tools/ssctl/Formatters.Snapshot.ProcessResources.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureSettings.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureCadence.cs",
            "tools/ssctl/Formatters.Snapshot.AvSync.cs",
            "tools/ssctl/Formatters.Snapshot.Source.cs",
            "tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.cs",
            "tools/ssctl/Formatters.Snapshot.Mjpeg.cs",
            "tools/ssctl/Formatters.Snapshot.Preview.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.cs",
            "tools/ssctl/Formatters.Snapshot.Runtime.cs",
            "tools/ssctl/Formatters.Snapshot.ThreadHealth.cs",
        });

    public static string ReadSourceFamily(IReadOnlyList<string> files)
    {
        var parts = new string[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        return string.Join("\n", parts);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
