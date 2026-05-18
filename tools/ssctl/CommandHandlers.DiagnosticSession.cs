using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleDiagnosticSessionAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var scenario = ParseOptionalStringFlag(context.Rest, "--scenario") ?? DiagnosticSessionOptions.DefaultScenario;
        var seconds = ParseOptionalIntFlag(context.Rest, "--seconds") ?? DiagnosticSessionOptions.DefaultDurationSeconds;
        var sampleIntervalMs = ParseOptionalIntFlag(context.Rest, "--sample-ms") ?? DiagnosticSessionOptions.DefaultSampleIntervalMs;
        var outputDirectory = ParseOptionalStringFlag(context.Rest, "--output");
        var presentMonPath = ParseOptionalStringFlag(context.Rest, "--presentmon-path");
        var includePresentMon = ConsumeFlag(context.Rest, "--presentmon");
        var verify = ConsumeFlag(context.Rest, "--verify");
        var leaveRunning = ConsumeFlag(context.Rest, "--leave-running");
        EnsureNoArgs(context.Rest, DiagnosticSessionOptions.CliUsage);

        var result = await DiagnosticSessionRunner.RunAsync(
                new DiagnosticSessionOptions
                {
                    Scenario = scenario,
                    DurationSeconds = seconds,
                    SampleIntervalMs = sampleIntervalMs,
                    OutputDirectory = outputDirectory,
                    IncludePresentMon = includePresentMon,
                    PresentMonPath = presentMonPath,
                    VerifyRecording = verify,
                    LeaveRunning = leaveRunning
                },
                (command, payload, responseTimeoutMs) => context.Transport.SendCommandAsync(command, payload, responseTimeoutMs))
            .ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : DiagnosticSessionRunner.Format(result));
        return result.Success ? 0 : 3;
    }
}
