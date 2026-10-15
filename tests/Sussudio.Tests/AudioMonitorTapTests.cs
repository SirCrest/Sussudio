using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the capture-delivery tap ring (docs/architecture/performance-telemetry.md section 2.9). The writer is
// the audio capture thread; the reader is the probe. Nothing here may block, allocate on the writer side, or hand back
// samples that a newer write was overwriting:
//
//   1. A ring that has not been written must snapshot to nothing.
//   2. Packets written without wrap must round-trip exactly, with drain-minus-duration start times.
//   3. A ring written past its capacity must return only the newest whole packets, contiguous and uncorrupted.
//   4. A write that laps the ring while a snapshot is copying must cost the snapshot its overwritten prefix, never
//      return torn samples (the seam below makes that race deterministic).
//   5. A packet-slot table smaller than the sample ring must still yield a gapless run.
//   6. A packet larger than the whole ring is rejected and counted, leaving the ring untouched.
//   7. A concurrent writer thread and a snapshotting reader must never observe a corrupted sample.
//   8. The process-wide tap must be a no-op while disarmed, arm exactly once, and re-arm after disarm; only the ring that
//      armed it may disarm it.
// The tap facade shares process-wide state, so its cases live in one class and are not run in parallel with themselves.
public sealed class AudioMonitorTapTests
{
    private const long Frequency = 10_000_000; // ticks are 100 ns, so QPC ticks and 100 ns units coincide

    // Every sample of frame f carries the value f (exact in float up to 2^24), so any torn or misplaced frame is visible.
    private static float[] FramePattern(long firstFrame, int frames)
    {
        var samples = new float[frames * 2];
        for (var index = 0; index < frames; index++)
        {
            samples[index * 2] = firstFrame + index;
            samples[index * 2 + 1] = firstFrame + index;
        }

        return samples;
    }

    private static void AssertPatternIntact(AudioRingSnapshot snapshot)
    {
        var frames = snapshot.Samples.Length / 2;
        for (var index = 0; index < frames; index++)
        {
            var expected = snapshot.FirstFrame + index;
            Assert.Equal((float)expected, snapshot.Samples[index * 2]);
            Assert.Equal((float)expected, snapshot.Samples[index * 2 + 1]);
        }

        var offset = 0L;
        foreach (var packet in snapshot.Packets)
        {
            Assert.Equal(offset, packet.FrameOffset);
            offset += packet.Frames;
        }

        Assert.Equal(frames, offset);
    }

    private static void WritePackets(AudioMonitorRing ring, long firstFrame, int count, int packetFrames)
    {
        for (var index = 0; index < count; index++)
        {
            var start = firstFrame + (long)index * packetFrames;
            Assert.True(ring.Write(FramePattern(start, packetFrames), qpcTicks: 1_000_000 + start * 100, flags: 0));
        }
    }

    [Fact]
    public void AnUnwrittenRingSnapshotsToNothing()
    {
        Assert.Null(new AudioMonitorRing(4_800, 64).Snapshot(Frequency));
    }

    [Fact]
    public void PacketsWithoutWrapRoundTripWithDrainMinusDurationStartTimes()
    {
        var ring = new AudioMonitorRing(48_000, 256);
        ring.Write(FramePattern(0, 480), qpcTicks: 7_000_000, flags: AudioPacketFlags.DataDiscontinuity);
        ring.Write(FramePattern(480, 240), qpcTicks: 7_100_000, flags: 0);

        var snapshot = ring.Snapshot(Frequency);

        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot!.FirstFrame);
        Assert.Equal(720, snapshot.Samples.Length / 2);
        AssertPatternIntact(snapshot);
        Assert.Equal(2, snapshot.Packets.Length);
        Assert.Equal(AudioPacketFlags.DataDiscontinuity, snapshot.Packets[0].Flags);
        Assert.Equal(7_000_000 - 100_000, snapshot.Packets[0].StartTime100Ns); // 480 frames = 10 ms
        Assert.Equal(7_100_000 - 50_000, snapshot.Packets[1].StartTime100Ns);   // 240 frames = 5 ms
        Assert.Equal(720, snapshot.FramesWritten);
    }

    [Fact]
    public void ARingWrittenPastItsCapacityKeepsTheNewestWholePacketsUncorrupted()
    {
        var ring = new AudioMonitorRing(capacityFrames: 4_800, maxPackets: 1_024);
        WritePackets(ring, 0, count: 100, packetFrames: 480); // 48,000 frames through a 4,800-frame ring

        var snapshot = ring.Snapshot(Frequency);

        Assert.NotNull(snapshot);
        Assert.Equal(48_000, snapshot!.FramesWritten);
        Assert.True(snapshot.FirstFrame >= 48_000 - 4_800);
        Assert.True(snapshot.Samples.Length / 2 <= 4_800);
        Assert.True(snapshot.Samples.Length / 2 >= 4_800 - 480, "at most one packet is lost to the boundary");
        Assert.Equal(48_000, snapshot.FirstFrame + snapshot.Samples.Length / 2);
        AssertPatternIntact(snapshot);
    }

    [Fact]
    public void AWriteThatLapsTheRingDuringTheCopyCostsTheOverwrittenPrefixNotCorruption()
    {
        var ring = new AudioMonitorRing(capacityFrames: 4_800, maxPackets: 1_024);
        WritePackets(ring, 0, count: 10, packetFrames: 480); // exactly full: frames 0..4799, nothing wrapped yet

        // Just before the snapshot copies the samples, the writer claims and fills three more packets, overwriting
        // frames 0..1439 of the ring. Without validation the copy would carry frames 4800.. at positions 0..1439.
        var snapshot = ring.Snapshot(Frequency, () => WritePackets(ring, 4_800, count: 3, packetFrames: 480));

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.FirstFrame >= 1_440, $"first surviving frame {snapshot.FirstFrame}");
        Assert.Equal(4_800, snapshot.FirstFrame + snapshot.Samples.Length / 2);
        AssertPatternIntact(snapshot);
    }

    [Fact]
    public void ASnapshotWhoseEveryFrameWasOverwrittenReturnsNothing()
    {
        var ring = new AudioMonitorRing(capacityFrames: 960, maxPackets: 64);
        WritePackets(ring, 0, count: 2, packetFrames: 480);

        var snapshot = ring.Snapshot(Frequency, () => WritePackets(ring, 960, count: 2, packetFrames: 480));

        Assert.Null(snapshot);
    }

    [Fact]
    public void ASmallPacketSlotTableStillYieldsAGaplessRun()
    {
        var ring = new AudioMonitorRing(capacityFrames: 48_000, maxPackets: 8);
        WritePackets(ring, 0, count: 30, packetFrames: 480);

        var snapshot = ring.Snapshot(Frequency);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.Packets.Length <= 8);
        Assert.Equal(30 * 480, snapshot.FirstFrame + snapshot.Samples.Length / 2);
        AssertPatternIntact(snapshot);
    }

    [Fact]
    public void APacketLargerThanTheRingIsRejectedAndCounted()
    {
        var ring = new AudioMonitorRing(capacityFrames: 480, maxPackets: 8);
        ring.Write(FramePattern(0, 480), 1_000_000, 0);

        Assert.False(ring.Write(FramePattern(480, 481), 2_000_000, 0));

        Assert.Equal(1, ring.RejectedPackets);
        Assert.Equal(480, ring.FramesWritten);
        var snapshot = ring.Snapshot(Frequency);
        Assert.NotNull(snapshot);
        Assert.Equal(480, snapshot!.Samples.Length / 2);
        AssertPatternIntact(snapshot);
    }

    [Fact]
    public void AConcurrentWriterThreadNeverShowsTheReaderACorruptedSample()
    {
        var ring = new AudioMonitorRing(capacityFrames: 9_600, maxPackets: 512);
        using var stop = new CancellationTokenSource();
        var writer = new Thread(() =>
        {
            var frame = 0L;
            var packet = FramePattern(0, 480);
            while (!stop.IsCancellationRequested)
            {
                for (var index = 0; index < 480; index++)
                {
                    packet[index * 2] = frame + index;
                    packet[index * 2 + 1] = frame + index;
                }

                ring.Write(packet, 1_000_000 + frame * 100, 0);
                frame += 480;
            }
        }) { IsBackground = true, Name = "AudioMonitorTapTests.Writer" };
        writer.Start();

        var snapshots = 0;
        var deadline = DateTime.UtcNow.AddMilliseconds(400);
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                var snapshot = ring.Snapshot(Frequency);
                if (snapshot is not null)
                {
                    AssertPatternIntact(snapshot);
                    snapshots++;
                }
            }
        }
        finally
        {
            stop.Cancel();
            Assert.True(writer.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.True(snapshots > 0, "the reader never obtained a snapshot");
    }

    [Fact]
    public void TheProcessWideTapIsANoOpWhenDisarmedArmsOnceAndRearmsAfterDisarm()
    {
        Assert.False(AudioMonitorTap.IsArmed);
        AudioMonitorTap.Offer(FramePattern(0, 480), 1, 0); // disarmed: must not throw or record anywhere

        var first = AudioMonitorTap.TryArm(4_800, 64);
        Assert.NotNull(first);
        try
        {
            Assert.True(AudioMonitorTap.IsArmed);
            Assert.Null(AudioMonitorTap.TryArm(4_800, 64));

            AudioMonitorTap.Offer(FramePattern(0, 480), 5_000_000, 0);
            Assert.Equal(480, first!.FramesWritten);

            var stranger = new AudioMonitorRing(480, 8);
            AudioMonitorTap.Disarm(stranger);
            Assert.True(AudioMonitorTap.IsArmed);
        }
        finally
        {
            AudioMonitorTap.Disarm(first!);
        }

        Assert.False(AudioMonitorTap.IsArmed);
        AudioMonitorTap.Offer(FramePattern(480, 480), 6_000_000, 0);
        Assert.Equal(480, first!.FramesWritten);

        var second = AudioMonitorTap.TryArm(4_800, 64);
        Assert.NotNull(second);
        AudioMonitorTap.Disarm(second!);
    }
}
