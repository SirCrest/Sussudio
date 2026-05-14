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
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout not found.");

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

    private static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingLifecycleText, "Logger.LogException(ex);");
        AssertContains(recordingLifecycleText, "IsRecording = _sessionCoordinator.Snapshot.IsRecording;");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException ex)\n            {\n                transitionError = ex;\n                Logger.Log($\"Recording transition wait canceled: {ex.Message}\");\n            }");
        AssertContains(
            recordingLifecycleText,
            "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))\n            {\n                throw transitionCanceled;\n            }");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            IsRecording = _sessionCoordinator.Snapshot.IsRecording;\n            StatusText = \"Recording start canceled\";\n            throw;\n        }");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            IsRecording = _sessionCoordinator.Snapshot.IsRecording;\n            StatusText = \"Stop recording canceled\";\n            throw;\n        }");
        AssertContains(recordingLifecycleText, "StatusText = $\"Recording failed: {ex.Message}\";");
        AssertContains(recordingLifecycleText, "StatusText = $\"Stop recording failed: {ex.Message}\";");
        AssertContains(recordingLifecycleText, "throw;");

        return Task.CompletedTask;
    }

    private static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(closeLifecycleText, "const int StopBudgetMs = 120_000;");
        AssertContains(closeLifecycleText, "close cancelled to protect recording");
        AssertContains(closeLifecycleText, "RequestWindowClose();");
        AssertContains(closeLifecycleText, "GetWindowCloseCompletionTask(cancellationToken)");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest();");
        AssertDoesNotContain(closeLifecycleText, "MP4 may be truncated.");

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

    private static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n                {\n                    result = sinkResult;");

        return Task.CompletedTask;
    }

    private static Task MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation()
    {
        var windowText = ReadRepoFile("Sussudio/MainWindow.Screenshot.cs")
            .Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var method = ExtractTextBetween(
            controllerText,
            "public Task<WindowScreenshotResult> CaptureAsync",
            "    private WindowScreenshotResult CaptureCore");

        AssertContains(windowText, "public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync");
        AssertContains(windowText, "=> _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);");
        AssertContains(method, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(method, "Message = \"Screenshot canceled.\"");
        AssertContains(method, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(method, "cancellationToken.Register(() =>");
        AssertContains(method, "_ = completion.Task.ContinueWith(");
        AssertContains(method, "cancellationRegistration.Dispose()");
        AssertContains(method, "if (!_dispatcherQueue.TryEnqueue(() =>");
        AssertContains(method, "Message = \"Failed to enqueue screenshot capture on the UI thread.\"");

        return Task.CompletedTask;
    }

    private static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
                .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
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

    private static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingLifecycleText, "internal Task StopRecordingForEmergencyAsync");
        // Fix #12: emergency stop now routes through the coordinator's emergency-flagged path
        // so LibAvRecordingSink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s).
        AssertContains(recordingLifecycleText, "=> _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingLifecycleText, "if (!IsRecording)");
        AssertDoesNotContain(captureText, "internal Task StopRecordingForEmergencyAsync");

        return Task.CompletedTask;
    }

}
