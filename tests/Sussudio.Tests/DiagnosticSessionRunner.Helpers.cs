static partial class Program
{
    private static string ReadDiagnosticSessionRunnerSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Scenario.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.ResultRequest.cs");
    }

    private static string ReadDiagnosticSessionRunExecutionRootSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs");
    }

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Scenario.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs");
    }
}
