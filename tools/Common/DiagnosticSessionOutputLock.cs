using System;
using System.IO;

namespace Sussudio.Tools;

internal static class DiagnosticSessionOutputLock
{
    internal static FileStream Acquire(string outputDirectory)
    {
        // Per-output-directory exclusive lock. Prevents two concurrent diagnostic-session
        // invocations from corrupting the manifest, final.snapshot.json, and per-scenario
        // JSON files in the same OutputDirectory. FileShare.None blocks other openers;
        // DeleteOnClose self-cleans on normal exit, and the OS releases the handle on crash.
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");
        try
        {
            return new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Another diagnostic session is already running in '{outputDirectory}'. " +
                $"Wait for it to finish or choose a different output directory. ({ex.Message})",
                ex);
        }
    }
}
