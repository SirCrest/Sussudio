using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation()
    {
        var sourceText = ReadFlashbackExporterSource();
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.PacketBuffers.cs")
            .Replace("\r\n", "\n");
        var runtimePolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private readonly object _lifetimeSync = new();");
        AssertContains(sourceText, "return Task.FromResult(CreateDisposedExportResult(request.OutputPath));");
        AssertEqual(2, sourceText.Split("return Task.FromResult(CreateDisposedExportResult(outputPath));", StringSplitOptions.None).Length - 1, "Single and segment wrappers return disposed result");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            cancellationResult = CreateDisposedExportResult(outputPath);\n            return false;\n        }");
        AssertContains(sourceText, "linkedCts = CreateExportCancellationSource(ct);");
        AssertContains(sourceText, "var segmentSnapshot = SnapshotSegments(segments);");
        AssertContains(sourceText, "private static IReadOnlyList<FlashbackExportSegment> SnapshotSegments(IReadOnlyList<FlashbackExportSegment>? segments)");
        AssertContains(sourceText, "snapshot[i] = segment == null\n                ? new FlashbackExportSegment { Path = string.Empty }\n                : segment with { };");
        AssertContains(sourceText, "CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposed, this);");
        AssertContains(sourceText, "private static FinalizeResult CreateDisposedExportResult(string outputPath)");
        AssertContains(sourceText, "const string message = \"Flashback exporter is disposed.\";");
        AssertContains(sourceText, "private const int ExportLockWaitTimeoutSeconds = 30;");
        AssertContains(runtimePolicyText, "private const int ExportWriterYieldPacketInterval = 256;");
        AssertContains(runtimePolicyText, "private const int ExportWriterThrottlePacketInterval = 4096;");
        AssertContains(runtimePolicyText, "private const int ExportWriterThrottleSleepMs = 1;");
        AssertContains(runtimePolicyText, "private const int ExportWriterAdaptiveThrottlePacketInterval = 4;");
        AssertContains(runtimePolicyText, "private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;");
        AssertContains(sourceText, "_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportCore(inputTsPath, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"single_export\"));");
        AssertContains(sourceText, "return RunWithBackgroundPriority(\n                () => RunWithAdaptiveThrottle(\n                    adaptiveThrottleDelayMsProvider,\n                    () => ExportSegmentsCore(segmentSnapshot, inPoint, outPoint, outputPath, fastStart, allowOverwrite, progress, linkedCts.Token)),\n                () => DisposeLinkedCtsBestEffort(linkedCts, \"segment_export\"));");
        AssertContains(sourceText, "thread.Priority = ThreadPriority.BelowNormal;");
        AssertContains(sourceText, "thread.Priority = previousPriority;");
        AssertContains(sourceText, "Func<int>? adaptiveThrottleDelayMsProvider");
        AssertContains(runtimePolicyText, "private readonly object _adaptiveThrottleSync = new();");
        AssertContains(runtimePolicyText, "private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)");
        AssertContains(runtimePolicyText, "private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()");
        AssertContains(runtimePolicyText, "[ThreadStatic]\n    private static Func<int>? s_adaptiveThrottleDelayMsProvider;");
        AssertContains(runtimePolicyText, "private static FinalizeResult RunWithAdaptiveThrottle(");
        AssertContains(runtimePolicyText, "private static void ThrottleExportWriterIfNeeded(long packetsWritten)");
        AssertContains(runtimePolicyText, "packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0");
        AssertContains(runtimePolicyText, "ExportWriterMaxAdaptiveThrottleSleepMs");
        AssertContains(runtimePolicyText, "Thread.Sleep(ExportWriterThrottleSleepMs);");
        AssertContains(runtimePolicyText, "Thread.Yield();");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(totalPackets);");
        AssertContains(sourceText, "ThrottleExportWriterIfNeeded(written);");
        AssertContains(sourceText, "private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN");
        AssertContains(packetBuffersText, "private long FlushBufferedPackets(");
        AssertContains(packetBuffersText, "NormalizePacketTimestampsBeforeWrite(buffPkt);");
        AssertContains(packetBuffersText, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertContains(packetBuffersText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(packetBuffersText, "ffmpeg.av_packet_free(&p);");
        AssertContains(packetBuffersText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(packetBuffersText, "var clone = ffmpeg.av_packet_clone(packet);");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"single_export\");");
        AssertContains(sourceText, "ReleaseExportLockBestEffort(\"segment_export\");");
        AssertContains(sourceText, "private void ReleaseExportLockBestEffort(string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_RELEASE_WARN");
        AssertDoesNotContain(sourceText, "catch (ObjectDisposedException) { }");
        AssertDoesNotContain(sourceText, "}, linkedCts.Token);");
        AssertDoesNotContain(sourceText, "_disposeCts!.Token");

        return Task.CompletedTask;
    }

}
