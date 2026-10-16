using System.Diagnostics;
using System.Reflection;
using Sussudio.Services.Runtime;
using Xunit;

namespace Sussudio.Tests;

public sealed class AppProcessStartupTests
{
    private const string AdmissionChildArgument = "--startup-admission-test-child";
    private const string AbandonChildArgument = "--startup-admission-test-abandon";
    private const string AdmittedMarker = "STARTUP_TEST_ADMITTED";

    [Fact]
    public void CompiledApphostRejectsInvalidPrivateArgumentsBeforeApplicationStartup()
    {
        using var directory = new TestDirectory();
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.ChangeExtension(SussudioAssembly.Load().Location, ".exe"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--native-ffmpeg-split-probe");
        startInfo.Environment["PATH"] = string.Empty;
        startInfo.Environment["SUSSUDIO_LOG_ROOT"] = Path.Combine(directory.Path, "unexpected-app-log");

        var result = RunOwnedChild(startInfo);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Invalid native FFmpeg probe arguments.", result.Output);
        Assert.Empty(Directory.GetFileSystemEntries(directory.Path));
    }

    [Fact]
    public void DuplicateProcessPreservesLogAndAdmissionSucceedsAfterRelease()
    {
        using var directory = new TestDirectory();
        var logPath = Path.Combine(directory.Path, "Sussudio_Debug.log");
        File.WriteAllText(logPath, "active-session-sentinel");
        var originalBytes = File.ReadAllBytes(logPath);
        var mutexName = UniqueMutexName();
        var appAssemblyPath = SussudioAssembly.Load().Location;

        using (var owner = new Mutex(initiallyOwned: false, name: mutexName))
        {
            Assert.True(owner.WaitOne(TimeSpan.Zero));
            try
            {
                var duplicate = RunChild(AdmissionChildArgument, mutexName, directory.Path, appAssemblyPath);
                Assert.True(duplicate.ExitCode == 0, duplicate.Output);
                Assert.DoesNotContain(AdmittedMarker, duplicate.Output);
                Assert.Equal(originalBytes, File.ReadAllBytes(logPath));
                Assert.Equal(new[] { logPath }, Directory.GetFiles(directory.Path));
            }
            finally
            {
                owner.ReleaseMutex();
            }
        }

        var admitted = RunChild(AdmissionChildArgument, mutexName, directory.Path, appAssemblyPath);
        Assert.True(admitted.ExitCode == 0, admitted.Output);
        Assert.Contains(AdmittedMarker, admitted.Output);
        Assert.Contains(AdmittedMarker, File.ReadAllText(logPath));
        var rotated = Assert.Single(Directory.GetFiles(directory.Path, "Sussudio_Debug_*.log"));
        Assert.Equal(originalBytes, File.ReadAllBytes(rotated));
    }

    [Fact]
    public void ApplicationExceptionPropagatesAndReleasesAdmission()
    {
        var mutexName = UniqueMutexName();
        using var retainedHandle = new Mutex(initiallyOwned: false, name: mutexName);
        var expected = new InvalidOperationException("application-construction-failed");

        var actual = Assert.Throws<InvalidOperationException>(
            () => AppProcessStartup.RunAsSingleInstance(() => throw expected, mutexName));

        Assert.Same(expected, actual);
        Assert.True(CanAcquireOnAnotherThread(mutexName));
    }

    [Fact]
    public void NormalReturnReleasesAdmission()
    {
        var mutexName = UniqueMutexName();
        using var retainedHandle = new Mutex(initiallyOwned: false, name: mutexName);
        var entered = false;

        Assert.Equal(0, AppProcessStartup.RunAsSingleInstance(() => entered = true, mutexName));

        Assert.True(entered);
        Assert.True(CanAcquireOnAnotherThread(mutexName));
    }

    [Fact]
    public void ExistingDifferentKernelObjectRejectsBeforeApplicationWork()
    {
        var mutexName = UniqueMutexName();
        using var conflictingEvent = new EventWaitHandle(false, EventResetMode.ManualReset, mutexName);
        var entered = false;

        var exitCode = AppProcessStartup.RunAsSingleInstance(() => entered = true, mutexName);

        Assert.Equal(1, exitCode);
        Assert.False(entered);
    }

    [Fact]
    public void AbandonedOwnerAllowsNextApplicationToStart()
    {
        var mutexName = UniqueMutexName();
        // Keep the named object alive after the child exits with ownership.
        using var retainedHandle = new Mutex(initiallyOwned: false, name: mutexName);
        var abandoned = RunChild(AbandonChildArgument, mutexName);
        Assert.True(abandoned.ExitCode == 0, abandoned.Output);
        Assert.Contains("STARTUP_TEST_ABANDONING", abandoned.Output);
        var entered = false;

        Assert.Equal(0, AppProcessStartup.RunAsSingleInstance(() => entered = true, mutexName));

        Assert.True(entered);
        Assert.True(CanAcquireOnAnotherThread(mutexName));
    }

    // HarnessCore dispatches these test-only child modes before its smoke check.
    internal static bool TryRunChildProcess(string[] args, out int exitCode)
    {
        exitCode = 2;
        if (args.Length == 0 ||
            (args[0] != AdmissionChildArgument && args[0] != AbandonChildArgument))
        {
            return false;
        }

        if (args[0] == AbandonChildArgument)
        {
            if (args.Length != 2)
            {
                return true;
            }

            var ownedMutex = new Mutex(initiallyOwned: false, name: args[1]);
            if (!ownedMutex.WaitOne(TimeSpan.Zero))
            {
                ownedMutex.Dispose();
                exitCode = 3;
                return true;
            }

            Console.WriteLine("STARTUP_TEST_ABANDONING");
            GC.KeepAlive(ownedMutex);
            Environment.Exit(0);
            return true;
        }

        if (args.Length != 4)
        {
            return true;
        }

        Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", args[2]);
        exitCode = AppProcessStartup.RunAsSingleInstance(() =>
        {
            var assembly = Assembly.LoadFrom(args[3]);
            var logger = assembly.GetType("Sussudio.Logger", throwOnError: true)!;
            logger.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { args[2] });
            logger.GetMethod("Log", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, new object[] { AdmittedMarker, "startup-test" });
            var shutdown = (Task)logger.GetMethod("ShutdownAsync", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, new object[] { TimeSpan.FromSeconds(5) })!;
            shutdown.GetAwaiter().GetResult();
            Console.WriteLine(AdmittedMarker);
        }, args[1]);
        return true;
    }

    private static bool CanAcquireOnAnotherThread(string mutexName)
        => Task.Run(() =>
        {
            using var mutex = new Mutex(initiallyOwned: false, name: mutexName);
            var acquired = mutex.WaitOne(TimeSpan.Zero);
            if (acquired)
            {
                mutex.ReleaseMutex();
            }

            return acquired;
        }).GetAwaiter().GetResult();

    private static string UniqueMutexName()
        => @"Local\Sussudio.Startup.Tests." + Guid.NewGuid().ToString("N");

    private static ChildResult RunChild(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(typeof(AppProcessStartupTests).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (arguments[0] == AdmissionChildArgument)
        {
            startInfo.Environment["SUSSUDIO_LOG_ROOT"] = arguments[2];
        }

        return RunOwnedChild(startInfo);
    }

    private static ChildResult RunOwnedChild(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Startup test child did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.True(process.WaitForExit(5_000), "Startup test child termination was not confirmed.");
            throw new TimeoutException("Startup test child exceeded its deadline.");
        }

        return new ChildResult(process.ExitCode, output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
    }

    private sealed record ChildResult(int ExitCode, string Output);

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "Sussudio-startup-tests", Guid.NewGuid().ToString("N"));

        public TestDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
