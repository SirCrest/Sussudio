using System.IO;
using System.Threading.Tasks;

// Tests for named-pipe automation server framing plus app-surface auth wiring.
static partial class Program
{
    internal static Task NamedPipeAutomationServer_RequestTimeoutsUseBoundedDispatchCancellation()
    {
        var pipeServerText = ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            .Replace("\r\n", "\n");

        AssertContains(pipeServerText, "private sealed class ConnectionSession");
        AssertContains(pipeServerText, "var session = new ConnectionSession(this, server, cancellationToken);");
        AssertContains(pipeServerText, "var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, _serverCancellation);");
        AssertContains(pipeServerText, "if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))");
        AssertContains(pipeServerText, "using var registration = cancellationToken.Register(");
        AssertContains(pipeServerText, "ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);");
        AssertContains(pipeServerText, "Request timed out after {_owner._requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");

        return Task.CompletedTask;
    }

    internal static Task MainWindowAutomation_WiresPipeAuthFallbackPolicy()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupControllerText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchStartupController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "_automationHostLifecycleController = new WindowAutomationHostLifecycleController(");
        AssertContains(mainWindowText, "GetPreviewRuntimeSnapshotAsync,\n            this);");
        AssertContains(mainWindowText, "private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");
        AssertContains(automationHostControllerText, "var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);");
        AssertContains(automationHostControllerText, "var automationPipeName = Environment.GetEnvironmentVariable(\"SUSSUDIO_AUTOMATION_PIPE\");");
        AssertContains(automationHostControllerText, "automationPipeName = NamedPipeAutomationServer.DefaultPipeName;");
        AssertContains(automationHostControllerText, "var automationPorts = AutomationViewModelPorts.From(viewModel);");
        AssertContains(automationHostControllerText, "new AutomationDiagnosticsHub(\n            automationPorts.SnapshotQuery,\n            previewSnapshotProvider,\n            new RecordingVerifier())");
        AssertContains(automationHostControllerText, "new AutomationCommandDispatcher(\n            automationPorts,\n            _diagnosticsHub,\n            windowControl,\n            automationToken)");
        AssertContains(automationHostControllerText, "_tokenRequired = !string.IsNullOrWhiteSpace(automationToken);");
        AssertContains(automationHostControllerText, "new NamedPipeAutomationServer(\n            automationDispatcher,\n            _pipeName,\n            _tokenRequired)");
        AssertDoesNotContain(mainWindowText, "Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar)");
        AssertDoesNotContain(mainWindowText, "new NamedPipeAutomationServer(");
        AssertDoesNotContain(startupText, "new NamedPipeAutomationServer(");
        AssertContains(startupText, "StartAutomationHost = _automationHostLifecycleController.Start,");
        AssertContains(launchStartupControllerText, "_context.StartAutomationHost();");
        AssertContains(automationHostControllerText, "if (_pipeServer.Start())\n        {\n            _diagnosticsHub.Start();");
        AssertContains(automationHostControllerText, "Automation control ready on pipe '{_pipeName}' (token required={_tokenRequired}).");
        AssertContains(automationHostControllerText, "Automation control disabled on pipe '{_pipeName}' (token required={_tokenRequired}).");

        return Task.CompletedTask;
    }

    internal static Task StreamDeckPluginScope_DocumentsAutomationAuthEnvelope()
    {
        var docs = ReadRepoFile("docs/stream-deck-plugin-scope.md")
            .Replace("\r\n", "\n");

        AssertContains(docs, "\"authToken\": \"<token-or-null>\"");
        AssertContains(docs, "SUSSUDIO_AUTOMATION_TOKEN");
        AssertContains(docs, "AutomationPipeProtocol.CreateRequestEnvelope");
        AssertContains(docs, "ErrorCode: \"unauthorized\"");
        AssertContains(docs, "optional auth token");
        AssertContains(docs, "automation is disabled instead of opening a default");

        return Task.CompletedTask;
    }
}
