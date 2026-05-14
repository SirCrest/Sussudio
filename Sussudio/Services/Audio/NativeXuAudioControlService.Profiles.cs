using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Audio;

internal sealed partial class NativeXuAudioControlService
{
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
    private static bool SupportsAnalogGainReadback => GainByteIndexes.Length > 0;

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

    private readonly record struct GainProfile(int Percent, string Label, byte[] ReferenceBytes);
    private readonly record struct AudioDecodeDecision(string Label, double Confidence);
    private readonly record struct AnalogGainDecision(string Label, double Confidence, int Percent);
}
