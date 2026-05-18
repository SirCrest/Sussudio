static partial class Program
{
    private static void AssertDiagnosticSessionToolSurfaceOwnership()
    {
        var diagnosticSessionToolSources = ReadDiagnosticSessionToolSurfaceSourceFamily();
        var ssctlProgramText = diagnosticSessionToolSources.SsctlProgramText;
        var ssctlHelpText = diagnosticSessionToolSources.SsctlHelpText;
        var ssctlCommandHandlersText = diagnosticSessionToolSources.SsctlCommandHandlersText;
        var mcpDiagnosticSessionText = diagnosticSessionToolSources.McpDiagnosticSessionText;
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertDoesNotContain(ssctlProgramText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(ssctlHelpText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionScenarioCatalog.Description");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertDoesNotContain(mcpDiagnosticSessionText, "Session scenario: observe,");
        AssertDoesNotContain(mcpDiagnosticSessionText, "string scenario = \"observe\"");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int seconds = 10");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int sampleIntervalMs = 1000");
    }
}
