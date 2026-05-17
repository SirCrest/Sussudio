static partial class Program
{
    private static string ReadAutomationDiagnosticsHubCountersSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.cs");
    }

    private static string ReadCaptureServiceDiagnosticsRefreshSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.DisposalLifecycle.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
    }

    private static string ReadFlashbackBackendResourcesSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
    }

    private static MfSourceReaderVideoCaptureSourceFamily ReadMfSourceReaderVideoCaptureSourceFamily()
    {
        var rootText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs");
        var diagnosticsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs");
        var dxgiBuffersText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs");
        var frameLayoutText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs");
        var lifecycleText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs");
        var initializationText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs");
        var readLoopText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ReadLoop.cs");
        var frameDeliveryText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs");
        var cadenceText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs");

        return new MfSourceReaderVideoCaptureSourceFamily(
            rootText,
            diagnosticsText,
            dxgiBuffersText,
            frameLayoutText,
            lifecycleText,
            initializationText,
            readLoopText,
            frameDeliveryText,
            string.Join(
                "\n",
                new[]
                {
                    rootText,
                    diagnosticsText,
                    dxgiBuffersText,
                    frameLayoutText,
                    lifecycleText,
                    initializationText,
                    readLoopText,
                    frameDeliveryText,
                    cadenceText,
                }));
    }

    private static DiagnosticSessionSourceFamily ReadDiagnosticSessionSourceFamily()
    {
        return new DiagnosticSessionSourceFamily(
            ReadDiagnosticSessionRunnerSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
                + "\n" + ReadDiagnosticSessionScenarioStartupSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
                + "\n" + ReadDiagnosticSessionCleanupActionsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
                + "\n" + ReadDiagnosticSessionResultBuilderSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionSummaryWriter.cs")
                + "\n" + ReadDiagnosticSessionBackgroundTasksSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunState.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionLiveStateWriter.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunBootstrap.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
                + "\n" + ReadDiagnosticSessionFlashbackCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportsSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportScenariosSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
                + "\n" + ReadDiagnosticSessionFlashbackMetricsSource()
                + "\n" + ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackRejectedExportsSource()
                + "\n" + ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
                + "\n" + ReadDiagnosticSessionFlashbackStressScenarioSource()
                + "\n" + ReadDiagnosticSessionFlashbackValidationSource()
                + "\n" + ReadDiagnosticSessionFlashbackWaitsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthTolerances.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs")
                + "\n" + ReadDiagnosticSessionMetricsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs")
                + "\n" + ReadDiagnosticSessionResultFormatterSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionSampler.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionOptionalTextFormatter.cs"),
            ReadDiagnosticSessionModelsSource(),
            ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarios.cs"));
    }

    private static DiagnosticSessionToolSurfaceSourceFamily ReadDiagnosticSessionToolSurfaceSourceFamily()
    {
        return new DiagnosticSessionToolSurfaceSourceFamily(
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/SsctlHelpWriter.cs")
                + "\n" + ReadNormalizedRepoFile("tools/ssctl/SsctlHelpWriter.Sections.cs")
                + "\n" + ReadNormalizedRepoFile("tools/ssctl/SsctlHelpWriter.Catalog.cs"),
            (ReadRepoFile("tools/ssctl/CommandHandlers.cs")
                + "\n" + ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs")
                + "\n" + ReadRepoFile("tools/ssctl/CommandHandlers.DiagnosticSession.cs"))
                .Replace("\r\n", "\n"),
            ReadNormalizedRepoFile("tools/McpServer/Tools/DiagnosticSessionTools.cs"));
    }

    private static string ReadDiagnosticSessionFlashbackValidationSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Recording.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Playback.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs");
    }

    private static string ReadDiagnosticSessionFlashbackExportsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.SegmentCount.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.VerifyPayload.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.AudioSwitch.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.SelectionCleanup.cs");
    }

    private static string ReadDiagnosticSessionFlashbackRejectedExportsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.Inactive.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.Recording.cs");
    }

    private static string ReadNormalizedRepoFile(string path)
    {
        return ReadRepoFile(path).Replace("\r\n", "\n");
    }

    private readonly record struct MfSourceReaderVideoCaptureSourceFamily(
        string RootText,
        string DiagnosticsText,
        string DxgiBuffersText,
        string FrameLayoutText,
        string LifecycleText,
        string InitializationText,
        string ReadLoopText,
        string FrameDeliveryText,
        string SourceFamilyText);

    private readonly record struct DiagnosticSessionSourceFamily(
        string SourceFamilyText,
        string ModelsText,
        string ScenariosText);

    private readonly record struct DiagnosticSessionToolSurfaceSourceFamily(
        string SsctlProgramText,
        string SsctlHelpText,
        string SsctlCommandHandlersText,
        string McpDiagnosticSessionText);
}
