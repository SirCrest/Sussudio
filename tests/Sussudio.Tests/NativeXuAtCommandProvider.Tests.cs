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
}
