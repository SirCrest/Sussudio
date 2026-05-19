using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task NativeXuAudioControlService_ProfilesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var profilesText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.Profiles.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        AssertContains(ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj"), "NativeXuAudioControlService.Profiles.cs");
        AssertContains(profilesText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(profilesText, "private static readonly int[] InputByteIndexes");
        AssertContains(profilesText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(profilesText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(profilesText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(profilesText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(profilesText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(profilesText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(profilesText, "private static byte[] ParseHex(string hex)");
        AssertContains(profilesText, "private readonly record struct GainProfile");
        AssertDoesNotContain(rootText, "private static readonly int[] InputByteIndexes");
        AssertDoesNotContain(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertDoesNotContain(rootText, "private static byte[] ParseHex(string hex)");
        AssertDoesNotContain(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertDoesNotContain(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");

        return Task.CompletedTask;
    }

    private static Task NativeXuAudioControlService_TransportLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var transportText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.Transport.cs")
            .Replace("\r\n", "\n");
        var rawTransportText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.RawTransport.cs")
            .Replace("\r\n", "\n");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(transportText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(transportText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(transportText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(transportText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(transportText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(rawTransportText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(rawTransportText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(rawTransportText, "private static bool TryReadRawPayload(");
        AssertContains(rawTransportText, "private static bool TryWriteRawPayload(");
        AssertContains(rawTransportText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(rawTransportText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(rawTransportText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(rawTransportText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(rawTransportText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(rawTransportText, "private readonly record struct RawControlCandidate");
        AssertContains(rawTransportText, "private readonly record struct RawPayloadSnapshot");
        AssertDoesNotContain(transportText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(");
        AssertContains(deviceSupportText, "public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertContains(probeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertContains(probeProjectText, "NativeXuDeviceSupport.cs");
        AssertDoesNotContain(rootText, "private static readonly Guid XuGuid");
        AssertDoesNotContain(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertDoesNotContain(rootText, "private static bool TryReadRawPayload(");
        AssertDoesNotContain(transportText, "private static bool TryWriteRawPayload(");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAudioMeters_OwnCallbackMeterState()
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
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.AudioLevelUpdated += _viewModel.OnAudioLevelUpdated;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.MicrophoneAudioLevelUpdated += _viewModel.OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "private const double MeterFloorDb");
        AssertDoesNotContain(baseText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertDoesNotContain(baseText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");

        return Task.CompletedTask;
    }
}
