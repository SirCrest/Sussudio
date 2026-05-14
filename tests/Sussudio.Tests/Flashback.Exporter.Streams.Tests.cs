using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_InputStreamCountsAreBounded()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private static bool TryGetInputStreamCount(");
        AssertContains(sourceText, "if (nativeStreamCount == 0)");
        AssertContains(sourceText, "if (nativeStreamCount > MaxSupportedInputStreams)");
        AssertContains(sourceText, "streamCount = (int)nativeStreamCount;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"single_export\", out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{streamCountFailure}'\");");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_template\", out var candidateStreamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_activeInputContext, \"segment_export\", out var currentStreamCount, out var streamCountFailure))");
        AssertContains(sourceText, "FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segPath)}' reason='invalid_stream_count'");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, streamCount)");
        AssertContains(sourceText, "CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount)");
        AssertContains(sourceText, "private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)");
        AssertDoesNotContain(sourceText, "checked((int)_activeInputContext->nb_streams)");
        AssertDoesNotContain(sourceText, "checked((int)inputContext->nb_streams)");

        return Task.CompletedTask;
    }
}
