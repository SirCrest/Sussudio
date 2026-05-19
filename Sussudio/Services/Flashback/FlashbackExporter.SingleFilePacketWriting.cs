using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private SingleFilePacketWriteResult WriteSingleFilePacketsToActiveOutput(
        int streamCount,
        int videoStreamIndex,
        int[] streamMap,
        TimeSpan outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        var packetState = CreateSingleFilePacketWriteState(streamCount);
        WriteSingleFilePacketReadLoop(
            streamCount,
            videoStreamIndex,
            streamMap,
            ToAvTimeBaseTimestampOrMax(outPoint),
            progress,
            ct,
            ref packetState);

        LogTimestampBaseDrift(packetState.TimestampBasesUs, packetState.HasTimestampBase);

        if (videoStreamIndex >= 0 && videoStreamIndex < streamCount && packetState.PacketCounts[videoStreamIndex] == 0)
        {
            const string message = "Flashback export failed: no video packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        for (var i = 0; i < streamCount; i++)
        {
            if (i != videoStreamIndex && packetState.HasTimestampBase[i] && packetState.PacketCounts[i] == 0)
            {
                Logger.Log($"FLASHBACK_EXPORT_WARN stream={i} reason='no_packets_written' (non-video stream)");
            }
        }

        if (packetState.TotalPackets == 0)
        {
            const string message = "Flashback export failed: no packets were written.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            return new SingleFilePacketWriteResult(FinalizeResult.Failure(outputPath, message), packetState.TotalPackets);
        }

        return new SingleFilePacketWriteResult(null, packetState.TotalPackets);
    }

    private readonly record struct SingleFilePacketWriteResult(FinalizeResult? Failure, long TotalPackets);
}
