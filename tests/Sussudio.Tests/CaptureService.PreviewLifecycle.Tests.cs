using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceAudioSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelPreviewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs")
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
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    internal static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
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

    internal static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_PreviewLifecycleLivesInFocusedPartials()
    {
        var startText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStart.cs").Replace("\r\n", "\n");
        var audioGraphText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");
        var stopText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStop.cs").Replace("\r\n", "\n");
        var videoPipelineLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs").Replace("\r\n", "\n");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CaptureVideoPipelineResources.cs").Replace("\r\n", "\n");
        var flashbackPreviewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs").Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"))
            .Replace("\r\n", "\n");
        var recordingRollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewLifecycle.cs")),
            "mixed preview lifecycle partial should stay removed");
        AssertContains(startText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(startText, "await RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "if (await TryStartPreviewFromRetainedPipelineAsync(settings, transitionToken).ConfigureAwait(false))");
        AssertContains(startText, "await StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "private async Task RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=settings_changed");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
        AssertContains(startText, "PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
        AssertContains(startText, "private async Task<bool> TryStartPreviewFromRetainedPipelineAsync(");
        AssertContains(startText, "FLASHBACK_FAST_PATH_FORMAT_MISMATCH");
        AssertContains(startText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken, \"preview_fast_path\")");
        AssertContains(startText, "private async Task StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)");
        AssertContains(startText, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startText, "private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)");
        AssertContains(startText, "private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)");
        AssertContains(startText, "private static CaptureSettings CloneCaptureSettings(CaptureSettings source)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.Recycle.cs")),
            "old preview-start recycle partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FastPath.cs")),
            "old preview-start fast-path partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FreshPipeline.cs")),
            "old preview-start fresh-pipeline partial removed");
        AssertContains(audioGraphText, "private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(");
        AssertContains(audioGraphText, "private async Task StartPreviewMicrophoneMonitorAsync(");
        AssertContains(audioGraphText, "private async Task RollbackPreviewAudioCaptureStartupAsync(");
        AssertContains(stopText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private async Task DisposePreviewPipelineAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewReuse.cs")),
            "preview reuse helper partial folded into preview start");
        AssertContains(videoPipelineResourcesText, "internal sealed class CaptureVideoPipelineResources");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture? Capture { get; set; }");
        AssertContains(videoPipelineResourcesText, "public IPreviewFrameSink? PreviewFrameSink { get; set; }");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture.MjpegPipelineTimingMetrics LastMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public ParallelMjpegDecodePipeline.PipelineTimingMetrics? LastFullMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public void CacheMjpegTimingMetrics(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public CaptureMjpegTimingSnapshot GetMjpegTimingSnapshot(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_CLEANUP_END");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureVideoPipelineResources _videoPipeline = new();");
        AssertDoesNotContain(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "_unifiedVideoCapture");
        AssertContains(videoPipelineLifecycleText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(videoPipelineLifecycleText, "private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)");
        AssertContains(videoPipelineLifecycleText, "private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertContains(videoPipelineLifecycleText, "private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertDoesNotContain(videoPipelineLifecycleText, "private IPreviewFrameSink? _previewFrameSink");
        AssertDoesNotContain(videoPipelineLifecycleText, "private Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertDoesNotContain(videoPipelineLifecycleText, "ThrowIfPendingLibAvDrainBlocksReentry");
        AssertDoesNotContain(videoPipelineLifecycleText, "PendingLibAvDrainTask");
        AssertContains(stopText, "_recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();");
        AssertContains(videoPipelineLifecycleText, "private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)");
        AssertContains(videoPipelineLifecycleText, "_videoPipeline.CacheMjpegTimingMetrics(unifiedVideoCapture);");
        AssertContains(cleanupText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(stopText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvFinalizeText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(recordingRollbackText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertDoesNotContain(videoPipelineLifecycleText, "private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(videoPipelineLifecycleText, "private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(flashbackPreviewBackendText, "ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewPipeline.cs")),
            "old preview pipeline partial removed after video lifecycle promotion");
        AssertDoesNotContain(startText, "private Task StopVideoPreviewCoreAsync(");
        AssertDoesNotContain(startText, "private async Task DisposePreviewPipelineAsync(");
        AssertDoesNotContain(startText, "private void AttachUnifiedVideoCapture(");
        AssertDoesNotContain(startText, "new WasapiAudioCapture()");
        AssertDoesNotContain(startText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(stopText, "public Task StartVideoPreviewAsync(");
        AssertDoesNotContain(stopText, "private bool CanReuseVideoCaptureForPreview(");
        AssertDoesNotContain(stopText, "private static CaptureSettings CloneCaptureSettings(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewDisposal.cs")),
            "old preview disposal partial removed");

        return Task.CompletedTask;
    }

    internal static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        SetPropertyOrBackingField(device, "AudioDeviceId", null);
        SetPropertyOrBackingField(device, "AudioDeviceName", null);
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        string? lastStatus = null;
        var handler = new EventHandler<string>((_, status) => lastStatus = status);
        var statusChanged = captureService.GetType().GetEvent("StatusChanged", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CaptureService.StatusChanged event not found.");
        statusChanged.AddEventHandler(captureService, handler);

        try
        {
            var startAudioPreview = captureService.GetType().GetMethod(
                "StartAudioPreviewAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null);
            if (startAudioPreview == null)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync method not found.");
            }

            if (startAudioPreview.Invoke(captureService, new object?[] { CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
            AssertEqual("Audio preview unavailable", lastStatus, "StatusChanged");
        }
        finally
        {
            statusChanged.RemoveEventHandler(captureService, handler);
            await DisposeAsync(captureService).ConfigureAwait(false);
        }
    }

    internal static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_previewAudioGraph.ProgramCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }
}
