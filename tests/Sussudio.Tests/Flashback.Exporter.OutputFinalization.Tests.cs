using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
}
