using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class PresentMonCancellationTests
    {
        [Fact]
        public Task CancellationBeforeLaunchDoesNotStartTheChild()
            => global::Program.PresentMonCancellation_OwnedChildExits(preCanceled: true);

        [Fact]
        public Task CancellationTerminatesTheOwnedChildAndFinishesOutputDrains()
            => global::Program.PresentMonCancellation_OwnedChildExits(preCanceled: false);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task CancellationHonorsCsvOwnership(bool keepCsv)
            => global::Program.PresentMonCancellation_HonorsCsvOwnership(keepCsv);

        [Fact]
        public Task DiagnosticCancellationReapsBackgroundPresentMonBeforeFinalizing()
            => global::Program.PresentMonCancellation_DiagnosticReapsBackgroundChild();

        internal static bool TryRunChildProcess(string[] args, out int exitCode)
        {
            exitCode = 0;
            var outputIndex = Array.IndexOf(args, "--output_file");
            var sessionIndex = Array.IndexOf(args, "--session_name");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length ||
                sessionIndex < 0 || sessionIndex + 1 >= args.Length ||
                !args[sessionIndex + 1].StartsWith("SussudioPresentMon", StringComparison.Ordinal))
            {
                return false;
            }

            var outputPath = args[outputIndex + 1];
            File.WriteAllText(outputPath, "Application,ProcessID,SwapChainAddress,MsBetweenPresents\nTest,1,0x1,16.67\n");
            File.WriteAllText(outputPath + ".pid.tmp", Environment.ProcessId.ToString());
            File.Move(outputPath + ".pid.tmp", outputPath + ".pid");
            Console.WriteLine("test PresentMon child ready");
            Console.Error.WriteLine("test PresentMon stderr ready");
            Thread.Sleep(Timeout.Infinite);
            return true;
        }
    }
}

static partial class Program
{
    internal static async Task PresentMonCancellation_HonorsCsvOwnership(bool keepCsv)
    {
        var directory = Path.Combine(GetRepoRoot(), "temp", $"presentmon-run-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var csvPath = Path.Combine(directory, "capture.csv");
        using var cancellation = new CancellationTokenSource();
        var probe = RequireSharedToolType("Sussudio.Tools.PresentMonProbe");
        var optionsType = probe.Assembly.GetType("Sussudio.Tools.PresentMonProbeOptions")!;
        var options = Activator.CreateInstance(optionsType)!;
        optionsType.GetProperty("ProcessId")!.SetValue(options, Environment.ProcessId);
        optionsType.GetProperty("DurationSeconds")!.SetValue(options, 30);
        optionsType.GetProperty("PresentMonPath")!.SetValue(options, PresentMonTestExecutable());
        optionsType.GetProperty("OutputFile")!.SetValue(options, csvPath);
        optionsType.GetProperty("KeepCsv")!.SetValue(options, keepCsv);
        var run = (Task)probe.GetMethod("RunAsync")!.Invoke(null, new[] { options, (object)cancellation.Token })!;
        int? childId = null;
        try
        {
            childId = await WaitForPresentMonTestChildAsync(csvPath + ".pid", run).ConfigureAwait(false);
            Assert.True(File.Exists(csvPath));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => run.WaitAsync(TimeSpan.FromSeconds(8))).ConfigureAwait(false);
            Assert.False(PresentMonTestChildStillRunning(childId.Value));
            Assert.Equal(keepCsv, File.Exists(csvPath));
        }
        finally
        {
            cancellation.Cancel();
            await StopPresentMonTestRunAsync(run, childId).ConfigureAwait(false);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    internal static async Task PresentMonCancellation_DiagnosticReapsBackgroundChild()
    {
        var directory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-presentmon-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var csvPath = Path.Combine(directory, "presentmon.csv");
        // The runner resolves the newest Sussudio process. A private copy of the
        // Windows command interpreter provides that target while waiting on its
        // test-owned stdin; no command is sent to it.
        var targetPath = Path.Combine(directory, "Sussudio.exe");
        File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), targetPath);
        using var target = Process.Start(new ProcessStartInfo
        {
            FileName = targetPath,
            Arguments = "/d /q",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        var targetOutput = target.StandardOutput.ReadToEndAsync();
        var targetError = target.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource();
        Task<object>? run = null;
        int? childId = null;
        var snapshotCount = 0;
        async Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? _, int? __, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (command == "GetSnapshot")
            {
                if (++snapshotCount == 3)
                {
                    childId = await WaitForPresentMonTestChildAsync(csvPath + ".pid").ConfigureAwait(false);
                    cancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                }
                return DiagnosticCancellationSnapshot(false, false, false, "Live");
            }
            return ParseDiagnosticSessionJson("""{"Success":true,"Data":[]}""");
        }

        try
        {
            Assert.False(target.HasExited, "The test-owned target must exist for PresentMon process resolution.");
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(assembly, "observe", 30, 100, directory);
            options.GetType().GetProperty("IncludePresentMon")!.SetValue(options, true);
            options.GetType().GetProperty("PresentMonPath")!.SetValue(options, PresentMonTestExecutable());
            run = RunTokenAwareDiagnosticSessionAsync(assembly, options, SendAsync, cancellation.Token);
            var result = await run.WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.NotNull(childId);
            Assert.False(PresentMonTestChildStillRunning(childId!.Value));
            Assert.True(File.Exists(csvPath), "Diagnostic sessions retain their PresentMon CSV evidence.");
            Assert.True(File.Exists(Path.Combine(directory, "summary.json")));
            Assert.DoesNotContain((IEnumerable<string>)GetPropertyValue(result, "Warnings")!,
                warning => warning.Contains("task still running", StringComparison.Ordinal));
        }
        finally
        {
            cancellation.Cancel();
            if (run is not null) await StopPresentMonTestRunAsync(run, childId).ConfigureAwait(false);
            if (!target.HasExited)
            {
                target.StandardInput.Close();
                try
                {
                    await target.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    target.Kill(entireProcessTree: true);
                    await target.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
            }
            await Task.WhenAll(targetOutput, targetError).WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static string PresentMonTestExecutable()
    {
        var path = Path.ChangeExtension(typeof(Sussudio.Tests.PresentMonCancellationTests).Assembly.Location, ".exe");
        Assert.True(File.Exists(path), $"The test apphost is required: {path}");
        return path;
    }

    private static async Task<int> WaitForPresentMonTestChildAsync(string pidPath, Task? run = null)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!File.Exists(pidPath))
        {
            if (run is { IsCompleted: true })
            {
                await run.ConfigureAwait(false);
                throw new InvalidOperationException("PresentMon returned before its test child became ready.");
            }
            await Task.Delay(25, deadline.Token).ConfigureAwait(false);
        }
        return int.Parse(await File.ReadAllTextAsync(pidPath, deadline.Token).ConfigureAwait(false));
    }

    private static async Task StopPresentMonTestRunAsync(Task run, int? childId)
    {
        try { await run.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        finally
        {
            if (childId is { } ownedId)
            {
                try
                {
                    using var child = Process.GetProcessById(ownedId);
                    if (!child.HasExited)
                    {
                        child.Kill(entireProcessTree: true);
                        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    }
                }
                catch (ArgumentException) { }
            }
        }
    }

    internal static async Task PresentMonCancellation_OwnedChildExits(bool preCanceled)
    {
        var directory = Path.Combine(GetRepoRoot(), "temp", $"presentmon-child-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var readyPath = Path.Combine(directory, "child.pid");
        var escapedReadyPath = readyPath.Replace("'", "''", StringComparison.Ordinal);
        var script = "[IO.File]::WriteAllText('" + escapedReadyPath +
            ".tmp', $PID.ToString()); [IO.File]::Move('" + escapedReadyPath + ".tmp', '" + escapedReadyPath +
            "'); [Console]::Out.WriteLine('owned child ready'); " +
            "[Console]::Error.WriteLine('owned child stderr'); while ($true) { Start-Sleep -Seconds 1 }";
        var arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " +
            Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        var probeType = RequireSharedToolType("Sussudio.Tools.PresentMonProbe");
        var runProcess = probeType.GetMethod("RunProcessAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var cancellation = new CancellationTokenSource();
        if (preCanceled) cancellation.Cancel();
        int? childId = null;
        Task? run = null;
        try
        {
            run = (Task)runProcess.Invoke(null, new object[] { executable, arguments, 30_000, cancellation.Token })!;
            if (!preCanceled)
            {
                using var startupDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                while (!File.Exists(readyPath))
                {
                    if (run.IsCompleted) await run.ConfigureAwait(false);
                    await Task.Delay(25, startupDeadline.Token).ConfigureAwait(false);
                }
                childId = int.Parse(await File.ReadAllTextAsync(readyPath, startupDeadline.Token).ConfigureAwait(false));
                cancellation.Cancel();
            }

            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => run.WaitAsync(TimeSpan.FromSeconds(8))).ConfigureAwait(false);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(run.IsCanceled);
            if (preCanceled)
            {
                Assert.False(File.Exists(readyPath));
            }
            else
            {
                Assert.False(PresentMonTestChildStillRunning(childId!.Value),
                    "Canceled PresentMon work must observe its owned child exit before returning.");
            }
        }
        finally
        {
            cancellation.Cancel();
            if (childId is null && File.Exists(readyPath) && int.TryParse(File.ReadAllText(readyPath), out var recordedId))
                childId = recordedId;
            if (childId is { } ownedId)
            {
                try
                {
                    using var child = Process.GetProcessById(ownedId);
                    if (!child.HasExited)
                    {
                        child.Kill(entireProcessTree: true);
                        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    }
                }
                catch (ArgumentException) { }
            }
            if (run is not null)
            {
                try { await run.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static bool PresentMonTestChildStillRunning(int processId)
    {
        try
        {
            using var child = Process.GetProcessById(processId);
            return !child.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
