using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Audio;

internal sealed partial class NativeXuAudioControlService
{
    private async Task<bool> UpdatePayloadAsync(
        CaptureDevice? device,
        Func<byte[], bool> mutator,
        CancellationToken cancellationToken)
    {
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
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
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken)
    {
        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
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
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }
}
