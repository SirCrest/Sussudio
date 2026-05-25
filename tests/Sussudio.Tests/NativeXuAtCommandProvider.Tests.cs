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
        var rollingCommandGroupsText = rollingPollText;
        var snapshotAssemblyText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs")
            .Replace("\r\n", "\n");
        var telemetryDetailsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs")
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
        AssertContains(rollingPollText, "private AtCommandResult SendRollingCommand(");
        AssertContains(rollingPollText, "private void PopulateInitialRollingCache(");
        AssertContains(rollingPollText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(rollingCommandGroupsText, "private AtCommandResult SendRollingCommand(");
        AssertContains(rollingCommandGroupsText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(rollingCommandGroupsText, "private void PopulateInitialRollingCache(");
        AssertContains(rollingCommandGroupsText, "private void RefreshRollingGroup(");
        AssertContains(rollingCommandGroupsText, "case 5: // Diagnostics");
        AssertDoesNotContain(rollingCommandGroupsText, "private static bool IsUnsupportedNodeFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.RollingCommandGroups.cs")),
            "rolling command batch dispatch folded into NativeXuAtCommandProvider.RollingPoll.cs");
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
        AssertContains(telemetryDetailsText, "private static string ResolveSnapshotAudioInputOrigin(");
        AssertContains(telemetryDetailsText, "\"nativexu-flash-audio\"");
        AssertDoesNotContain(snapshotAssemblyText, "TelemetryLabels.AnalogGain");
        AssertDoesNotContain(snapshotAssemblyText, "Math.Exp(4.0 * y)");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.RollingCommandGroups.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");

        return Task.CompletedTask;
    }

    internal static Task NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial()
    {
        var deviceCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs")
            .Replace("\r\n", "\n");
        var audioCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs")
            .Replace("\r\n", "\n");
        var atProtocolText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs")
            .Replace("\r\n", "\n");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(deviceCommandsText, "public static async Task<bool> SendAtSetCommandAsync(");
        AssertContains(deviceCommandsText, "public static Task<bool> SetInputSourceAsync(");
        AssertContains(deviceCommandsText, "public static async Task<byte[]?> ReadAtCommandAsync(");
        AssertContains(deviceCommandsText, "SendAtCommand(handle, node.NodeId, label, cmdCode)");
        AssertContains(deviceCommandsText, "NATIVEXU_GET_EXCEPTION");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.DeviceCommandReads.cs")),
            "Native XU public read commands stay folded into DeviceCommands.cs with the generic SET surface.");
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
        AssertContains(audioCommandsText, "private static bool ExecuteAudioSwitch(");
        AssertContains(audioCommandsText, "NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i}");
        AssertContains(audioCommandsText, "commands=14");
        AssertContains(audioCommandsText, "private static bool ExecuteGainChange(");
        AssertContains(audioCommandsText, "internal static void ComputeGainRegisters(");
        AssertDoesNotContain(audioCommandsText, "private static bool SendSelector4Command(");
        AssertContains(audioCommandsText, "SendSelector4Command(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AudioSwitch.cs")),
            "audio switch execution folded into audio command owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.AnalogGain.cs")),
            "analog gain execution folded into audio command owner");
        AssertContains(atProtocolText, "private static bool SendSelector4Command(");
        AssertContains(atProtocolText, "BuildAtWriteFrame(cmdCode, inputData)");
        AssertContains(atProtocolText, "TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32)");
        AssertContains(deviceSupportText, "internal static class NativeXuDeviceSupport");
        AssertContains(deviceSupportText, "public static bool TryGetSupported4kXIds(");
        AssertContains(deviceSupportText, "public static bool IsSupported4kXDevice(");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
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
        var telemetryDetailsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(telemetryDetailsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(telemetryDetailsText, "private static IReadOnlyList<SourceTelemetryDetailEntry> BuildDetailEntries(");
        AssertContains(telemetryDetailsText, "private static void AddAtDetail(");
        AssertContains(telemetryDetailsText, "private static string? TryFormatAtDetailValue(");
        AssertContains(telemetryDetailsText, "private static bool IsValidFlashAudioData(AtCommandResult flashResult)");
        AssertContains(telemetryDetailsText, "private static string? ResolveAudioInputSource(");
        AssertContains(telemetryDetailsText, "private static SourceAudioInputMode? ResolveAudioInputMode(");
        AssertContains(telemetryDetailsText, "private static int? ResolveAnalogGainByte(AtCommandResult flashResult)");
        AssertContains(telemetryDetailsText, "private static IReadOnlyList<SourceTelemetryDetailEntry> AppendFlashAudioAnalogGainDetail(");
        AssertContains(telemetryDetailsText, "TelemetryLabels.AnalogGain");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatInputSourceDetail(byte[] data)");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(byte[] data)");
        AssertContains(telemetryDetailsText, "private static (string Value, string? RawValue) FormatAsciiOrHexDetail(byte[] data)");
        AssertDoesNotContain(telemetryDetailsText, "private static string? DecodeCString(byte[] buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs")),
            "Native XU audio input detail helpers folded into the telemetry details owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.Build.cs")),
            "Native XU detail row assembly folded into the telemetry details owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs")),
            "Native XU AT detail formatters folded into the telemetry details owner");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");

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
