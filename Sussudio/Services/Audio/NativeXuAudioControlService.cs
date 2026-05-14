using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Audio;

// Controls the 4K X audio input route through the vendor UVC extension-unit
// payload. Only the observed stable bytes are mutated; volatile counters and
// timer bytes are preserved so read/modify/write does not trample firmware
// state outside the HDMI/Analog selection.
internal sealed partial class NativeXuAudioControlService
{
    public IReadOnlyList<string> GetSupportedModes() => SupportedModes;

    public async Task<DeviceAudioControlState> ReadStateAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        var payload = await ReadPreferredPayloadAsync(device, cancellationToken).ConfigureAwait(false);
        if (payload == null)
        {
            return DeviceAudioControlState.Unsupported("payload-unavailable");
        }

        var snapshot = payload.Value;
        var input = DecodeInput(snapshot.NormalizedPayload);
        var gain = SupportsAnalogGainReadback ? DecodeGain(snapshot.NormalizedPayload) : default(AnalogGainDecision?);
        var gainDescription = gain.HasValue
            ? $"{gain.Value.Label}({gain.Value.Confidence:0.00})"
            : "unavailable(no_gain_indexes)";
        Logger.Log($"NATIVEXU_AUDIO_STATE input={input.Label}({input.Confidence:0.00}) gain={gainDescription} len={snapshot.NormalizedPayload.Length}");
        return new DeviceAudioControlState(
            IsSupported: true,
            InterfacePath: snapshot.InterfacePath,
            Mode: input.Label,
            AnalogGainPercent: gain?.Percent,
            RawGainValue: gain?.Percent);
    }

    internal async Task<NativeXuAudioPayloadSnapshot?> ReadPayloadSnapshotAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        ushort? vendorId = null;
        ushort? productId = null;
        if (NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out var parsedVendorId, out var parsedProductId))
        {
            vendorId = parsedVendorId;
            productId = parsedProductId;
        }

        var snapshot = await ReadPreferredPayloadAsync(device, cancellationToken).ConfigureAwait(false);
        if (snapshot == null)
        {
            return null;
        }

        var payload = snapshot.Value;
        var controlByteIndexes = InputByteIndexes
            .Concat(GainByteIndexes)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();

        return new NativeXuAudioPayloadSnapshot(
            DeviceId: device?.Id,
            DeviceName: device?.Name,
            VendorId: vendorId,
            ProductId: productId,
            InterfacePath: payload.InterfacePath,
            NodeId: payload.NodeId,
            SelectorId: SelectorId,
            TimestampUtc: DateTimeOffset.UtcNow,
            RawPayload: payload.RawPayload.ToArray(),
            NormalizedPayload: payload.NormalizedPayload.ToArray(),
            ControlByteIndexes: controlByteIndexes,
            VolatileByteIndexes: DynamicByteIndexes.ToArray());
    }

    public async Task<bool> SetAudioModeAsync(
        CaptureDevice? device,
        string mode,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetTargetInputReference(mode, out var reference))
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_SET_SKIPPED unsupported-mode='{mode ?? "(null)"}'");
            return false;
        }

        var updated = await UpdatePayloadAsync(
            device,
            normalizedPayload =>
            {
                foreach (var index in InputByteIndexes)
                {
                    if (index >= normalizedPayload.Length || index >= reference.Length)
                    {
                        return false;
                    }

                    normalizedPayload[index] = reference[index];
                }

                return true;
            },
            cancellationToken).ConfigureAwait(false);

        Logger.Log(updated
            ? $"NATIVEXU_AUDIO_MODE_SET_OK mode='{mode}'"
            : $"NATIVEXU_AUDIO_MODE_SET_FAILED mode='{mode}'");
        return updated;
    }

    public async Task<bool> SetAnalogGainPercentAsync(
        CaptureDevice? device,
        double percent,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsAnalogGainReadback)
        {
            Logger.Log("NATIVEXU_ANALOG_GAIN_SET_SKIPPED unsupported=no_gain_indexes");
            return false;
        }

        var profile = ResolveGainProfile(percent);
        var updated = await UpdatePayloadAsync(
            device,
            normalizedPayload =>
            {
                foreach (var index in GainByteIndexes)
                {
                    if (index >= normalizedPayload.Length || index >= profile.ReferenceBytes.Length)
                    {
                        return false;
                    }

                    normalizedPayload[index] = profile.ReferenceBytes[index];
                }

                return true;
            },
            cancellationToken).ConfigureAwait(false);

        Logger.Log(updated
            ? $"NATIVEXU_ANALOG_GAIN_SET_OK percent={profile.Percent}"
            : $"NATIVEXU_ANALOG_GAIN_SET_FAILED percent={profile.Percent}");
        return updated;
    }

    internal sealed record DeviceAudioControlState(
        bool IsSupported,
        string? InterfacePath,
        string? Mode,
        int? AnalogGainPercent,
        int? RawGainValue)
    {
        public static DeviceAudioControlState Unsupported(string reason)
            => new(false, reason, null, null, null);
    }
}

internal sealed record NativeXuAudioPayloadSnapshot(
    string? DeviceId,
    string? DeviceName,
    ushort? VendorId,
    ushort? ProductId,
    string InterfacePath,
    int NodeId,
    int SelectorId,
    DateTimeOffset TimestampUtc,
    byte[] RawPayload,
    byte[] NormalizedPayload,
    IReadOnlyList<int> ControlByteIndexes,
    IReadOnlyList<int> VolatileByteIndexes);
