using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the audio output analysis (docs/architecture/performance-telemetry.md section 2.9), written down
// before the code. A live capture cannot reach most of these on demand, so they are exercised on synthetic signals:
//
//   1. Digital silence must read as silent (floor dBFS, 100% silent blocks), never as healthy audio or as an error.
//   2. An empty capture must not throw and must say "empty".
//   3. A clean tone must produce zero findings (no false positive on the healthy case).
//   4. Hard clipping must be reported, and its sample and event counts must agree.
//   5. A single-sample click in a tone must be found once, at the right time.
//   6. A short exact-zero run inside active audio is a dropout and must not also be counted as two clicks.
//   7. Leading, trailing and long inter-sound digital silence is silence, not a dropout.
//   8. Bright tones and white noise (large but legitimate second differences) must not trigger the click detector.
//   8b. A level step in broadband audio must not be a click, but an isolated spike and a DC step must be.
//   9. A dropout inside broadband noise must still be found.
//  10. One silent channel against an active one is a channel imbalance finding, not a silent verdict.
//  11. NaN and infinity must be counted and neutralised, never propagated into the statistics.
//  12. Packet flags (discontinuity, silent, timestamp error) and timestamp gaps must be counted from the packets.
//  13. The silent-block percentage must follow the share of 10 ms blocks below -60 dBFS.
//  14. The WAV file must be a valid IEEE-float WAV whose data round-trips exactly.
public sealed class AudioSignalAnalysisTests
{
    private static AudioAnalysis Analyze(float[] samples, AudioPacket[]? packets = null) =>
        AudioSignalAnalyzer.Analyze(samples, packets ?? Array.Empty<AudioPacket>());

    [Fact]
    public void DigitalSilenceIsReportedAsSilentWithFlooredLevels()
    {
        var result = Analyze(AudioTestSignals.Silence(AudioTestSignals.Rate));

        Assert.Equal("silent", result.Verdict);
        Assert.Equal(new[] { "silent" }, result.Findings);
        Assert.Equal(AudioSignalAnalyzer.FloorDbfs, result.LeftRmsDbfs);
        Assert.Equal(AudioSignalAnalyzer.FloorDbfs, result.RightPeakDbfs);
        Assert.Equal(100.0, result.SilentBlockPercent);
        Assert.Equal(AudioTestSignals.Rate, result.TotalFrames);
        Assert.Equal(0, result.Dropouts);
    }

    [Fact]
    public void AnEmptyCaptureDoesNotThrowAndIsLabelledEmpty()
    {
        var result = Analyze(Array.Empty<float>());

        Assert.Equal("empty", result.Verdict);
        Assert.Equal(0, result.TotalFrames);
        Assert.Equal(0, result.BlockCount);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void ACleanToneHasExactLevelsAndNoFindings()
    {
        var result = Analyze(AudioTestSignals.Sine(AudioTestSignals.Rate * 2, 440, 0.25));

        Assert.Equal("ok", result.Verdict);
        Assert.Empty(result.Findings);
        Assert.Equal(-15.05, result.LeftRmsDbfs, 0.05);
        Assert.Equal(-12.04, result.LeftPeakDbfs, 0.05);
        Assert.Equal(0.0, result.ChannelBalanceDb, 0.001);
        Assert.Equal(0, result.Clicks);
        Assert.Equal(0, result.Dropouts);
        Assert.Equal(0, result.ClipEvents);
        Assert.Equal(0.0, result.SilentBlockPercent);
        Assert.Equal(200, result.BlockCount);
    }

    [Fact]
    public void HardClippingIsCountedAsSamplesAndEvents()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate, 220, 1.6);
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = Math.Clamp(samples[index], -1f, 1f);
        }

        var result = Analyze(samples);

        Assert.Contains("clipping", result.Findings);
        Assert.True(result.ClippedSamples > 100, $"clipped samples {result.ClippedSamples}");
        Assert.InRange(result.ClipEvents, 870, 890); // 220 Hz for 1 s: 440 half-cycle runs on each of two channels
        Assert.Equal(0.0, result.LeftPeakDbfs, 0.01);
    }

    [Fact]
    public void ASingleSampleClickInAToneIsFoundOnceAtItsTime()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate, 440, 0.3);
        samples[24_000 * 2] += 0.6f;

        var result = Analyze(samples);

        Assert.Equal(1, result.Clicks);
        Assert.Contains("clicks", result.Findings);
        Assert.Single(result.ClickTimesMs);
        Assert.InRange(result.ClickTimesMs[0], 499.0, 501.0);
        Assert.Equal(0, result.Dropouts);
    }

    [Fact]
    public void AClickOnBothChannelsIsOneClick()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate, 440, 0.3);
        samples[30_000 * 2] += 0.5f;
        samples[30_000 * 2 + 1] += 0.5f;

        Assert.Equal(1, Analyze(samples).Clicks);
    }

    [Fact]
    public void AShortZeroRunInsideActiveAudioIsADropoutAndNotClicks()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate, 440, 0.3);
        const int start = 20_000;
        const int length = 240; // 5 ms
        Array.Clear(samples, start * 2, length * 2);

        var result = Analyze(samples);

        Assert.Equal(1, result.Dropouts);
        Assert.Equal(5.0, result.LongestDropoutMs, 0.01);
        Assert.Contains("dropouts", result.Findings);
        Assert.Equal(0, result.Clicks);
        Assert.InRange(result.DropoutTimesMs[0], 416.0, 417.0);
    }

    [Fact]
    public void ADropoutInsideBroadbandNoiseIsStillFound()
    {
        var samples = AudioTestSignals.WhiteNoise(AudioTestSignals.Rate, 0.3, seed: 7);
        Array.Clear(samples, 30_000 * 2, 144 * 2); // 3 ms

        var result = Analyze(samples);

        Assert.Equal(1, result.Dropouts);
        Assert.Equal(0, result.Clicks);
    }

    [Fact]
    public void LeadingTrailingAndLongInterSoundSilenceAreNotDropouts()
    {
        const int rate = AudioTestSignals.Rate;
        var samples = new float[rate * 2 * 2];
        var tone = AudioTestSignals.Sine(rate / 2, 440, 0.3);
        // 100 ms lead silence, 500 ms tone, 300 ms silence, 500 ms tone, then trailing silence to the end.
        var lead = rate / 10;
        var gap = rate * 3 / 10;
        Array.Copy(tone, 0, samples, lead * 2, tone.Length);
        Array.Copy(tone, 0, samples, (lead + rate / 2 + gap) * 2, tone.Length);

        var result = Analyze(samples);

        Assert.Equal(0, result.Dropouts);
        Assert.DoesNotContain("dropouts", result.Findings);
    }

    [Theory]
    [InlineData(6000.0, 0.5)]
    [InlineData(12000.0, 0.4)]
    public void BrightTonesDoNotTriggerTheClickDetector(double frequency, double amplitude)
    {
        Assert.Equal(0, Analyze(AudioTestSignals.Sine(AudioTestSignals.Rate, frequency, amplitude)).Clicks);
    }

    [Fact]
    public void WhiteNoiseDoesNotTriggerTheClickDetector()
    {
        var result = Analyze(AudioTestSignals.WhiteNoise(AudioTestSignals.Rate * 2, 0.3, seed: 3));

        Assert.Equal(0, result.Clicks);
        Assert.Equal("ok", result.Verdict);
    }

    [Fact]
    public void ALevelStepInBroadbandAudioIsNotAClick()
    {
        // 200 ms of quiet noise, then 200 ms of noise five times louder, repeated: the loud segments raise the second
        // difference everywhere, so the first loud samples are not isolated spikes.
        var quiet = AudioTestSignals.WhiteNoise(AudioTestSignals.Rate / 5, 0.004, seed: 21);
        var loud = AudioTestSignals.WhiteNoise(AudioTestSignals.Rate / 5, 0.02, seed: 22);
        var samples = new float[(quiet.Length + loud.Length) * 3];
        for (var repeat = 0; repeat < 3; repeat++)
        {
            Array.Copy(quiet, 0, samples, repeat * (quiet.Length + loud.Length), quiet.Length);
            Array.Copy(loud, 0, samples, repeat * (quiet.Length + loud.Length) + quiet.Length, loud.Length);
        }

        Assert.Equal(0, Analyze(samples).Clicks);
    }

    [Fact]
    public void ADcStepIsAClick()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate, 100, 0.2);
        for (var frame = 30_000; frame < 30_050; frame++)
        {
            samples[frame * 2] += 0.3f;
            samples[frame * 2 + 1] += 0.3f;
        }

        // Both edges of the 50-frame offset are isolated discontinuities.
        Assert.Equal(2, Analyze(samples).Clicks);
    }

    [Fact]
    public void OneSilentChannelIsAnImbalanceNotASilentCapture()
    {
        var mono = AudioTestSignals.Sine(AudioTestSignals.Rate, 440, 0.3);
        for (var frame = 0; frame < AudioTestSignals.Rate; frame++)
        {
            mono[frame * 2 + 1] = 0f;
        }

        var result = Analyze(mono);

        Assert.Equal("issues", result.Verdict);
        Assert.Contains("channel-imbalance", result.Findings);
        Assert.True(result.ChannelBalanceDb > 60, $"balance {result.ChannelBalanceDb}");
        Assert.Equal(AudioSignalAnalyzer.FloorDbfs, result.RightRmsDbfs);
    }

    [Fact]
    public void NonFiniteSamplesAreCountedAndNeutralised()
    {
        var samples = AudioTestSignals.Sine(AudioTestSignals.Rate / 2, 440, 0.3);
        samples[100] = float.NaN;
        samples[501] = float.PositiveInfinity;
        samples[900] = float.NegativeInfinity;

        var result = Analyze(samples);

        Assert.Equal(3, result.NonFiniteSamples);
        Assert.Contains("non-finite-samples", result.Findings);
        Assert.True(double.IsFinite(result.LeftRmsDbfs) && double.IsFinite(result.RightPeakDbfs));
        Assert.True(result.LeftRmsDbfs < 0);
    }

    [Fact]
    public void PacketFlagsAndTimestampGapsAreCountedFromThePackets()
    {
        var samples = AudioTestSignals.Sine(4_800, 440, 0.3);
        var packets = new[]
        {
            new AudioPacket(0, 480, 1_000_000, 0),
            new AudioPacket(480, 480, 1_000_000 + 100_000, AudioPacketFlags.DataDiscontinuity),
            new AudioPacket(960, 480, 1_000_000 + 300_000, AudioPacketFlags.Silent | AudioPacketFlags.TimestampError),
        };

        var result = Analyze(samples, packets);

        Assert.Equal(3, result.Packets);
        Assert.Equal(1, result.DiscontinuityPackets);
        Assert.Equal(1, result.SilentPackets);
        Assert.Equal(1, result.TimestampErrorPackets);
        // Packet 2 should start at 1_100_000 + 100_000 (10 ms later) but starts at 1_300_000: a 10 ms gap.
        Assert.Equal(10.0, result.MaxPacketGapMs, 0.01);
        Assert.Contains("packet-discontinuities", result.Findings);
        Assert.Contains("timestamp-errors", result.Findings);
    }

    [Fact]
    public void SilentBlockPercentFollowsTheShareOfQuietBlocks()
    {
        var samples = new float[AudioTestSignals.Rate * 2 * 2];
        var tone = AudioTestSignals.Sine(AudioTestSignals.Rate, 440, 0.3);
        Array.Copy(tone, samples, tone.Length);

        var result = Analyze(samples);

        Assert.Equal(200, result.BlockCount);
        Assert.Equal(50.0, result.SilentBlockPercent, 0.5);
    }

    [Fact]
    public void TheWavWriterProducesAValidFloatWavThatRoundTrips()
    {
        var samples = new[] { 0.5f, -0.5f, 0.25f, -0.25f, 1.0f, -1.0f };
        using var stream = new MemoryStream();
        AudioWavWriter.WriteFloat(stream, samples, 2, 48_000);
        var bytes = stream.ToArray();

        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal((uint)(bytes.Length - 8), BitConverter.ToUInt32(bytes, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.Equal("fmt ", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));
        Assert.Equal(16u, BitConverter.ToUInt32(bytes, 16));
        Assert.Equal(3, BitConverter.ToUInt16(bytes, 20)); // IEEE float
        Assert.Equal(2, BitConverter.ToUInt16(bytes, 22));
        Assert.Equal(48_000u, BitConverter.ToUInt32(bytes, 24));
        Assert.Equal(48_000u * 8u, BitConverter.ToUInt32(bytes, 28));
        Assert.Equal(8, BitConverter.ToUInt16(bytes, 32));
        Assert.Equal(32, BitConverter.ToUInt16(bytes, 34));
        Assert.Equal("fact", System.Text.Encoding.ASCII.GetString(bytes, 36, 4));
        Assert.Equal(3u, BitConverter.ToUInt32(bytes, 44));
        Assert.Equal("data", System.Text.Encoding.ASCII.GetString(bytes, 48, 4));
        Assert.Equal((uint)(samples.Length * 4), BitConverter.ToUInt32(bytes, 52));
        for (var index = 0; index < samples.Length; index++)
        {
            Assert.Equal(samples[index], BitConverter.ToSingle(bytes, 56 + index * 4));
        }
    }
}
