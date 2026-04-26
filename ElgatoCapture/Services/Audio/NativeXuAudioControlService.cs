using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.Win32.SafeHandles;
using ElgatoCapture.Services.Devices;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Audio;

internal sealed class NativeXuAudioControlService
{
    private static readonly Guid XuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    // Control byte indexes: stable bytes that differ between HDMI and Analog modes.
    // Captured from PID 0x009B firmware via Elgato Studio toggling.
    // Dynamic bytes (counters/timers) are excluded by the diagnostic snapshot below.
    private static readonly int[] InputByteIndexes =
    {
        0, 1, 2, 4, 5, 7, 8, 12, 13, 15, 16, 19, 20, 21, 22, 23, 24, 26, 27,
        28, 29, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45,
        46, 47, 48, 51, 53, 54, 55, 56, 57, 58, 59, 60, 61, 63, 64, 65, 66,
        67, 68, 71, 72, 74, 76, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88,
        89, 90, 91, 97, 98, 99, 100, 101, 102, 103, 105, 106, 107, 108, 109,
        110, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127,
        129, 130, 131, 132, 133, 134, 135, 136, 138, 139, 140, 142, 143, 147,
        148, 149
    };
    private static readonly int[] DynamicByteIndexes =
    {
        92, 93, 94, 95, 96, 104, 111, 112, 114, 128, 144, 145, 146
    };
    private static readonly int[] GainByteIndexes = Array.Empty<int>();
    // PID 0x009B HDMI reference — first ~87 bytes are zero, data starts at the end.
    private static readonly byte[] HdmiReference = ParseHex(
        "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003400000059A80029A8000029A800A8007900F043301ACE1040C600C00000060606001B301A001D301A601C301A2CA80029A800A800A8000A00010000000000");
    // PID 0x009B Analog reference — data throughout.
    private static readonly byte[] AnalogReference = ParseHex(
        "058545008004009A080000000E9F002F8000006F0899905E2D0029C398A80079F09F433080CE109FE4A2433080CE10103900001A00060606801B301A2CA80029A8D6905EA8000029A800A8007900F043301ACE10B8F043301ACE101039000030000606061A1B301A1A2DA80029A80029A8000029A800A8007900F043301ACE10B8E400C00000060606001B301A001D301A601C301A2D");
    private static readonly IReadOnlyList<string> SupportedModes = new[] { DeviceAudioMode.Hdmi, DeviceAudioMode.Analog };
    // Gain profiles not yet captured for PID 0x009B firmware — gain control disabled until fresh data.
    private static readonly GainProfile[] GainProfiles =
    {
        new(0, "0%", AnalogReference),
        new(50, "50%", AnalogReference),
        new(100, "100%", AnalogReference)
    };

    private const int SelectorId = 3;
    private const int SelectorBufferSize = 2048;
    private const int RawHeaderBytes = 0;
    private const int TransportGateAttempts = 6;
    private const int TransportGateRetryDelayMs = 250;
    private const string PreferredInterfaceFragment = "{65e8773d-8f56-11d0-a3b9-00a0c9223196}";
    private static bool SupportsAnalogGainReadback => GainByteIndexes.Length > 0;

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

    private static bool TryGetTargetInputReference(string? mode, out byte[] reference)
    {
        if (string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            reference = AnalogReference;
            return true;
        }

        if (string.Equals(mode, DeviceAudioMode.Hdmi, StringComparison.OrdinalIgnoreCase))
        {
            reference = HdmiReference;
            return true;
        }

        reference = Array.Empty<byte>();
        return false;
    }

    private static GainProfile ResolveGainProfile(double percent)
    {
        var clamped = Math.Clamp(percent, 0.0, 100.0);
        return GainProfiles
            .OrderBy(profile => Math.Abs(profile.Percent - clamped))
            .ThenBy(profile => profile.Percent)
            .First();
    }

    private static AudioDecodeDecision DecodeInput(byte[] payload)
    {
        var hdmiDistance = 0;
        var analogDistance = 0;
        foreach (var index in InputByteIndexes)
        {
            var live = ByteAt(payload, index);
            var hdmi = ByteAt(HdmiReference, index);
            var analog = ByteAt(AnalogReference, index);
            if (live < 0 || hdmi < 0 || analog < 0)
            {
                hdmiDistance += 255;
                analogDistance += 255;
                continue;
            }

            hdmiDistance += Math.Abs(live - hdmi);
            analogDistance += Math.Abs(live - analog);
        }

        if (hdmiDistance == analogDistance)
        {
            return new AudioDecodeDecision("Unknown", 0d);
        }

        var label = hdmiDistance < analogDistance ? DeviceAudioMode.Hdmi : DeviceAudioMode.Analog;
        var best = Math.Min(hdmiDistance, analogDistance);
        var worst = Math.Max(hdmiDistance, analogDistance);
        var confidence = worst == 0 ? 1d : (worst - best) / (double)worst;
        return new AudioDecodeDecision(label, confidence);
    }

    private static AnalogGainDecision DecodeGain(byte[] payload)
    {
        var bestProfile = GainProfiles
            .Select(profile => new
            {
                Profile = profile,
                Distance = ComputeDistance(payload, profile.ReferenceBytes, GainByteIndexes)
            })
            .OrderBy(result => result.Distance)
            .ThenBy(result => result.Profile.Percent)
            .First();

        var worstDistance = GainProfiles
            .Select(profile => ComputeDistance(payload, profile.ReferenceBytes, GainByteIndexes))
            .Max();
        var confidence = worstDistance == 0 ? 1d : (worstDistance - bestProfile.Distance) / (double)worstDistance;

        return new AnalogGainDecision(bestProfile.Profile.Label, confidence, bestProfile.Profile.Percent);
    }

    private static int ComputeDistance(byte[] payload, byte[] reference, IReadOnlyList<int> indexes)
    {
        var distance = 0;
        foreach (var index in indexes)
        {
            var live = ByteAt(payload, index);
            var target = ByteAt(reference, index);
            if (live < 0 || target < 0)
            {
                distance += 255;
                continue;
            }

            distance += Math.Abs(live - target);
        }

        return distance;
    }

    private static int ByteAt(byte[] payload, int index)
        => index >= 0 && index < payload.Length ? payload[index] : -1;

    private async Task<bool> UpdatePayloadAsync(
        CaptureDevice? device,
        Func<byte[], bool> mutator,
        CancellationToken cancellationToken)
    {
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            Logger.Log("NATIVEXU_AUDIO_PAYLOAD device-unsupported");
            return false;
        }

        if (string.IsNullOrWhiteSpace(device?.NativeXuInterfacePath))
        {
            Logger.Log("NATIVEXU_AUDIO_PAYLOAD missing-selected-interface");
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                Logger.Log("NATIVEXU_AUDIO_PAYLOAD gate-timeout");
                return false;
            }

            var candidateCount = 0;
            foreach (var candidate in EnumerateCandidates(vendorId, productId, device?.NativeXuInterfacePath))
            {
                candidateCount++;
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadRawPayload(candidate, out var rawPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} read-failed path={candidate.InterfacePath}");
                    continue;
                }

                var normalizedPayload = NormalizePayload(rawPayload);
                if (normalizedPayload.Length == 0)
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} normalize-empty rawLen={rawPayload.Length}");
                    continue;
                }

                var mutatedPayload = normalizedPayload.ToArray();
                if (!mutator(mutatedPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} mutator-failed");
                    continue;
                }

                if (mutatedPayload.SequenceEqual(normalizedPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} already-correct");
                    return true;
                }

                var rawMutatedPayload = RehydrateRawPayload(rawPayload, mutatedPayload);
                if (!TryWriteRawPayload(candidate, rawMutatedPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} write-failed");
                    continue;
                }

                // Give firmware time to commit the write before verifying
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                if (!TryReadRawPayload(candidate, out var verifyRawPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verify-read-failed");
                    continue;
                }

                var verifyNormalizedPayload = NormalizePayload(verifyRawPayload);
                if (verifyNormalizedPayload.SequenceEqual(mutatedPayload))
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verified-ok");
                    return true;
                }

                // The selector 3 payload contains dynamic bytes (counters, status) that
                // change between reads. Only verify the specific bytes we mutated
                // (InputByteIndexes or GainByteIndexes) — ignore the rest.
                var controlBytesMatch = true;
                var checkedCount = 0;
                var mismatchCount = 0;
                foreach (var index in InputByteIndexes.Concat(GainByteIndexes))
                {
                    if (index >= mutatedPayload.Length || index >= verifyNormalizedPayload.Length)
                        continue;
                    checkedCount++;
                    if (mutatedPayload[index] != verifyNormalizedPayload[index])
                    {
                        mismatchCount++;
                        controlBytesMatch = false;
                    }
                }

                if (controlBytesMatch)
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verified-control-bytes checked={checkedCount} ok");
                    return true;
                }

                Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verify-mismatch control={mismatchCount}/{checkedCount}");
            }

            Logger.Log($"NATIVEXU_AUDIO_PAYLOAD no-candidate-succeeded count={candidateCount}");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuAtCommandProvider.ReleaseTransportGate();
            }
        }
    }

    private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken)
    {
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(device?.NativeXuInterfacePath))
        {
            Logger.Log("NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
            return null;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return null;
            }

            foreach (var candidate in EnumerateCandidates(vendorId, productId, device?.NativeXuInterfacePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadRawPayload(candidate, out var rawPayload))
                {
                    continue;
                }

                var normalizedPayload = NormalizePayload(rawPayload);
                if (normalizedPayload.Length == 0)
                {
                    continue;
                }

                return new RawPayloadSnapshot(candidate.InterfacePath, candidate.NodeId, rawPayload, normalizedPayload);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuAtCommandProvider.ReleaseTransportGate();
            }
        }
    }

    private static IEnumerable<RawControlCandidate> EnumerateCandidates(
        ushort vendorId,
        ushort productId,
        string? selectedInterfacePath)
    {
        if (string.IsNullOrWhiteSpace(selectedInterfacePath))
        {
            yield break;
        }

        var orderedInterfaces = new[] { new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty) };

        foreach (var ksInterface in orderedInterfaces)
        {
            using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
            if (handle is null)
            {
                continue;
            }

            if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
            {
                continue;
            }

            var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
            foreach (var node in nodeList.Where(node => node.IsDevSpecific))
            {
                yield return new RawControlCandidate(ksInterface.Path, node.NodeId);
            }
        }
    }

    private static bool TryReadRawPayload(RawControlCandidate candidate, out byte[] payload)
    {
        payload = Array.Empty<byte>();
        using var handle = KsExtensionUnitNative.TryOpen(candidate.InterfacePath, out _);
        if (handle is null)
        {
            return false;
        }

        return KsExtensionUnitNative.TryXuGetDirect(
                   handle,
                   candidate.NodeId,
                   XuGuid,
                   SelectorId,
                   SelectorBufferSize,
                   out payload,
                   out var bytesReturned,
                   out _)
               && bytesReturned > RawHeaderBytes;
    }

    private static bool TryWriteRawPayload(RawControlCandidate candidate, byte[] payload)
    {
        using var handle = KsExtensionUnitNative.TryOpen(candidate.InterfacePath, out _);
        if (handle is null)
        {
            return false;
        }

        return KsExtensionUnitNative.TryXuSetViaOutput(handle, candidate.NodeId, XuGuid, SelectorId, payload, out _);
    }

    private static byte[] NormalizePayload(byte[] rawPayload)
    {
        if (rawPayload.Length <= RawHeaderBytes)
        {
            return Array.Empty<byte>();
        }

        return rawPayload.AsSpan(RawHeaderBytes).ToArray();
    }

    private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)
    {
        var updated = rawPayload.ToArray();
        normalizedPayload.CopyTo(updated.AsSpan(RawHeaderBytes));
        return updated;
    }

    private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < TransportGateAttempts; attempt++)
        {
            if (await NativeXuAtCommandProvider.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            if (attempt + 1 < TransportGateAttempts)
            {
                await Task.Delay(TransportGateRetryDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static byte[] ParseHex(string hex)
    {
        var normalized = NormalizeHex(hex);
        if (normalized.Length % 2 != 0)
        {
            throw new InvalidOperationException("Hex payload must have an even number of characters.");
        }

        var bytes = new byte[normalized.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(normalized.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    private static string NormalizeHex(string value)
        => string.Concat(value.Where(Uri.IsHexDigit));

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

    private readonly record struct GainProfile(int Percent, string Label, byte[] ReferenceBytes);
    private readonly record struct AudioDecodeDecision(string Label, double Confidence);
    private readonly record struct AnalogGainDecision(string Label, double Confidence, int Percent);
    private readonly record struct RawControlCandidate(string InterfacePath, int NodeId);
    private readonly record struct RawPayloadSnapshot(string InterfacePath, int NodeId, byte[] RawPayload, byte[] NormalizedPayload);
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
