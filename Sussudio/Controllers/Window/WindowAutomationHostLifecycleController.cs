using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Sussudio.Services.Contracts;
using Sussudio.Services.Recording;
using Sussudio.Tools;

namespace Sussudio.Controllers;

internal sealed class WindowAutomationHostLifecycleController : IAsyncDisposable
{
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly NamedPipeAutomationServer _pipeServer;
    private readonly bool _tokenRequired;
    private readonly string _pipeName;
    private int _started;

    public WindowAutomationHostLifecycleController(
        IAutomationViewModel viewModel,
        Func<CancellationToken, Task<PreviewRuntimeSnapshot>> previewSnapshotProvider,
        IAutomationWindowControl windowControl)
    {
        var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
        var automationPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = NamedPipeAutomationServer.DefaultPipeName;
        }

        _diagnosticsHub = new AutomationDiagnosticsHub(
            viewModel,
            previewSnapshotProvider,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            viewModel,
            _diagnosticsHub,
            windowControl,
            automationToken);

        _tokenRequired = !string.IsNullOrWhiteSpace(automationToken);
        _pipeName = automationPipeName;
        _pipeServer = new NamedPipeAutomationServer(
            automationDispatcher,
            _pipeName,
            _tokenRequired);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        if (_pipeServer.Start())
        {
            _diagnosticsHub.Start();
            Logger.Log(
                $"Automation control ready on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
        else
        {
            Logger.Log(
                $"Automation control disabled on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _pipeServer.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            await _diagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation diagnostics shutdown cleanup failed: {ex.Message}");
        }
    }
}
