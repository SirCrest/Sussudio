using System;
using System.Threading.Tasks;
using Xunit;

public sealed partial class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public void CaptureService_HealthSnapshotRecordingFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs")
            .Replace("\r\n", "\n");
        var activeBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecordingActiveBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);");
        AssertContains(healthSnapshotAssemblerText, "RecordingEncodingFailed = recordingHealth.EncodingFailed,");
        AssertContains(healthSnapshotAssemblerText, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueCapacity = recordingHealth.CudaQueueCapacity,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueMaxDepth = recordingHealth.CudaQueueMaxDepth,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaFramesEnqueued = recordingHealth.CudaFramesEnqueued,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped,");
        AssertDoesNotContain(healthSnapshotAssemblerText, "RecordingCudaQueueDepth = sink?.CudaQueueCount ?? 0,");
        AssertDoesNotContain(healthSnapshotAssemblerText, "RecordingCudaFramesDropped = sink?.CudaFramesDropped ?? 0,");
        AssertDoesNotContain(healthSnapshotText, "var activeRecordingVideoQueueLatencyMetrics");
        AssertDoesNotContain(healthSnapshotText, "var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();");

        AssertContains(recordingText, "private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(");
        AssertContains(recordingText, "GetLastFailureTelemetry()");
        AssertContains(recordingText, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(recordingText, "CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(recordingText, "activeRecording.FlashbackVideoQueueLatencyMetrics");
        AssertContains(recordingText, "sink?.CudaQueueCount ?? 0");
        AssertContains(recordingText, "sink?.CudaQueueCapacityFrames ?? 0");
        AssertContains(recordingText, "sink?.CudaQueueMaxDepth ?? 0");
        AssertContains(recordingText, "sink?.CudaFramesEnqueued ?? 0");
        AssertContains(recordingText, "sink?.CudaFramesDropped ?? 0");
        AssertContains(recordingText, "int CudaQueueDepth");
        AssertContains(recordingText, "long CudaFramesDropped");
        AssertContains(recordingText, "private readonly record struct RecordingHealthSnapshotFields");
        AssertDoesNotContain(recordingText, "var flashbackVideoQueueLatencyMetrics");
        AssertDoesNotContain(recordingText, "sink?.VideoQueueLatencyMetrics ??");
        AssertDoesNotContain(recordingText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertDoesNotContain(recordingText, "flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0");

        AssertContains(activeBackendText, "private ActiveRecordingBackendHealthSnapshotFields CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(activeBackendText, "sink?.VideoQueueLatencyMetrics ??");
        AssertContains(activeBackendText, "flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0");
        AssertContains(activeBackendText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertContains(activeBackendText, "private readonly record struct ActiveRecordingBackendHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotSourceTelemetryFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryBackend = sourceTelemetry.Backend,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryCircuitState = sourceTelemetry.CircuitState,");
        AssertContains(healthSnapshotText, "private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetryBackend(telemetry)");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(healthSnapshotText, "private readonly record struct SourceTelemetryHealthSnapshotFields");
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotSourceTelemetry.cs")));

    }
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

}
