using System.Diagnostics;
using System.Reflection;
using Sussudio.Services.Runtime;
using Xunit;

namespace Sussudio.Tests;

public sealed class LoggerLifecycleTests
{
    private const string ChildFlag = "--logger-lifecycle-test-child";
    private const string LogName = "Sussudio_Debug.log";

    [Fact]
    public void GettersEarlyDiagnosticsAndPrematureShutdownLeaveSharedFilesUntouched()
    {
        using var files = new OwnedFiles();

        RunChild(files, "early");

        files.AssertSharedUntouched();
        Assert.False(Directory.Exists(files.PrivateRoot));
    }

    [Fact]
    public void AdmittedInitializationRotatesOnceAndDrainsBeforeFatalDirectWrite()
    {
        using var files = new OwnedFiles();
        Directory.CreateDirectory(files.PrivateRoot);
        File.WriteAllText(Path.Combine(files.PrivateRoot, LogName), "prior-private-session");

        RunChild(files, "admitted");

        files.AssertSharedUntouched();
        var rotated = Assert.Single(Directory.GetFiles(files.PrivateRoot, "Sussudio_Debug_*.log"));
        Assert.Equal("prior-private-session", File.ReadAllText(rotated));
        var current = File.ReadAllText(Path.Combine(files.PrivateRoot, LogName));
        Assert.Equal(1, current.Split("=== Sussudio Debug Log ===").Length - 1);
        Assert.Equal(300, current.Split('\n').Count(line => line.Contains("[lifecycle] entry-", StringComparison.Ordinal)));
        Assert.True(current.IndexOf("entry-0\n", StringComparison.Ordinal) < current.IndexOf("entry-299\n", StringComparison.Ordinal));
        Assert.Contains("fatal-after-drain", current);
        Assert.False(Directory.Exists(Path.Combine(files.Root, "retarget")));
    }

    [Fact]
    public void RejectedAdmissionCannotInitializeEvenAfterEarlyLoggerUse()
    {
        using var files = new OwnedFiles();
        using var owner = new Mutex(false, files.MutexName);
        Assert.True(owner.WaitOne(TimeSpan.Zero));
        try
        {
            RunChild(files, "duplicate");
        }
        finally
        {
            owner.ReleaseMutex();
        }

        files.AssertSharedUntouched();
        Assert.False(Directory.Exists(files.PrivateRoot));
    }

    [Fact]
    public void PrivateProbeSelectsItsExplicitRootDespiteEarlierLoggerAndRuntimePathGetters()
    {
        using var files = new OwnedFiles();

        RunChild(files, "probe");

        files.AssertSharedUntouched();
        var log = File.ReadAllText(Path.Combine(files.PrivateRoot, LogName));
        Assert.Contains("fatal-after-probe-drain", log);
        Assert.Empty(Directory.GetFiles(files.PrivateRoot, "Sussudio_Debug_*.log"));
        Assert.False(Directory.Exists(Path.Combine(files.Root, "retarget")));
    }

    [Theory]
    [InlineData("probe-malformed")]
    [InlineData("probe-existing")]
    [InlineData("probe-blocked")]
    public void RejectedPrivateProbeCannotStartLogging(string scenario)
    {
        using var files = new OwnedFiles();
        if (scenario == "probe-existing") Directory.CreateDirectory(files.PrivateRoot);
        if (scenario == "probe-blocked") File.WriteAllText(files.PrivateRoot, "private-root-blocker");

        RunChild(files, scenario);

        files.AssertSharedUntouched();
        if (scenario == "probe-existing") Assert.Empty(Directory.GetFileSystemEntries(files.PrivateRoot));
        if (scenario == "probe-blocked") Assert.Equal("private-root-blocker", File.ReadAllText(files.PrivateRoot));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnavailableRootUsesTraceWithoutRetargetingOrBreakingShutdown(bool brokenTraceListener)
    {
        using var files = new OwnedFiles();
        File.WriteAllText(files.PrivateRoot, "root-blocker");

        RunChild(files, brokenTraceListener ? "failure-broken-trace" : "failure");

        files.AssertSharedUntouched();
        Assert.Equal("root-blocker", File.ReadAllText(files.PrivateRoot));
        Assert.False(Directory.Exists(Path.Combine(files.Root, "retarget")));
    }

    [Fact]
    public void TemporaryFileFailureKeepsItsSelectedPathForLaterWrites()
    {
        using var files = new OwnedFiles();
        Directory.CreateDirectory(Path.Combine(files.PrivateRoot, LogName));

        RunChild(files, "recover-file");

        files.AssertSharedUntouched();
        Assert.Contains("write-after-recovery", File.ReadAllText(Path.Combine(files.PrivateRoot, LogName)));
    }

    [Fact]
    public void FailedRootSelectionCanInitializeTraceWithoutUsingTheCurrentDirectory()
    {
        using var files = new OwnedFiles();

        RunChild(files, "failure-empty");

        files.AssertSharedUntouched();
        Assert.False(File.Exists(Path.Combine(files.Root, LogName)));
        Assert.False(Directory.Exists(files.PrivateRoot));
    }

    internal static bool TryRunChildProcess(string[] args, out int exitCode)
    {
        exitCode = 2;
        if (args.Length == 0 || args[0] != ChildFlag) return false;
        if (args.Length != 5) return true;
        try
        {
            RunChildScenario(args[1], args[2], args[3], args[4]);
            exitCode = 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            exitCode = 1;
        }
        return true;
    }

    private static void RunChildScenario(string scenario, string root, string mutexName, string appAssemblyPath)
    {
        var assembly = Assembly.LoadFrom(appAssemblyPath);
        var logger = assembly.GetType("Sussudio.Logger", throwOnError: true)!;
        var privateRoot = Path.Combine(root, "private");
        using var traceText = new StringWriter();
        using var listener = new TextWriterTraceListener(traceText);
        Trace.Listeners.Add(listener);
        try
        {
            Assert.Equal("NotInitialized", State());
            Assert.Equal(string.Empty, Call("GetLogFilePath"));
            Call("Log", "early-entry", "lifecycle");
            Call("LogFatalBreadcrumb", "early-fatal", null);
            Call("LogException", new IOException("early-exception"), "lifecycle");
            Call("LogStructured", "early-structured", new { Value = 3 }, "lifecycle");
            Call("LogSystemInfo");
            Assert.Equal(0, logger.GetField("_systemInfoLogged", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null));
            Shutdown();
            Assert.Equal("NotInitialized", State());
            Assert.Equal(string.Empty, Call("GetLogFilePath"));
            Assert.Contains("early-entry", traceText.ToString());
            Assert.Contains("early-fatal", traceText.ToString());

            if (scenario == "early") return;
            if (scenario is "admitted" or "duplicate")
            {
                var admitted = false;
                var result = AppProcessStartup.RunNormal(() =>
                {
                    admitted = true;
                    Parallel.For(0, 16, _ => Call("Initialize", privateRoot));
                    Call("Initialize", Path.Combine(root, "retarget"));
                    Assert.Equal(Path.Combine(privateRoot, LogName), Call("GetLogFilePath"));
                    Assert.Equal("Healthy", State());
                    for (var i = 0; i < 300; i++) Call("Log", $"entry-{i}", "lifecycle");
                    Shutdown();
                    Call("Initialize", privateRoot);
                    Call("LogFatalBreadcrumb", "fatal-after-drain", null);
                }, mutexName);
                Assert.Equal(0, result);
                Assert.Equal(scenario == "admitted", admitted);
                if (!admitted) Assert.Equal("NotInitialized", State());
                return;
            }

            if (scenario.StartsWith("probe", StringComparison.Ordinal))
            {
                var paths = assembly.GetType("Sussudio.RuntimePaths", throwOnError: true)!;
                Assert.Equal(Path.Combine(root, "shared"), paths.GetMethod("GetRepoLogRoot", BindingFlags.Static | BindingFlags.Public)!
                    .Invoke(null, null));
                var probe = assembly.GetType("Sussudio.Services.Runtime.NativeFfmpegCapabilityProbe", throwOnError: true)!;
                var probeRoot = scenario == "probe-blocked" ? Path.Combine(privateRoot, "child") : privateRoot;
                var probeArgs = scenario == "probe-malformed"
                    ? new[] { "--native-ffmpeg-split-probe" }
                    : new[] { "--native-ffmpeg-split-probe", "--protocol-version", "1", "--runtime-root",
                        Path.Combine(root, "missing-native-runtime"), "--mode", "2", "--log-root", probeRoot };
                var invokeArgs = new object?[] { probeArgs, null };
                Assert.True((bool)probe.GetMethod("TryRunChildProcess", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, invokeArgs)!);
                if (scenario == "probe")
                {
                    // Missing native files fail before native binding selection or allocation.
                    Assert.Equal(1, invokeArgs[1]);
                    Assert.Equal("Healthy", State());
                    Assert.Equal(Path.Combine(privateRoot, LogName), Call("GetLogFilePath"));
                    Call("Initialize", Path.Combine(root, "retarget"));
                    Call("LogFatalBreadcrumb", "fatal-after-probe-drain", null);
                }
                else
                {
                    Assert.Equal(2, invokeArgs[1]);
                    Assert.Equal("NotInitialized", State());
                    Assert.Equal(string.Empty, Call("GetLogFilePath"));
                }
                return;
            }

            if (scenario.StartsWith("failure", StringComparison.Ordinal))
            {
                using var broken = new ThrowingTraceListener();
                if (scenario == "failure-broken-trace") Trace.Listeners.Insert(0, broken);
                try
                {
                    var initializationRoot = scenario == "failure-empty" ? string.Empty : Path.Combine(privateRoot, "child");
                    Assert.Equal(0, AppProcessStartup.RunNormal(() => Call("Initialize", initializationRoot), mutexName));
                    Assert.Equal("FileIoFailed", State());
                    Assert.Equal(string.Empty, Call("GetLogFilePath"));
                    Call("Initialize", Path.Combine(root, "retarget"));
                    Call("Log", "fallback-entry", "lifecycle");
                    Call("LogFatalBreadcrumb", "fallback-fatal", null);
                    Shutdown();
                    if (scenario != "failure-broken-trace") Assert.Contains("fallback-entry", traceText.ToString());
                }
                finally { Trace.Listeners.Remove(broken); }
                return;
            }

            Assert.Equal("recover-file", scenario);
            Call("Initialize", privateRoot);
            Assert.Equal("FileIoFailed", State());
            Assert.Equal(Path.Combine(privateRoot, LogName), Call("GetLogFilePath"));
            Directory.Delete(Path.Combine(privateRoot, LogName));
            Call("Log", "write-after-recovery", "lifecycle");
            Shutdown();
            Assert.Equal("FileIoFailed", State());
        }
        finally
        {
            Shutdown();
            Trace.Listeners.Remove(listener);
        }

        object? Call(string name, params object?[] values)
            => logger.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, values);
        string State() => logger.GetProperty("InitState")!.GetValue(null)!.ToString()!;
        void Shutdown() => ((Task)Call("ShutdownAsync", TimeSpan.FromSeconds(5))!).GetAwaiter().GetResult();
    }

    private static void RunChild(OwnedFiles files, string scenario)
    {
        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            WorkingDirectory = files.Root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "exec", typeof(LoggerLifecycleTests).Assembly.Location, ChildFlag, scenario,
            files.Root, files.MutexName, SussudioAssembly.Load().Location }) start.ArgumentList.Add(argument);
        start.Environment["SUSSUDIO_LOG_ROOT"] = files.SharedRoot;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Logger child did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.True(process.WaitForExit(5000), "Logger child termination was not confirmed.");
            throw new TimeoutException("Logger test child exceeded its deadline.");
        }
        Assert.True(process.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    private sealed class ThrowingTraceListener : TraceListener
    {
        public override void Write(string? message) => throw new IOException("listener unavailable");
        public override void WriteLine(string? message) => throw new IOException("listener unavailable");
    }

    private sealed class OwnedFiles : IDisposable
    {
        internal OwnedFiles()
        {
            Directory.CreateDirectory(SharedRoot);
            File.WriteAllText(Path.Combine(SharedRoot, LogName), "active-session-sentinel");
        }
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), $"sussudio-logger-lifecycle-{Guid.NewGuid():N}");
        internal string SharedRoot => Path.Combine(Root, "shared");
        internal string PrivateRoot => Path.Combine(Root, "private");
        internal string MutexName { get; } = @"Local\Sussudio.Logger.Tests." + Guid.NewGuid().ToString("N");
        internal void AssertSharedUntouched()
        {
            var path = Path.Combine(SharedRoot, LogName);
            Assert.Equal("active-session-sentinel", File.ReadAllText(path));
            Assert.Equal(new[] { path }, Directory.GetFileSystemEntries(SharedRoot));
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
