using System.Globalization;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionRunBootstrap(
    string Scenario,
    DiagnosticSessionScenarioPlan ScenarioPlan,
    int DurationSeconds,
    int SampleIntervalMs,
    string SessionId,
    string OutputDirectory,
    DateTimeOffset StartedUtc,
    int RunnerProcessId)
{
    internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)
    {
        var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);
        var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);
        var durationSeconds = Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60);
        var sampleIntervalMs = Math.Clamp(options.SampleIntervalMs, 100, 60_000);
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "diagnostic-sessions", sessionId)
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        return new DiagnosticSessionRunBootstrap(
            scenario,
            scenarioPlan,
            durationSeconds,
            sampleIntervalMs,
            sessionId,
            outputDirectory,
            DateTimeOffset.UtcNow,
            Environment.ProcessId);
    }
}
