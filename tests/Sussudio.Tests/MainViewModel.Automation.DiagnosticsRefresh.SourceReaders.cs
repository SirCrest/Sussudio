static partial class Program
{
    private static AutomationDiagnosticsHubCountersSourceFamily ReadAutomationDiagnosticsHubCountersSource()
    {
        var realtimePreviewText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs");
        var mjpegText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.Mjpeg.cs");
        var flashbackRecordingText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.FlashbackRecording.cs");

        return new AutomationDiagnosticsHubCountersSourceFamily(
            realtimePreviewText,
            mjpegText,
            flashbackRecordingText,
            string.Join(
                "\n",
                new[]
                {
                    realtimePreviewText,
                    mjpegText,
                    flashbackRecordingText,
                }));
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
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportBackendSnapshot.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRangeResolution.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportForceRotate.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRequestPreparation.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportProgress.cs")
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
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.Rollback.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Producers.cs")
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
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
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
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionLiveStateWriter.Sampling.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunBootstrap.cs")
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
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.InitialSnapshot.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.LiveState.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.Lifetime.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs")
                + "\n" + ReadDiagnosticSessionMetricsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.RawSending.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs")
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
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Recording.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Playback.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs");
    }

    private static string ReadDiagnosticSessionScenarioCatalogSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Names.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Requirements.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.Core.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackPlayback.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackExport.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackRecording.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.Combined.cs");
    }

    private static string ReadDiagnosticSessionScenarioPlanSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioPlan.Policies.cs");
    }

    private static string ReadDiagnosticSessionFlashbackExportsSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
            + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackExports.AudioSwitch.cs");
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
        string MjpegText,
        string FlashbackRecordingText,
        string SourceFamilyText);
}
