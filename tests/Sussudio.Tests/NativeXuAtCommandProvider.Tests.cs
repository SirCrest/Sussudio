using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task NativeXuAtCommandProvider_RollingPollLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs")
            .Replace("\r\n", "\n");
        var rollingPollText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "public async Task<SourceSignalTelemetrySnapshot> ReadAsync(");
        AssertContains(rootText, "var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);");
        AssertDoesNotContain(rootText, "private NodeReadAttempt TryReadRolling(");
        AssertDoesNotContain(rootText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertDoesNotContain(rootText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertContains(rollingPollText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(rollingPollText, "private int _rollingGroup;");
        AssertContains(rollingPollText, "private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap");
        AssertContains(rollingPollText, "private static readonly double[] CanonicalFrameRates");
        AssertContains(rollingPollText, "private NodeReadAttempt TryReadRolling(");
        AssertContains(rollingPollText, "private NodeReadAttempt BuildSnapshotFromCachedResults(");
        AssertContains(rollingPollText, "BuildDetailEntries(");
        AssertContains(rollingPollText, "new SourceSignalTelemetrySnapshot");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");

        return Task.CompletedTask;
    }

    private static Task NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial()
    {
        var deviceCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs")
            .Replace("\r\n", "\n");
        var audioCommandsText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(deviceCommandsText, "public static async Task<bool> SendAtSetCommandAsync(");
        AssertContains(deviceCommandsText, "public static Task<bool> SetInputSourceAsync(");
        AssertContains(deviceCommandsText, "public static async Task<byte[]?> ReadAtCommandAsync(");
        AssertDoesNotContain(deviceCommandsText, "public static async Task<bool> SwitchAudioInputAsync(");
        AssertDoesNotContain(deviceCommandsText, "public static async Task<bool> SetAnalogGainAsync(");
        AssertDoesNotContain(deviceCommandsText, "internal static void ComputeGainRegisters(");
        AssertContains(audioCommandsText, "public sealed partial class NativeXuAtCommandProvider");
        AssertContains(audioCommandsText, "public static async Task<bool> SwitchAudioInputAsync(");
        AssertContains(audioCommandsText, "public static async Task<bool> SetAnalogGainAsync(");
        AssertContains(audioCommandsText, "private static bool ExecuteGainChange(");
        AssertContains(audioCommandsText, "internal static void ComputeGainRegisters(");
        AssertContains(audioCommandsText, "private static bool ExecuteAudioSwitch(");
        AssertContains(audioCommandsText, "private static bool SendSelector4Command(");
        AssertContains(audioCommandsText, "NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i}");
        AssertContains(probeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");

        return Task.CompletedTask;
    }

    private static Task NativeXuAtCommandProvider_PayloadDecodingLivesInFocusedPartial()
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

    private static Task NativeXuAtCommandProvider_TelemetryDetailsLiveInFocusedPartials()
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

    private static async Task NativeXuTelemetry_AcceptsKnown4kXProductRevisions()
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
