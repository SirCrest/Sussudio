static partial class Program
{
    private static string ReadDiagnosticSessionRunnerSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.ResultRequest.cs");
    }
}
