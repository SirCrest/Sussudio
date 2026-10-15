using System;
using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>Bits of the WASAPI <c>GetBuffer</c> flags word that the analysis reports (values match AUDCLNT_BUFFERFLAGS_*).</summary>
public static class AudioPacketFlags
{
    public const uint DataDiscontinuity = 0x1;
    public const uint Silent = 0x2;
    public const uint TimestampError = 0x4;
}

/// <summary>
/// One capture packet inside a concatenated interleaved-stereo sample buffer. <see cref="StartTime100Ns"/> is the QPC
/// time of the packet's first frame in 100 ns units (zero means the producer had no timestamp).
/// </summary>
public readonly record struct AudioPacket(long FrameOffset, int Frames, long StartTime100Ns, uint Flags);

/// <summary>Per-capture measurements of one rendered or captured audio stream. dBFS values floor at -120 (digital silence).</summary>
public sealed record AudioAnalysis(
    int SampleRate,
    long TotalFrames,
    double DurationSeconds,
    double LeftRmsDbfs,
    double RightRmsDbfs,
    double LeftPeakDbfs,
    double RightPeakDbfs,
    double ChannelBalanceDb,
    int BlockCount,
    double SilentBlockPercent,
    long ClippedSamples,
    int ClipEvents,
    int Clicks,
    IReadOnlyList<double> ClickTimesMs,
    int Dropouts,
    double LongestDropoutMs,
    IReadOnlyList<double> DropoutTimesMs,
    long NonFiniteSamples,
    int Packets,
    int DiscontinuityPackets,
    int SilentPackets,
    int TimestampErrorPackets,
    double MaxPacketGapMs,
    string Verdict,
    IReadOnlyList<string> Findings);

/// <summary>
/// Pure analysis of captured audio (interleaved 32-bit float stereo). Everything here is heuristic and bounded:
/// one pass per measurement, no allocation proportional to anything but the block count, and every threshold is a
/// named constant so a test or a reader can see what a finding means.
/// Contract: docs/architecture/performance-telemetry.md section 2.9.
/// </summary>
public static class AudioSignalAnalyzer
{
    public const int DefaultSampleRate = 48_000;

    /// <summary>A block is silent when its RMS (both channels) is below this level.</summary>
    public const double SilentBlockDbfs = -60.0;

    public const double BlockMilliseconds = 10.0;

    /// <summary>A sample counts as clipped at or above this magnitude; a clip event is a run of at least two.</summary>
    public const float ClipThreshold = 0.999f;

    /// <summary>Exact-zero frame runs of at least this many frames (0.5 ms at 48 kHz) are candidate dropouts.</summary>
    public const int DropoutMinFrames = 24;

    /// <summary>Longer exact-zero runs are silence between sounds, not a dropout (100 ms at 48 kHz).</summary>
    public const int DropoutMaxFrames = 4_800;

    /// <summary>A dropout needs audible audio this long on both sides, or it is leading or trailing silence.</summary>
    public const int DropoutContextFrames = 480;

    public const double DropoutContextDbfs = -50.0;

    /// <summary>A click is a second difference at least this large and at least <see cref="ClickSigmaFactor"/> times the local RMS second difference.</summary>
    public const float ClickMinSecondDifference = 0.05f;

    public const float ClickSigmaFactor = 10.0f;

    public const int ClickCooldownFrames = 48;

    /// <summary>Discontinuities within this many frames of an exact-zero run edge belong to that run, not to the click count.</summary>
    public const int ClickZeroRunGuardFrames = 4;

    public const double ChannelImbalanceDb = 6.0;

    public const double ChannelImbalanceMinActiveDbfs = -50.0;

    public const int MaxReportedEventTimes = 16;

    public const double FloorDbfs = -120.0;

    private const int ClickSeedFrames = 480;
    private const float ClickScaleAlpha = 1f / 480f;

    public static double ToDbfs(double linear)
    {
        if (!(linear > 1e-6))
        {
            return FloorDbfs;
        }

        return Math.Max(FloorDbfs, 20.0 * Math.Log10(linear));
    }

    public static AudioAnalysis Analyze(ReadOnlySpan<float> interleavedStereo, ReadOnlySpan<AudioPacket> packets, int sampleRate = DefaultSampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        var frames = interleavedStereo.Length / 2;
        var blockFrames = Math.Max(1, (int)Math.Round(sampleRate * BlockMilliseconds / 1000.0));

        double leftSquares = 0, rightSquares = 0;
        float leftPeak = 0, rightPeak = 0;
        long nonFinite = 0;
        long clippedSamples = 0;
        var clipEvents = 0;
        var leftClipRun = 0;
        var rightClipRun = 0;

        var blockCount = frames / blockFrames;
        var silentBlocks = 0;
        double blockSquares = 0;
        var blockFill = 0;

        // Exact-zero frame runs (both channels zero), kept for dropout classification and click suppression.
        var zeroRuns = new List<(int Start, int End)>();
        var zeroStart = -1;

        for (var frame = 0; frame < frames; frame++)
        {
            var left = interleavedStereo[frame * 2];
            var right = interleavedStereo[frame * 2 + 1];
            if (!float.IsFinite(left))
            {
                nonFinite++;
                left = 0;
            }

            if (!float.IsFinite(right))
            {
                nonFinite++;
                right = 0;
            }

            var absLeft = Math.Abs(left);
            var absRight = Math.Abs(right);
            leftSquares += (double)left * left;
            rightSquares += (double)right * right;
            if (absLeft > leftPeak)
            {
                leftPeak = absLeft;
            }

            if (absRight > rightPeak)
            {
                rightPeak = absRight;
            }

            clippedSamples += CountClip(absLeft, ref leftClipRun, ref clipEvents);
            clippedSamples += CountClip(absRight, ref rightClipRun, ref clipEvents);

            if (left == 0f && right == 0f)
            {
                if (zeroStart < 0)
                {
                    zeroStart = frame;
                }
            }
            else if (zeroStart >= 0)
            {
                if (frame - zeroStart >= DropoutMinFrames)
                {
                    zeroRuns.Add((zeroStart, frame));
                }

                zeroStart = -1;
            }

            blockSquares += (double)left * left + (double)right * right;
            if (++blockFill == blockFrames)
            {
                var blockRms = Math.Sqrt(blockSquares / (blockFrames * 2.0));
                if (ToDbfs(blockRms) < SilentBlockDbfs)
                {
                    silentBlocks++;
                }

                blockSquares = 0;
                blockFill = 0;
            }
        }

        if (zeroStart >= 0 && frames - zeroStart >= DropoutMinFrames)
        {
            zeroRuns.Add((zeroStart, frames));
        }

        var leftRms = frames > 0 ? Math.Sqrt(leftSquares / frames) : 0.0;
        var rightRms = frames > 0 ? Math.Sqrt(rightSquares / frames) : 0.0;
        var leftRmsDb = ToDbfs(leftRms);
        var rightRmsDb = ToDbfs(rightRms);
        var leftPeakDb = ToDbfs(leftPeak);
        var rightPeakDb = ToDbfs(rightPeak);

        var dropoutTimes = new List<double>();
        var dropouts = 0;
        var longestDropoutFrames = 0;
        foreach (var run in zeroRuns)
        {
            var length = run.End - run.Start;
            if (length > DropoutMaxFrames ||
                run.Start < DropoutContextFrames ||
                run.End + DropoutContextFrames > frames)
            {
                continue;
            }

            if (!IsActive(interleavedStereo, run.Start - DropoutContextFrames, run.Start) ||
                !IsActive(interleavedStereo, run.End, run.End + DropoutContextFrames))
            {
                continue;
            }

            dropouts++;
            longestDropoutFrames = Math.Max(longestDropoutFrames, length);
            if (dropoutTimes.Count < MaxReportedEventTimes)
            {
                dropoutTimes.Add(Math.Round(run.Start * 1000.0 / sampleRate, 3));
            }
        }

        var clickFrames = DetectClicks(interleavedStereo, frames, zeroRuns);
        var clickTimes = new List<double>();
        foreach (var clickFrame in clickFrames)
        {
            if (clickTimes.Count >= MaxReportedEventTimes)
            {
                break;
            }

            clickTimes.Add(Math.Round(clickFrame * 1000.0 / sampleRate, 3));
        }

        var packetStats = SummarizePackets(packets, sampleRate);
        var balance = leftRmsDb - rightRmsDb;

        var findings = new List<string>();
        string verdict;
        if (frames == 0)
        {
            verdict = "empty";
        }
        else if (Math.Max(leftPeakDb, rightPeakDb) < SilentBlockDbfs)
        {
            verdict = "silent";
            findings.Add("silent");
        }
        else
        {
            if (clipEvents > 0)
            {
                findings.Add("clipping");
            }

            if (clickFrames.Count > 0)
            {
                findings.Add("clicks");
            }

            if (dropouts > 0)
            {
                findings.Add("dropouts");
            }

            if (Math.Abs(balance) >= ChannelImbalanceDb && Math.Max(leftRmsDb, rightRmsDb) > ChannelImbalanceMinActiveDbfs)
            {
                findings.Add("channel-imbalance");
            }

            if (packetStats.Discontinuities > 0)
            {
                findings.Add("packet-discontinuities");
            }

            if (packetStats.TimestampErrors > 0)
            {
                findings.Add("timestamp-errors");
            }

            if (nonFinite > 0)
            {
                findings.Add("non-finite-samples");
            }

            verdict = findings.Count == 0 ? "ok" : "issues";
        }

        return new AudioAnalysis(
            sampleRate,
            frames,
            Math.Round(frames / (double)sampleRate, 3),
            Math.Round(leftRmsDb, 2),
            Math.Round(rightRmsDb, 2),
            Math.Round(leftPeakDb, 2),
            Math.Round(rightPeakDb, 2),
            Math.Round(balance, 2),
            blockCount,
            blockCount > 0 ? Math.Round(silentBlocks * 100.0 / blockCount, 2) : 0.0,
            clippedSamples,
            clipEvents,
            clickFrames.Count,
            clickTimes,
            dropouts,
            Math.Round(longestDropoutFrames * 1000.0 / sampleRate, 3),
            dropoutTimes,
            nonFinite,
            packetStats.Count,
            packetStats.Discontinuities,
            packetStats.SilentPackets,
            packetStats.TimestampErrors,
            Math.Round(packetStats.MaxGapMs, 3),
            verdict,
            findings);
    }

    private static int CountClip(float magnitude, ref int run, ref int events)
    {
        if (magnitude >= ClipThreshold)
        {
            run++;
            if (run == 2)
            {
                events++;
            }

            return 1;
        }

        run = 0;
        return 0;
    }

    private static bool IsActive(ReadOnlySpan<float> interleavedStereo, int startFrame, int endFrame)
    {
        double squares = 0;
        for (var frame = startFrame; frame < endFrame; frame++)
        {
            var left = interleavedStereo[frame * 2];
            var right = interleavedStereo[frame * 2 + 1];
            if (float.IsFinite(left))
            {
                squares += (double)left * left;
            }

            if (float.IsFinite(right))
            {
                squares += (double)right * right;
            }
        }

        var rms = Math.Sqrt(squares / ((endFrame - startFrame) * 2.0));
        return ToDbfs(rms) >= DropoutContextDbfs;
    }

    // Second-difference spike detector, per channel. Smooth audio has a tiny second difference and a click or an
    // abrupt step has a large one, so the detector compares each sample's second difference with an exponentially
    // weighted local RMS of the same quantity (outliers clamped so a click does not desensitize its own neighbours).
    // Steady tonal content is the intended target; dense broadband material raises the local RMS and hides small
    // clicks, which is documented as a limit of the finding, not hidden.
    private static List<int> DetectClicks(ReadOnlySpan<float> interleavedStereo, int frames, List<(int Start, int End)> zeroRuns)
    {
        var raw = new List<int>();
        if (frames < ClickSeedFrames + 2)
        {
            return raw;
        }

        for (var channel = 0; channel < 2; channel++)
        {
            // Seed the local scale with the plain mean over the first ClickSeedFrames so a bright tone or noise is not
            // judged against an empty history (the clamped update below can only grow the scale slowly).
            double seedSquares = 0;
            for (var frame = 1; frame <= ClickSeedFrames; frame++)
            {
                var previous = Finite(interleavedStereo[(frame - 1) * 2 + channel]);
                var current = Finite(interleavedStereo[frame * 2 + channel]);
                var next = Finite(interleavedStereo[(frame + 1) * 2 + channel]);
                var second = next - 2f * current + previous;
                seedSquares += (double)second * second;
            }

            var scaleSquared = (float)(seedSquares / ClickSeedFrames);
            var cooldown = 0;
            for (var frame = 1; frame < frames - 1; frame++)
            {
                var previous = Finite(interleavedStereo[(frame - 1) * 2 + channel]);
                var current = Finite(interleavedStereo[frame * 2 + channel]);
                var next = Finite(interleavedStereo[(frame + 1) * 2 + channel]);
                var magnitude = Math.Abs(next - 2f * current + previous);
                var threshold = Math.Max(ClickMinSecondDifference, ClickSigmaFactor * MathF.Sqrt(scaleSquared));
                if (cooldown == 0 && magnitude >= threshold)
                {
                    raw.Add(frame);
                    cooldown = ClickCooldownFrames;
                }

                var clamped = magnitude > threshold ? threshold : magnitude;
                scaleSquared += ClickScaleAlpha * (clamped * clamped - scaleSquared);
                if (cooldown > 0)
                {
                    cooldown--;
                }
            }
        }

        raw.Sort();
        var merged = new List<int>(raw.Count);
        foreach (var frame in raw)
        {
            if (NearZeroRunEdge(frame, zeroRuns))
            {
                continue;
            }

            if (merged.Count > 0 && frame - merged[^1] < ClickCooldownFrames)
            {
                continue;
            }

            merged.Add(frame);
        }

        return merged;
    }

    private static float Finite(float value) => float.IsFinite(value) ? value : 0f;

    private static bool NearZeroRunEdge(int frame, List<(int Start, int End)> zeroRuns)
    {
        foreach (var run in zeroRuns)
        {
            if (Math.Abs(frame - run.Start) <= ClickZeroRunGuardFrames || Math.Abs(frame - run.End) <= ClickZeroRunGuardFrames)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct PacketStats(int Count, int Discontinuities, int SilentPackets, int TimestampErrors, double MaxGapMs);

    private static PacketStats SummarizePackets(ReadOnlySpan<AudioPacket> packets, int sampleRate)
    {
        var discontinuities = 0;
        var silent = 0;
        var timestampErrors = 0;
        var maxGapMs = 0.0;
        for (var index = 0; index < packets.Length; index++)
        {
            var packet = packets[index];
            if ((packet.Flags & AudioPacketFlags.DataDiscontinuity) != 0)
            {
                discontinuities++;
            }

            if ((packet.Flags & AudioPacketFlags.Silent) != 0)
            {
                silent++;
            }

            if ((packet.Flags & AudioPacketFlags.TimestampError) != 0)
            {
                timestampErrors++;
            }

            if (index > 0 && packets[index - 1].StartTime100Ns > 0 && packet.StartTime100Ns > 0)
            {
                var previous = packets[index - 1];
                var expectedStart = previous.StartTime100Ns + previous.Frames * (double)QpcMath.HundredNsPerSecond / sampleRate;
                var gapMs = (packet.StartTime100Ns - expectedStart) / 10_000.0;
                if (gapMs > maxGapMs)
                {
                    maxGapMs = gapMs;
                }
            }
        }

        return new PacketStats(packets.Length, discontinuities, silent, timestampErrors, maxGapMs);
    }
}
