using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task NativeXuAudioControlService_LivesInCohesiveServiceFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "internal sealed class NativeXuAudioControlService");
        AssertDoesNotContain(rootText, "partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static readonly int[] InputByteIndexes");
        AssertContains(rootText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(rootText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(rootText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(rootText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(rootText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(rootText, "private static byte[] ParseHex(string hex)");
        AssertContains(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(rootText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(rootText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(rootText, "private static bool TryReadRawPayload(");
        AssertContains(rootText, "private static bool TryWriteRawPayload(");
        AssertContains(rootText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(rootText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(rootText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(rootText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(rootText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(rootText, "private readonly record struct GainProfile");
        AssertContains(rootText, "private readonly record struct RawControlCandidate");
        AssertContains(rootText, "private readonly record struct RawPayloadSnapshot");
        AssertDoesNotContain(rootText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(");
        AssertContains(deviceSupportText, "public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertContains(probeProjectText, "NativeXuDeviceSupport.cs");
        foreach (var removedFile in new[]
        {
            "NativeXuAudioControlService.Profiles.cs",
            "NativeXuAudioControlService.Transport.cs",
            "NativeXuAudioControlService.RawTransport.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAudioMeters_OwnCallbackMeterState()
    {
        var baseText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs")
            .Replace("\r\n", "\n");
        var metersText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMeters.cs")
            .Replace("\r\n", "\n");

        AssertContains(metersText, "public double AudioMeterTarget;");
        AssertContains(metersText, "public double MicrophoneMeterTarget;");
        AssertContains(metersText, "public event Action? AudioMeterActivated;");
        AssertContains(metersText, "public event Action? MicrophoneMeterActivated;");
        AssertContains(metersText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(metersText, "private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(metersText, "private void ResetAudioMeter()");
        AssertContains(metersText, "public void ResetAudioMeterTimerFlag()");
        AssertContains(metersText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertDoesNotContain(baseText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "private const double MeterFloorDb");
        AssertDoesNotContain(baseText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertDoesNotContain(baseText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");

        return Task.CompletedTask;
    }
}
