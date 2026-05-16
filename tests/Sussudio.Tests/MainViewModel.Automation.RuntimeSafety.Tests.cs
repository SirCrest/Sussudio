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
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs")
            .Replace("\r\n", "\n");
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "RegisterCloseLifecycle(appWindow);");
        AssertContains(closeLifecycleText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(closeLifecycleText, "if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(closeLifecycleText, "RequestWindowClose();");
        AssertContains(closeLifecycleText, "_windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken)");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest();");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "var enqueueFailure = new InvalidOperationException(\"Failed to enqueue window close action on the UI thread.\");");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(closeRecordingFinalizationControllerText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");
        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
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
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
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
        AssertContains(controllerText, "=> WindowScreenshotNativeCapture.Capture(_windowHandleProvider(), outputPath);");

        return Task.CompletedTask;
    }

    private static Task WindowScreenshotNativeCapture_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var nativeCaptureText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(controllerText, "=> WindowScreenshotNativeCapture.Capture(_windowHandleProvider(), outputPath);");
        AssertDoesNotContain(controllerText, "[DllImport(");
        AssertDoesNotContain(controllerText, "PrintWindow(");
        AssertDoesNotContain(controllerText, "CreateCompatibleBitmap(");
        AssertDoesNotContain(controllerText, "GetDIBits(");
        AssertDoesNotContain(controllerText, "struct BITMAPINFOHEADER");
        AssertContains(nativeCaptureText, "internal static class WindowScreenshotNativeCapture");
        AssertContains(nativeCaptureText, "internal static WindowScreenshotResult Capture(IntPtr hwnd, string outputPath)");
        AssertContains(nativeCaptureText, "Message = \"Window handle not available.\"");
        AssertContains(nativeCaptureText, "Message = \"PrintWindow failed.\"");
        AssertContains(nativeCaptureText, "var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);");
        AssertContains(nativeCaptureText, "GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);");
        AssertContains(nativeCaptureText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertContains(nativeCaptureText, "Message = $\"Window screenshot saved: {width}x{height}\"");

        return Task.CompletedTask;
    }

    private static Task WindowScreenshotImageEncoding_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var nativeCaptureText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs")
            .Replace("\r\n", "\n");
        var encoderText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(controllerText, "WindowScreenshotNativeCapture.Capture(");
        AssertContains(nativeCaptureText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertDoesNotContain(controllerText, "private static void WritePngToStream");
        AssertDoesNotContain(controllerText, "private static void WriteBmpToStream");
        AssertContains(encoderText, "internal static class WindowScreenshotImageEncoder");
        AssertContains(encoderText, "internal static void WritePngToStream");
        AssertContains(encoderText, "internal static void WriteBmpToStream");
        AssertContains(encoderText, "internal static uint[] InitCrc32Table()");

        var encoderType = RequireType("Sussudio.Controllers.WindowScreenshotImageEncoder");
        var writePng = encoderType.GetMethod("WritePngToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WritePngToStream missing.");
        var writeBmp = encoderType.GetMethod("WriteBmpToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WriteBmpToStream missing.");
        var bgra = new byte[] { 0, 0, 255, 255 };

        using var pngStream = new MemoryStream();
        writePng.Invoke(null, new object[] { pngStream, 1, 1, bgra });
        var pngBytes = pngStream.ToArray();
        AssertSequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, pngBytes.Take(8).ToArray(), "PNG signature");
        AssertEqual((byte)73, pngBytes[12], "PNG IHDR I");
        AssertEqual((byte)72, pngBytes[13], "PNG IHDR H");
        AssertEqual((byte)68, pngBytes[14], "PNG IHDR D");
        AssertEqual((byte)82, pngBytes[15], "PNG IHDR R");

        using var bmpStream = new MemoryStream();
        writeBmp.Invoke(null, new object[] { bmpStream, 1, 1, bgra });
        var bmpBytes = bmpStream.ToArray();
        AssertEqual((byte)0x42, bmpBytes[0], "BMP signature B");
        AssertEqual((byte)0x4D, bmpBytes[1], "BMP signature M");
        AssertEqual(58, bmpBytes.Length, "BMP byte length");
        AssertEqual(1, BitConverter.ToInt32(bmpBytes, 18), "BMP width");
        AssertEqual(-1, BitConverter.ToInt32(bmpBytes, 22), "BMP top-down height");

        return Task.CompletedTask;
    }

    private static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceAudioSource();
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
