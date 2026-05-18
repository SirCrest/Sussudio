using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    private static Task CaptureService_PreviewLifecycleLivesInFocusedPartials()
    {
        var startText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStart.cs").Replace("\r\n", "\n");
        var audioGraphText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");
        var stopText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStop.cs").Replace("\r\n", "\n");
        var reuseText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewReuse.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewDisposal.cs").Replace("\r\n", "\n");
        var videoPipelineLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs").Replace("\r\n", "\n");
        var deferredCleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewLifecycle.cs")),
            "mixed preview lifecycle partial should stay removed");
        AssertContains(startText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(startText, "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)");
        AssertContains(startText, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(audioGraphText, "private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(");
        AssertContains(audioGraphText, "private async Task StartPreviewMicrophoneMonitorAsync(");
        AssertContains(audioGraphText, "private async Task RollbackPreviewAudioCaptureStartupAsync(");
        AssertContains(stopText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)");
        AssertContains(reuseText, "private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)");
        AssertContains(reuseText, "private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)");
        AssertContains(reuseText, "private static CaptureSettings CloneCaptureSettings(CaptureSettings source)");
        AssertContains(disposalText, "private async Task DisposePreviewPipelineAsync(");
        AssertContains(videoPipelineLifecycleText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(videoPipelineLifecycleText, "private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)");
        AssertContains(videoPipelineLifecycleText, "private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertContains(videoPipelineLifecycleText, "private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertContains(videoPipelineLifecycleText, "private Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(videoPipelineLifecycleText, "private void ThrowIfPendingLibAvDrainTaskBlocksReentry()");
        AssertContains(videoPipelineLifecycleText, "private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)");
        AssertDoesNotContain(deferredCleanupText, "ScheduleDeferredUnifiedVideoCaptureCleanup");
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
        AssertDoesNotContain(stopText, "private static CaptureSettings CloneCaptureSettings(");
        AssertDoesNotContain(reuseText, "public Task StartVideoPreviewAsync(");
        AssertDoesNotContain(reuseText, "private async Task DisposePreviewPipelineAsync(");
        AssertDoesNotContain(disposalText, "public Task StartVideoPreviewAsync(");
        AssertDoesNotContain(disposalText, "private bool CanReuseVideoCaptureForPreview(");
        AssertDoesNotContain(disposalText, "private Task ScheduleDeferredUnifiedVideoCaptureCleanup(");

        return Task.CompletedTask;
    }

    private static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
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

    private static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_wasapiAudioCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }
}
