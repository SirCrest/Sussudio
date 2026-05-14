static partial class Program
{
    private static void AssertDiagnosticSessionToolSurfaceOwnership()
    {
        var diagnosticSessionToolSources = ReadDiagnosticSessionToolSurfaceSourceFamily();
        var ssctlProgramText = diagnosticSessionToolSources.SsctlProgramText;
        var ssctlCommandHandlersText = diagnosticSessionToolSources.SsctlCommandHandlersText;
        var mcpDiagnosticSessionText = diagnosticSessionToolSources.McpDiagnosticSessionText;
        AssertContains(ssctlProgramText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(mcpDiagnosticSessionText, "flashback-export-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-segment-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-encoder-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export-audio-switch");
        AssertContains(mcpDiagnosticSessionText, "flashback-disable-during-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-rotated-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-settings-deferred");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-export-rejected");
    }
}
