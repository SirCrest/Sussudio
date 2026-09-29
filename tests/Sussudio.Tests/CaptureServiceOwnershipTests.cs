using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public async Task IdleCaptureOwners_ExposeEmptyMetricsAndTypedAbsentPlayback()
    {
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var captureType = assembly.GetType("Sussudio.Services.Capture.CaptureService", throwOnError: true)!;
        var unifiedType = assembly.GetType("Sussudio.Services.Capture.UnifiedVideoCapture", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(captureType)!;
        await using var unified = (IAsyncDisposable)Activator.CreateInstance(unifiedType, nonPublic: true)!;

        var sourceCadence = unifiedType.GetMethod("GetSourceCadenceMetrics")!.Invoke(unified, null)!;
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(sourceCadence, "RecentIntervalsMs")));
        Assert.Equal(0, GetMetricProperty(sourceCadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(sourceCadence, "ExpectedIntervalMs"));
        Assert.Equal(0d, GetMetricProperty(sourceCadence, "ObservedFps"));
        var jitter = unifiedType.GetMethod("GetMjpegPreviewJitterMetrics")!.Invoke(unified, null)!;
        Assert.Equal(string.Empty, GetMetricProperty(jitter, "LastDropReason"));
        Assert.Equal(string.Empty, GetMetricProperty(jitter, "LastUnderflowReason"));
        Assert.Equal(false, GetMetricProperty(jitter, "Enabled"));
        Assert.Equal(0, GetMetricProperty(jitter, "QueueDepth"));
        Assert.Equal(0L, GetMetricProperty(jitter, "TotalDropped"));

        var beforeSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var health = captureType.GetMethod("GetHealthSnapshot")!.Invoke(capture, null)!;
        var afterSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.Equal(GetMetricProperty(capture, "SessionState"), GetMetricProperty(health, "SessionState"));
        Assert.Null(GetMetricProperty(health, "FlashbackExportVerificationFormat"));
        Assert.Equal(-1L, GetMetricProperty(health, "LastFrameArrivalMs"));
        Assert.InRange(((DateTimeOffset)GetMetricProperty(health, "TimestampUtc")!).ToUnixTimeMilliseconds(),
            beforeSnapshotMs, afterSnapshotMs);
        foreach (var property in new[] { "CaptureCadenceRecentIntervalsMs", "FlashbackPlaybackRecentFrameIntervalsMs" })
        {
            Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(health, property)));
        }
        Assert.Equal(string.Empty, GetMetricProperty(health, "MjpegPreviewJitterLastDropReason"));
        Assert.Equal(string.Empty, GetMetricProperty(health, "MjpegPreviewJitterLastUnderflowReason"));
        Assert.Null(GetMetricProperty(health, "FlashbackPlaybackState"));
        Assert.Equal(0, GetMetricProperty(health, "CaptureCadenceSampleCount"));
        Assert.Equal(0d, GetMetricProperty(health, "CaptureCadenceExpectedIntervalMs"));
        Assert.Equal(0, GetMetricProperty(health, "FlashbackPlaybackCadenceSampleCount"));
        Assert.Equal(0L, GetMetricProperty(health, "FlashbackPlaybackSlowFrames"));
        Assert.Equal(0L, GetMetricProperty(health, "FlashbackPlaybackFrameCount"));
        Assert.Equal(0L, GetMetricProperty(health, "MjpegPreviewJitterTotalDropped"));

        var snapshot = Sussudio.Tests.AutomationSnapshotRegressionFixture.BuildResult(
            assembly, populated: false, healthOverride: health);
        foreach (var property in new[] { "CaptureCadenceRecentIntervalsMs", "FlashbackPlaybackRecentFrameIntervalsMs" })
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Array, snapshot.GetProperty(property).ValueKind);
            Assert.Equal(0, snapshot.GetProperty(property).GetArrayLength());
        }
        Assert.Equal("N/A", snapshot.GetProperty("FlashbackPlaybackState").GetString());
        Assert.Equal(string.Empty, snapshot.GetProperty("MjpegPreviewJitterLastDropReason").GetString());
        Assert.Equal(string.Empty, snapshot.GetProperty("MjpegPreviewJitterLastUnderflowReason").GetString());
    }

    [Fact]
    public async Task SourceReaderWithoutSamples_RetainsConfiguredExpectedCadence()
    {
        var type = Sussudio.Tests.SussudioAssembly.Load().GetType(
            "Sussudio.Services.Capture.MfSourceReaderVideoCapture", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(type)!;
        type.GetMethod("SetExpectedFrameRate")!.Invoke(capture, new object[] { 120d });
        var cadence = type.GetMethod("GetSourceCadenceMetrics")!.Invoke(capture, null)!;

        Assert.Equal(1000d / 120, GetMetricProperty(cadence, "ExpectedIntervalMs"));
        Assert.Equal(0, GetMetricProperty(cadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(cadence, "ObservedFps"));
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(cadence, "RecentIntervalsMs")));
    }

    [Fact]
    public void PlaybackWithoutSamples_RetainsAccumulatedSlowFrames()
    {
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var bufferType = assembly.GetType("Sussudio.Services.Flashback.FlashbackBufferManager", throwOnError: true)!;
        var playbackType = assembly.GetType("Sussudio.Services.Flashback.FlashbackPlaybackController", throwOnError: true)!;
        using var buffer = (IDisposable)Activator.CreateInstance(bufferType, new object?[] { null })!;
        using var playback = (IDisposable)Activator.CreateInstance(playbackType, buffer)!;
        playbackType.GetField("_playbackSlowFrameCount", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(playback, 7L);
        var cadence = playbackType.GetMethod("GetPlaybackCadenceMetrics")!.Invoke(playback, null)!;

        Assert.Equal(7L, GetMetricProperty(cadence, "SlowFrameCount"));
        Assert.Equal(0, GetMetricProperty(cadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(cadence, "OnePercentLowFps"));
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(cadence, "RecentFrameIntervalsMs")));
    }

    [Fact]
    public async Task HealthSnapshotObservesCurrentSessionSettingsAndArrivalAge()
    {
        const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var captureType = assembly.GetType("Sussudio.Services.Capture.CaptureService", throwOnError: true)!;
        var unifiedType = assembly.GetType("Sussudio.Services.Capture.UnifiedVideoCapture", throwOnError: true)!;
        var settingsType = assembly.GetType("Sussudio.Models.CaptureSettings", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(captureType)!;
        await using var unified = (IAsyncDisposable)Activator.CreateInstance(unifiedType, nonPublic: true)!;
        var pipeline = captureType.GetField("_videoPipeline", instanceFlags)!.GetValue(capture)!;
        var ownedCapture = pipeline.GetType().GetProperty("Capture", instanceFlags)!;
        var settingsField = captureType.GetField("_currentSettings", instanceFlags)!;
        var settings = Activator.CreateInstance(settingsType)!;
        var formatProperty = settingsType.GetProperty("Format")!;
        var priorState = GetMetricProperty(capture, "SessionState");
        captureType.GetMethod("EnterFaultedState", instanceFlags)!.Invoke(capture, null);
        var currentState = GetMetricProperty(capture, "SessionState");
        Assert.NotEqual(priorState, currentState);
        Assert.Equal("Faulted", currentState!.ToString());

        try
        {
            ownedCapture.SetValue(pipeline, unified);
            settingsField.SetValue(capture, settings);
            var arrivalTick = Math.Max(1, Environment.TickCount64 - 50);
            unifiedType.GetField("_lastVideoFrameArrivedTick", instanceFlags)!.SetValue(unified, arrivalTick);
            foreach (var format in Enum.GetValues(formatProperty.PropertyType))
            {
                formatProperty.SetValue(settings, format);
                var beforeTick = Environment.TickCount64;
                var beforeSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var health = captureType.GetMethod("GetHealthSnapshot")!.Invoke(capture, null)!;
                var afterSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var afterTick = Environment.TickCount64;

                Assert.Equal(currentState, GetMetricProperty(health, "SessionState"));
                Assert.Equal(format.ToString(), GetMetricProperty(health, "FlashbackExportVerificationFormat"));
                Assert.InRange((long)GetMetricProperty(health, "LastFrameArrivalMs")!,
                    Math.Max(0, beforeTick - arrivalTick), Math.Max(0, afterTick - arrivalTick));
                Assert.InRange(((DateTimeOffset)GetMetricProperty(health, "TimestampUtc")!).ToUnixTimeMilliseconds(),
                    beforeSnapshotMs, afterSnapshotMs);
            }
        }
        finally
        {
            ownedCapture.SetValue(pipeline, null);
            settingsField.SetValue(capture, null);
        }
    }

    private static object? GetMetricProperty(object value, string name)
        => value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(value);













}


static partial class Program
{
    internal static async Task CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
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

        var detailEntryType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
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

    internal static async Task GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var videoPipeline = GetPrivateField(captureService, "_videoPipeline")
            ?? throw new InvalidOperationException("CaptureService video pipeline resources were missing.");
        SetPrivateField(
            videoPipeline,
            "<LastMjpegPipelineTimingMetrics>k__BackingField",
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
            videoPipeline,
            "<LastFullMjpegPipelineTimingMetrics>k__BackingField",
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
        SetPropertyOrBackingField(videoPipeline, "Capture", null);

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
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesQueued"), "MjpegCompressedFramesQueued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsQueueFull"), "MjpegCompressedDropsQueueFull");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsDisposed"), "MjpegCompressedDropsDisposed");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegDecodeFailures"), "MjpegDecodeFailures");
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

    internal static async Task GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var videoPipeline = GetPrivateField(captureService, "_videoPipeline")
            ?? throw new InvalidOperationException("CaptureService video pipeline resources were missing.");
        SetPrivateField(
            videoPipeline,
            "<LastMjpegPipelineTimingMetrics>k__BackingField",
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
            videoPipeline,
            "<LastFullMjpegPipelineTimingMetrics>k__BackingField",
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
        SetPropertyOrBackingField(videoPipeline, "Capture", null);

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
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesDequeued"), "MjpegCompressedFramesDequeued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsByteBudget"), "MjpegCompressedDropsByteBudget");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegEmitFailures"), "MjpegEmitFailures");
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
        var type = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture+MjpegPipelineTimingMetrics");
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
        object[] perDecoder,
        long compressedFramesQueued = 0,
        long compressedFramesDequeued = 0,
        long compressedDropsQueueFull = 0,
        long compressedDropsByteBudget = 0,
        long compressedDropsDisposed = 0,
        long loadShedDrops = 0,
        long decodeFailures = 0,
        long reorderCollisions = 0,
        long emitFailures = 0,
        int compressedQueueDepth = 0,
        long compressedQueueBytes = 0,
        long compressedQueueByteBudget = 0,
        int peakReorderDepth = 0,
        long peakCompressedQueueBytes = 0,
        long reorderRingForceDrops = 0)
    {
        var type = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderArray = Array.CreateInstance(
            RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics"),
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
                   compressedFramesQueued,
                   compressedFramesDequeued,
                   compressedDropsQueueFull,
                   compressedDropsByteBudget,
                   compressedDropsDisposed,
                   loadShedDrops,
                   decodeFailures,
                   reorderCollisions,
                   emitFailures,
                   compressedQueueDepth,
                   compressedQueueBytes,
                   compressedQueueByteBudget,
                   reorderSkips,
                   reorderBufferDepth,
                   peakReorderDepth,
                   peakCompressedQueueBytes,
                   reorderRingForceDrops,
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
        var type = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        return Activator.CreateInstance(type, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
               ?? throw new InvalidOperationException("Failed to create per-decoder MJPEG metrics.");
    }



    internal static Task CaptureService_SnapshotProducerEpoch_AdvancesWhenRecordingStateChanges()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var sessionGenerationProperty = captureService.GetType().GetProperty("SessionGeneration", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureService.SessionGeneration not found.");

        var initialEpoch = Convert.ToInt64(sessionGenerationProperty.GetValue(captureService));
        SetPrivateField(captureService, "_isRecording", true);
        var recordingEpoch = Convert.ToInt64(sessionGenerationProperty.GetValue(captureService));

        AssertEqual(
            true,
            recordingEpoch > initialEpoch,
            "Capture snapshot producer epoch should advance when _isRecording changes inside an active transition.");
        return Task.CompletedTask;
    }

    internal static async Task CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
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

    internal static Task CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var stateType = RequireType("Sussudio.Models.CaptureSessionState");
        var canEnter = policyType.GetMethod(
            "CanEnterTransition",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { stateType, stateType },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.CanEnterTransition not found.");

        var states = new[]
        {
            "Uninitialized",
            "Initializing",
            "Ready",
            "Previewing",
            "Recording",
            "CleaningUp",
            "Faulted",
            "Disposed"
        };

        var allowedTransitions = new HashSet<string>
        {
            "Uninitialized->Uninitialized",
            "Uninitialized->Initializing",
            "Uninitialized->Ready",
            "Uninitialized->Previewing",
            "Uninitialized->CleaningUp",
            "Initializing->Initializing",
            "Initializing->Ready",
            "Initializing->Previewing",
            "Initializing->CleaningUp",
            "Ready->Initializing",
            "Ready->Ready",
            "Ready->Previewing",
            "Ready->Recording",
            "Ready->CleaningUp",
            "Previewing->Initializing",
            "Previewing->Ready",
            "Previewing->Previewing",
            "Previewing->Recording",
            "Previewing->CleaningUp",
            "Recording->Initializing",
            "Recording->Ready",
            "Recording->Previewing",
            "Recording->Recording",
            "Recording->CleaningUp",
            "CleaningUp->CleaningUp",
            "Faulted->Initializing",
            "Faulted->Ready",
            "Faulted->Previewing",
            "Faulted->CleaningUp",
            "Faulted->Faulted"
        };

        foreach (var currentState in states)
        {
            foreach (var transitionState in states)
            {
                var key = $"{currentState}->{transitionState}";
                AssertCanEnterTransition(
                    canEnter,
                    stateType,
                    currentState,
                    transitionState,
                    expected: allowedTransitions.Contains(key));
            }
        }

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var method = policyType.GetMethod(
            "ResolveSteadyState",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.ResolveSteadyState not found.");

        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Disposed"),
            ResolveState(method, isDisposed: true, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Disposed steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"),
            ResolveState(method, isDisposed: false, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Recording steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Previewing"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: true, isInitialized: true),
            "Audio preview steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Ready"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: true),
            "Initialized steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: false),
            "Uninitialized steady state");

        return Task.CompletedTask;
    }




    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.cs"
    };

    private static string ReadCaptureServiceAudioSource()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

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
}
