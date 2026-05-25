using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_InputStreamCountsAreBounded()
    {
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Streams.cs")
            .Replace("\r\n", "\n");
        var streamTemplatesText = streamsText;
        var singleFileText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs")
            .Replace("\r\n", "\n");
        var segmentTemplateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Segments.cs")
            .Replace("\r\n", "\n");
        var segmentInputPreflightText = segmentTemplateText;

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(streamsText, "private static bool TryGetInputStreamCount(");
        AssertContains(streamsText, "if (nativeStreamCount == 0)");
        AssertContains(streamsText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(streamsText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_template\", out var candidateStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out currentStreamCount, out var streamCountFailure))");
        AssertContains(segmentInputPreflightText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='invalid_stream_count'");
        AssertContains(singleFileText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(segmentTemplateText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount)");
        AssertContains(streamTemplatesText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }
}
