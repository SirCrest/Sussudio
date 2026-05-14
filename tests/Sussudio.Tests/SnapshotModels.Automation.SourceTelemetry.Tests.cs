using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs").Replace("\r\n", "\n");
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs").Replace("\r\n", "\n")
            + "\n" + sourceSignalProjectionText
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs").Replace("\r\n", "\n");

        AssertContains(contractsText, "public string? SourceFirmware { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioFormat { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioSampleRate { get; init; }");
        AssertContains(contractsText, "public string? SourceInputSource { get; init; }");
        AssertContains(contractsText, "public string? SourceUsbHostProtocol { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpMode { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRxTxHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRawTimingHex { get; init; }");

        AssertContains(diagnosticsHubText, "SourceFirmware = sourceSignal.Firmware,");
        AssertContains(diagnosticsHubText, "SourceAudioFormat = sourceSignal.AudioFormat,");
        AssertContains(diagnosticsHubText, "SourceAudioSampleRate = sourceSignal.AudioSampleRate,");
        AssertContains(diagnosticsHubText, "SourceInputSource = sourceSignal.InputSource,");
        AssertContains(diagnosticsHubText, "SourceUsbHostProtocol = sourceSignal.UsbHostProtocol,");
        AssertContains(diagnosticsHubText, "SourceHdcpMode = sourceSignal.HdcpMode,");
        AssertContains(diagnosticsHubText, "SourceHdcpVersion = sourceSignal.HdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRxTxHdcpVersion = sourceSignal.RxTxHdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
        AssertContains(sourceSignalProjectionText, "Firmware = captureRuntime.SourceFirmware,");
        AssertContains(sourceSignalProjectionText, "AudioFormat = captureRuntime.SourceAudioFormat,");
        AssertContains(sourceSignalProjectionText, "RawTimingHex = captureRuntime.SourceRawTimingHex");

        return Task.CompletedTask;
    }
}
