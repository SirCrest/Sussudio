using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationProtocol_SetRecordingUsesRecordingSizedTimeout()
    {
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n");
        var clientText = ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n");

        AssertContains(protocolText, "public const int DefaultResponseTimeoutMs = 15000;");
        AssertContains(protocolText, "public const int ExtendedResponseTimeoutMs = 60000;");
        AssertContains(protocolText, "public const int RecordingResponseTimeoutMs = 150000;");
        AssertContains(protocolText, "public const int FlashbackMutationResponseTimeoutMs = 305000;");
        AssertContains(protocolText, "commandName = ResolveCanonicalCommandName(commandName);");
        AssertContains(protocolText, "AutomationCommandCatalog.TryGet(commandName, out var metadata)");
        AssertContains(protocolText, "? metadata.ResponseTimeoutMs");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        AssertContains(catalogText, "AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogText, "AutomationPipeProtocol.RecordingResponseTimeoutMs");
        AssertContains(catalogText, "AutomationCommandKind.FlashbackExport");
        AssertContains(catalogText, "AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs");
        AssertDoesNotContain(protocolText, "AlignResponseTimeoutWithServerRequest");
        AssertContains(clientText, "AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)");
        AssertContains(clientText, "AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)");
        AssertContains(clientText, "public int? ResponseTimeoutMs { get; set; }");
        var pipeClientText = ReadAutomationPipeClientSource();
        AssertDoesNotContain(pipeClientText, "AlignResponseTimeoutWithServerRequest");

        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var getDefaultResponseTimeout = protocolType.GetMethod(
            "GetDefaultResponseTimeout",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout(string) not found.");

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(150000, timeoutMs, $"SetRecordingEnabled timeout for '{acceptedName}'");
        }

        var defaultTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "GetSnapshot" })!;
        AssertEqual(15000, defaultTimeoutMs, "GetSnapshot timeout remains bounded");

        var flashbackExportTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "FlashbackExport" })!;
        AssertEqual(305000, flashbackExportTimeoutMs, "FlashbackExport uses flashback mutation timeout");

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(305000, timeoutMs, $"Flashback mutation timeout for '{acceptedName}' outlives server cancellation");
        }

        return Task.CompletedTask;
    }

    private static Task ExternalProcessProbes_UseBoundedProcessSupervisor()
    {
        var ffmpegText = ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        var hdrText = ReadRepoFile("Sussudio/Services/Recording/HdrValidationRunner.cs")
            .Replace("\r\n", "\n");

        AssertContains(ffmpegText, "private const int ProbeTimeoutMs = 10_000;");
        AssertContains(ffmpegText, "new ProcessSupervisor().RunAsync");
        AssertContains(ffmpegText, "TimeoutMs = ProbeTimeoutMs");
        AssertContains(ffmpegText, "if (!result.Started || result.TimedOut || result.ExitCode != 0)");
        AssertContains(ffmpegText, "return result.Started && !result.TimedOut && result.ExitCode == 0;");
        AssertContains(hdrText, "private const int ValidationTimeoutMs = 30_000;");
        AssertContains(hdrText, "new ProcessSupervisor().RunAsync");
        AssertContains(hdrText, "validator-timeout");

        return Task.CompletedTask;
    }

    private static Task AutomationDispatch_CancellationAndTimeoutsStayBounded()
    {
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.Dispatching.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs")
                .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var pipeServerText = (
            ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.Connections.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.Responses.cs"))
            .Replace("\r\n", "\n");

        AssertContains(viewModelText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        AssertContains(coordinatorText, "return Task.FromCanceled(cancellationToken);");
        AssertContains(coordinatorText, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(coordinatorText, "cancellationRegistration = cancellationToken.Register");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(cancellationRegistration, \"enqueue_failed\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(workItem.CancellationRegistration, \"begin_process\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(pending.CancellationRegistration, \"fail_pending\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_CANCEL_REG_DISPOSE_WARN");
        AssertContains(coordinatorText, "CancelWorkerBestEffort();");
        AssertContains(coordinatorText, "DisposeWorkerCancellationBestEffort(\"worker_completed\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CANCEL_WARN");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN");
        AssertContains(coordinatorText, "public bool PropagateCancellationToOperation { get; init; }");
        AssertContains(coordinatorText, "bool propagateCancellationToOperation = false");
        AssertContains(coordinatorText, "propagateCancellationToOperation: true");
        AssertContains(pipeServerText, "var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);");
        AssertContains(pipeServerText, "if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))");
        AssertContains(pipeServerText, "using var registration = cancellationToken.Register(");
        AssertContains(pipeServerText, "ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);");
        AssertContains(pipeServerText, "Request timed out after {_requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");
        AssertDoesNotContain(dispatcherText, "WaitConditionRefreshCadenceMs");

        return Task.CompletedTask;
    }

    private static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceAudioSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureServiceText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(captureServiceText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(coordinatorText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(coordinatorText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(coordinatorText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(viewModelText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    private static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
    {
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureService");
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureSessionCoordinator");
        return Task.CompletedTask;
    }

    private static void AssertPreviewStopSurface(string typeName)
    {
        var type = RequireType(typeName);
        AssertStopSurface(type, "StopVideoPreviewAsync", "StopVideoPreviewWithTeardownAsync");
        AssertStopSurface(type, "StopAudioPreviewAsync", "StopAudioPreviewWithTeardownAsync");
    }

    private static void AssertStopSurface(Type type, string stopMethodName, string teardownMethodName)
    {
        var publicInstance = BindingFlags.Instance | BindingFlags.Public;
        var oneParameterStopOverloads = type.GetMethods(publicInstance)
            .Where(method => method.Name == stopMethodName && method.GetParameters().Length == 1)
            .ToArray();

        AssertEqual(1, oneParameterStopOverloads.Length, $"{type.FullName}.{stopMethodName} one-parameter overload count");
        AssertEqual(
            typeof(CancellationToken).FullName,
            oneParameterStopOverloads[0].GetParameters()[0].ParameterType.FullName,
            $"{type.FullName}.{stopMethodName} single parameter");

        var boolFirstParameterOverloads = type.GetMethods(publicInstance)
            .Where(method =>
            {
                if (method.Name != stopMethodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(bool);
            })
            .ToArray();
        AssertEqual(0, boolFirstParameterOverloads.Length, $"{type.FullName}.{stopMethodName} bool-first overload count");

        var teardownMethod = type.GetMethod(teardownMethodName, publicInstance, binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
        AssertNotNull(teardownMethod, $"{type.FullName}.{teardownMethodName}(CancellationToken)");
    }

}
