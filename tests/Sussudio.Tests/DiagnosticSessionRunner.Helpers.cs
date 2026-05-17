static partial class Program
{
    private static string ReadDiagnosticSessionRunnerSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs")
            + "\n" + ReadDiagnosticSessionRunContextSource()
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Completion.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Scenario.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.ResultRequest.cs");
    }

    private static string ReadDiagnosticSessionRunExecutionRootSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs");
    }

    private static string ReadDiagnosticSessionRunContextSource()
    {
        return ReadDiagnosticSessionRunContextRootSource()
            + "\n" + ReadDiagnosticSessionRunContextPhaseContextsSource();
    }

    private static string ReadDiagnosticSessionRunContextRootSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");
    }

    private static string ReadDiagnosticSessionRunContextPhaseContextsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs");
    }

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Scenario.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs");
    }

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Completion.cs");
    }
}
