using System;
using Sussudio.Services.Automation;
using Sussudio.Services.Contracts;
using Sussudio.Services.Recording;
using Sussudio.Tools;

namespace Sussudio;

// Automation host composition for the shell. Startup owns when services start;
// this partial owns how the diagnostics hub, dispatcher, and pipe server are wired.
public sealed partial class MainWindow
{
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private readonly bool _automationTokenRequired;
    private readonly string _automationPipeName;

    private AutomationHostComposition CreateAutomationHost()
    {
        var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
        var automationPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = NamedPipeAutomationServer.DefaultPipeName;
        }

        var diagnosticsHub = new AutomationDiagnosticsHub(
            ViewModel,
            GetPreviewRuntimeSnapshotAsync,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            ViewModel,
            diagnosticsHub,
            this,
            automationToken);
        var pipeServer = new NamedPipeAutomationServer(
            automationDispatcher,
            automationPipeName,
            !string.IsNullOrWhiteSpace(automationToken));

        return new AutomationHostComposition(
            diagnosticsHub,
            pipeServer,
            !string.IsNullOrWhiteSpace(automationToken),
            automationPipeName);
    }

    private readonly record struct AutomationHostComposition(
        IAutomationDiagnosticsHub DiagnosticsHub,
        NamedPipeAutomationServer PipeServer,
        bool TokenRequired,
        string PipeName);
}
