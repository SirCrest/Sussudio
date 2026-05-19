using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Audio;

internal sealed partial class NativeXuAudioControlService
{
    private static readonly Guid XuGuid = NativeXuDeviceSupport.ExtensionUnitGuid;
    private const int SelectorId = 3;
    private const int SelectorBufferSize = 2048;
    private const int RawHeaderBytes = 0;
    private const int TransportGateAttempts = 6;
    private const int TransportGateRetryDelayMs = 250;

    private static IEnumerable<RawControlCandidate> EnumerateCandidates(
        ushort vendorId,
        ushort productId,
        string? selectedInterfacePath)
    {
        if (string.IsNullOrWhiteSpace(selectedInterfacePath))
        {
            yield break;
        }

        var orderedInterfaces = NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath);

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
            if (await NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false))
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

    private readonly record struct RawControlCandidate(string InterfacePath, int NodeId);
    private readonly record struct RawPayloadSnapshot(string InterfacePath, int NodeId, byte[] RawPayload, byte[] NormalizedPayload);
}
