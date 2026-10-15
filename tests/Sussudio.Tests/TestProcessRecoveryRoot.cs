using System.Runtime.CompilerServices;

namespace Sussudio.Tests;

// Owns the recording-recovery root for every process that loads this assembly:
// the xUnit host, the smoke entry point, and child processes it launches.
//
// Production writes unresolved and active recording markers to
// %LOCALAPPDATA%\Sussudio\RecordingRecovery and restores the newest one when the
// app starts. A test that drives CaptureService finalization without opting into a
// private root therefore leaves the developer's real app showing "An earlier
// recording failed". Installing the root before any test code runs makes isolation
// the default instead of a per-test opt-in. Tests that need their own root still
// push and restore the variable inside RecoveryEnvironmentCollection, and a restore
// lands back here.
internal static class TestProcessRecoveryRoot
{
    // Mirrors RecordingFinalizationRecoveryArtifacts.RecoveryDirectoryOverrideEnvVar.
    internal const string EnvironmentVariable = "SUSSUDIO_RECOVERY_DIRECTORY";

    [ModuleInitializer]
    internal static void Install()
    {
        // A parent process that handed this one a private root keeps ownership of it.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            return;
        }

        // The app creates the directory on first marker write; most processes never do.
        var root = Path.Combine(Path.GetTempPath(), $"sussudio-test-recovery-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable(EnvironmentVariable, root);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => TryDelete(root);
    }

    private static void TryDelete(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort at process exit; a leftover private temp directory is harmless.
        }
    }
}
