using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task ReliabilityGates_RunToolsAndOfflineHarness()
    {
        var scriptText = ReadRepoFile("tools/reliability-gates.ps1")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadDiagnosticSessionRunnerSource();
        var diagnosticSessionCleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();

        AssertContains(scriptText, "$testProjectPath = Join-Path $repoRoot \"tests\\Sussudio.Tests\\Sussudio.Tests.csproj\"");
        AssertContains(scriptText, "$ssctlProjectPath = Join-Path $repoRoot \"tools\\ssctl\\ssctl.csproj\"");
        AssertContains(scriptText, "$mcpServerProjectPath = Join-Path $repoRoot \"tools\\McpServer\\McpServer.csproj\"");
        AssertContains(scriptText, "$nativeXuProbeProjectPath = Join-Path $repoRoot \"tools\\NativeXuAudioProbe\\NativeXuAudioProbe.csproj\"");
        AssertContains(scriptText, "-t:Rebuild");
        AssertContains(scriptText, "\"run\"");
        AssertContains(scriptText, "--no-build");
        AssertContains(scriptText, "$appAssemblyPath");
        AssertContains(scriptText, "Build, tool, and offline regression gates passed.");
        AssertDoesNotContain(scriptText, "docs/testing/README.md");

        AssertContains(diagnosticSessionCleanupActionsText, "var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);");
        AssertContains(diagnosticSessionCleanupActionsText, "CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs))");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = true }");
        return Task.CompletedTask;
    }
}
