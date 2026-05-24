using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    // Shared Flashback test helpers stay here; feature coverage lives in focused partial files.

    private static string ReadFlashbackPlaybackControllerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.SeekDisplay.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackEncoderSinkSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateRequests.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Video.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Audio.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackExporterSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Validation.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackDecoderSource()
    {
        var parts = new[]
        {
            // Keep audio first so source-shape checks still see audio delivery
            // before the root file's frame-conversion section marker.
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoConversion.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Timestamps.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackBufferManagerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static (int, int) GetTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (Convert.ToInt32(item1), Convert.ToInt32(item2));
    }

    private static (int?, int?) GetNullableTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (item1 == null ? null : Convert.ToInt32(item1), item2 == null ? null : Convert.ToInt32(item2));
    }

    private static void SeedCommandFailure(object controller, string failure)
        => InvokeNonPublicInstanceMethod(controller, "SetLastCommandFailure", new object[] { failure });

    private static bool ValidateFinalOutputFailureAfterMove(string outputPath, out long outputBytes, out string failureMessage)
    {
        if (outputPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            outputBytes = new FileInfo(outputPath).Length;
            failureMessage = string.Empty;
            return outputBytes > 0;
        }

        outputBytes = -1;
        failureMessage = $"forced final validation failure for '{outputPath}'";
        return false;
    }

}
