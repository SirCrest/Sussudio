using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var segment = Activator.CreateInstance(segmentType)!;
            SetPropertyBackingField(segment, "Path", Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.mp4"));
            var segments = Array.CreateInstance(segmentType, 1);
            segments.SetValue(segment, 0);
            var outputPath = Path.Combine(Path.GetTempPath(), $"fb_cancelled_{Guid.NewGuid():N}.mp4");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

            var result = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                segments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                outputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Cancelled export reports failure result");
            AssertContains(GetStringProperty(result, "StatusMessage"), "cancelled");
            AssertEqual(false, File.Exists(outputPath), "Cancelled export does not create output");
            AssertEqual(false, File.Exists(outputPath + ".tmp"), "Cancelled export does not leave temp output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_CancellationWinsBeforeValidation()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
            var singleOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_single_{Guid.NewGuid():N}.mp4");
            var singleResult = exportCore.Invoke(exporter, new object?[]
            {
                Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.ts"),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                singleOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportCore returned null.");

            AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Cancelled single-file export reports failure");
            AssertContains(GetStringProperty(singleResult, "StatusMessage"), "cancelled");
            AssertDoesNotContain(GetStringProperty(singleResult, "StatusMessage"), "not found");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
            var emptySegments = Array.CreateInstance(segmentType, 0);
            var segmentOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_segments_{Guid.NewGuid():N}.mp4");
            var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                emptySegments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                segmentOutputPath,
                true,
                false,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Cancelled segment export reports failure");
            AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "cancelled");
            AssertDoesNotContain(GetStringProperty(segmentResult, "StatusMessage"), "no segment paths");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", Path.Combine(tempDir, "missing-segment.ts"));
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "missing-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Missing segment export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "no readable segment files");
                AssertEqual(false, File.Exists(outputPath), "Missing segment export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Missing segment export does not leave temp output");
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

    private static Task FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState()
    {
        var sourceText = ReadFlashbackExporterSource();

        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "    private FinalizeResult ExportCore");
        AssertContains(disposeBlock, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n        }");
        AssertOccursBefore(disposeBlock, "FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN", "var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));");
        AssertContains(disposeBlock, "ReleaseExportLockBestEffort(\"dispose\");");
        AssertContains(disposeBlock, "DisposeExportLockBestEffort();");
        AssertContains(disposeBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose\");");
        AssertContains(sourceText, "FLASHBACK_EXPORT_LOCK_DISPOSE_WARN");

        var timeoutBlock = ExtractTextBetween(
            sourceText,
            "if (!lockAcquired)",
            "        try\n        {\n            CleanupNativeState();");

        AssertContains(timeoutBlock, "FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock");
        AssertContains(timeoutBlock, "DisposeLinkedCtsBestEffort(disposeCts, \"dispose_timeout\");");
        AssertContains(timeoutBlock, "ClearDisposeCtsReference(disposeCts);");
        AssertContains(timeoutBlock, "return;");
        AssertDoesNotContain(timeoutBlock, "CleanupNativeState()");
        AssertDoesNotContain(timeoutBlock, "_exportLock.Dispose()");

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_paths_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "fb_source_0001.mp4");
            File.WriteAllBytes(sourcePath, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    sourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single-file export rejects source overwrite");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Single-file rejection preserves source bytes");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", sourcePath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects source overwrite");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Segment rejection preserves source bytes");

                var outputPath = Path.Combine(tempDir, "fb_output.mp4");
                var tempSourcePath = outputPath + ".tmp";
                File.WriteAllBytes(tempSourcePath, new byte[] { 0x01, 0x02, 0x03, 0x04 });

                var tempSingleResult = exportCore.Invoke(exporter, new object?[]
                {
                    tempSourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(tempSingleResult, "Succeeded"), "Single-file export rejects temp source overwrite");
                AssertContains(GetStringProperty(tempSingleResult, "StatusMessage"), "temporary output path must not overwrite source segment");
                AssertEqual(4L, new FileInfo(tempSourcePath).Length, "Single-file temp rejection preserves source bytes");

                var tempSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(tempSegment, "Path", tempSourcePath);
                var tempSegments = Array.CreateInstance(segmentType, 1);
                tempSegments.SetValue(tempSegment, 0);
                var tempSegmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    tempSegments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(tempSegmentResult, "Succeeded"), "Segment export rejects temp source overwrite");
                AssertContains(GetStringProperty(tempSegmentResult, "StatusMessage"), "temporary output path must not overwrite source segment");
                AssertEqual(4L, new FileInfo(tempSourcePath).Length, "Segment temp rejection preserves source bytes");
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

    private static Task FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_finalize_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-export.mp4");
            var tmpPath = outputPath + ".tmp";
            var existingBytes = new byte[] { 0x65, 0x78, 0x70, 0x6f, 0x72, 0x74 };
            File.WriteAllBytes(outputPath, existingBytes);
            File.WriteAllBytes(tmpPath, Array.Empty<byte>());

            // Pass allowOverwrite=true so we exercise the empty-temp guard rather than
            // the destination-exists guard: the existing export must still be preserved
            // when the temp file itself is invalid.
            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(false, finalized, "Invalid temp output is rejected");
            AssertContains((string)args[4]!, "temporary output file is empty before replacing");
            AssertEqual(true, File.Exists(outputPath), "Existing export remains present");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing export length is preserved");
            AssertEqual(false, File.Exists(tmpPath), "Invalid temp output is deleted");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_refuse_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var tmpPath = outputPath + ".tmp";
            var existingBytes = new byte[] { 0x66, 0x69, 0x72, 0x73, 0x74 };
            var freshTempBytes = new byte[] { 0x6e, 0x65, 0x77 };
            File.WriteAllBytes(outputPath, existingBytes);
            File.WriteAllBytes(tmpPath, freshTempBytes);

            // allowOverwrite=false → destination must be preserved, tmp must be deleted,
            // and a structured refusal message must surface in the out failureMessage.
            var args = new object?[] { tmpPath, outputPath, false, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(false, finalized, "Refuse-on-collision rejects the overwrite");
            AssertContains((string)args[4]!, "destination file already exists");
            AssertEqual(true, File.Exists(outputPath), "Existing take is preserved on refusal");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing take bytes are preserved on refusal");
            AssertEqual(false, File.Exists(tmpPath), "Temporary export is cleaned up on refusal");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_OverwritesWhenForceTrue()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeTemp = exporterType.GetMethod("TryFinalizeTempOutputFile", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFile not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_force_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var tmpPath = outputPath + ".tmp";
            File.WriteAllBytes(outputPath, new byte[] { 0x6f, 0x6c, 0x64 });
            var freshTempBytes = new byte[] { 0x6e, 0x65, 0x77, 0x65, 0x72 };
            File.WriteAllBytes(tmpPath, freshTempBytes);

            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty };
            var finalized = (bool)(finalizeTemp.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFile returned null."));

            AssertEqual(true, finalized, "Force=true overwrites the destination");
            AssertEqual(true, File.Exists(outputPath), "Destination remains present after overwrite");
            AssertEqual(freshTempBytes.Length, new FileInfo(outputPath).Length, "Destination contains the fresh export bytes");
            AssertEqual(false, File.Exists(tmpPath), "Temporary export was moved into place");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_FinalValidationFailureDeletesMovedOutput()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var finalizeCore = exporterType.GetMethod("TryFinalizeTempOutputFileCore", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryFinalizeTempOutputFileCore not found.");
        var validatorType = exporterType.GetNestedType("CompletedOutputValidator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CompletedOutputValidator not found.");
        var validatorMethod = typeof(Program).GetMethod(
            nameof(ValidateFinalOutputFailureAfterMove),
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateFinalOutputFailureAfterMove not found.");
        var validator = Delegate.CreateDelegate(validatorType, validatorMethod);

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_final_validate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var tmpPath = outputPath + ".tmp";
            File.WriteAllBytes(tmpPath, new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c });

            var args = new object?[] { tmpPath, outputPath, true, 0L, string.Empty, validator };
            var finalized = (bool)(finalizeCore.Invoke(null, args)
                ?? throw new InvalidOperationException("TryFinalizeTempOutputFileCore returned null."));

            AssertEqual(false, finalized, "Final validation failure is rejected");
            AssertContains((string)args[4]!, "forced final validation failure");
            AssertEqual(false, File.Exists(tmpPath), "Temporary output was moved before final validation");
            AssertEqual(false, File.Exists(outputPath), "Invalid moved final output is deleted");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_blocked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input.ts");
            File.WriteAllBytes(inputPath, new byte[] { 0x47 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-blocked.mp4");
                Directory.CreateDirectory(singleOutputPath + ".tmp");

                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    singleOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single export rejects blocked temp output");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "temporary output path is a directory");
                AssertEqual(false, File.Exists(singleOutputPath), "Single blocked temp export does not create output");
                AssertEqual(true, Directory.Exists(singleOutputPath + ".tmp"), "Single blocked temp directory is preserved");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-blocked.mp4");
                Directory.CreateDirectory(segmentOutputPath + ".tmp");

                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    false,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects blocked temp output");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "temporary output path is a directory");
                AssertEqual(false, File.Exists(segmentOutputPath), "Segment blocked temp export does not create output");
                AssertEqual(true, Directory.Exists(segmentOutputPath + ".tmp"), "Segment blocked temp directory is preserved");
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

}
