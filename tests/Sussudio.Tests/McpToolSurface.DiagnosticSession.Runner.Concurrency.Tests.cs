using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-concurrent-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");

        // Simulate a concurrent in-flight diagnostic session by holding the same exclusive
        // lock file the runner uses. A second RunAsync against this OutputDirectory must
        // fail fast with InvalidOperationException rather than corrupt the artifact set.
        FileStream? holderLock = null;
        try
        {
            holderLock = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "observe");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (_, _, _) =>
                Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "should-not-be-called"
                    }
                    """));

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");

            Exception? captured = null;
            try
            {
                var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                    ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                captured = ex.InnerException;
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            if (captured is null)
            {
                throw new InvalidOperationException("Assertion failed: expected concurrent invocation to throw, but RunAsync completed.");
            }

            AssertEqual(typeof(InvalidOperationException), captured.GetType(), "diagnostic concurrent invocation exception type");
            AssertContains(captured.Message ?? string.Empty, "Another diagnostic session");

            // Artifacts must NOT have been written; only the lock file should exist.
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "summary.json")), "diagnostic concurrent invocation must not write summary");
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "session-live.json")), "diagnostic concurrent invocation must not write live state");
        }
        finally
        {
            holderLock?.Dispose();
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
