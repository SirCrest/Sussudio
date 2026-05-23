static partial class Program
{
    private static AutomationDiagnosticsHubCountersSourceFamily ReadAutomationDiagnosticsHubCountersSource()
    {
        var countersText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs");

        return new AutomationDiagnosticsHubCountersSourceFamily(
            countersText,
            countersText);
    }

    private static string ReadCaptureServiceDiagnosticsRefreshSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.TransitionExecution.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRangeResolution.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportForceRotate.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
    }

    private static string ReadFlashbackBackendResourcesSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.Lifecycle.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
    }

    private static MfSourceReaderVideoCaptureSourceFamily ReadMfSourceReaderVideoCaptureSourceFamily()
    {
        var rootText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs");
        var diagnosticsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs");
        var dxgiBuffersText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs");
        var frameLayoutText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs");
        var lifecycleText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs");
        var initializationText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs");
        var initializedSessionText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.InitializedSession.cs");
        var readLoopText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ReadLoop.cs");
        var frameDeliveryText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs");
        var rawFrameDeliveryText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.RawFrameDelivery.cs");
        var cadenceText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs");

        return new MfSourceReaderVideoCaptureSourceFamily(
            rootText,
            diagnosticsText,
            dxgiBuffersText,
            frameLayoutText,
            lifecycleText,
            initializationText,
            initializedSessionText,
            readLoopText,
            frameDeliveryText,
            rawFrameDeliveryText,
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
                    initializedSessionText,
                    readLoopText,
                    frameDeliveryText,
                    rawFrameDeliveryText,
                    cadenceText,
                }));
    }

    private static DiagnosticSessionSourceFamily ReadDiagnosticSessionSourceFamily()
    {
        return new DiagnosticSessionSourceFamily(
            ReadDiagnosticSessionRunnerSource()
                + "\n" + ReadDiagnosticSessionScenarioSetupSource()
                + "\n" + ReadDiagnosticSessionScenarioStartupSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
                + "\n" + ReadDiagnosticSessionCleanupActionsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
                + "\n" + ReadDiagnosticSessionResultBuilderSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
                + "\n" + ReadDiagnosticSessionBackgroundTasksSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunState.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionLiveStateWriter.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
                + "\n" + ReadDiagnosticSessionFlashbackCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportsSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackMetricsSource()
                + "\n" + ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackRejectedExportsSource()
                + "\n" + ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSegmentsSource()
                + "\n" + ReadDiagnosticSessionFlashbackStressScenarioSource()
                + "\n" + ReadDiagnosticSessionFlashbackValidationSource()
                + "\n" + ReadDiagnosticSessionFlashbackWaitsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthTolerances.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionAutomationResponseJson.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
                + "\n" + ReadDiagnosticSessionMetricsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
                + "\n" + ReadDiagnosticSessionResultFormatterSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionSampler.cs")
                + "\n" + ReadDiagnosticSessionScenarioCatalogSource()
                + "\n" + ReadDiagnosticSessionScenarioPlanSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionOptionalTextFormatter.cs"),
            ReadDiagnosticSessionModelsSource(),
            ReadDiagnosticSessionScenarioCatalogSource());
    }

    private static DiagnosticSessionToolSurfaceSourceFamily ReadDiagnosticSessionToolSurfaceSourceFamily()
    {
        return new DiagnosticSessionToolSurfaceSourceFamily(
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/SsctlHelpWriter.cs")
                + "\n" + ReadNormalizedRepoFile("tools/ssctl/SsctlHelpWriter.Sections.cs"),
            (ReadRepoFile("tools/ssctl/CommandHandlers.cs")
                + "\n" + ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs")
                + "\n" + ReadRepoFile("tools/ssctl/CommandHandlers.DiagnosticSession.cs"))
                .Replace("\r\n", "\n"),
            ReadNormalizedRepoFile("tools/McpServer/Tools/DiagnosticSessionTools.cs"));
    }

    private static string ReadDiagnosticSessionFlashbackValidationSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs");
    }

    private static string ReadDiagnosticSessionScenarioCatalogSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.cs");
    }

    private static string ReadDiagnosticSessionScenarioPlanSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs");
    }

    private static string ReadDiagnosticSessionFlashbackExportsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs");
    }

    private static string ReadDiagnosticSessionFlashbackRejectedExportsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs");
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
        string InitializedSessionText,
        string ReadLoopText,
        string FrameDeliveryText,
        string RawFrameDeliveryText,
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

    private readonly record struct AutomationDiagnosticsHubCountersSourceFamily(
        string RealtimePreviewText,
        string SourceFamilyText);
}
