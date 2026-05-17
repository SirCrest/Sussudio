using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task CaptureService_FlashbackLifecycleLogs_UseOutcomeNames()
    {
        var flashbackTexts = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("FLASHBACK_", StringComparison.Ordinal))
            .Select(path => File.ReadAllText(path).Replace("\r\n", "\n"))
            .ToArray();
        var captureServiceText = ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackSettingsControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
                .Replace("\r\n", "\n");
        var flashbackBackendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");
        var flashbackText = string.Join("\n", flashbackTexts);

        AssertNoRegex(
            flashbackText,
            @"""FLASHBACK_[^""]*_(BEGIN|DONE|END)\b",
            "Flashback lifecycle scaffold log tokens");

        foreach (var expectedToken in new[]
        {
            "FLASHBACK_RESTART_OK",
            "FLASHBACK_FORMAT_CHANGE_OK",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_OK",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP",
            "FLASHBACK_RECORDING_EXPORT_OK",
            "FLASHBACK_RECORDING_EXPORT_FAIL",
            "FLASHBACK_UNIFIED_RECORDING_STOP_OK",
            "FLASHBACK_UNIFIED_RECORDING_STOP_FAIL",
            "FLASHBACK_PREVIEW_INIT_OK",
            "FLASHBACK_PREVIEW_INIT_CANCELLED",
            "FLASHBACK_PREVIEW_DISPOSE_OK",
            "FLASHBACK_BUFFER_CYCLE_OK",
            "FLASHBACK_RECORDING_ACTIVE",
            "FLASHBACK_RECORDING_READY",
            "FLASHBACK_EXPORT_OK",
            "FLASHBACK_EXPORT_SEGMENT_OK",
            "FLASHBACK_EXPORT_SEGMENTS_OK",
            "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN",
            "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN",
            "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_PLAYBACK_DISPOSE_REQUEST"
        })
        {
            AssertContains(flashbackText, expectedToken);
        }

        var encoderSettingsChange = ExtractTextBetween(
            captureServiceText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "private void DisposeCoordinationLocksBestEffort");
        AssertContains(encoderSettingsChange, "var cycleFailed = false;");
        AssertContains(encoderSettingsChange, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(encoderSettingsChange, "cycleFailed = true;");
        AssertContains(encoderSettingsChange, "if (!cycleFailed)");
        AssertContains(encoderSettingsChange, "_currentSettings = previousSettings;");
        AssertContains(encoderSettingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertContains(encoderSettingsChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(encoderSettingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED");
        AssertContains(encoderSettingsChange, "string? splitEncodeMode = null");
        AssertContains(encoderSettingsChange, "_currentSettings.SplitEncodeMode = parsedSplitMode;");
        AssertContains(
            encoderSettingsChange,
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_FAIL quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode} type={ex.GetType().Name} error='{ex.Message}'");

        var formatChange = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateRecordingFormatAsync",
            "    public Task CycleFlashbackEncoderSettingsAsync");
        AssertContains(formatChange, "var cycleFailed = false;");
        AssertContains(formatChange, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(formatChange, "cycleFailed = true;");
        AssertContains(formatChange, "if (!cycleFailed)");
        AssertContains(formatChange, "_currentSettings = previousSettings;");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertContains(formatChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");

        var cycleBuffer = ExtractTextBetween(
            captureServiceText,
            "private async Task CycleFlashbackBufferAsync",
            "    private void OnFlashbackFrameEncoded");
        var backendCycleBuffer = ExtractTextBetween(
            flashbackBackendResourcesText,
            "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync",
            "    private async Task RollBackPreviewBackendStartAsync");
        AssertContains(cycleBuffer, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(cycleBuffer, "exportOperationLockAlreadyHeld: true");
        AssertContains(cycleBuffer, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");
        AssertContains(backendCycleBuffer, "var preservedInPoint = !request.PurgeSegments ? oldPlaybackController?.InPoint : null;");
        AssertContains(backendCycleBuffer, "var preservedOutPoint = !request.PurgeSegments ? oldPlaybackController?.OutPoint : null;");
        AssertContains(backendCycleBuffer, "var preservedInPointFilePts = !request.PurgeSegments ? oldPlaybackController?.InPointFilePts : null;");
        AssertContains(backendCycleBuffer, "var preservedOutPointFilePts = !request.PurgeSegments ? oldPlaybackController?.OutPointFilePts : null;");
        AssertDoesNotContain(backendCycleBuffer, "var preservedInPoint = oldPlaybackController?.InPoint;");
        AssertDoesNotContain(backendCycleBuffer, "var preservedOutPoint = oldPlaybackController?.OutPoint;");
        AssertContains(backendCycleBuffer, "playbackController.RestoreInOutPoints(");
        AssertContains(backendCycleBuffer, "preservedInPoint,");
        AssertContains(backendCycleBuffer, "preservedOutPoint,");
        AssertContains(backendCycleBuffer, "preservedInPointFilePts,");
        AssertContains(backendCycleBuffer, "preservedOutPointFilePts);");
        var ensureFlashbackPreviewBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task EnsureFlashbackPreviewBackendAsync",
            "private async Task DisposeFlashbackPreviewBackendAsync");
        var createFlashbackSessionContext = ExtractTextBetween(
            captureServiceText,
            "private FlashbackSessionContext CreateFlashbackSessionContext",
            "    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts");
        AssertContains(createFlashbackSessionContext, "var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);");
        AssertContains(createFlashbackSessionContext, "frameRate = frameRateParts.EffectiveFrameRate;");
        AssertContains(createFlashbackSessionContext, "FrameRateNumerator = fpsNum");
        AssertContains(captureServiceText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertContains(captureServiceText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferFlashbackSessionFrameRateParts(double deliveryFrameRate)");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_REJECT");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_INFER");
        AssertContains(captureServiceText, "deltaFps > toleranceFps");
        AssertContains(createFlashbackSessionContext, "RecordingFormat.Av1Mp4 => \"av1_nvenc\"");
        AssertContains(createFlashbackSessionContext, "AV1 recording requires the av1_nvenc encoder");
        AssertDoesNotContain(createFlashbackSessionContext, "UseTransportStreamFlashbackCodec");
        AssertContains(captureServiceText, "settings.Format == RecordingFormat.Av1Mp4");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(captureServiceText, "forceRotateResult.Status == FlashbackForceRotateStatus.Failed");
        AssertContains(captureServiceText, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertDoesNotContain(
            ExtractTextBetween(
                captureServiceText,
                "if (segmentPaths.Count == 0)",
                "// Fallback: single-file export if no segments available"),
            "force_rotate_failed");
        AssertDoesNotContain(captureServiceText, "? RecordingFormat.HevcMp4.ToString()");
        AssertContains(createFlashbackSessionContext, "var flashbackNvencPreset = settings.NvencPreset;");
        AssertContains(createFlashbackSessionContext, "NvencPreset = flashbackNvencPreset");
        AssertContains(createFlashbackSessionContext, "SplitEncodeMode = SplitEncodeModeParser.ToWireString(settings.SplitEncodeMode)");
        // Flashback must honor user codec/preset settings directly. The legacy snapshot
        // field remains for compatibility, but the old silent AV1->HEVC path must stay gone.
        AssertDoesNotContain(createFlashbackSessionContext, "FLASHBACK_CODEC_DOWNGRADE");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackCodecDowngradeReason(");
        AssertContains(captureServiceText, "=> null;");
        AssertDoesNotContain(captureServiceText, "AV1->HEVC: software MJPEG pipeline at");
        AssertDoesNotContain(captureServiceText, "NVENC preset '");
        // Snapshot field remains populated from the compatibility resolver so
        // downstream consumers share the same no-downgrade contract.
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotsText, "FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(requestedSettings, unifiedVideoCapture),");
        var contractsText = ReadAutomationSnapshotFamilyText();
        AssertContains(contractsText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertContains(contractsText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        var automationDiagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
                .Replace("\r\n", "\n");
        AssertContains(automationDiagnosticsHubText, "FlashbackExportVerificationFormat = flashbackRecording.ExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(automationDiagnosticsHubText, "FlashbackCodecDowngradeReason = flashbackRecording.CodecDowngradeReason,");
        AssertContains(automationDiagnosticsHubText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,");
        AssertDoesNotContain(captureServiceText, "var fbFileNameFormatOverride =");
        AssertDoesNotContain(captureServiceText, "FileNameFormatOverride = fbFileNameFormatOverride");
        AssertContains(ensureFlashbackPreviewBackend, "var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_CANCELLED");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_FAIL");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN");
        AssertContains(backendCycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(backendCycleBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(backendCycleBuffer, "await oldSink.StopAsync(committedCycleToken)");
        AssertContains(backendCycleBuffer, "await newSink.StartAsync(");
        AssertContains(backendCycleBuffer, "request.CreateSessionContext(),");
        AssertContains(backendCycleBuffer, "committedCycleToken,");
        AssertContains(backendCycleBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertOccursBefore(
            backendCycleBuffer,
            "await oldSink.DisposeAsync().ConfigureAwait(false);",
            "ClearSinkAndSettings();");

        return Task.CompletedTask;
    }
}
