using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task NativeXuAtCommandProvider_RollingPollLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");
        var interfaceReadText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.InterfaceRead.cs")
            .Replace("\r\n", "\n");
        var rollingPollText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs")
            .Replace("\r\n", "\n");
        var rollingCommandGroupsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingCommandGroups.cs")
            .Replace("\r\n", "\n");
        var snapshotAssemblyText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs")
            .Replace("\r\n", "\n");
        var audioInputText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "public async Task<SourceSignalTelemetrySnapshot> ReadAsync(");
        AssertContains(rootText, "var attempt = TryReadInterface(ksInterface, cancellationToken);");
        AssertDoesNotContain(rootText, "using var handle = KsExtensionUnitNative.TryOpen(");
        AssertDoesNotContain(rootText, "KsExtensionUnitNative.TryReadTopologyNodes(");
        AssertDoesNotContain(rootText, "private readonly record struct VicTiming(");
        AssertContains(interfaceReadText, "private NodeReadAttempt TryReadInterface(");
        AssertContains(interfaceReadText, "using var handle = KsExtensionUnitNative.TryOpen(");
        AssertContains(interfaceReadText, "KsExtensionUnitNative.TryReadTopologyNodes(");
        AssertContains(interfaceReadText, "var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);");
        AssertContains(interfaceReadText, "private static NodeReadAttempt CreateUnavailableNodeResult(");
        AssertContains(interfaceReadText, "private static NodeReadAttempt HandleFailedCommand(");
        AssertContains(interfaceReadText, "private static bool IsUnsupportedNodeFailure(");
        AssertContains(interfaceReadText, "private static string DescribeCommandFailure(");
        AssertContains(interfaceReadText, "private static string DescribeWin32Detail(");
        AssertDoesNotContain(rootText, "private NodeReadAttempt TryReadRolling(");
        AssertDoesNotContain(rootText, "private static NodeReadAttempt HandleFailedCommand(");
        AssertDoesNotContain(rootText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertDoesNotContain(rootText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertContains(rollingPollText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(rollingPollText, "private int _rollingGroup;");
        AssertDoesNotContain(rollingPollText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertDoesNotContain(rollingPollText, "private static readonly double[] CanonicalFrameRates");
        AssertContains(rollingPollText, "private NodeReadAttempt TryReadRolling(");
        AssertContains(rollingPollText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertContains(rollingPollText, "BuildSnapshotFromCommandResults(");
        AssertDoesNotContain(rollingPollText, "BuildDetailEntries(");
        AssertDoesNotContain(rollingPollText, "new SourceSignalTelemetrySnapshot");
        AssertContains(rollingPollText, "PopulateInitialRollingCache(handle, nodeId, cancellationToken);");
        AssertContains(rollingPollText, "RefreshRollingGroup(handle, nodeId, _rollingGroup, cancellationToken);");
        AssertDoesNotContain(rollingPollText, "private AtCommandResult SendRollingCommand(");
        AssertDoesNotContain(rollingPollText, "private void PopulateInitialRollingCache(");
        AssertDoesNotContain(rollingPollText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(rollingCommandGroupsText, "private AtCommandResult SendRollingCommand(");
        AssertContains(rollingCommandGroupsText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(rollingCommandGroupsText, "private void PopulateInitialRollingCache(");
        AssertContains(rollingCommandGroupsText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "case 5: // Diagnostics");
        AssertDoesNotContain(rollingCommandGroupsText, "private static bool IsUnsupportedNodeFailure(");
        AssertContains(snapshotAssemblyText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertContains(snapshotAssemblyText, "private static readonly double[] CanonicalFrameRates");
        AssertContains(snapshotAssemblyText, "private readonly record struct VicTiming(");
        AssertContains(snapshotAssemblyText, "private readonly record struct NativeXuSnapshotCommandResults(");
        AssertContains(snapshotAssemblyText, "AtCommandResult RawTiming");
        AssertContains(snapshotAssemblyText, "private static NodeReadAttempt BuildSnapshotFromCommandResults(");
        AssertContains(snapshotAssemblyText, "BuildDetailEntries(");
        AssertContains(snapshotAssemblyText, "AppendFlashAudioAnalogGainDetail(detailEntries, results.FlashAudio)");
        AssertContains(snapshotAssemblyText, "new SourceSignalTelemetrySnapshot");
        AssertDoesNotContain(snapshotAssemblyText, "private static string ResolveSnapshotAudioInputOrigin(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs")),
            "snapshot command result DTO folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs")),
            "snapshot timing policy folded into NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertContains(audioInputText, "private static string ResolveSnapshotAudioInputOrigin(");
        AssertContains(audioInputText, "\"nativexu-flash-audio\"");
        AssertDoesNotContain(snapshotAssemblyText, "TelemetryLabels.AnalogGain");
        AssertDoesNotContain(snapshotAssemblyText, "Math.Exp(4.0 * y)");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.RollingCommandGroups.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial()
    {
        var deviceCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs")
            .Replace("\r\n", "\n");
        var deviceCommandReadsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommandReads.cs")
            .Replace("\r\n", "\n");
        var audioCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs")
            .Replace("\r\n", "\n");
        var analogGainText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AnalogGain.cs")
            .Replace("\r\n", "\n");
        var audioSwitchText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioSwitch.cs")
            .Replace("\r\n", "\n");
        var atProtocolText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs")
            .Replace("\r\n", "\n");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(deviceCommandsText, "public static async Task<bool> SendAtSetCommandAsync(");
        AssertContains(deviceCommandsText, "public static Task<bool> SetInputSourceAsync(");
        AssertDoesNotContain(deviceCommandsText, "public static async Task<byte[]?> ReadAtCommandAsync(");
        AssertContains(deviceCommandReadsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(deviceCommandReadsText, "public static async Task<byte[]?> ReadAtCommandAsync(");
        AssertContains(deviceCommandReadsText, "SendAtCommand(handle, node.NodeId, label, cmdCode)");
        AssertContains(deviceCommandReadsText, "NATIVEXU_GET_EXCEPTION");
        AssertDoesNotContain(deviceCommandReadsText, "public static async Task<bool> SendAtSetCommandAsync(");
        AssertDoesNotContain(deviceCommandReadsText, "public static Task<bool> SetInputSourceAsync(");
        AssertDoesNotContain(deviceCommandsText, "public static async Task<bool> SwitchAudioInputAsync(");
        AssertDoesNotContain(deviceCommandsText, "public static async Task<bool> SetAnalogGainAsync(");
        AssertDoesNotContain(deviceCommandsText, "internal static void ComputeGainRegisters(");
        AssertContains(audioCommandsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(audioCommandsText, "public static async Task<bool> SwitchAudioInputAsync(");
        AssertContains(audioCommandsText, "public static async Task<bool> SetAnalogGainAsync(");
        AssertContains(audioCommandsText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(audioCommandsText, "NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device)");
        AssertContains(audioCommandsText, "ExecuteAudioSwitch(handle, node.NodeId, analog, gainByte, sourceLabel, ct)");
        AssertContains(audioCommandsText, "ExecuteGainChange(handle, node.NodeId, gainByte, persistFlash, ct)");
        AssertDoesNotContain(audioCommandsText, "private static bool ExecuteGainChange(");
        AssertDoesNotContain(audioCommandsText, "internal static void ComputeGainRegisters(");
        AssertDoesNotContain(audioCommandsText, "private static bool ExecuteAudioSwitch(");
        AssertDoesNotContain(audioCommandsText, "private static bool SendSelector4Command(");
        AssertContains(analogGainText, "private static bool ExecuteGainChange(");
        AssertContains(analogGainText, "internal static void ComputeGainRegisters(");
        AssertContains(analogGainText, "SendSelector4Command(");
        AssertContains(audioSwitchText, "private static bool ExecuteAudioSwitch(");
        AssertContains(audioSwitchText, "NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i}");
        AssertContains(audioSwitchText, "commands=14");
        AssertContains(atProtocolText, "private static bool SendSelector4Command(");
        AssertContains(atProtocolText, "BuildAtWriteFrame(cmdCode, inputData)");
        AssertContains(atProtocolText, "TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32)");
        AssertContains(deviceSupportText, "internal static class NativeXuDeviceSupport");
        AssertContains(deviceSupportText, "public static bool TryGetSupported4kXIds(");
        AssertContains(deviceSupportText, "public static bool IsSupported4kXDevice(");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.Selector4.cs");
        AssertContains(probeProjectText, "NativeXuDeviceSupport.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_PayloadDecodingLivesInFocusedPartial()
    {
        var atProtocolText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs")
            .Replace("\r\n", "\n");
        var payloadDecodingText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(atProtocolText, "private static AtCommandResult SendAtCommand(");
        AssertContains(atProtocolText, "private static byte[] BuildAtWriteFrame(int cmdCode, byte[] inputData)");
        AssertContains(atProtocolText, "private static byte[] StripAtFrameEnvelope(byte[] responseFrame, int frameLength)");
        AssertDoesNotContain(atProtocolText, "private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)");
        AssertDoesNotContain(atProtocolText, "private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)");
        AssertDoesNotContain(atProtocolText, "private static string? InferFrameRateRational(double? frameRate)");
        AssertContains(payloadDecodingText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(payloadDecodingText, "private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)");
        AssertContains(payloadDecodingText, "private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)");
        AssertContains(payloadDecodingText, "private static string? InferFrameRateRational(double? frameRate)");
        AssertContains(payloadDecodingText, "private static SourceTelemetryConfidence ResolveConfidence(");
        AssertContains(payloadDecodingText, "private static string? TryDecodePrintableAscii(byte[] buffer)");
        AssertContains(payloadDecodingText, "private static string? DecodeCString(byte[] buffer)");
        AssertContains(payloadDecodingText, "private static string BoolToToken(bool? value)");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.PayloadDecoding.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_TelemetryDetailsLiveInFocusedPartials()
    {
        var buildText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.Build.cs")
            .Replace("\r\n", "\n");
        var audioInputText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs")
            .Replace("\r\n", "\n");
        var formattersText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(buildText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(buildText, "private static IReadOnlyList<SourceTelemetryDetailEntry> BuildDetailEntries(");
        AssertContains(buildText, "private static void AddAtDetail(");
        AssertContains(buildText, "private static string? TryFormatAtDetailValue(");
        AssertDoesNotContain(buildText, "private static bool IsValidFlashAudioData(");
        AssertDoesNotContain(buildText, "private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(");
        AssertContains(audioInputText, "private static bool IsValidFlashAudioData(AtCommandResult flashResult)");
        AssertContains(audioInputText, "private static string? ResolveAudioInputSource(");
        AssertContains(audioInputText, "private static SourceAudioInputMode? ResolveAudioInputMode(");
        AssertContains(audioInputText, "private static int? ResolveAnalogGainByte(AtCommandResult flashResult)");
        AssertContains(audioInputText, "private static IReadOnlyList<SourceTelemetryDetailEntry> AppendFlashAudioAnalogGainDetail(");
        AssertContains(audioInputText, "TelemetryLabels.AnalogGain");
        AssertContains(audioInputText, "private static (string Value, string? RawValue) FormatInputSourceDetail(byte[] data)");
        AssertContains(formattersText, "private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(byte[] data)");
        AssertContains(formattersText, "private static (string Value, string? RawValue) FormatAsciiOrHexDetail(byte[] data)");
        AssertDoesNotContain(formattersText, "private static string? DecodeCString(byte[] buffer)");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");

        return Task.CompletedTask;
    }

    internal static async Task NativeXuTelemetry_AcceptsKnown4kXProductRevisions()
    {
        var provider = CreateInstance("Sussudio.Services.Telemetry.NativeXuAtCommandProvider");

        foreach (var productId in new[] { "009b", "009c", "009d" })
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
}
