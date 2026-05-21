using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

// Single-file packet state for timestamp-base discovery, early-packet buffering,
// and EOF partial-base rescue.
internal sealed unsafe partial class FlashbackExporter
{
    private static readonly AVRational SingleFilePacketUsTimeBase = new() { num = 1, den = 1_000_000 };
    private const int SingleFileMaxBufferedPackets = 600;

    private static SingleFilePacketWriteState CreateSingleFilePacketWriteState(int streamCount)
        => new(
            new long[streamCount],
            new bool[streamCount],
            new long[streamCount],
            new List<IntPtr>(),
            new List<int>());

    private static bool TryRecordSingleFileTimestampBase(
        ref SingleFilePacketWriteState state,
        AVPacket* packet,
        int streamIndex,
        AVStream* outputStream)
    {
        if (!TryResolveTimestampBase(packet, out var timestampBase))
        {
            return false;
        }

        var baseUs = ffmpeg.av_rescale_q(timestampBase, outputStream->time_base, SingleFilePacketUsTimeBase);
        state.TimestampBasesUs[streamIndex] = baseUs;
        state.HasTimestampBase[streamIndex] = true;
        if (state.GlobalMinBaseUs == null || baseUs < state.GlobalMinBaseUs.Value)
        {
            state.GlobalMinBaseUs = baseUs;
        }

        return true;
    }

    private void BufferSingleFilePacketOrFlushReady(
        int streamCount,
        int[] streamMap,
        int streamIndex,
        ref SingleFilePacketWriteState state,
        AVPacket* packet)
    {
        var clone = ClonePacketOrThrow(packet, "single_buffer");
        state.BufferedPackets.Add((IntPtr)clone);
        state.BufferedStreamIndices.Add(streamIndex);

        state.AllBasesDiscovered = HasDiscoveredAllMappedSingleFileBases(in state, streamCount, streamMap);
        if (!state.AllBasesDiscovered && state.BufferedPackets.Count >= SingleFileMaxBufferedPackets)
        {
            state.AllBasesDiscovered = true;
            state.GlobalMinBaseUs ??= 0;
            Logger.Log(
                $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH buffered={state.BufferedPackets.Count} streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount}");
        }

        if (state.AllBasesDiscovered)
        {
            state.TotalPackets += FlushBufferedPackets(
                state.BufferedPackets,
                state.BufferedStreamIndices,
                streamMap,
                state.GlobalMinBaseUs!.Value,
                SingleFilePacketUsTimeBase,
                state.PacketCounts);
        }
    }

    private void FlushSingleFileBufferedPacketsAtEof(
        int streamCount,
        int[] streamMap,
        ref SingleFilePacketWriteState state)
    {
        if (state.AllBasesDiscovered || state.BufferedPackets.Count == 0)
        {
            return;
        }

        state.GlobalMinBaseUs ??= 0;
        Logger.Log(
            $"FLASHBACK_EXPORT_PARTIAL_BASE_FLUSH streams_discovered={CountDiscoveredSingleFileBases(in state, streamCount)}/{streamCount} buffered={state.BufferedPackets.Count}");
        state.TotalPackets += FlushBufferedPackets(
            state.BufferedPackets,
            state.BufferedStreamIndices,
            streamMap,
            state.GlobalMinBaseUs!.Value,
            SingleFilePacketUsTimeBase,
            state.PacketCounts);
    }

    private static bool HasDiscoveredAllMappedSingleFileBases(
        in SingleFilePacketWriteState state,
        int streamCount,
        int[] streamMap)
    {
        for (var i = 0; i < streamCount; i++)
        {
            if (streamMap[i] >= 0 && !state.HasTimestampBase[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int CountDiscoveredSingleFileBases(in SingleFilePacketWriteState state, int streamCount)
    {
        var discoveredCount = 0;
        for (var i = 0; i < streamCount; i++)
        {
            if (state.HasTimestampBase[i])
            {
                discoveredCount++;
            }
        }

        return discoveredCount;
    }

    private struct SingleFilePacketWriteState
    {
        public SingleFilePacketWriteState(
            long[] timestampBasesUs,
            bool[] hasTimestampBase,
            long[] packetCounts,
            List<IntPtr> bufferedPackets,
            List<int> bufferedStreamIndices)
        {
            TimestampBasesUs = timestampBasesUs;
            HasTimestampBase = hasTimestampBase;
            PacketCounts = packetCounts;
            BufferedPackets = bufferedPackets;
            BufferedStreamIndices = bufferedStreamIndices;
        }

        public long[] TimestampBasesUs { get; }
        public bool[] HasTimestampBase { get; }
        public long[] PacketCounts { get; }
        public List<IntPtr> BufferedPackets { get; }
        public List<int> BufferedStreamIndices { get; }
        public long? GlobalMinBaseUs { get; set; }
        public bool AllBasesDiscovered { get; set; }
        public long TotalPackets { get; set; }
        public long LastProgressHeartbeatTick;
    }
}
