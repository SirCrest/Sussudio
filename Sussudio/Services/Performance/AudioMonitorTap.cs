using System;
using System.Collections.Generic;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>A contiguous window of tapped audio: interleaved stereo plus the packet boundaries and drain timestamps.</summary>
public sealed record AudioRingSnapshot(float[] Samples, AudioPacket[] Packets, long FirstFrame, long FramesWritten, long RejectedPackets);

/// <summary>
/// Preallocated single-writer ring of 48 kHz interleaved float stereo with per-packet drain timestamps. The writer is
/// the capture thread and must never block, allocate or lock; readers run on any other thread and validate what they
/// copied (seqlock style), so a snapshot that raced the writer loses the overwritten prefix instead of returning torn
/// samples. A write claims its frame range before touching the sample region and publishes it afterwards; a reader
/// keeps only frames that were not inside any range claimed while it copied.
/// Contract: docs/architecture/performance-telemetry.md section 2.9.
/// </summary>
public sealed class AudioMonitorRing
{
    public const int Channels = 2;
    public const int SampleRate = AudioSignalAnalyzer.DefaultSampleRate;

    private const long StampInProgress = -1;

    private readonly float[] _samples;
    private readonly PacketSlot[] _packets;
    private readonly int _capacityFrames;

    // Writer-private cursors (single writer), mirrored into the published fields below after each write.
    private long _writeFrame;
    private long _writePacket;

    private long _claimedFrames;
    private long _publishedFrames;
    private long _publishedPackets;
    private long _rejectedPackets;

    public AudioMonitorRing(int capacityFrames, int maxPackets)
    {
        if (capacityFrames < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityFrames));
        }

        if (maxPackets < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPackets));
        }

        _capacityFrames = capacityFrames;
        _samples = new float[checked(capacityFrames * Channels)];
        _packets = new PacketSlot[maxPackets];
    }

    public int CapacityFrames => _capacityFrames;

    public long FramesWritten => Volatile.Read(ref _publishedFrames);

    public long RejectedPackets => Volatile.Read(ref _rejectedPackets);

    /// <summary>
    /// Appends one packet. Capture thread only. Returns false (and counts the packet) when it is larger than the whole
    /// ring, which cannot happen with a sensibly sized ring.
    /// </summary>
    public bool Write(ReadOnlySpan<float> interleavedStereo, long qpcTicks, uint flags)
    {
        var frames = interleavedStereo.Length / Channels;
        if (frames <= 0)
        {
            return true;
        }

        if (frames > _capacityFrames)
        {
            Interlocked.Increment(ref _rejectedPackets);
            return false;
        }

        var startFrame = _writeFrame;
        var endFrame = startFrame + frames;

        // Full fence: the claim must be visible before any sample store, or a reader could validate a frame that this
        // write is about to overwrite.
        Interlocked.Exchange(ref _claimedFrames, endFrame);

        var position = (int)(startFrame % _capacityFrames);
        var firstSegment = Math.Min(frames, _capacityFrames - position);
        interleavedStereo.Slice(0, firstSegment * Channels).CopyTo(_samples.AsSpan(position * Channels));
        if (firstSegment < frames)
        {
            interleavedStereo.Slice(firstSegment * Channels).CopyTo(_samples.AsSpan(0));
        }

        var sequence = _writePacket;
        ref var slot = ref _packets[(int)(sequence % _packets.Length)];
        Interlocked.Exchange(ref slot.Stamp, StampInProgress);
        slot.StartFrame = startFrame;
        slot.Frames = frames;
        slot.QpcTicks = qpcTicks;
        slot.Flags = flags;
        Volatile.Write(ref slot.Stamp, sequence + 1);

        _writePacket = sequence + 1;
        _writeFrame = endFrame;
        Volatile.Write(ref _publishedPackets, sequence + 1);
        Volatile.Write(ref _publishedFrames, endFrame);
        return true;
    }

    /// <summary>
    /// Copies the newest contiguous run of whole packets that survived intact. Packet start times are the drain
    /// timestamp minus the packet duration (the drain happens after the packet's last frame was captured).
    /// Returns null when nothing usable was written.
    /// </summary>
    public AudioRingSnapshot? Snapshot(long qpcFrequency) => Snapshot(qpcFrequency, null);

    /// <summary>
    /// Test seam: <paramref name="beforeSamplesCopied"/> runs after the packet list was validated and before the samples
    /// are copied, so a test can have the writer lap the ring at the worst possible moment.
    /// </summary>
    internal AudioRingSnapshot? Snapshot(long qpcFrequency, Action? beforeSamplesCopied)
    {
        var publishedFrames = Volatile.Read(ref _publishedFrames);
        var publishedPackets = Volatile.Read(ref _publishedPackets);
        if (publishedFrames == 0 || publishedPackets == 0)
        {
            return null;
        }

        var oldestFrame = Math.Max(0, publishedFrames - _capacityFrames);
        var firstSequence = Math.Max(0, publishedPackets - _packets.Length);
        var kept = new List<PacketCopy>((int)(publishedPackets - firstSequence));
        for (var sequence = firstSequence; sequence < publishedPackets; sequence++)
        {
            ref var slot = ref _packets[(int)(sequence % _packets.Length)];
            var expected = sequence + 1;
            if (Volatile.Read(ref slot.Stamp) != expected)
            {
                continue;
            }

            var copy = new PacketCopy(slot.StartFrame, slot.Frames, slot.QpcTicks, slot.Flags);
            Interlocked.MemoryBarrier();
            if (Volatile.Read(ref slot.Stamp) != expected || copy.StartFrame < oldestFrame)
            {
                continue;
            }

            // A dropped slot leaves a hole; only the newest gapless run is usable.
            if (kept.Count > 0 && kept[^1].StartFrame + kept[^1].Frames != copy.StartFrame)
            {
                kept.Clear();
            }

            kept.Add(copy);
        }

        if (kept.Count == 0)
        {
            return null;
        }

        var firstFrame = kept[0].StartFrame;
        var endFrame = kept[^1].StartFrame + kept[^1].Frames;
        var totalFrames = checked((int)(endFrame - firstFrame));
        var samples = new float[totalFrames * Channels];
        beforeSamplesCopied?.Invoke();
        var copied = 0;
        while (copied < totalFrames)
        {
            var position = (int)((firstFrame + copied) % _capacityFrames);
            var segment = Math.Min(totalFrames - copied, _capacityFrames - position);
            Array.Copy(_samples, position * Channels, samples, copied * Channels, segment * Channels);
            copied += segment;
        }

        Interlocked.MemoryBarrier();

        // Anything below this was inside a range claimed (and possibly being overwritten) while we copied.
        var safeFrom = Math.Max(0, Volatile.Read(ref _claimedFrames) - _capacityFrames);
        var dropFrames = 0;
        var dropPackets = 0;
        while (dropPackets < kept.Count && kept[dropPackets].StartFrame < safeFrom)
        {
            dropFrames += kept[dropPackets].Frames;
            dropPackets++;
        }

        if (dropPackets == kept.Count)
        {
            return null;
        }

        var packets = new AudioPacket[kept.Count - dropPackets];
        var frameOffset = 0L;
        for (var index = dropPackets; index < kept.Count; index++)
        {
            var packet = kept[index];
            var durationHundredNs = (long)Math.Round(packet.Frames * (double)QpcMath.HundredNsPerSecond / SampleRate);
            var drainHundredNs = QpcMath.TicksToHundredNs(packet.QpcTicks, qpcFrequency);
            packets[index - dropPackets] = new AudioPacket(frameOffset, packet.Frames, drainHundredNs - durationHundredNs, packet.Flags);
            frameOffset += packet.Frames;
        }

        var trimmed = dropFrames == 0
            ? samples
            : samples.AsSpan(dropFrames * Channels).ToArray();
        return new AudioRingSnapshot(trimmed, packets, firstFrame + dropFrames, publishedFrames, Volatile.Read(ref _rejectedPackets));
    }

    private readonly record struct PacketCopy(long StartFrame, int Frames, long QpcTicks, uint Flags);

    private struct PacketSlot
    {
        public long Stamp;
        public long StartFrame;
        public long QpcTicks;
        public int Frames;
        public uint Flags;
    }
}

/// <summary>
/// Process-wide tap point for the program-audio capture path. The capture thread calls <see cref="Offer"/> for every
/// converted packet; while no probe is armed that is one volatile read of a static reference and a return. A probe
/// arms it with a ring it preallocated (off the capture thread) and disarms it when the measurement ends.
/// </summary>
public static class AudioMonitorTap
{
    private static AudioMonitorRing? s_ring;

    public static bool IsArmed => Volatile.Read(ref s_ring) is not null;

    /// <summary>Capture thread: offers one converted 48 kHz interleaved float stereo packet. No allocation, no lock.</summary>
    public static void Offer(ReadOnlySpan<float> interleavedStereo, long qpcTicks, uint flags)
    {
        var ring = Volatile.Read(ref s_ring);
        if (ring is null)
        {
            return;
        }

        ring.Write(interleavedStereo, qpcTicks, flags);
    }

    /// <summary>Arms the tap with a fresh ring; returns null when another probe already has it armed.</summary>
    public static AudioMonitorRing? TryArm(int capacityFrames, int maxPackets)
    {
        var ring = new AudioMonitorRing(capacityFrames, maxPackets);
        return Interlocked.CompareExchange(ref s_ring, ring, null) is null ? ring : null;
    }

    /// <summary>Disarms the tap if <paramref name="ring"/> is still the armed one.</summary>
    public static void Disarm(AudioMonitorRing ring)
    {
        Interlocked.CompareExchange(ref s_ring, null, ring);
    }
}
