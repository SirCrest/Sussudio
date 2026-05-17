using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class ToolFormatterContractsTests
{
    [Fact]
    public void ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireStaticMethod(formatterType, "IsSuccess");

        using (var docTrue = JsonDocument.Parse("{\"Success\": true, \"Message\": \"ok\"}"))
        {
            Assert.True((bool)isSuccess.Invoke(null, new object[] { docTrue.RootElement })!);
        }

        using (var docFalse = JsonDocument.Parse("{\"Success\": false, \"Message\": \"fail\"}"))
        {
            Assert.False((bool)isSuccess.Invoke(null, new object[] { docFalse.RootElement })!);
        }

        using (var docMissing = JsonDocument.Parse("{\"Message\": \"no success field\"}"))
        {
            Assert.False((bool)isSuccess.Invoke(null, new object[] { docMissing.RootElement })!);
        }
    }

    [Fact]
    public void ResponseFormatter_Get_HandlesAllJsonValueKinds()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var get = RequireStaticMethod(formatterType, "Get");

        const string json = """
                            {
                                "str": "hello",
                                "num": 42,
                                "boolTrue": true,
                                "boolFalse": false,
                                "nullVal": null,
                                "emptyArr": [],
                                "nonEmptyArr": [1, 2],
                                "obj": { "nested": true },
                                "emptyStr": ""
                            }
                            """;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        Assert.Equal("hello", (string)get.Invoke(null, new object[] { el, "str", "N/A" })!);
        Assert.Equal("42", (string)get.Invoke(null, new object[] { el, "num", "N/A" })!);
        Assert.Equal("true", (string)get.Invoke(null, new object[] { el, "boolTrue", "N/A" })!);
        Assert.Equal("false", (string)get.Invoke(null, new object[] { el, "boolFalse", "N/A" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "nullVal", "N/A" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "nonExistent", "N/A" })!);
        Assert.Equal("custom", (string)get.Invoke(null, new object[] { el, "nonExistent", "custom" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "emptyArr", "N/A" })!);
        Assert.Equal(string.Empty, (string)get.Invoke(null, new object[] { el, "emptyStr", "N/A" })!);
    }

    [Fact]
    public void SharedFormatter_RendersMjpegTimingSection_WhenFieldsExist()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireStaticMethod(formatterType, "FormatSnapshot");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","ShowAllCaptureOptions":true,"PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"PreviewPacingLikelySlowStage":"MjpegDecode","PreviewPacingSlowStageConfidence":"Medium","PreviewPacingSlowStageEvidence":"decode p95 over budget","DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                            """;
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement, false })?.ToString()
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot returned null.");

        Assert.Contains("== MJPEG Pipeline Timing ==", output);
        Assert.Contains("Preset: P5", output);
        Assert.Contains("Video Format: MJPG | Split Encode: Auto | MJPEG Decoders: 2", output);
        Assert.Contains("UI: Show All Options=true | Preview Volume=42.5% | Stats Visible=true", output);
        Assert.Contains("Decode: avg=2.1ms", output);
        Assert.Contains("Interop Copy: avg=0.9ms", output);
        Assert.Contains("Total Callback: avg=4.5ms", output);
        Assert.Contains("Decoders: 2 | Decoded=301 Emitted=300 Dropped=1", output);
        Assert.Contains("Reorder: avg=0.4ms", output);
        Assert.Contains("Pipeline: avg=5.1ms", output);
        Assert.Contains("== Diagnostics ==", output);
        Assert.Contains("Legacy Score:", output);
        Assert.Contains("Pacing Classifier: stage=MjpegDecode confidence=Medium evidence=decode p95 over budget", output);
        Assert.Contains("Frame Time:", output);
        Assert.Contains("Average Rate:", output);
        Assert.Contains("Decoder[0]: avg=2.0ms", output);
        Assert.Contains("Decoder[1]: avg=2.2ms", output);
    }

    [Fact]
    public void SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter()
    {
        var mcpFields = ExtractSnapshotFields(ReadAutomationSnapshotFormatterSource());
        var ssctlFields = ExtractSnapshotFields(ReadSsctlSnapshotFormatterSource());

        Assert.NotEmpty(mcpFields);
        Assert.NotEmpty(ssctlFields);

        var missingInSsctl = new List<string>();
        foreach (var field in mcpFields)
        {
            if (!ssctlFields.Contains(field))
            {
                missingInSsctl.Add(field);
            }
        }

        Assert.True(
            missingInSsctl.Count == 0,
            $"AutomationSnapshotFormatter references {missingInSsctl.Count} snapshot field(s) missing from ssctl Formatters: {string.Join(", ", missingInSsctl)}");
    }

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = ToolFormatterTestAssembly.Load(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static MethodInfo RequireStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static string ReadAutomationSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/Common/AutomationSnapshotFormatter.cs",
            "tools/Common/AutomationSnapshotFormatter.CoreSections.cs",
            "tools/Common/AutomationSnapshotFormatter.CaptureSettings.cs",
            "tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs",
            "tools/Common/AutomationSnapshotFormatter.Diagnostics.cs",
            "tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs",
            "tools/Common/AutomationSnapshotFormatter.Values.cs",
            "tools/Common/AutomationSnapshotFormatter.DisplayValues.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.cs",
            "tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs",
            "tools/Common/AutomationSnapshotFormatter.Preview.cs",
            "tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs",
            "tools/Common/AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs",
            "tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs"
        });

    private static string ReadSsctlSnapshotFormatterSource()
        => ReadSourceFamily(new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
            "tools/ssctl/Formatters.Snapshot.CoreSections.cs",
            "tools/ssctl/Formatters.Snapshot.AvSync.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureCadence.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureSettings.cs",
            "tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.cs",
            "tools/ssctl/Formatters.Snapshot.Mjpeg.cs",
            "tools/ssctl/Formatters.Snapshot.Preview.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.cs",
            "tools/ssctl/Formatters.Snapshot.ThreadHealth.cs",
            "tools/ssctl/Formatters.Snapshot.VideoPipeline.cs",
            "tools/ssctl/Formatters.Snapshot.Source.cs"
        });

    private static string ReadSourceFamily(string[] files)
    {
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = RuntimeContractSource.ReadRepoFile(files[i]).Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        return string.Join("\n", parts);
    }

    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var callPrefix in new[]
        {
            "Get(snapshot,",
            "GetInt(snapshot,",
            "GetDouble(snapshot,",
            "GetLong(snapshot,",
            "GetNullableLong(snapshot,",
            "GetBool(snapshot,",
            "GetString(snapshot,",
            "FormatFrameBudgetMs(snapshot,",
            "FormatIntervalMs(snapshot,"
        })
        {
            ExtractSnapshotFieldsFromCalls(sourceText, callPrefix, fields);
        }

        return fields;
    }

    private static void ExtractSnapshotFieldsFromCalls(string sourceText, string callPrefix, HashSet<string> fields)
    {
        var index = 0;
        while (index < sourceText.Length)
        {
            var callIdx = sourceText.IndexOf(callPrefix, index, StringComparison.Ordinal);
            if (callIdx < 0)
            {
                break;
            }

            var afterComma = callIdx + callPrefix.Length;
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
            {
                fields.Add(fieldName);
            }

            index = endQuoteIdx + 1;
        }
    }
}

internal static class ToolFormatterTestAssembly
{
    private static readonly Dictionary<string, Assembly> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    public static Assembly Load(string relativeAssemblyPath)
    {
        var repoRoot = FindRepoRoot();
        var fullPath = Path.GetFullPath(Path.Combine(repoRoot, relativeAssemblyPath));
        lock (CacheLock)
        {
            if (Cache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Required tool assembly was not found: {relativeAssemblyPath}.");
            }

            var loadContext = new ToolFormatterTestAssemblyLoadContext(fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            Cache[fullPath] = assembly;
            return assembly;
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private sealed class ToolFormatterTestAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ToolFormatterTestAssemblyLoadContext(string mainAssemblyToLoadPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }
}
