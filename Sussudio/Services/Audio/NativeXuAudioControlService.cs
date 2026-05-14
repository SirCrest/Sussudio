using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Audio;

// Controls the 4K X audio input route through the vendor UVC extension-unit
// payload. Only the observed stable bytes are mutated; volatile counters and
// timer bytes are preserved so read/modify/write does not trample firmware
// state outside the HDMI/Analog selection.
internal sealed partial class NativeXuAudioControlService
{
    private static readonly Guid XuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private const int SelectorId = 3;
    private const int SelectorBufferSize = 2048;
    private const int RawHeaderBytes = 0;
    private const int TransportGateAttempts = 6;
    private const int TransportGateRetryDelayMs = 250;
    private const string PreferredInterfaceFragment = "{65e8773d-8f56-11d0-a3b9-00a0c9223196}";

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
