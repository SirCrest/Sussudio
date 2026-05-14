using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot()
    {
        var runtimePathsType = RequireType("Sussudio.RuntimePaths");
        var getRepoLogFile = runtimePathsType.GetMethod(
            "GetRepoLogFile",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        if (getRepoLogFile == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogFile not found.");

        var logPath = (string)getRepoLogFile.Invoke(null, new object[] { "test.log" })!;
        AssertContains(logPath, "test.log");

        // The log path should be a rooted path
        if (!Path.IsPathRooted(logPath))
            throw new InvalidOperationException(
                $"GetRepoLogFile returned non-rooted path: {logPath}");

        return Task.CompletedTask;
    }

    private static Task RuntimePaths_PathsContainExpectedDirectoryNames()
    {
        var runtimePathsType = RequireType("Sussudio.RuntimePaths");

        var getRepoLogRoot = runtimePathsType.GetMethod(
            "GetRepoLogRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoLogRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogRoot not found.");
        var logRoot = (string)getRepoLogRoot.Invoke(null, null)!;
        AssertContains(logRoot, "logs");

        var getRepoTempRoot = runtimePathsType.GetMethod(
            "GetRepoTempRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoTempRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoTempRoot not found.");
        var tempRoot = (string)getRepoTempRoot.Invoke(null, null)!;
        AssertContains(tempRoot, "temp");

        return Task.CompletedTask;
    }

    private static Task MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint()
    {
        var source = ReadRepoFile("Sussudio/Services/Runtime/MmcssThreadRegistration.cs");
        AssertContains(source, "EntryPoint = \"AvSetMmThreadCharacteristicsW\"");
        AssertContains(source, "MMCSS registered task=");

        return Task.CompletedTask;
    }

    private static Task ProcessSpec_DefaultTimeout_Is30Seconds()
    {
        var specType = RequireType("Sussudio.Services.Runtime.ProcessSpec");
        var spec = RuntimeHelpers.GetUninitializedObject(specType);
        // ProcessSpec uses init-only with defaults; GetUninitializedObject bypasses ctor.
        // So test the contract by checking the source.
        var sourceText = ReadRepoFile("Sussudio/Services/Runtime/ProcessSupervisor.cs");
        AssertContains(sourceText, "public int TimeoutMs { get; init; } = 30_000;");
        AssertContains(sourceText, "public string Arguments { get; init; } = string.Empty;");
        AssertContains(sourceText, "public ProcessPriorityClass? PriorityClass { get; init; }");
        AssertContains(sourceText, "process.PriorityClass = priorityClass;");

        // ProcessRunResult contract
        AssertContains(sourceText, "public bool Started { get; init; }");
        AssertContains(sourceText, "public bool TimedOut { get; init; }");
        AssertContains(sourceText, "public string StdOut { get; init; } = string.Empty;");
        AssertContains(sourceText, "public string StdErr { get; init; } = string.Empty;");

        return Task.CompletedTask;
    }
}
