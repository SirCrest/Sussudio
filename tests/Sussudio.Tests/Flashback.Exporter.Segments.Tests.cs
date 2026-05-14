using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_RejectsInvalidExportRanges()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_invalid_range_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.ts");
        File.WriteAllBytes(inputPath, new byte[] { 0x47 });

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-invalid.mp4");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5),
                    singleOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Empty single-file export range reports failure");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "export range is empty or invalid");
                AssertEqual(false, File.Exists(singleOutputPath), "Invalid single-file range does not create output");
                AssertEqual(false, File.Exists(singleOutputPath + ".tmp"), "Invalid single-file range does not leave temp output");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-invalid.mp4");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.FromSeconds(-1),
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Negative segment export in point reports failure");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "in point must not be negative");
                AssertEqual(false, File.Exists(segmentOutputPath), "Invalid segment range does not create output");
                AssertEqual(false, File.Exists(segmentOutputPath + ".tmp"), "Invalid segment range does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExportRejectedDiagnostics_PreserveAttemptedRange()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(captureServiceText, "if (inPointFilePts.HasValue || outPointFilePts.HasValue)");
        AssertContains(captureServiceText, "var absoluteInPoint = inPointFilePts ?? validStart;");
        AssertContains(captureServiceText, "var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;");
        AssertContains(captureServiceText, "\"Flashback export in point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "\"Flashback export out point has been evicted from the buffer.\"");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_SNAPSHOT_FAIL op=range type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_SNAPSHOT_FAIL op=last_n type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceText, "_flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;");
        AssertContains(captureServiceText, "outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "private static void FreeBufferedPackets(List<IntPtr> bufferedPackets, List<int>? bufferedStreamIndices = null)");
        AssertContains(sourceText, "FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);");
        AssertContains(sourceText, "FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);");
        AssertContains(sourceText, "bufferedStreamIndices?.Clear();");
        AssertContains(sourceText, "private static AVPacket* ClonePacketOrThrow(AVPacket* packet, string operation)");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PACKET_CLONE_FAIL operation={operation}");
        AssertContains(sourceText, "var clone = ClonePacketOrThrow(packet, \"single_buffer\");");
        AssertContains(sourceText, "var clone = ClonePacketOrThrow(packet, \"segment_buffer\");");

        var segmentLoopBlock = ExtractTextBetween(
            sourceText,
            "var segmentVideoFrameDurUs = 33333L;",
            "// Update cross-segment offset:");
        // The inline flush body was extracted into a local function FlushSegmentBufferedPackets
        // so the EOF rescue path can call it too. Both call sites must exist.
        AssertContains(segmentLoopBlock, "int FlushSegmentBufferedPackets(out bool stopFlushing)");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(out var stopFlushing);");
        AssertContains(segmentLoopBlock, "totalPackets += FlushSegmentBufferedPackets(out _);");
        // The local function's finally block must release buffered packets.
        AssertContains(segmentLoopBlock, "finally\n                        {\n                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);\n                        }");
        AssertOccursBefore(
            segmentLoopBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\");",
            "finally\n                        {\n                            FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);\n                        }");
        // EOF rescue: when Phase 1 never completed because some configured stream never
        // produced packets, flush whatever is buffered using a fallback base of 0 so we
        // do not silently discard video. (Was: bare FreeBufferedPackets that dropped video.)
        AssertContains(segmentLoopBlock, "if (!segAllBasesDiscovered && segBufferedPackets.Count > 0)");
        AssertContains(segmentLoopBlock, "segMinBaseUs ??= 0;");
        AssertContains(segmentLoopBlock, "FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH seg={segIdx}");
        // The else branch still calls FreeBufferedPackets for the empty-buffer case.
        AssertContains(segmentLoopBlock, "FreeBufferedPackets(segBufferedPackets, segBufferedStreamIndices);");

        var sharedFlushBlock = ExtractTextBetween(
            sourceText,
            "private long FlushBufferedPackets(",
            "private static void FreeBufferedPackets(");
        AssertContains(sharedFlushBlock, "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");
        AssertOccursBefore(
            sharedFlushBlock,
            "ThrowIfError(ffmpeg.av_interleaved_write_frame(_activeOutputContext, buffPkt), \"av_interleaved_write_frame\");",
            "finally\n        {\n            FreeBufferedPackets(bufferedPackets, bufferedStreamIndices);\n        }");

        return Task.CompletedTask;
    }
}
