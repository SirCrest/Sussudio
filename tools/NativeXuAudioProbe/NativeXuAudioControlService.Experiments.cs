using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.NativeXu;

namespace Sussudio.Services.Audio;

// Raw selector-3 payload writes are kept in the probe assembly only. The app
// build includes the base service source without these experiment methods.
internal sealed partial class NativeXuAudioControlService
{
    public async Task<bool> ExperimentSetAudioModeAsync(
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

    public async Task<bool> ExperimentSetAnalogGainPercentAsync(
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

                // Selector 3 contains dynamic counters and status bytes. Verify
                // only the stable controls requested by this experiment.
                var controlBytesMatch = ControlBytesMatch(
                    mutatedPayload, verifyNormalizedPayload,
                    out var checkedCount, out var mismatchCount, out var missingCount);

                if (controlBytesMatch)
                {
                    Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verified-control-bytes checked={checkedCount} ok");
                    return true;
                }

                Logger.Log($"NATIVEXU_AUDIO_PAYLOAD candidate={candidateCount} verify-mismatch control={mismatchCount}/{checkedCount} missing={missingCount}");
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

    private static bool ControlBytesMatch(
        byte[] expected,
        byte[] actual,
        out int checkedCount,
        out int mismatchCount,
        out int missingCount)
    {
        checkedCount = 0;
        mismatchCount = 0;
        missingCount = 0;
        foreach (var index in InputByteIndexes.Concat(GainByteIndexes))
        {
            if (index >= expected.Length || index >= actual.Length)
            {
                missingCount++;
                continue;
            }

            checkedCount++;
            if (expected[index] != actual[index])
            {
                mismatchCount++;
            }
        }

        return checkedCount > 0 && missingCount == 0 && mismatchCount == 0;
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

    private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)
    {
        var updated = rawPayload.ToArray();
        normalizedPayload.CopyTo(updated.AsSpan(RawHeaderBytes));
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
}
