using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Assembly LoadDiagnosticSessionRunnerAssembly()
    {
        return LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
    }

    private static object CreateDiagnosticSessionOptions(
        Assembly assembly,
        string scenario,
        int durationSeconds,
        int sampleIntervalMs,
        string outputDirectory)
    {
        var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
        var options = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");

        optionsType.GetProperty("Scenario")!.SetValue(options, scenario);
        optionsType.GetProperty("DurationSeconds")!.SetValue(options, durationSeconds);
        optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, sampleIntervalMs);
        optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);
        return options;
    }

    private static async Task<object> RunDiagnosticSessionRunnerAsync(
        Assembly assembly,
        object options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand,
        CancellationToken cancellationToken = default)
    {
        var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
            ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
        var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
        var task = runAsync.Invoke(null, new object?[] { options, sendCommand, cancellationToken }) as Task
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");
    }

    private static JsonElement ParseDiagnosticSessionJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
