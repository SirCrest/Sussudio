using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the capture-to-mixer latency measurement (docs/architecture/performance-telemetry.md section 2.9).
// A wrong latency that looks plausible is worse than no latency, so the cases below are mostly about refusing:
//
//   1. A known delayed copy (with a gain change) must be measured within 1 ms, with the right gain difference.
//   2. Delays near both ends of the 0-1000 ms search window must still be measured.
//   3. A delay outside the search window must be refused, never reported as some other lag.
//   4. Silent input and silent output must each be refused with their own reason.
//   5. Unrelated audio must be refused as low confidence.
//   6. Steady and exactly repeating program audio carry no (or an ambiguous) delay and must be refused.
//   7. Missing packets or missing timestamps must be "unavailable", not a guess.
//   8. Capture delivery jitter (spikes on a few packets) must not move the result.
//   9. A gap in the loopback stream must not break the alignment of the audio around it.
//  10. An output the waveform of which no longer matches (inverted polarity) must fall back to the 1 ms envelope
//      resolution and say so, not report sample precision it does not have.
public sealed class AudioLatencyAnalysisTests
{
    private const long Origin = 5_000_000_000; // 500 s of QPC, in 100 ns units
    private const int PacketFrames = 480;
    private static readonly int Frames5s = AudioTestSignals.Rate * 5;
    private static readonly int Frames6s = AudioTestSignals.Rate * 6;

    private static AudioLatencyResult Measure(
        float[] input,
        float[] output,
        Func<int, long>? inputOffset = null,
        Func<int, long>? outputOffset = null,
        Func<int, bool>? dropOutputPacket = null)
    {
        var inputPackets = AudioTestSignals.Packets(input.Length / 2, PacketFrames, Origin, inputOffset);
        var allOutputPackets = AudioTestSignals.Packets(output.Length / 2, PacketFrames, Origin, outputOffset);
        var outputPackets = new List<AudioPacket>();
        for (var index = 0; index < allOutputPackets.Length; index++)
        {
            if (dropOutputPacket?.Invoke(index) != true)
            {
                outputPackets.Add(allOutputPackets[index]);
            }
        }

        return AudioLatencyAnalyzer.Measure(input, inputPackets, output, outputPackets.ToArray());
    }

    private static void AssertOk(AudioLatencyResult result) =>
        Assert.True(result.Status == "ok", $"{result.Status}: {result.Reason} (confidence {result.Confidence}, second-peak ratio {result.SecondPeakRatio})");

    [Fact]
    public void AKnownDelayedAttenuatedCopyIsMeasuredWithinOneMillisecond()
    {
        var input = AudioTestSignals.BurstNoise(Frames5s, seed: 11);
        var output = AudioTestSignals.Delayed(input, 1_790, 0.5f); // 37.29 ms, -6.02 dB

        var result = Measure(
            input,
            output,
            inputOffset: index => (long)(index * 7919 % 31) * 1_000,        // 0 to 3 ms of delivery delay, never negative
            outputOffset: index => (index % 3 - 1) * 500L);                 // +/- 0.05 ms engine timestamp jitter

        AssertOk(result);
        Assert.NotNull(result.LatencyMs);
        Assert.Equal(37.29, result.LatencyMs!.Value, 1.0);
        Assert.Equal("sample", result.Resolution);
        Assert.True(result.Confidence >= 0.9, $"confidence {result.Confidence}");
        Assert.NotNull(result.GainDifferenceDb);
        Assert.Equal(-6.02, result.GainDifferenceDb!.Value, 0.3);
        Assert.True(result.InputTimelineSpreadMs <= 3.2, $"spread {result.InputTimelineSpreadMs}");
        Assert.Equal(AudioLatencyAnalyzer.MeasurementScope, result.Scope);
    }

    [Theory]
    [InlineData(5.0)]
    [InlineData(250.0)]
    [InlineData(900.0)]
    public void DelaysAcrossTheSearchWindowAreMeasured(double delayMs)
    {
        var input = AudioTestSignals.BurstNoise(Frames6s, seed: 21);
        var delayFrames = (int)Math.Round(delayMs * AudioTestSignals.Rate / 1000.0);
        var output = AudioTestSignals.Delayed(input, delayFrames, 1.0f);

        var result = Measure(input, output);

        AssertOk(result);
        Assert.Equal(delayMs, result.LatencyMs!.Value, 1.0);
        Assert.Equal(0.0, result.GainDifferenceDb!.Value, 0.2);
    }

    [Fact]
    public void ADelayBeyondTheSearchWindowIsRefusedNotMisreported()
    {
        var input = AudioTestSignals.BurstNoise(Frames6s, seed: 31);
        var output = AudioTestSignals.Delayed(input, 62_400, 1.0f); // 1300 ms

        var result = Measure(input, output);

        Assert.Equal("insufficient-signal", result.Status);
        Assert.Null(result.LatencyMs);
        Assert.Null(result.GainDifferenceDb);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Fact]
    public void SilentInputIsRefusedWithItsOwnReason()
    {
        var result = Measure(AudioTestSignals.Silence(Frames5s), AudioTestSignals.BurstNoise(Frames5s, seed: 5));

        Assert.Equal("insufficient-signal", result.Status);
        Assert.Contains("input is silent", result.Reason);
        Assert.Null(result.LatencyMs);
    }

    [Fact]
    public void SilentOutputIsRefusedWithItsOwnReason()
    {
        var result = Measure(AudioTestSignals.BurstNoise(Frames5s, seed: 5), AudioTestSignals.Silence(Frames5s));

        Assert.Equal("insufficient-signal", result.Status);
        Assert.Contains("output is silent", result.Reason);
        Assert.Null(result.LatencyMs);
    }

    [Fact]
    public void UnrelatedAudioIsRefusedAsLowConfidence()
    {
        var result = Measure(AudioTestSignals.BurstNoise(Frames5s, seed: 1), AudioTestSignals.BurstNoise(Frames5s, seed: 2));

        Assert.Equal("insufficient-signal", result.Status);
        Assert.True(result.Confidence < AudioLatencyAnalyzer.MinConfidence, $"confidence {result.Confidence}");
        Assert.Null(result.LatencyMs);
    }

    [Theory]
    [InlineData(1000.0)] // exactly one cycle per 1 ms bin: a perfectly flat envelope
    [InlineData(440.0)]  // a ripple at the beat of the tone: every cycle is an equally good lag
    public void SteadyTonesCarryNoDelayAndAreRefused(double frequency)
    {
        var input = AudioTestSignals.Sine(Frames5s, frequency, 0.3);
        var output = AudioTestSignals.Delayed(input, 1_790, 0.5f);

        var result = Measure(input, output);

        Assert.Equal("insufficient-signal", result.Status);
        Assert.Null(result.LatencyMs);
    }

    [Fact]
    public void ExactlyRepeatingAudioIsRefusedAsAmbiguous()
    {
        var input = AudioTestSignals.RepeatedBurstNoise(Frames5s, periodFrames: 12_000, seed: 4);
        var output = AudioTestSignals.Delayed(input, 1_920, 1.0f); // 40 ms; 290, 540, 790 ms fit just as well

        var result = Measure(input, output);

        Assert.Equal("insufficient-signal", result.Status);
        Assert.Contains("Ambiguous", result.Reason);
        Assert.Null(result.LatencyMs);
    }

    [Fact]
    public void MissingPacketsAndMissingTimestampsAreUnavailableNotGuessed()
    {
        var input = AudioTestSignals.BurstNoise(Frames5s, seed: 9);
        var output = AudioTestSignals.Delayed(input, 1_790, 1.0f);
        var inputPackets = AudioTestSignals.Packets(Frames5s, PacketFrames, Origin);
        var outputPackets = AudioTestSignals.Packets(Frames5s, PacketFrames, Origin);

        Assert.Equal("unavailable", AudioLatencyAnalyzer.Measure(input, Array.Empty<AudioPacket>(), output, outputPackets).Status);
        Assert.Equal("unavailable", AudioLatencyAnalyzer.Measure(input, inputPackets, output, Array.Empty<AudioPacket>()).Status);
        Assert.Equal("unavailable", AudioLatencyAnalyzer.Measure(Array.Empty<float>(), inputPackets, output, outputPackets).Status);

        var untimed = (AudioPacket[])outputPackets.Clone();
        untimed[7] = untimed[7] with { StartTime100Ns = 0 };
        var result = AudioLatencyAnalyzer.Measure(input, inputPackets, output, untimed);
        Assert.Equal("unavailable", result.Status);
        Assert.Contains("timestamps", result.Reason);
        Assert.Null(result.LatencyMs);

        var untimedInput = (AudioPacket[])inputPackets.Clone();
        untimedInput[0] = untimedInput[0] with { StartTime100Ns = 0 };
        Assert.Equal("unavailable", AudioLatencyAnalyzer.Measure(input, untimedInput, output, outputPackets).Status);
    }

    [Fact]
    public void DeliveryJitterSpikesOnFewPacketsDoNotMoveTheResult()
    {
        var input = AudioTestSignals.BurstNoise(Frames5s, seed: 41);
        var output = AudioTestSignals.Delayed(input, 2_400, 1.0f); // 50 ms

        var result = Measure(input, output, inputOffset: index => index % 20 == 5 ? 150_000L : 0L); // every 20th packet 15 ms late

        AssertOk(result);
        Assert.Equal(50.0, result.LatencyMs!.Value, 1.0);
        Assert.Equal(15.0, result.InputTimelineSpreadMs, 0.1);
    }

    [Fact]
    public void AGapInTheLoopbackStreamDoesNotBreakTheAlignmentAroundIt()
    {
        var input = AudioTestSignals.BurstNoise(Frames5s, seed: 51);
        var output = AudioTestSignals.Delayed(input, 1_920, 1.0f); // 40 ms

        // 200 ms with no loopback packets at all, in the middle of the capture.
        var result = Measure(input, output, dropOutputPacket: index => index is >= 250 and < 270);

        AssertOk(result);
        Assert.Equal(40.0, result.LatencyMs!.Value, 1.0);
    }

    [Fact]
    public void AnOutputWithInvertedPolarityFallsBackToEnvelopeResolutionAndSaysSo()
    {
        var input = AudioTestSignals.BurstNoise(Frames5s, seed: 61);
        var output = AudioTestSignals.Delayed(input, 1_790, -0.5f);

        var result = Measure(input, output);

        AssertOk(result);
        Assert.Equal("envelope-1ms", result.Resolution);
        Assert.Equal(37.29, result.LatencyMs!.Value, 1.5);
        Assert.Equal(-6.02, result.GainDifferenceDb!.Value, 0.3);
    }
}
