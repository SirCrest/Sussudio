using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// Representative xUnit slice ported from the legacy Program.cs runner.
//
// The test project targets net8.0 while Sussudio targets
// net8.0-windows10.0.19041.0, so a ProjectReference would force a Windows
// target onto the test rig and pull WinUI deps into discovery. xUnit tests
// therefore reach the assembly the same way the legacy runner does:
// Assembly.LoadFrom against the staged Sussudio.dll. The
// [assembly: InternalsVisibleTo("Sussudio.Tests")] attributes on Sussudio
// and ssctl mean reflection no longer needs to crack open private members,
// just resolve the type via its public/internal name.
//
// SussudioAssembly.Path is set by the test entry point (Program.Main today;
// later: a custom xUnit fixture) before any [Fact] runs.
public class RecordingContractsTests
{
    [Fact]
    public void GpuPipelineHandles_None_IsZeroed()
    {
        var asm = SussudioAssembly.Load();
        var handlesType = asm.GetType("Sussudio.Services.Contracts.GpuPipelineHandles", throwOnError: true)!;

        var none = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DevicePtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DeviceContextPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwDeviceCtxPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwFramesCtxPtr")!.GetValue(none)!);
    }

    [Fact]
    public void FinalizeResult_Success_HasEmptyPreservedArtifacts()
    {
        var asm = SussudioAssembly.Load();
        var resultType = asm.GetType("Sussudio.Services.Contracts.FinalizeResult", throwOnError: true)!;

        var success = resultType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { "/tmp/out.mp4", "Stopped" })!;

        Assert.True((bool)resultType.GetProperty("Succeeded")!.GetValue(success)!);
        Assert.Equal("/tmp/out.mp4", (string)resultType.GetProperty("OutputPath")!.GetValue(success)!);
        Assert.Equal("Stopped", (string)resultType.GetProperty("StatusMessage")!.GetValue(success)!);

        var artifacts = (System.Collections.IEnumerable)resultType.GetProperty("PreservedArtifacts")!.GetValue(success)!;
        Assert.Empty(artifacts.Cast<object>());
    }
}

// Resolves the staged Sussudio.dll the same way the legacy runner does.
// Lives next to the xUnit slice rather than in Program.cs so a future cleanup
// can lift this into an IClassFixture without touching the legacy runner.
internal static class SussudioAssembly
{
    private static Assembly? _cached;

    public static Assembly Load()
    {
        if (_cached != null) return _cached;
        var path = System.Environment.GetEnvironmentVariable("SUSSUDIO_TEST_ASSEMBLY")
            ?? "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll";
        if (!System.IO.File.Exists(path))
        {
            var repoRoot = FindRepoRoot();
            var rooted = System.IO.Path.Combine(repoRoot, path.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(rooted)) path = rooted;
        }
        _cached = Assembly.LoadFrom(path);
        return _cached;
    }

    private static string FindRepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);
        while (dir != null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? System.Environment.CurrentDirectory;
    }
}

internal static class LinqShim
{
    public static System.Collections.Generic.IEnumerable<T> Cast<T>(this System.Collections.IEnumerable source)
    {
        foreach (var item in source) yield return (T)item;
    }
}

internal static class ReflectionFlags
{
    public const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    public const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}

internal static class EnvVarScope
{
    public static IDisposable Push(string name, string? value)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return new Restore(name, previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;
        public Restore(string name, string? previous) { _name = name; _previous = previous; }
        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }
}
