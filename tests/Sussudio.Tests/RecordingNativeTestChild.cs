using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

// Child processes own native workers and process-wide recovery paths until exit.
// The parent alone owns deadlines, kill confirmation, and temporary-file cleanup.
internal static class RecordingNativeTestChild
{
    private const string ChildFlag = "--recording-native-test-child";

    internal sealed record Result(int ExitCode, bool TimedOut, string Output, string Errors, string Logs, string Directory)
    {
        internal string FailureDetail => $"Child exit={ExitCode}; timed out={TimedOut}\nstdout:\n{Output}\nstderr:\n{Errors}\nlogs:\n{Logs}";
    }

    internal static async Task VerifyLifecycleAsync(string scenario)
    {
        var result = await RunAsync(scenario, TimeSpan.FromSeconds(45));
        Assert.False(result.TimedOut, result.FailureDetail);
        Assert.True(result.ExitCode == 0 && string.IsNullOrWhiteSpace(result.Errors), result.FailureDetail);
        Assert.Equal("RECORDING_LIFECYCLE_PASSED:" + scenario, result.Output.Trim());
    }

    internal static async Task<Result> RunAsync(string scenario, TimeSpan deadline)
    {
        var assembly = SussudioAssembly.Load();
        var directory = System.IO.Directory.CreateTempSubdirectory("sussudio-recording-child-").FullName;
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        foreach (var argument in new[] { "exec", typeof(RecordingNativeTestChild).Assembly.Location, ChildFlag, assembly.Location, directory, scenario })
            process.StartInfo.ArgumentList.Add(argument);
        process.StartInfo.Environment["SUSSUDIO_LOG_ROOT"] = Path.Combine(directory, "logs");
        process.StartInfo.Environment["SUSSUDIO_RECOVERY_DIRECTORY"] = Path.Combine(directory, "recovery");
        var started = false;
        var exitConfirmed = false;
        try
        {
            started = process.Start();
            if (!started) throw new InvalidOperationException("Recording test child did not start.");
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            var timedOut = false;
            try { await process.WaitForExitAsync().WaitAsync(deadline); }
            catch (TimeoutException) { timedOut = true; }
            if (timedOut) await TerminateAsync();
            exitConfirmed = process.HasExited;
            var logs = string.Join("\n", System.IO.Directory.EnumerateFiles(directory, "*.log", SearchOption.AllDirectories)
                .Select(path => path + "\n" + File.ReadAllText(path)));
            return new(process.ExitCode, timedOut, await output, await errors, logs, directory);
        }
        finally
        {
            // Also covers unexpected parent-side failures after Process.Start.
            if (started && !exitConfirmed) await TerminateAsync();
            if (!started || exitConfirmed) System.IO.Directory.Delete(directory, recursive: true);
        }

        async Task TerminateAsync()
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (process.HasExited) { }
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (TimeoutException error)
            {
                throw new TimeoutException($"Recording test child {process.Id} did not confirm exit; private files retained at {directory}.", error);
            }
            exitConfirmed = process.HasExited;
        }
    }

    internal static bool TryRunChildProcess(string[] args, out int exitCode)
    {
        exitCode = 2;
        if (args.Length == 0 || args[0] != ChildFlag) return false;
        if (args.Length != 4 || args[3] is not ("capability" or "p010" or "mismatch" or "hold")) return true;
        // Set once for the entire process, before any app type or native worker starts.
        Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", Path.Combine(args[2], "logs"));
        Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", Path.Combine(args[2], "recovery"));
        if (args[3] == "hold")
        {
            Console.WriteLine("RECORDING_CHILD_HOLDING_PRIVATE_ENVIRONMENT");
            Console.Out.Flush();
            Thread.Sleep(Timeout.Infinite);
            return true;
        }
        Assembly? assembly = null;
        try
        {
            var resolver = new AssemblyDependencyResolver(args[1]);
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                var path = resolver.ResolveAssemblyToPath(name);
                return path == null ? null : context.LoadFromAssemblyPath(path);
            };
            assembly = Assembly.LoadFrom(args[1]);
            if (args[3] == "capability")
            {
                var runtime = assembly.GetType("Sussudio.Services.Runtime.FfmpegRuntimeInit", throwOnError: true)!;
                runtime.GetMethod("EnsureInitializedAtRoot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
                    .Invoke(null, new object[] { Path.Combine(Path.GetDirectoryName(args[1])!, "ffmpeg") });
                Console.WriteLine(JsonSerializer.Serialize(HevcP010Capability.OpenCodec()));
            }
            else
            {
                LibAvRecordingDrainBehaviorTests.RunCaptureServiceChildAsync(args[3], assembly, args[2]).GetAwaiter().GetResult();
                Console.WriteLine("RECORDING_LIFECYCLE_PASSED:" + args[3]);
            }
            exitCode = 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            exitCode = 1;
        }
        finally
        {
            if (assembly != null)
            {
                try
                {
                    var logger = assembly.GetType("Sussudio.Logger", throwOnError: true)!;
                    ((Task)logger.GetMethod("ShutdownAsync", BindingFlags.Public | BindingFlags.Static)!
                        .Invoke(null, new object[] { TimeSpan.FromSeconds(3) })!).GetAwaiter().GetResult();
                }
                catch (Exception error) { Console.Error.WriteLine(error); exitCode = 1; }
            }
        }
        return true;
    }
}

public sealed class RecordingNativeTestChildTests
{
    [Fact]
    public async Task TimeoutTerminatesChildBeforeDeletingItsPrivateFiles()
    {
        var parentRecovery = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        var parentLogs = Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT");
        var result = await RecordingNativeTestChild.RunAsync("hold", TimeSpan.FromSeconds(1));
        Assert.True(result.TimedOut, result.FailureDetail);
        Assert.False(Directory.Exists(result.Directory));
        Assert.Equal(parentRecovery, Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY"));
        Assert.Equal(parentLogs, Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT"));
    }
}
