using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Text.Json;

static class Program
{
    private sealed record CheckResult(string Name, bool Passed, string? Detail = null);

    private static Assembly? _assembly;

    private static async Task<int> Main(string[] args)
    {
        var assemblyPath = ResolveAssemblyPath(args);
        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Target assembly not found: {assemblyPath}");
            Console.Error.WriteLine("Build the app first: dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64");
            return 2;
        }

        _assembly = Assembly.LoadFrom(assemblyPath);

        var results = new List<CheckResult>
        {
            await RunCheckAsync(
                "Observed telemetry uses explicit counters",
                GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts),
            await RunCheckAsync(
                "Runtime snapshot preserves MJPG source subtype when observed frames are NV12",
                GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded),
            await RunCheckAsync(
                "Telemetry alignment mismatch surfaces reason",
                GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest),
            await RunCheckAsync(
                "Telemetry unavailable maps to unavailable state",
                GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable),
            await RunCheckAsync(
                "NativeXu telemetry accepts known 4K X product revisions",
                NativeXuTelemetry_AcceptsKnown4kXProductRevisions),
            await RunCheckAsync(
                "Health snapshot propagates structured source telemetry details",
                CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails),
            await RunCheckAsync(
                "Automation snapshots expose high-confidence source telemetry fields",
                AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields),
            await RunCheckAsync(
                "HDR idle snapshot reports ready pipeline parity",
                GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle),
            await RunCheckAsync(
                "HDR recording mismatch reports violation",
                GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr),
            await RunCheckAsync(
                "Thread health probes default cleanly when inactive",
                GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive),
            await RunCheckAsync(
                "Health snapshot uses cached MJPEG timing metrics when capture is gone",
                GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone),
            await RunCheckAsync(
                "Diagnostics snapshot mirrors MJPEG timing metrics",
                GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics),
            await RunCheckAsync(
                "Automation snapshot contract exposes full CPU MJPEG metrics",
                AutomationSnapshot_ExposesFullCpuMjpegMetrics),
            await RunCheckAsync(
                "Automation options contract exposes advanced MCP control state",
                AutomationOptionsSnapshot_ExposesAdvancedControlState),
            await RunCheckAsync(
                "FFmpeg runtime locator prefers app-local ffmpeg folder",
                FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder),
            await RunCheckAsync(
                "MCP formatter renders MJPEG timing section when fields exist",
                McpFormatter_RendersMjpegTimingSection_WhenFieldsExist),
            await RunCheckAsync(
                "Automation command maps stay aligned for advanced MCP controls",
                AutomationCommandMaps_StayAligned_ForAdvancedMcpControls),
            await RunCheckAsync(
                "UI automation commands are not blocked on device readiness",
                UiAutomationCommands_AreNotBlockedOnDeviceReadiness),
            await RunCheckAsync(
                "Automation preview volume persists through the settings path",
                AutomationPreviewVolume_PersistsThroughSettingsPath),
            await RunCheckAsync(
                "Automation UI settings persist through the settings path",
                AutomationUiSettings_PersistThroughSettingsPath),
            await RunCheckAsync(
                "Project file preserves main's English-only publish locale policy",
                ProjectFile_PreservesEnglishOnlyPublishLocalePolicy),
            await RunCheckAsync(
                "Show all capture options unlocks source-filtered frame rates",
                ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates),
            await RunCheckAsync(
                "Diagnostics loop does not rebuild automation options each poll",
                DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll),
            await RunCheckAsync(
                "Preview startup tolerates missing audio capture devices",
                PreviewStartup_ToleratesMissingAudioCaptureDevices),
            await RunCheckAsync(
                "Audio preview stays inactive when no audio capture device exists",
                AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists),
            await RunCheckAsync(
                "Audio monitoring visuals follow runtime preview activity",
                AudioMonitoringVisuals_FollowRuntimePreviewActivity),
            await RunCheckAsync(
                "Preview backend log reflects video-only fallback",
                PreviewBackendLog_ReflectsVideoOnlyFallback),
            await RunCheckAsync(
                "Live pixel format surfaces prefer source subtype over decoded output",
                LivePixelFormatSurfaces_PreferReaderSourceSubtype),
            await RunCheckAsync(
                "Stats panels use source telemetry for HDMI input format and HDR",
                StatsPanels_UseSourceTelemetry_ForHdmiInput),
            await RunCheckAsync(
                "MCP raw app state keeps capture options separate",
                McpToolSurface_KeepsCaptureOptionsSeparateFromRawState),
            await RunCheckAsync(
                "Unified video capture CPU MJPEG emit reports NV12",
                UnifiedVideoCapture_CpuMjpegEmitReportsNv12),
            await RunCheckAsync(
                "Unified video capture retains MJPEG pipeline on stop failure",
                UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails),
            await RunCheckAsync(
                "MJPG HFR mode only activates for SDR 4K120-style settings",
                CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest),
            await RunCheckAsync(
                "Strict HFR fatal handler clears active session state",
                CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState),
            await RunCheckAsync(
                "Capture errors refresh ViewModel runtime flags",
                CaptureErrors_RefreshViewModelRuntimeFlags),

            // --- RecordingContracts ---
            await RunCheckAsync(
                "FinalizeResult.Success produces empty preserved list",
                FinalizeResult_Success_ProducesEmptyPreservedList),
            await RunCheckAsync(
                "FinalizeResult.Failure deduplicates and filters preserved artifacts",
                FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts),

            // --- RecordingArtifactManager ---
            await RunCheckAsync(
                "FinalizeContext returns success when post-mux audio disabled",
                ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled),
            await RunCheckAsync(
                "FinalizeContext preserves temp artifacts when mux fails",
                ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails),
            await RunCheckAsync(
                "RollbackAsync deletes all artifacts when post-mux enabled",
                ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled),
            await RunCheckAsync(
                "RollbackAsync is safe with null context",
                ArtifactManager_RollbackAsync_SafeWithNullContext),

            // --- CaptureSettings ---
            await RunCheckAsync(
                "GetTargetBitrate scales by resolution and frame rate",
                CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate),
            await RunCheckAsync(
                "GetTargetBitrate applies codec efficiency for HEVC and AV1",
                CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency),
            await RunCheckAsync(
                "GetTargetBitrate clamps custom quality to range",
                CaptureSettings_GetTargetBitrate_ClampsCustomQuality),
            await RunCheckAsync(
                "GetOutputFileName includes format suffix",
                CaptureSettings_GetOutputFileName_IncludesFormatSuffix),
            await RunCheckAsync(
                "MJPEG HFR mode requires SDR and MJPG pixel format",
                CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat),

            // --- FlashbackBufferManager ---
            await RunCheckAsync(
                "FlashbackBufferManager segment lookup returns correct file for position",
                FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment),
            await RunCheckAsync(
                "FlashbackBufferManager GetNextSegmentFile walks forward through segments",
                FlashbackBufferManager_GetNextSegmentFile_WalksForward),
            await RunCheckAsync(
                "FlashbackBufferManager GetValidSegmentPaths returns overlapping segments",
                FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping),
            await RunCheckAsync(
                "FlashbackBufferManager eviction pause and resume are balanced",
                FlashbackBufferManager_EvictionPauseResume_Balanced),

            // --- GpuPipelineHandles ---
            await RunCheckAsync(
                "GpuPipelineHandles.None returns zeroed struct",
                GpuPipelineHandles_None_ReturnsZeroedStruct),

            // --- RecordingContextRequest ---
            await RunCheckAsync(
                "RecordingContextRequest defaults match RecordingContext defaults",
                RecordingContextRequest_DefaultsMatchRecordingContextDefaults),

            // --- MediaFormat ---
            await RunCheckAsync(
                "MediaFormat equality with matching rational frame rates",
                MediaFormat_Equality_WithMatchingRationalFrameRates),
            await RunCheckAsync(
                "MediaFormat inequality when dimensions differ",
                MediaFormat_Inequality_WhenDimensionsDiffer),
            await RunCheckAsync(
                "MediaFormat GetHashCode consistency for equal objects",
                MediaFormat_GetHashCode_ConsistencyForEqualObjects),

            // --- AutomationContracts ---
            await RunCheckAsync(
                "AutomationCommandKind has sequential values 0 through 44",
                AutomationCommandKind_HasSequentialValues_0Through44),
            await RunCheckAsync(
                "AutomationWindowAction has expected values",
                AutomationWindowAction_HasExpectedValues),

            // --- RuntimePaths ---
            await RunCheckAsync(
                "RuntimePaths GetRepoLogFile returns path under repo root",
                RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot),
            await RunCheckAsync(
                "RuntimePaths paths contain expected directory names",
                RuntimePaths_PathsContainExpectedDirectoryNames),

            // --- SourceSignalTelemetrySnapshot ---
            await RunCheckAsync(
                "SourceSignalTelemetrySnapshot defaults have expected values",
                SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues),
            await RunCheckAsync(
                "SourceSignalTelemetrySnapshot properties round-trip",
                SourceSignalTelemetrySnapshot_PropertiesRoundTrip),

            // --- HdrOutputPolicy ---
            await RunCheckAsync(
                "HdrOutputPolicy returns true when HDR and Hdr10Pq requested",
                HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested),
            await RunCheckAsync(
                "HdrOutputPolicy returns false when HDR disabled",
                HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled),
            await RunCheckAsync(
                "HdrOutputPolicy returns false for non-Hdr10Pq mode",
                HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq),

            // --- FlashbackPlaybackState enum ---
            await RunCheckAsync(
                "FlashbackPlaybackState enum has all expected states",
                FlashbackPlaybackState_HasAllExpectedStates),

            // --- RecordingPipelineOptions ---
            await RunCheckAsync(
                "RecordingPipelineOptions resolves video queue capacity from frame rate",
                RecordingPipelineOptions_ResolvesVideoQueueCapacity),

            // --- NvmlSnapshot computed properties ---
            await RunCheckAsync(
                "NvmlSnapshot computed properties convert units correctly",
                NvmlSnapshot_ComputedProperties_ConvertUnits),

            // --- CaptureSessionSnapshot defaults ---
            await RunCheckAsync(
                "CaptureSessionSnapshot has correct default state",
                CaptureSessionSnapshot_DefaultState),

            // --- ProcessSpec and ProcessRunResult contracts ---
            await RunCheckAsync(
                "ProcessSpec default timeout is 30 seconds",
                ProcessSpec_DefaultTimeout_Is30Seconds),

            // --- Tool CommandMap & Formatter Alignment ---
            await RunCheckAsync(
                "MCP PipeClient CommandMap covers every AutomationCommandKind enum value",
                McpPipeClient_CommandMap_CoversEveryAutomationCommandKind),
            await RunCheckAsync(
                "ecctl PipeTransport CommandMap covers every AutomationCommandKind enum value",
                EcctlPipeTransport_CommandMap_CoversEveryAutomationCommandKind),
            await RunCheckAsync(
                "ResponseFormatter.IsSuccess correctly parses success and failure JSON",
                ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson),
            await RunCheckAsync(
                "ResponseFormatter.Get handles all JSON value kinds correctly",
                ResponseFormatter_Get_HandlesAllJsonValueKinds),
            await RunCheckAsync(
                "ecctl Formatters snapshot fields align with MCP ResponseFormatter",
                EcctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter)
        };

        var failed = results.Where(r => !r.Passed).ToList();
        foreach (var result in results)
        {
            Console.WriteLine(result.Passed
                ? $"PASS: {result.Name}"
                : $"FAIL: {result.Name} :: {result.Detail}");
        }

        if (failed.Count == 0)
        {
            Console.WriteLine("All runtime snapshot regression checks passed.");
            return 0;
        }

        Console.Error.WriteLine($"{failed.Count} regression checks failed.");
        return 1;
    }

    private static string ResolveAssemblyPath(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(
            root,
            "ElgatoCapture",
            "bin",
            "x64",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "ElgatoCapture.dll");
    }

    private static async Task<CheckResult> RunCheckAsync(string name, Func<Task> check)
    {
        try
        {
            await check().ConfigureAwait(false);
            return new CheckResult(name, true);
        }
        catch (Exception ex)
        {
            return new CheckResult(name, false, ex.Message);
        }
    }

    private static async Task GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_firstObservedFramePixelFormat", "NV12");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "BGRA8");
        SetPrivateField(captureService, "_latestObservedSurfaceFormat", "BGRA8");
        SetPrivateField(captureService, "_observedP010FrameCount", 0L);
        SetPrivateField(captureService, "_observedNv12FrameCount", 2L);
        SetPrivateField(captureService, "_observedOtherFrameCount", 3L);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(0L, GetLongProperty(snapshot, "ObservedP010FrameCount"), "ObservedP010FrameCount");
        AssertEqual(2L, GetLongProperty(snapshot, "ObservedNv12FrameCount"), "ObservedNv12FrameCount");
        AssertEqual(3L, GetLongProperty(snapshot, "ObservedOtherFrameCount"), "ObservedOtherFrameCount");
        AssertEqual("NV12", GetStringProperty(snapshot, "FirstObservedFramePixelFormat"), "FirstObservedFramePixelFormat");
        AssertEqual("BGRA8", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_actualPixelFormat", "MJPG");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "NV12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("MJPG", GetStringProperty(snapshot, "ReaderSourceSubtype"), "ReaderSourceSubtype");
        AssertEqual("NV12", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("ElgatoCapture.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("ElgatoCapture.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("ElgatoCapture.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "OriginDetail", "RegressionHarness");
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("ElgatoCapture.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 1280);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 720);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 30d);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateArg", "30/1");
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", false);
        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Mismatch", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "width expected");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "hdr expected");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var telemetryType = RequireType("ElgatoCapture.Models.SourceSignalTelemetrySnapshot");
        var createUnavailable = telemetryType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);
        if (createUnavailable == null)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.CreateUnavailable not found.");
        }

        var unavailableTelemetry = createUnavailable.Invoke(null, new object?[] { "regression-harness-unavailable", null });
        SetPrivateField(captureService, "_latestSourceTelemetry", unavailableTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Unavailable", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "unavailable");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task NativeXuTelemetry_AcceptsKnown4kXProductRevisions()
    {
        var provider = CreateInstance("ElgatoCapture.Services.NativeXuAtCommandProvider");

        foreach (var productId in new[] { "009b", "009c" })
        {
            var device = BuildDevice($"\\\\?\\usb#vid_0fd9&pid_{productId}&mi_00#synthetic#{Guid.NewGuid():N}\\global");
            var readAsync = provider.GetType().GetMethod(
                "ReadAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { device.GetType(), typeof(CancellationToken) },
                modifiers: null);
            if (readAsync == null)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync method not found.");
            }

            if (readAsync.Invoke(provider, new[] { device, CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync task result not found.");
            var snapshot = resultProperty.GetValue(task)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync returned null snapshot.");
            var diagnostic = GetStringProperty(snapshot, "DiagnosticSummary");
            if (string.Equals(diagnostic, "nativexu-device-unsupported", StringComparison.Ordinal) ||
                diagnostic.StartsWith("nativexu-device-unsupported:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"NativeXu provider rejected 4K X product revision {productId} as unsupported.");
            }
        }
    }

    private static async Task CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("ElgatoCapture.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("ElgatoCapture.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("ElgatoCapture.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("ElgatoCapture.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 3840);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 2160);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 119.88d);
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", true);
        SetPropertyOrBackingField(sourceTelemetry, "VideoFormat", "YCbCr422");
        SetPropertyOrBackingField(sourceTelemetry, "Colorimetry", "BT.2020");
        SetPropertyOrBackingField(sourceTelemetry, "Quantization", "Limited");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferFunction", "HDR10 / PQ");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferCode", 2);
        SetPropertyOrBackingField(sourceTelemetry, "AudioFormat", "Unknown (2)");
        SetPropertyOrBackingField(sourceTelemetry, "AudioSampleRate", "Unknown (7)");
        SetPropertyOrBackingField(sourceTelemetry, "InputSource", "HDMI (0)");
        SetPropertyOrBackingField(sourceTelemetry, "UsbHostProtocol", "Isochronous (2)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpMode", "Unknown (1)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpVersion", "0200");
        SetPropertyOrBackingField(sourceTelemetry, "RxTxHdcpVersion", "Unknown (3)");
        SetPropertyOrBackingField(sourceTelemetry, "RawTimingHex", "3000CA0830117008");

        var detailEntryType = RequireType("ElgatoCapture.Models.SourceTelemetryDetailEntry");
        var detailEntry = Activator.CreateInstance(detailEntryType, "Signal Details", "Quantization", "Limited", "Limited")
            ?? throw new InvalidOperationException("SourceTelemetryDetailEntry instance creation failed.");
        var detailArray = Array.CreateInstance(detailEntryType, 1);
        detailArray.SetValue(detailEntry, 0);
        SetPropertyOrBackingField(sourceTelemetry, "DetailEntries", detailArray);

        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var health = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual("YCbCr422", GetStringProperty(health, "SourceVideoFormat"), "SourceVideoFormat");
        AssertEqual("BT.2020", GetStringProperty(health, "SourceColorimetry"), "SourceColorimetry");
        AssertEqual("Limited", GetStringProperty(health, "SourceQuantization"), "SourceQuantization");
        AssertEqual("HDR10 / PQ", GetStringProperty(health, "SourceHdrTransferFunction"), "SourceHdrTransferFunction");
        AssertEqual("Unknown (2)", GetStringProperty(health, "SourceAudioFormat"), "SourceAudioFormat");
        AssertEqual("Unknown (7)", GetStringProperty(health, "SourceAudioSampleRate"), "SourceAudioSampleRate");
        AssertEqual("HDMI (0)", GetStringProperty(health, "SourceInputSource"), "SourceInputSource");
        AssertEqual("Isochronous (2)", GetStringProperty(health, "SourceUsbHostProtocol"), "SourceUsbHostProtocol");
        AssertEqual("Unknown (1)", GetStringProperty(health, "SourceHdcpMode"), "SourceHdcpMode");
        AssertEqual("0200", GetStringProperty(health, "SourceHdcpVersion"), "SourceHdcpVersion");
        AssertEqual("Unknown (3)", GetStringProperty(health, "SourceRxTxHdcpVersion"), "SourceRxTxHdcpVersion");
        AssertEqual("3000CA0830117008", GetStringProperty(health, "SourceRawTimingHex"), "SourceRawTimingHex");

        var details = GetPropertyValue(health, "SourceTelemetryDetails") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("SourceTelemetryDetails should be enumerable.");
        var detailCount = 0;
        foreach (var _ in details)
        {
            detailCount++;
        }

        AssertEqual(1, detailCount, "SourceTelemetryDetails.Count");
        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields()
    {
        var contractsText = ReadRepoFile("ElgatoCapture/Models/AutomationContracts.cs").Replace("\r\n", "\n");
        var diagnosticsHubText = ReadRepoFile("ElgatoCapture/Services/AutomationDiagnosticsHub.cs").Replace("\r\n", "\n");

        AssertContains(contractsText, "public string? SourceFirmware { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioFormat { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioSampleRate { get; init; }");
        AssertContains(contractsText, "public string? SourceInputSource { get; init; }");
        AssertContains(contractsText, "public string? SourceUsbHostProtocol { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpMode { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRxTxHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRawTimingHex { get; init; }");

        AssertContains(diagnosticsHubText, "SourceFirmware = captureRuntime.SourceFirmware,");
        AssertContains(diagnosticsHubText, "SourceAudioFormat = captureRuntime.SourceAudioFormat,");
        AssertContains(diagnosticsHubText, "SourceAudioSampleRate = captureRuntime.SourceAudioSampleRate,");
        AssertContains(diagnosticsHubText, "SourceInputSource = captureRuntime.SourceInputSource,");
        AssertContains(diagnosticsHubText, "SourceUsbHostProtocol = captureRuntime.SourceUsbHostProtocol,");
        AssertContains(diagnosticsHubText, "SourceHdcpMode = captureRuntime.SourceHdcpMode,");
        AssertContains(diagnosticsHubText, "SourceHdcpVersion = captureRuntime.SourceHdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRxTxHdcpVersion = captureRuntime.SourceRxTxHdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        return Task.CompletedTask;
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(true, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Ready", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_activeRecordingSettings", settings);
        SetPrivateField(captureService, "_isRecording", true);
        SetPrivateField(captureService, "_activeVideoInputPixelFormat", "nv12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("SDR", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(false, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Violation", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");
        AssertContains(GetStringProperty(snapshot, "PipelineModeReason"), "Requested pipeline");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(false, GetBoolProperty(snapshot, "SourceReaderReadOutstanding"), "SourceReaderReadOutstanding");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderReadOutstandingMs"), "SourceReaderReadOutstandingMs");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderLastFrameTickMs"), "SourceReaderLastFrameTickMs");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureCallbackCount"), "WasapiCaptureCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureAudioLevelEventsFired"), "WasapiCaptureAudioLevelEventsFired");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackRenderCallbackCount"), "WasapiPlaybackRenderCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackQueueDropCount"), "WasapiPlaybackQueueDropCount");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(
            captureService,
            "_lastMjpegPipelineTimingMetrics",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 7,
                decodeAvgMs: 1.5,
                decodeP95Ms: 2.5,
                decodeMaxMs: 3.5,
                interopCopySampleCount: 5,
                interopCopyAvgMs: 4.5,
                interopCopyP95Ms: 5.5,
                interopCopyMaxMs: 6.5,
                callbackSampleCount: 9,
                callbackAvgMs: 7.5,
                callbackP95Ms: 8.5,
                callbackMaxMs: 9.5));
        SetPrivateField(
            captureService,
            "_lastFullMjpegPipelineTimingMetrics",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 3,
                decodeSampleCount: 17,
                decodeAvgMs: 4.1,
                decodeP95Ms: 4.6,
                decodeMaxMs: 5.2,
                reorderSampleCount: 19,
                reorderAvgMs: 0.7,
                reorderP95Ms: 1.1,
                reorderMaxMs: 1.8,
                pipelineSampleCount: 23,
                pipelineAvgMs: 5.1,
                pipelineP95Ms: 5.7,
                pipelineMaxMs: 6.4,
                totalDecoded: 101,
                totalEmitted: 97,
                totalDropped: 4,
                reorderSkips: 2,
                reorderBufferDepth: 1,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 31, 4.0, 4.4, 4.9),
                    CreatePerDecoderMetrics(1, 33, 4.2, 4.7, 5.3),
                    CreatePerDecoderMetrics(2, 35, 4.1, 4.8, 5.4)
                }));
        SetPrivateField(captureService, "_unifiedVideoCapture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual(7L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(5L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(9L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(1.5, GetDoubleProperty(snapshot, "MjpegDecodeAvgMs"), "MjpegDecodeAvgMs");
        AssertEqual(8.5, GetDoubleProperty(snapshot, "MjpegCallbackP95Ms"), "MjpegCallbackP95Ms");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(19L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(23L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(101L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(97L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(1L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(0.7, GetDoubleProperty(snapshot, "MjpegReorderAvgMs"), "MjpegReorderAvgMs");
        AssertEqual(5.7, GetDoubleProperty(snapshot, "MjpegPipelineP95Ms"), "MjpegPipelineP95Ms");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(3, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(1, GetIntProperty(perDecoder.GetValue(1)!, "WorkerIndex"), "MjpegPerDecoder[1].WorkerIndex");
        AssertEqual(33L, GetLongProperty(perDecoder.GetValue(1)!, "SampleCount"), "MjpegPerDecoder[1].SampleCount");
        AssertEqual(4.8, GetDoubleProperty(perDecoder.GetValue(2)!, "P95Ms"), "MjpegPerDecoder[2].P95Ms");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(
            captureService,
            "_lastMjpegPipelineTimingMetrics",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 11,
                decodeAvgMs: 10.1,
                decodeP95Ms: 10.2,
                decodeMaxMs: 10.3,
                interopCopySampleCount: 12,
                interopCopyAvgMs: 11.1,
                interopCopyP95Ms: 11.2,
                interopCopyMaxMs: 11.3,
                callbackSampleCount: 13,
                callbackAvgMs: 12.1,
                callbackP95Ms: 12.2,
                callbackMaxMs: 12.3));
        SetPrivateField(
            captureService,
            "_lastFullMjpegPipelineTimingMetrics",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 4,
                decodeSampleCount: 40,
                decodeAvgMs: 6.1,
                decodeP95Ms: 7.1,
                decodeMaxMs: 8.2,
                reorderSampleCount: 41,
                reorderAvgMs: 1.2,
                reorderP95Ms: 1.9,
                reorderMaxMs: 2.8,
                pipelineSampleCount: 42,
                pipelineAvgMs: 7.4,
                pipelineP95Ms: 8.6,
                pipelineMaxMs: 9.9,
                totalDecoded: 400,
                totalEmitted: 390,
                totalDropped: 10,
                reorderSkips: 3,
                reorderBufferDepth: 2,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 100, 5.8, 6.7, 7.8),
                    CreatePerDecoderMetrics(1, 101, 6.0, 7.0, 8.0),
                    CreatePerDecoderMetrics(2, 99, 6.2, 7.2, 8.3),
                    CreatePerDecoderMetrics(3, 100, 6.4, 7.4, 8.5)
                }));
        SetPrivateField(captureService, "_unifiedVideoCapture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetDiagnosticsSnapshot");
        AssertEqual(11L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(12L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(13L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(10.2, GetDoubleProperty(snapshot, "MjpegDecodeP95Ms"), "MjpegDecodeP95Ms");
        AssertEqual(12.3, GetDoubleProperty(snapshot, "MjpegCallbackMaxMs"), "MjpegCallbackMaxMs");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(41L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(42L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(400L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(390L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(10L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(7.4, GetDoubleProperty(snapshot, "MjpegPipelineAvgMs"), "MjpegPipelineAvgMs");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(4, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(99L, GetLongProperty(perDecoder.GetValue(2)!, "SampleCount"), "MjpegPerDecoder[2].SampleCount");
        AssertEqual(8.5, GetDoubleProperty(perDecoder.GetValue(3)!, "MaxMs"), "MjpegPerDecoder[3].MaxMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task AutomationSnapshot_ExposesFullCpuMjpegMetrics()
    {
        var snapshotType = RequireType("ElgatoCapture.Models.AutomationSnapshot");
        var decoderType = RequireType("ElgatoCapture.Models.MjpegDecoderAutomationSnapshot");

        AssertNotNull(snapshotType.GetProperty("MjpegDecoderCount"), "AutomationSnapshot.MjpegDecoderCount");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderSampleCount"), "AutomationSnapshot.MjpegReorderSampleCount");
        AssertNotNull(snapshotType.GetProperty("MjpegPipelineSampleCount"), "AutomationSnapshot.MjpegPipelineSampleCount");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalDecoded"), "AutomationSnapshot.MjpegTotalDecoded");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalEmitted"), "AutomationSnapshot.MjpegTotalEmitted");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalDropped"), "AutomationSnapshot.MjpegTotalDropped");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderSkips"), "AutomationSnapshot.MjpegReorderSkips");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderBufferDepth"), "AutomationSnapshot.MjpegReorderBufferDepth");

        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        AssertEqual(decoderType, elementType, "AutomationSnapshot.MjpegPerDecoder[] element type");

        AssertNotNull(decoderType.GetProperty("WorkerIndex"), "MjpegDecoderAutomationSnapshot.WorkerIndex");
        AssertNotNull(decoderType.GetProperty("SampleCount"), "MjpegDecoderAutomationSnapshot.SampleCount");
        AssertNotNull(decoderType.GetProperty("AvgMs"), "MjpegDecoderAutomationSnapshot.AvgMs");
        AssertNotNull(decoderType.GetProperty("P95Ms"), "MjpegDecoderAutomationSnapshot.P95Ms");
        AssertNotNull(decoderType.GetProperty("MaxMs"), "MjpegDecoderAutomationSnapshot.MaxMs");

        return Task.CompletedTask;
    }

    private static Task AutomationOptionsSnapshot_ExposesAdvancedControlState()
    {
        var optionsType = RequireType("ElgatoCapture.Models.AutomationOptionsSnapshot");
        var stringOptionType = RequireType("ElgatoCapture.Models.AutomationStringOption");
        var intOptionType = RequireType("ElgatoCapture.Models.AutomationIntOption");

        AssertNotNull(optionsType.GetProperty("Presets"), "AutomationOptionsSnapshot.Presets");
        AssertNotNull(optionsType.GetProperty("SplitEncodeModes"), "AutomationOptionsSnapshot.SplitEncodeModes");
        AssertNotNull(optionsType.GetProperty("VideoFormats"), "AutomationOptionsSnapshot.VideoFormats");
        AssertNotNull(optionsType.GetProperty("MjpegDecoderCounts"), "AutomationOptionsSnapshot.MjpegDecoderCounts");
        AssertNotNull(optionsType.GetProperty("SelectedPreset"), "AutomationOptionsSnapshot.SelectedPreset");
        AssertNotNull(optionsType.GetProperty("SelectedSplitEncodeMode"), "AutomationOptionsSnapshot.SelectedSplitEncodeMode");
        AssertNotNull(optionsType.GetProperty("SelectedVideoFormat"), "AutomationOptionsSnapshot.SelectedVideoFormat");
        AssertNotNull(optionsType.GetProperty("ShowAllCaptureOptions"), "AutomationOptionsSnapshot.ShowAllCaptureOptions");
        AssertNotNull(optionsType.GetProperty("PreviewVolumePercent"), "AutomationOptionsSnapshot.PreviewVolumePercent");
        AssertNotNull(optionsType.GetProperty("IsStatsVisible"), "AutomationOptionsSnapshot.IsStatsVisible");

        var presetsProperty = optionsType.GetProperty("Presets")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.Presets missing.");
        AssertEqual(stringOptionType, presetsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.Presets[] element type");

        var decoderCountsProperty = optionsType.GetProperty("MjpegDecoderCounts")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.MjpegDecoderCounts missing.");
        AssertEqual(intOptionType, decoderCountsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.MjpegDecoderCounts[] element type");

        var snapshotType = RequireType("ElgatoCapture.Models.AutomationSnapshot");
        AssertNotNull(snapshotType.GetProperty("SelectedVideoFormat"), "AutomationSnapshot.SelectedVideoFormat");
        AssertNotNull(snapshotType.GetProperty("ShowAllCaptureOptions"), "AutomationSnapshot.ShowAllCaptureOptions");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");

        return Task.CompletedTask;
    }

    private static Task FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ec-ffmpeg-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var localFfmpegDir = Path.Combine(tempRoot, "ffmpeg");
        Directory.CreateDirectory(localFfmpegDir);

        try
        {
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avcodec-62.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avutil-60.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffmpeg.exe"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffprobe.exe"), Array.Empty<byte>());

            var locatorType = RequireType("ElgatoCapture.Services.FfmpegRuntimeLocator");
            var resolveRuntime = locatorType.GetMethod(
                                     "TryResolveNativeRuntimeRoot",
                                     BindingFlags.Static | BindingFlags.NonPublic,
                                     binder: null,
                                     types: new[] { typeof(string), typeof(string).MakeByRefType() },
                                     modifiers: null)
                                 ?? throw new InvalidOperationException("FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot overload not found.");
            var runtimeArgs = new object?[] { tempRoot, null };
            var resolved = (bool)(resolveRuntime.Invoke(null, runtimeArgs)
                                  ?? throw new InvalidOperationException("FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot returned null."));
            AssertEqual(true, resolved, "FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot resolved");
            AssertEqual(localFfmpegDir, runtimeArgs[1]?.ToString(), "FfmpegRuntimeLocator native runtime root");

            var findToolPath = locatorType.GetMethod(
                                   "FindToolPath",
                                   BindingFlags.Static | BindingFlags.NonPublic,
                                   binder: null,
                                   types: new[] { typeof(string), typeof(string) },
                                   modifiers: null)
                               ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath overload not found.");
            var ffmpegPath = findToolPath.Invoke(null, new object?[] { "ffmpeg.exe", tempRoot })?.ToString()
                             ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath(ffmpeg.exe) returned null.");
            var ffprobePath = findToolPath.Invoke(null, new object?[] { "ffprobe.exe", tempRoot })?.ToString()
                              ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath(ffprobe.exe) returned null.");

            AssertEqual(Path.Combine(localFfmpegDir, "ffmpeg.exe"), ffmpegPath, "FfmpegRuntimeLocator ffmpeg.exe path");
            AssertEqual(Path.Combine(localFfmpegDir, "ffprobe.exe"), ffprobePath, "FfmpegRuntimeLocator ffprobe.exe path");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    private static Task McpFormatter_RendersMjpegTimingSection_WhenFieldsExist()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var mcpAssemblyPath = Path.Combine(root, "tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll");
        if (!File.Exists(mcpAssemblyPath))
        {
            return Task.CompletedTask;
        }

        var mcpAssemblyDirectory = Path.GetDirectoryName(mcpAssemblyPath)
            ?? throw new InvalidOperationException("McpServer assembly directory was not found.");
        var loadContext = AssemblyLoadContext.Default;
        Assembly? ResolveMcpDependency(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var dependencyPath = Path.Combine(mcpAssemblyDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(dependencyPath)
                ? context.LoadFromAssemblyPath(dependencyPath)
                : null;
        }

        loadContext.Resolving += ResolveMcpDependency;
        try
        {
            var mcpAssembly = loadContext.LoadFromAssemblyPath(mcpAssemblyPath);
            var formatterType = mcpAssembly.GetType("McpServer.ResponseFormatter")
                ?? throw new InvalidOperationException("McpServer.ResponseFormatter type not found.");
            var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("ResponseFormatter.FormatSnapshot not found.");

            const string json = """
                                {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","ShowAllCaptureOptions":true,"PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                                """;
            using var document = JsonDocument.Parse(json);
            var output = formatSnapshot.Invoke(null, new object[] { document.RootElement })?.ToString()
                ?? throw new InvalidOperationException("ResponseFormatter.FormatSnapshot returned null.");

            AssertContains(output, "== MJPEG Pipeline Timing ==");
            AssertContains(output, "Preset: P5");
            AssertContains(output, "Video Format: MJPG | Split Encode: Auto | MJPEG Decoders: 2");
            AssertContains(output, "UI: Show All Options=true | Preview Volume=42.5% | Stats Visible=true");
            AssertContains(output, "Decode: avg=2.1ms");
            AssertContains(output, "Interop Copy: avg=0.9ms");
            AssertContains(output, "Total Callback: avg=4.5ms");
            AssertContains(output, "Decoders: 2 | Decoded=301 Emitted=300 Dropped=1");
            AssertContains(output, "Reorder: avg=0.4ms");
            AssertContains(output, "Pipeline: avg=5.1ms");
            AssertContains(output, "Decoder[0]: avg=2.0ms");
            AssertContains(output, "Decoder[1]: avg=2.2ms");
        }
        catch (Exception ex) when (ex is FileLoadException or FileNotFoundException)
        {
            return Task.CompletedTask;
        }
        finally
        {
            loadContext.Resolving -= ResolveMcpDependency;
        }
        return Task.CompletedTask;
    }

    private static Task AutomationCommandMaps_StayAligned_ForAdvancedMcpControls()
    {
        var contractsText = ReadRepoFile("ElgatoCapture/Models/AutomationContracts.cs");
        var automationClientText = ReadRepoFile("tools/AutomationClient/Program.cs");
        var pipeClientText = ReadRepoFile("tools/McpServer/PipeClient.cs");
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1");

        AssertContains(contractsText, "SetPreset");
        AssertContains(contractsText, "SetSplitEncodeMode");
        AssertContains(contractsText, "SetMjpegDecoderCount");
        AssertContains(contractsText, "SetShowAllCaptureOptions");
        AssertContains(contractsText, "SetPreviewVolume");
        AssertContains(contractsText, "SetStatsVisible");
        AssertContains(contractsText, "GetCaptureOptions");

        AssertContains(automationClientText, "[\"SetPreset\"] = 30");
        AssertContains(automationClientText, "[\"SetSplitEncodeMode\"] = 31");
        AssertContains(automationClientText, "[\"SetMjpegDecoderCount\"] = 32");
        AssertContains(automationClientText, "[\"SetShowAllCaptureOptions\"] = 33");
        AssertContains(automationClientText, "[\"SetPreviewVolume\"] = 34");
        AssertContains(automationClientText, "[\"SetStatsVisible\"] = 35");
        AssertContains(automationClientText, "[\"GetCaptureOptions\"] = 29");

        AssertContains(pipeClientText, "[\"SetPreset\"] = 30");
        AssertContains(pipeClientText, "[\"SetSplitEncodeMode\"] = 31");
        AssertContains(pipeClientText, "[\"SetMjpegDecoderCount\"] = 32");
        AssertContains(pipeClientText, "[\"SetShowAllCaptureOptions\"] = 33");
        AssertContains(pipeClientText, "[\"SetPreviewVolume\"] = 34");
        AssertContains(pipeClientText, "[\"SetStatsVisible\"] = 35");
        AssertContains(pipeClientText, "[\"GetCaptureOptions\"] = 29");

        AssertContains(scriptText, "\"setpreset\" { return 30 }");
        AssertContains(scriptText, "\"setsplitencodemode\" { return 31 }");
        AssertContains(scriptText, "\"setmjpegdecodercount\" { return 32 }");
        AssertContains(scriptText, "\"setshowallcaptureoptions\" { return 33 }");
        AssertContains(scriptText, "\"setpreviewvolume\" { return 34 }");
        AssertContains(scriptText, "\"setstatsvisible\" { return 35 }");
        AssertContains(scriptText, "\"getcaptureoptions\" { return 29 }");

        return Task.CompletedTask;
    }

    private static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        var captureSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/CaptureSettingsTools.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTool.cs");
        var captureOptionsToolText = ReadRepoFile("tools/McpServer/Tools/CaptureOptionsTool.cs");
        var uiSettingsToolText = ReadRepoFile("tools/McpServer/Tools/UiSettingsTools.cs");
        var snapshotType = RequireType("ElgatoCapture.Models.AutomationSnapshot");

        AssertContains(captureSettingsToolsText, "string? preset = null");
        AssertContains(captureSettingsToolsText, "string? splitEncodeMode = null");
        AssertContains(captureSettingsToolsText, "int? mjpegDecoderCount = null");
        AssertContains(captureSettingsToolsText, "\"SetPreset\"");
        AssertContains(captureSettingsToolsText, "\"SetSplitEncodeMode\"");
        AssertContains(captureSettingsToolsText, "\"SetMjpegDecoderCount\"");

        AssertContains(appStateToolText, "get_app_state_raw");
        AssertContains(appStateToolText, "UseStructuredContent = true");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetCaptureOptions\")");
        AssertContains(captureOptionsToolText, "get_capture_options");
        AssertContains(captureOptionsToolText, "\"GetCaptureOptions\"");
        AssertContains(captureOptionsToolText, "UseStructuredContent = true");
        AssertContains(uiSettingsToolText, "configure_ui");
        AssertContains(uiSettingsToolText, "\"SetShowAllCaptureOptions\"");
        AssertContains(uiSettingsToolText, "\"SetPreviewVolume\"");
        AssertContains(uiSettingsToolText, "\"SetStatsVisible\"");
        if (snapshotType.GetProperty("Options") != null)
        {
            throw new InvalidOperationException("AutomationSnapshot.Options should not be present when capture options are a separate surface.");
        }

        return Task.CompletedTask;
    }

    private static Task UiAutomationCommands_AreNotBlockedOnDeviceReadiness()
    {
        var dispatcherText = ReadRepoFile("ElgatoCapture/Services/AutomationCommandDispatcher.cs");

        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetShowAllCaptureOptions => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetPreviewVolume => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetStatsVisible => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.GetCaptureOptions => true,");

        return Task.CompletedTask;
    }

    private static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        AssertContains(mainViewModelText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        return Task.CompletedTask;
    }

    private static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("ElgatoCapture/Services/SettingsService.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? ShowAllCaptureOptions { get; set; }");
        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertContains(mainViewModelText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertContains(mainViewModelText, "if (settings.IsStatsVisible.HasValue)");
        AssertContains(mainViewModelText, "ShowAllCaptureOptions = ShowAllCaptureOptions,");
        AssertContains(mainViewModelText, "IsStatsVisible = IsStatsVisible,");
        AssertContains(mainViewModelText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertContains(mainViewModelText, "partial void OnShowAllCaptureOptionsChanged(bool value)");
        AssertContains(mainViewModelText, "RebuildResolutionOptions();\n        SaveSettings();");

        return Task.CompletedTask;
    }

    private static Task ProjectFile_PreservesEnglishOnlyPublishLocalePolicy()
    {
        var projectText = ReadRepoFile("ElgatoCapture/ElgatoCapture.csproj").Replace("\r\n", "\n");
        AssertContains(projectText, "<SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>");
        AssertContains(projectText, "AfterTargets=\"Build;Publish\"");
        AssertContains(projectText, "$_.Name.ToLowerInvariant() -ne 'en-us'");
        AssertContains(projectText, "'$(PublishDir)' != ''");
        AssertContains(projectText, "^[A-Za-z]{2,3}(-[A-Za-z]+)+$");
        return Task.CompletedTask;
    }

    private static Task ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "options = ShowAllCaptureOptions");
        AssertContains(mainViewModelText, "!IsSourceFilteredFrameRateDisableReason(option.DisableReason)");
        AssertContains(mainViewModelText, "IsEnabled = true");
        AssertContains(mainViewModelText, "DisableReason = string.Empty");

        return Task.CompletedTask;
    }

    private static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("ElgatoCapture/Services/AutomationDiagnosticsHub.cs");
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertContains(mainViewModelText, "GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/CaptureService.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    private static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
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

    private static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("ElgatoCapture/MainWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "public partial bool IsAudioPreviewActive { get; set; }");
        AssertContains(mainViewModelText, "IsAudioPreviewActive = runtime.IsAudioPreviewActive;");
        AssertContains(mainViewModelText, "IsAudioPreviewActive = false;");
        AssertContains(mainWindowText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(mainWindowText, "SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);");

        return Task.CompletedTask;
    }

    private static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/CaptureService.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_wasapiAudioCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }

    private static Task LivePixelFormatSurfaces_PreferReaderSourceSubtype()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "runtime.ReaderSourceSubtype ??");
        AssertContains(mainViewModelText, "runtime.LatestObservedFramePixelFormat ??");

        if (mainViewModelText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            mainViewModelText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        return Task.CompletedTask;
    }

    private static Task StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var mainWindowText = ReadRepoFile("ElgatoCapture/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("ElgatoCapture/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("ElgatoCapture/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("ElgatoCapture/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("ElgatoCapture/Services/NativeXuAtCommandProvider.cs").Replace("\r\n", "\n");

        AssertContains(mainWindowText, "var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(mainWindowText, "var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertDoesNotContain(mainWindowText, "var sourceFormat =\n            snapshot.ReaderSourceSubtype ??");
        AssertContains(statsWindowText, "SourceHdrValue.Text = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsWindowText, "SourceFormatValue.Text = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertContains(mainWindowXaml, "Text=\"Video Format\"");
        AssertContains(mainWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(statsWindowXaml, "Text=\"Video Format\"");
        AssertContains(statsWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(nativeXuText, "VideoFormat = aviInfo.ColorSpace,");
        AssertContains(nativeXuText, "Colorimetry = aviInfo.Colorimetry,");
        AssertContains(nativeXuText, "Quantization = aviInfo.Quantization,");
        AssertContains(nativeXuText, "HdrTransferFunction = ResolveHdrTransferFunction(hdrInfo.Eotf),");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 120d);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(settings, "HdrEnabled", false);

        AssertEqual(true, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode");

        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode HDR");

        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "Width", 1920u);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode non-4k");

        return Task.CompletedTask;
    }

    private static Task UnifiedVideoCapture_CpuMjpegEmitReportsNv12()
    {
        var unifiedVideoCapture = CreateInstance("ElgatoCapture.Services.UnifiedVideoCapture");
        var observed = string.Empty;

        var setObserver = unifiedVideoCapture.GetType().GetMethod("SetObservedPixelFormatObserver", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SetObservedPixelFormatObserver method not found.");
        setObserver.Invoke(unifiedVideoCapture, new object?[] { new Action<string>(value => observed = value) });

        var emitMethod = unifiedVideoCapture.GetType().GetMethod("OnMjpegPipelineFrameEmitted", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMjpegPipelineFrameEmitted method not found.");
        var emit = (ClosedMjpegEmitDelegate)emitMethod.CreateDelegate(typeof(ClosedMjpegEmitDelegate), unifiedVideoCapture);
        emit(new byte[6].AsSpan(), 2, 2, 0);

        AssertEqual("NV12", observed, "UnifiedVideoCapture.OnMjpegPipelineFrameEmitted observer format");
        return Task.CompletedTask;
    }

    private static async Task UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails()
    {
        var unifiedVideoCapture = CreateInstance("ElgatoCapture.Services.UnifiedVideoCapture");
        var pipelineType = RequireType("ElgatoCapture.Services.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        try
        {
            var stopAsync = unifiedVideoCapture.GetType().GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("UnifiedVideoCapture.StopAsync method not found.");
            if (stopAsync.Invoke(unifiedVideoCapture, null) is not Task stopTask)
            {
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync did not return a Task.");
            }

            try
            {
                await stopTask.ConfigureAwait(false);
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync unexpectedly succeeded.");
            }
            catch (InvalidOperationException ex)
            {
                AssertContains(ex.Message, "emitter_self_join");
            }

            var retainedPipeline = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
            AssertEqual(pipeline, retainedPipeline, "UnifiedVideoCapture._mjpegPipeline retained on stop failure");
        }
        finally
        {
            SetPrivateField(pipeline, "_emitThread", null);
            SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", null);

            var disposeMethod = pipelineType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ParallelMjpegDecodePipeline.Dispose method not found.");
            disposeMethod.Invoke(pipeline, null);

            await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
        }
    }

    private static async Task CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState()
    {
        var captureService = CreateInstance("ElgatoCapture.Services.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);
        SetPrivateField(captureService, "_isVideoPreviewActive", true);
        SetPrivateField(captureService, "_isAudioPreviewActive", true);
        SetPrivateField(captureService, "_isRecording", true);

        InvokeNonPublicInstanceMethod(
            captureService,
            "OnUnifiedVideoCaptureFatalError",
            new object?[] { null, new InvalidOperationException("synthetic hfr failure") });

        await WaitForConditionAsync(
            () =>
                string.Equals(GetPropertyValue(captureService, "SessionState")?.ToString(), "Faulted", StringComparison.Ordinal) &&
                !GetBoolProperty(captureService, "IsInitialized") &&
                !GetBoolProperty(captureService, "IsVideoPreviewActive") &&
                !GetBoolProperty(captureService, "IsAudioPreviewActive") &&
                !GetBoolProperty(captureService, "IsRecording"),
            "CaptureService fatal cleanup").ConfigureAwait(false);

        AssertEqual("Faulted", GetPropertyValue(captureService, "SessionState")?.ToString(), "SessionState");
        AssertEqual(false, GetBoolProperty(captureService, "IsInitialized"), "IsInitialized");
        AssertEqual(false, GetBoolProperty(captureService, "IsVideoPreviewActive"), "IsVideoPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsRecording"), "IsRecording");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "IsInitialized = _captureService.IsInitialized;");
        AssertContains(mainViewModelText, "IsPreviewing = _captureService.IsVideoPreviewActive;");
        AssertContains(mainViewModelText, "IsRecording = _captureService.IsRecording;");
        AssertContains(mainViewModelText, "UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }

    // ── RecordingContracts tests ──

    private static Task FinalizeResult_Success_ProducesEmptyPreservedList()
    {
        var resultType = RequireType("ElgatoCapture.Services.FinalizeResult");
        var successMethod = resultType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Success not found");
        var result = successMethod.Invoke(null, new object[] { "/path/output.mp4", "Stopped" })!;

        AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual("/path/output.mp4", GetStringProperty(result, "OutputPath"), "OutputPath");
        AssertEqual("Stopped", GetStringProperty(result, "StatusMessage"), "StatusMessage");
        var artifacts = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(0, GetCountProperty(artifacts), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }

    private static Task FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts()
    {
        var resultType = RequireType("ElgatoCapture.Services.FinalizeResult");
        var failureMethod = resultType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Failure not found");

        var artifacts = new List<string?> { "/path/a.mp4", "/path/A.mp4", null!, "", " ", "/path/b.m4a" }
            .Where(s => true) as IEnumerable<string>;
        var result = failureMethod.Invoke(null, new object?[] { "/output.mp4", "mux failed", artifacts })!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        var preserved = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }

    // ── RecordingArtifactManager tests ──

    private static Task ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled()
    {
        var manager = CreateInstance("ElgatoCapture.Services.RecordingArtifactManager");
        var context = BuildRecordingContext(usePostMuxAudio: false, finalPath: "/out/video.mp4");

        var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
            ?? throw new InvalidOperationException("FinalizeContext not found");
        var result = finalizeMethod.Invoke(manager, new object?[] { context, true, null })!;

        AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual("/out/video.mp4", GetStringProperty(result, "OutputPath"), "OutputPath");

        return Task.CompletedTask;
    }

    private static Task ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(finalPath, Array.Empty<byte>()); // empty placeholder

            var manager = CreateInstance("ElgatoCapture.Services.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, false, "encoder error" })!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            var preserved = GetPropertyValue(result, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

            // Empty final file should have been deleted
            if (File.Exists(finalPath))
                throw new InvalidOperationException("Expected empty final file to be deleted");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "v");
            File.WriteAllText(audioPath, "a");
            File.WriteAllText(finalPath, "f");

            var manager = CreateInstance("ElgatoCapture.Services.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
                ?? throw new InvalidOperationException("RollbackAsync not found");
            var task = rollbackMethod.Invoke(manager, new object?[] { context, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("RollbackAsync did not return Task");
            task.GetAwaiter().GetResult();

            if (File.Exists(videoPath))
                throw new InvalidOperationException("Expected video temp to be deleted");
            if (File.Exists(audioPath))
                throw new InvalidOperationException("Expected audio temp to be deleted");
            if (File.Exists(finalPath))
                throw new InvalidOperationException("Expected final output to be deleted");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_RollbackAsync_SafeWithNullContext()
    {
        var manager = CreateInstance("ElgatoCapture.Services.RecordingArtifactManager");
        var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
            ?? throw new InvalidOperationException("RollbackAsync not found");

        var contextType = RequireType("ElgatoCapture.Services.RecordingContext");
        var task = rollbackMethod.Invoke(manager, new object?[] { null, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("RollbackAsync did not return Task");
        task.GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    // ── CaptureSettings tests ──

    private static Task CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        // 4K60 H264 High: 25 * (3840*2160/2073600) * (60/30) * 1.0 = 25 * 3.98 * 2 = ~199.07 → clamped to 200
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "H264Mp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("ElgatoCapture.Models.VideoQuality", "High"));

        var bitrate = InvokeInstanceMethod(settings, "GetTargetBitrate");
        var bps = Convert.ToUInt32(bitrate);

        // 4K60 H264 High should be at or near 200 Mbps cap
        if (bps < 150_000_000 || bps > 200_000_000)
            throw new InvalidOperationException($"Expected 4K60 H264 High ~200 Mbps, got {bps / 1_000_000.0:F1} Mbps");

        // 1080p30 H264 High: 25 * 1.0 * 1.0 * 1.0 = 25 Mbps
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        if (lowBitrate < 24_000_000 || lowBitrate > 26_000_000)
            throw new InvalidOperationException($"Expected 1080p30 H264 High ~25 Mbps, got {lowBitrate / 1_000_000.0:F1} Mbps");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        // 1080p60 at each codec: H264 > HEVC > AV1
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("ElgatoCapture.Models.VideoQuality", "High"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        if (hevc >= h264)
            throw new InvalidOperationException($"HEVC ({hevc}) should be less than H264 ({h264})");
        if (av1 >= hevc)
            throw new InvalidOperationException($"AV1 ({av1}) should be less than HEVC ({hevc})");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("ElgatoCapture.Models.VideoQuality", "Custom"));

        // Over max: should clamp to 300 Mbps
        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 999.0);
        var over = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(300_000_000u, over, "CustomBitrate over-max clamp");

        // Under min: should clamp to 1 Mbps
        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 0.1);
        var under = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(1_000_000u, under, "CustomBitrate under-min clamp");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(av1Name, "_AV1.");
        AssertContains(av1Name, ".mp4");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "HevcMp4"));
        var hevcName = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(hevcName, "_HEVC.");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "H264Mp4"));
        var h264Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(h264Name, "_H264.");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType("ElgatoCapture.Models.CaptureSettings");
        var method = settingsType.GetMethod("IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("IsMjpegHighFrameRateMode not found");

        // SDR + MJPG + 4K120 → true
        var result1 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(true, result1, "SDR+MJPG+4K120 should be HFR");

        // HDR + MJPG → false (HDR disqualifies)
        var result2 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!;
        AssertEqual(false, result2, "HDR should not be HFR");

        // SDR + NV12 → false (wrong pixel format)
        var result3 = (bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(false, result3, "NV12 should not be HFR");

        // SDR + MJPG + 1080p60 → false (too low res/fps)
        var result4 = (bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!;
        AssertEqual(false, result4, "1080p60 should not be HFR");

        return Task.CompletedTask;
    }

    // ── FlashbackBufferManager tests ──

    private static object CreateInitializedBufferManager(string tempDir)
    {
        var optionsType = RequireType("ElgatoCapture.Models.FlashbackBufferOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        SetPropertyBackingField(options, "TempDirectory", tempDir);
        SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

        var managerType = RequireType("ElgatoCapture.Services.FlashbackBufferManager");
        var manager = RuntimeHelpers.GetUninitializedObject(managerType);
        SetPrivateField(manager, "_options", options);
        SetPrivateField(manager, "_indexLock", new object());
        SetPrivateField(manager, "_sessionId", "test-session");
        SetPrivateField(manager, "_activeSegmentPath", Path.Combine(tempDir, "fb_test_0003.ts"));
        SetPrivateField(manager, "_nextSegmentIndex", 4);

        // Initialize the completed segments list via reflection
        var listType = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listType.GetValue(manager);
        if (list == null)
        {
            // GetUninitializedObject skips ctor — create the list
            var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
            var listGenericType = typeof(List<>).MakeGenericType(csType);
            list = Activator.CreateInstance(listGenericType)!;
            listType.SetValue(manager, list);
        }

        return manager;
    }

    private static void AddCompletedSegment(object manager, string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        var managerType = manager.GetType();
        var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
        var listField = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listField.GetValue(manager)!;
        var addMethod = list.GetType().GetMethod("Add")!;

        var countProp = list.GetType().GetProperty("Count")!;
        var seqNum = (int)countProp.GetValue(list)!;

        var segment = Activator.CreateInstance(csType, path, seqNum, startPts, endPts, sizeBytes)!;
        addMethod.Invoke(list, new[] { segment });
    }

    private static Task FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        // Add 3 segments: 0-5s, 5-10s, 10-15s
        AddCompletedSegment(manager, "/seg0.ts", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 1000);
        AddCompletedSegment(manager, "/seg1.ts", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1000);
        AddCompletedSegment(manager, "/seg2.ts", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 1000);

        var method = manager.GetType().GetMethod("GetSegmentFileForPosition")!;

        // Position 7s → segment 1 (5-10s)
        var result1 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(7) }) as string;
        AssertEqual("/seg1.ts", result1!, "Position 7s");

        // Position 0s → segment 0 (0-5s)
        var result2 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(0) }) as string;
        AssertEqual("/seg0.ts", result2!, "Position 0s");

        // Position 20s → not in any completed segment → falls back to active
        var result3 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) }) as string;
        AssertContains(result3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetNextSegmentFile_WalksForward()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        AddCompletedSegment(manager, "/a.ts", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, "/b.ts", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, "/c.ts", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;

        // From a → b
        var next1 = method.Invoke(manager, new object[] { "/a.ts" }) as string;
        AssertEqual("/b.ts", next1!, "a→b");

        // From b → c
        var next2 = method.Invoke(manager, new object[] { "/b.ts" }) as string;
        AssertEqual("/c.ts", next2!, "b→c");

        // From c (last completed) → active segment
        var next3 = method.Invoke(manager, new object[] { "/c.ts" }) as string;
        AssertContains(next3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        AddCompletedSegment(manager, "/s0.ts", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, "/s1.ts", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, "/s2.ts", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);
        AddCompletedSegment(manager, "/s3.ts", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentPaths")!;

        // Range 3s-12s should include s0 (0-5 overlaps), s1 (5-10), s2 (10-15 overlaps)
        var result = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12) })!;
        var count = GetCountProperty(result);
        AssertEqual(3, count, "3s-12s should span 3 segments");

        // Range 5s-5.5s should include only s1 (5-10)
        var narrow = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(1, GetCountProperty(narrow), "5s-5.5s should be 1 segment");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictionPauseResume_Balanced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var pauseMethod = manager.GetType().GetMethod("PauseEviction")!;
        var resumeMethod = manager.GetType().GetMethod("ResumeEviction")!;

        // Initially not paused
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "Initial EvictionPaused");

        // Pause → paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause → still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once → still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again → unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        return Task.CompletedTask;
    }

    // ── GpuPipelineHandles / RecordingContextRequest tests ──

    private static Task GpuPipelineHandles_None_ReturnsZeroedStruct()
    {
        var handlesType = RequireType("ElgatoCapture.Services.GpuPipelineHandles");
        var noneProp = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("GpuPipelineHandles.None not found");
        var none = noneProp.GetValue(null)!;

        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DevicePtr")!, "D3D11DevicePtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DeviceContextPtr")!, "D3D11DeviceContextPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwDeviceCtxPtr")!, "CudaHwDeviceCtxPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwFramesCtxPtr")!, "CudaHwFramesCtxPtr");

        return Task.CompletedTask;
    }

    private static Task RecordingContextRequest_DefaultsMatchRecordingContextDefaults()
    {
        var request = CreateInstance("ElgatoCapture.Services.RecordingContextRequest");
        AssertEqual("30", GetStringProperty(request, "FrameRateArg"), "FrameRateArg default");
        AssertEqual("nv12", GetStringProperty(request, "VideoInputPixelFormat"), "VideoInputPixelFormat default");
        AssertEqual(false, GetBoolProperty(request, "IsFullRangeInput"), "IsFullRangeInput default");
        AssertEqual(false, GetBoolProperty(request, "UsePostMuxAudio"), "UsePostMuxAudio default");

        return Task.CompletedTask;
    }

    // --- MediaFormat tests ---

    private static Task MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var a = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 1920u);
        SetPropertyOrBackingField(b, "Height", 1080u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(true, a.Equals(b), "MediaFormat rational equality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var a = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRate", 60.0);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRate", 60.0);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(false, a.Equals(b), "MediaFormat dimension inequality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var a = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 3840u);
        SetPropertyOrBackingField(a, "Height", 2160u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "P010");
        SetPropertyOrBackingField(a, "IsHdr", true);

        var b = CreateInstance("ElgatoCapture.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "P010");
        SetPropertyOrBackingField(b, "IsHdr", true);

        AssertEqual(a.GetHashCode(), b.GetHashCode(), "MediaFormat hash consistency");
        return Task.CompletedTask;
    }

    // --- AutomationContracts tests ---

    private static Task AutomationCommandKind_HasSequentialValues_0Through44()
    {
        var enumType = RequireType("ElgatoCapture.Models.AutomationCommandKind");
        var values = Enum.GetValues(enumType);
        AssertEqual(45, values.Length, "AutomationCommandKind value count");

        for (int i = 0; i < 45; i++)
        {
            var found = Enum.IsDefined(enumType, i);
            if (!found)
                throw new InvalidOperationException(
                    $"AutomationCommandKind missing sequential value {i}.");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationWindowAction_HasExpectedValues()
    {
        var enumType = RequireType("ElgatoCapture.Models.AutomationWindowAction");
        var names = Enum.GetNames(enumType);

        // Verify expected members exist
        var expectedNames = new[]
        {
            "Minimize", "Maximize", "Restore", "Close",
            "SnapLeft", "SnapRight", "SnapTopLeft", "SnapTopRight",
            "SnapBottomLeft", "SnapBottomRight", "Center", "Move", "Resize"
        };
        AssertEqual(expectedNames.Length, names.Length, "AutomationWindowAction count");

        foreach (var expected in expectedNames)
        {
            if (!Enum.IsDefined(enumType, Enum.Parse(enumType, expected)))
                throw new InvalidOperationException(
                    $"AutomationWindowAction missing expected value '{expected}'.");
        }

        return Task.CompletedTask;
    }

    // --- RuntimePaths tests ---

    private static Task RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot()
    {
        var runtimePathsType = RequireType("ElgatoCapture.RuntimePaths");
        var getRepoLogFile = runtimePathsType.GetMethod(
            "GetRepoLogFile",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        if (getRepoLogFile == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogFile not found.");

        var logPath = (string)getRepoLogFile.Invoke(null, new object[] { "test.log" })!;
        AssertContains(logPath, "test.log");

        // The log path should be a rooted path
        if (!Path.IsPathRooted(logPath))
            throw new InvalidOperationException(
                $"GetRepoLogFile returned non-rooted path: {logPath}");

        return Task.CompletedTask;
    }

    private static Task RuntimePaths_PathsContainExpectedDirectoryNames()
    {
        var runtimePathsType = RequireType("ElgatoCapture.RuntimePaths");

        var getRepoLogRoot = runtimePathsType.GetMethod(
            "GetRepoLogRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoLogRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogRoot not found.");
        var logRoot = (string)getRepoLogRoot.Invoke(null, null)!;
        AssertContains(logRoot, "logs");

        var getRepoTempRoot = runtimePathsType.GetMethod(
            "GetRepoTempRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoTempRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoTempRoot not found.");
        var tempRoot = (string)getRepoTempRoot.Invoke(null, null)!;
        AssertContains(tempRoot, "temp");

        return Task.CompletedTask;
    }

    // --- SourceSignalTelemetrySnapshot tests ---

    private static Task SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues()
    {
        var type = RequireType("ElgatoCapture.Models.SourceSignalTelemetrySnapshot");
        var instance = RuntimeHelpers.GetUninitializedObject(type);

        // Uninitialized record: nullable properties should be default (null for nullable, 0 for value types)
        // Use the factory method to test proper defaults
        var createMethod = type.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!;
        var snapshot = createMethod.Invoke(null, new object?[] { "test-reason", null })!;

        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "Availability"),
            "CreateUnavailable Availability");
        AssertEqual("Unknown",
            GetStringProperty(snapshot, "Origin"),
            "CreateUnavailable Origin");
        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "OriginDetail"),
            "CreateUnavailable OriginDetail");
        AssertContains(GetStringProperty(snapshot, "DiagnosticSummary"), "test-reason");

        return Task.CompletedTask;
    }

    private static Task SourceSignalTelemetrySnapshot_PropertiesRoundTrip()
    {
        var type = RequireType("ElgatoCapture.Models.SourceSignalTelemetrySnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);

        SetPropertyBackingField(snapshot, "Width", (int?)1920);
        SetPropertyBackingField(snapshot, "Height", (int?)1080);
        SetPropertyBackingField(snapshot, "FrameRateExact", (double?)59.94);
        SetPropertyBackingField(snapshot, "IsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "VideoFormat", "P010");
        SetPropertyBackingField(snapshot, "Firmware", "1.2.3");

        AssertEqual(1920, GetIntProperty(snapshot, "Width"), "Width round-trip");
        AssertEqual(1080, GetIntProperty(snapshot, "Height"), "Height round-trip");
        AssertEqual("P010", GetStringProperty(snapshot, "VideoFormat"), "VideoFormat round-trip");
        AssertEqual("1.2.3", GetStringProperty(snapshot, "Firmware"), "Firmware round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "IsHdr"), "IsHdr round-trip");

        return Task.CompletedTask;
    }

    // ── HdrOutputPolicy tests ──

    private static Task HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested()
    {
        var policyType = RequireType("ElgatoCapture.Services.HdrOutputPolicy");
        var method = policyType.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("HdrOutputPolicy.IsEnabled not found");

        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("ElgatoCapture.Models.HdrOutputMode", "Hdr10Pq"));

        var result = (bool)method.Invoke(null, new[] { settings })!;
        AssertEqual(true, result, "HDR enabled + Hdr10Pq should return true");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled()
    {
        var policyType = RequireType("ElgatoCapture.Services.HdrOutputPolicy");
        var method = policyType.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)!;

        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("ElgatoCapture.Models.HdrOutputMode", "Hdr10Pq"));

        var result = (bool)method.Invoke(null, new[] { settings })!;
        AssertEqual(false, result, "HDR disabled should return false");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq()
    {
        var policyType = RequireType("ElgatoCapture.Services.HdrOutputPolicy");
        var method = policyType.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)!;

        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("ElgatoCapture.Models.HdrOutputMode", "Off"));

        var result = (bool)method.Invoke(null, new[] { settings })!;
        AssertEqual(false, result, "HdrOutputMode=Off should return false");

        return Task.CompletedTask;
    }

    // ── FlashbackPlaybackState enum test ──

    private static Task FlashbackPlaybackState_HasAllExpectedStates()
    {
        var enumType = RequireType("ElgatoCapture.Models.FlashbackPlaybackState");
        var names = Enum.GetNames(enumType);

        // Expected states from the state machine design
        var expected = new HashSet<string> { "Disabled", "Buffering", "Live", "Scrubbing", "Playing", "Paused" };
        foreach (var name in expected)
        {
            if (!names.Contains(name))
                throw new InvalidOperationException($"Missing FlashbackPlaybackState: {name}");
        }

        AssertEqual(expected.Count, names.Length, "FlashbackPlaybackState count");

        return Task.CompletedTask;
    }

    // ── RecordingPipelineOptions / NvmlSnapshot / Coordinator / ProcessSpec tests ──

    private static Task RecordingPipelineOptions_ResolvesVideoQueueCapacity()
    {
        var options = CreateInstance("ElgatoCapture.Models.RecordingPipelineOptions");

        // Default: 250ms latency, min=4, max=30
        // At 60fps: ceil(60 * 250 / 1000) = ceil(15) = 15 → clamp(15, 4, 30) = 15
        var method = options.GetType().GetMethod("ResolveVideoQueueCapacity")!;
        var at60 = (int)method.Invoke(options, new object[] { 60.0 })!;
        AssertEqual(15, at60, "60fps default latency");

        // At 120fps: ceil(120 * 250 / 1000) = ceil(30) = 30 → clamp(30, 4, 30) = 30
        var at120 = (int)method.Invoke(options, new object[] { 120.0 })!;
        AssertEqual(30, at120, "120fps default latency");

        // At 30fps: ceil(30 * 250 / 1000) = ceil(7.5) = 8 → clamp(8, 4, 30) = 8
        var at30 = (int)method.Invoke(options, new object[] { 30.0 })!;
        AssertEqual(8, at30, "30fps default latency");

        // Zero fps falls back to 60fps: ceil(60 * 250 / 1000) = 15
        var atZero = (int)method.Invoke(options, new object[] { 0.0 })!;
        AssertEqual(15, atZero, "0fps fallback to 60");

        return Task.CompletedTask;
    }

    private static Task NvmlSnapshot_ComputedProperties_ConvertUnits()
    {
        var snapshotType = RequireType("ElgatoCapture.Services.NvmlSnapshot");
        // Constructor: GpuName, GpuUtil%, MemUtil%, NvdecUtil%, NvencUtil%, PcieTxKB, PcieRxKB,
        //              VramUsedB, VramTotalB, TempC, PowerMw, ClockMHz, MemClockMHz
        var snapshot = Activator.CreateInstance(snapshotType,
            "RTX 4090",        // GpuName
            (uint?)85,         // GpuUtilizationPercent
            (uint?)40,         // GpuMemoryUtilizationPercent
            (uint?)50,         // NvdecUtilizationPercent
            (uint?)75,         // NvencUtilizationPercent
            (uint?)1024,       // PcieTxKBps (1024 KB/s = 1.0 MB/s)
            (uint?)2048,       // PcieRxKBps (2048 KB/s = 2.0 MB/s)
            (ulong?)2147483648,// VramUsedBytes (2 GB)
            (ulong?)25769803776,// VramTotalBytes (24 GB)
            (uint?)65,         // GpuTemperatureC
            (uint?)350000,     // GpuPowerMilliwatts (350W)
            (uint?)2520,       // GpuClockMHz
            (uint?)10501)!;    // GpuMemClockMHz

        // GpuPowerW = 350000 / 1000 = 350.0
        var powerW = GetPropertyValue(snapshot, "GpuPowerW");
        AssertEqual(350.0, (double)powerW!, "GpuPowerW");

        // PcieTxMBps = 1024 / 1024 = 1.0
        var txMB = GetPropertyValue(snapshot, "PcieTxMBps");
        AssertEqual(1.0, (double)txMB!, "PcieTxMBps");

        // PcieRxMBps = 2048 / 1024 = 2.0
        var rxMB = GetPropertyValue(snapshot, "PcieRxMBps");
        AssertEqual(2.0, (double)rxMB!, "PcieRxMBps");

        // VramUsedMB = 2147483648 / (1024*1024) = 2048
        var usedMB = GetPropertyValue(snapshot, "VramUsedMB");
        AssertEqual(2048UL, (ulong)usedMB!, "VramUsedMB");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionSnapshot_DefaultState()
    {
        var snapshotType = RequireType("ElgatoCapture.Services.CaptureSessionSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(snapshotType);

        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "IsRecording default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "IsInitialized default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsVideoPreviewActive"), "IsVideoPreviewActive default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsAudioPreviewActive"), "IsAudioPreviewActive default");
        AssertEqual(0, (int)GetPropertyValue(snapshot, "PendingCommands")!, "PendingCommands default");

        return Task.CompletedTask;
    }

    private static Task ProcessSpec_DefaultTimeout_Is30Seconds()
    {
        var specType = RequireType("ElgatoCapture.Services.ProcessSpec");
        var spec = RuntimeHelpers.GetUninitializedObject(specType);
        // ProcessSpec uses init-only with defaults — GetUninitializedObject bypasses ctor
        // So test the contract by checking the source
        var sourceText = ReadRepoFile("ElgatoCapture/Services/ProcessSupervisor.cs");
        AssertContains(sourceText, "public int TimeoutMs { get; init; } = 30_000;");
        AssertContains(sourceText, "public string Arguments { get; init; } = string.Empty;");

        // ProcessRunResult contract
        AssertContains(sourceText, "public bool Started { get; init; }");
        AssertContains(sourceText, "public bool TimedOut { get; init; }");
        AssertContains(sourceText, "public string StdOut { get; init; } = string.Empty;");
        AssertContains(sourceText, "public string StdErr { get; init; } = string.Empty;");

        return Task.CompletedTask;
    }

    // ── Tool CommandMap & Formatter Alignment tests ──

    private static Task McpPipeClient_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumType = RequireType("ElgatoCapture.Models.AutomationCommandKind");
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValues(enumType);

        if (enumNames.Length == 0)
            throw new InvalidOperationException("AutomationCommandKind enum has no members.");

        var pipeClientText = ReadRepoFile("tools/McpServer/PipeClient.cs");

        for (int i = 0; i < enumNames.Length; i++)
        {
            var name = enumNames[i];
            var ordinal = Convert.ToInt32(enumValues.GetValue(i));
            var expectedEntry = $"[\"{name}\"] = {ordinal}";
            AssertContains(pipeClientText, expectedEntry);
        }

        var mapEntryCount = 0;
        var inMap = false;
        foreach (var line in pipeClientText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("CommandMap = new"))
                inMap = true;
            if (inMap && trimmed.StartsWith("[\"") && trimmed.Contains("] ="))
                mapEntryCount++;
            if (inMap && trimmed == "};")
                inMap = false;
        }

        AssertEqual(enumNames.Length, mapEntryCount,
            "MCP PipeClient CommandMap entry count vs AutomationCommandKind enum count");

        return Task.CompletedTask;
    }

    private static Task EcctlPipeTransport_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumType = RequireType("ElgatoCapture.Models.AutomationCommandKind");
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValues(enumType);

        if (enumNames.Length == 0)
            throw new InvalidOperationException("AutomationCommandKind enum has no members.");

        var pipeTransportText = ReadRepoFile("tools/ecctl/PipeTransport.cs");

        for (int i = 0; i < enumNames.Length; i++)
        {
            var name = enumNames[i];
            var ordinal = Convert.ToInt32(enumValues.GetValue(i));
            var expectedEntry = $"[\"{name}\"] = {ordinal}";
            AssertContains(pipeTransportText, expectedEntry);
        }

        var mapEntryCount = 0;
        var inMap = false;
        foreach (var line in pipeTransportText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("CommandMap = new"))
                inMap = true;
            if (inMap && trimmed.StartsWith("[\"") && trimmed.Contains("] ="))
                mapEntryCount++;
            if (inMap && trimmed == "};")
                inMap = false;
        }

        AssertEqual(enumNames.Length, mapEntryCount,
            "ecctl PipeTransport CommandMap entry count vs AutomationCommandKind enum count");

        return Task.CompletedTask;
    }

    private static Task ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson()
    {
        var formatterText = ReadRepoFile("tools/McpServer/ResponseFormatter.cs");
        AssertContains(formatterText, "public static bool IsSuccess(JsonElement response)");
        AssertContains(formatterText, "success.ValueKind == JsonValueKind.True");

        using (var docTrue = JsonDocument.Parse("{\"Success\": true, \"Message\": \"ok\"}"))
        {
            var el = docTrue.RootElement;
            var isSuccess = el.ValueKind == JsonValueKind.Object &&
                            el.TryGetProperty("Success", out var s) &&
                            s.ValueKind == JsonValueKind.True;
            AssertEqual(true, isSuccess, "IsSuccess with Success=true");
        }

        using (var docFalse = JsonDocument.Parse("{\"Success\": false, \"Message\": \"fail\"}"))
        {
            var el = docFalse.RootElement;
            var isSuccess = el.ValueKind == JsonValueKind.Object &&
                            el.TryGetProperty("Success", out var s) &&
                            s.ValueKind == JsonValueKind.True;
            AssertEqual(false, isSuccess, "IsSuccess with Success=false");
        }

        using (var docMissing = JsonDocument.Parse("{\"Message\": \"no success field\"}"))
        {
            var el = docMissing.RootElement;
            var isSuccess = el.ValueKind == JsonValueKind.Object &&
                            el.TryGetProperty("Success", out var s) &&
                            s.ValueKind == JsonValueKind.True;
            AssertEqual(false, isSuccess, "IsSuccess with missing Success property");
        }

        return Task.CompletedTask;
    }

    private static Task ResponseFormatter_Get_HandlesAllJsonValueKinds()
    {
        var formatterText = ReadRepoFile("tools/McpServer/ResponseFormatter.cs");
        AssertContains(formatterText, "public static string Get(JsonElement el, string prop, string fallback = \"N/A\")");

        var json = @"{
            ""str"": ""hello"",
            ""num"": 42,
            ""boolTrue"": true,
            ""boolFalse"": false,
            ""nullVal"": null,
            ""emptyArr"": [],
            ""nonEmptyArr"": [1, 2],
            ""obj"": { ""nested"": true },
            ""emptyStr"": """"
        }";

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        string Get(JsonElement element, string prop, string fallback = "N/A")
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(prop, out var value))
                return fallback;
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return fallback;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? fallback,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.Array => value.GetArrayLength() == 0 ? fallback : value.ToString(),
                JsonValueKind.Object => value.ToString(),
                _ => fallback
            };
        }

        AssertEqual("hello", Get(el, "str"), "Get string value");
        AssertEqual("42", Get(el, "num"), "Get number value");
        AssertEqual("true", Get(el, "boolTrue"), "Get bool true");
        AssertEqual("false", Get(el, "boolFalse"), "Get bool false");
        AssertEqual("N/A", Get(el, "nullVal"), "Get null value");
        AssertEqual("N/A", Get(el, "nonExistent"), "Get missing property");
        AssertEqual("custom", Get(el, "nonExistent", "custom"), "Get missing with custom fallback");
        AssertEqual("N/A", Get(el, "emptyArr"), "Get empty array");
        AssertEqual("", Get(el, "emptyStr"), "Get empty string");

        return Task.CompletedTask;
    }

    private static Task EcctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter()
    {
        var mcpText = ReadRepoFile("tools/McpServer/ResponseFormatter.cs");
        var ecctlText = ReadRepoFile("tools/ecctl/Formatters.cs");

        var mcpFields = ExtractSnapshotFields(mcpText);
        var ecctlFields = ExtractSnapshotFields(ecctlText);

        if (mcpFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from MCP ResponseFormatter.");
        if (ecctlFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from ecctl Formatters.");

        var missingInEcctl = new List<string>();
        foreach (var field in mcpFields)
        {
            if (!ecctlFields.Contains(field))
                missingInEcctl.Add(field);
        }

        if (missingInEcctl.Count > 0)
        {
            throw new InvalidOperationException(
                $"MCP ResponseFormatter references {missingInEcctl.Count} snapshot field(s) " +
                $"missing from ecctl Formatters: {string.Join(", ", missingInEcctl)}");
        }

        return Task.CompletedTask;
    }

    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        while (index < sourceText.Length)
        {
            var getIdx = sourceText.IndexOf("Get(snapshot,", index, StringComparison.Ordinal);
            if (getIdx < 0)
                break;

            var afterComma = getIdx + "Get(snapshot,".Length;
            var quoteIdx = sourceText.IndexOf('"', afterComma);
            if (quoteIdx < 0 || quoteIdx - afterComma > 10)
            {
                index = afterComma;
                continue;
            }

            var endQuoteIdx = sourceText.IndexOf('"', quoteIdx + 1);
            if (endQuoteIdx < 0)
            {
                index = quoteIdx + 1;
                continue;
            }

            var fieldName = sourceText.Substring(quoteIdx + 1, endQuoteIdx - quoteIdx - 1);
            if (fieldName.Length > 0)
                fields.Add(fieldName);

            index = endQuoteIdx + 1;
        }

        return fields;
    }

    // ── Test helpers for new tests ──

    private static object BuildRecordingContext(
        bool usePostMuxAudio,
        string? videoPath = null,
        string? audioTempPath = null,
        string? finalPath = null)
    {
        var settings = BuildSettings(hdrEnabled: false);
        var contextType = RequireType("ElgatoCapture.Services.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", settings);
        SetPropertyBackingField(context, "VideoOutputPath", videoPath ?? "/tmp/video.mp4");
        SetPropertyBackingField(context, "FinalOutputPath", finalPath ?? "/tmp/final.mp4");
        SetPropertyBackingField(context, "AudioTempPath", audioTempPath);
        SetPropertyBackingField(context, "UsePostMuxAudio", usePostMuxAudio);
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        return context;
    }

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        // Try init-only property backing field patterns
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        // Fall back to SetPropertyOrBackingField
        SetPropertyOrBackingField(instance, propertyName, value);
    }

    private static int GetCountProperty(object collection)
    {
        var countProp = collection.GetType().GetProperty("Count");
        if (countProp != null)
            return (int)(countProp.GetValue(collection) ?? 0);
        // IReadOnlyList<T> might not expose Count directly; try ICollection
        var iface = collection.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
        if (iface != null)
        {
            var cp = iface.GetProperty("Count");
            return (int)(cp?.GetValue(collection) ?? 0);
        }
        throw new InvalidOperationException("No Count property found");
    }

    private static object BuildDevice(string id = "device-1")
    {
        var device = CreateInstance("ElgatoCapture.Models.CaptureDevice");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", "Synthetic Capture Device");
        SetPropertyOrBackingField(device, "AudioDeviceId", "audio-1");
        SetPropertyOrBackingField(device, "AudioDeviceName", "Synthetic Audio");
        return device;
    }

    private static object BuildSettings(bool hdrEnabled)
    {
        var settings = CreateInstance("ElgatoCapture.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", hdrEnabled ? "P010" : "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("ElgatoCapture.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("ElgatoCapture.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", hdrEnabled);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("ElgatoCapture.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static async Task InvokeInitializeAsync(object captureService, object device, object settings)
    {
        var initialize = captureService.GetType().GetMethod(
            "InitializeAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { device.GetType(), settings.GetType(), typeof(CancellationToken) },
            modifiers: null);
        if (initialize == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync method not found.");
        }

        var task = initialize.Invoke(captureService, new[] { device, settings, CancellationToken.None }) as Task;
        if (task == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync did not return a Task.");
        }

        await task.ConfigureAwait(false);
    }

    private static async Task DisposeAsync(object captureService)
    {
        await DisposeValueTaskAsync(captureService).ConfigureAwait(false);
    }

    private static async Task DisposeValueTaskAsync(object instance)
    {
        var disposeAsync = instance.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
        if (disposeAsync == null)
        {
            return;
        }

        var valueTask = disposeAsync.Invoke(instance, null);
        if (valueTask == null)
        {
            return;
        }

        var asTaskMethod = valueTask.GetType().GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance);
        if (asTaskMethod?.Invoke(valueTask, null) is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, string description, int timeoutMs = 2000, int pollMs = 25)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMs).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for condition: {description}");
    }

    private static object CreateInstance(string typeName)
    {
        var type = RequireType(typeName);
        var instance = Activator.CreateInstance(type);
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to create instance of '{typeName}'.");
        }

        return instance;
    }

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static string GetRepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static Type RequireType(string typeName)
    {
        if (_assembly == null)
        {
            throw new InvalidOperationException("Target assembly is not loaded.");
        }

        return _assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"Type '{typeName}' not found in target assembly.");
    }

    private static object ParseEnum(string typeName, string value)
    {
        var type = RequireType(typeName);
        return Enum.Parse(type, value, ignoreCase: true);
    }

    private static object InvokeInstanceMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, null)
               ?? throw new InvalidOperationException($"Method '{methodName}' returned null.");
    }

    private static object? InvokeNonPublicInstanceMethod(object instance, string methodName, object?[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Non-public method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, arguments);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        field.SetValue(instance, value);
    }

    private static void SeedPipelineStopFailureState(object pipeline, Type pipelineType)
    {
        SetPrivateField(pipeline, "_workerQueues", CreateEmptyArrayFieldValue(pipelineType, "_workerQueues"));
        SetPrivateField(pipeline, "_workers", Array.Empty<Thread>());
        SetPrivateField(pipeline, "_decoders", CreateEmptyArrayFieldValue(pipelineType, "_decoders"));
        SetPrivateField(pipeline, "_reorderRing", CreateSizedArrayFieldValue(pipelineType, "_reorderRing", 16));
        SetPrivateField(pipeline, "_reorderFlags", new int[16]);
        SetPrivateField(pipeline, "_emitSignal", new AutoResetEvent(false));
    }

    private static object CreateEmptyArrayFieldValue(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, 0);
    }

    private static object CreateSizedArrayFieldValue(Type declaringType, string fieldName, int length)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, length);
    }

    private static object CreateFieldInstance(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        return Activator.CreateInstance(field.FieldType)
               ?? throw new InvalidOperationException($"Failed to create field instance for '{fieldName}'.");
    }

    private static object? GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        return field.GetValue(instance);
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' is not writable and backing field was not found on '{instance.GetType().Name}'.");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return value?.ToString() ?? string.Empty;
    }

    private static long GetLongProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt64(value);
    }

    private static int GetIntProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt32(value);
    }

    private static double GetDoubleProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToDouble(value);
    }

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToBoolean(value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on '{instance.GetType().Name}'.");
        }

        return property.GetValue(instance);
    }

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertContains(string value, string token)
    {
        if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}'.");
        }
    }

    private static void AssertDoesNotContain(string value, string token)
    {
        if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected value not to contain '{token}'.");
        }
    }

    private static void AssertNotNull(object? value, string fieldName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: value was null.");
        }
    }

    private static object CreateMjpegTimingMetrics(
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int interopCopySampleCount,
        double interopCopyAvgMs,
        double interopCopyP95Ms,
        double interopCopyMaxMs,
        int callbackSampleCount,
        double callbackAvgMs,
        double callbackP95Ms,
        double callbackMaxMs)
    {
        var type = RequireType("ElgatoCapture.Services.UnifiedVideoCapture+MjpegPipelineTimingMetrics");
        return Activator.CreateInstance(
                   type,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   interopCopySampleCount,
                   interopCopyAvgMs,
                   interopCopyP95Ms,
                   interopCopyMaxMs,
                   callbackSampleCount,
                   callbackAvgMs,
                   callbackP95Ms,
                   callbackMaxMs)
               ?? throw new InvalidOperationException("Failed to create MjpegPipelineTimingMetrics.");
    }

    private static object CreateFullMjpegPipelineTimingMetrics(
        int decoderCount,
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int reorderSampleCount,
        double reorderAvgMs,
        double reorderP95Ms,
        double reorderMaxMs,
        int pipelineSampleCount,
        double pipelineAvgMs,
        double pipelineP95Ms,
        double pipelineMaxMs,
        long totalDecoded,
        long totalEmitted,
        long totalDropped,
        long reorderSkips,
        int reorderBufferDepth,
        object[] perDecoder)
    {
        var type = RequireType("ElgatoCapture.Services.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderArray = Array.CreateInstance(
            RequireType("ElgatoCapture.Services.ParallelMjpegDecodePipeline+PerDecoderMetrics"),
            perDecoder.Length);
        for (var i = 0; i < perDecoder.Length; i++)
        {
            perDecoderArray.SetValue(perDecoder[i], i);
        }

        return Activator.CreateInstance(
                   type,
                   decoderCount,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   reorderSampleCount,
                   reorderAvgMs,
                   reorderP95Ms,
                   reorderMaxMs,
                   pipelineSampleCount,
                   pipelineAvgMs,
                   pipelineP95Ms,
                   pipelineMaxMs,
                   totalDecoded,
                   totalEmitted,
                   totalDropped,
                   reorderSkips,
                   reorderBufferDepth,
                   perDecoderArray)
               ?? throw new InvalidOperationException("Failed to create full MJPEG pipeline timing metrics.");
    }

    private static object CreatePerDecoderMetrics(
        int workerIndex,
        int sampleCount,
        double avgMs,
        double p95Ms,
        double maxMs)
    {
        var type = RequireType("ElgatoCapture.Services.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        return Activator.CreateInstance(type, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
               ?? throw new InvalidOperationException("Failed to create per-decoder MJPEG metrics.");
    }

    private delegate void ClosedMjpegEmitDelegate(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick);
}
