namespace Sussudio.Tools;

internal static partial class DiagnosticSessionRunExecution
{
    private static Task RunScenarioPhaseAsync(DiagnosticSessionScenarioPhaseContext context)
    {
        return DiagnosticSessionScenarioPhaseRunner.RunAsync(context);
    }
}
