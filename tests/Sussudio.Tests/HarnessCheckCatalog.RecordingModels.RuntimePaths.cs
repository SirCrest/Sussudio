using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelRuntimePathChecksAsync(List<CheckResult> results)
    {
        // --- RuntimePaths ---
        await AddCheckAsync(results,
            "RuntimePaths GetRepoLogFile returns path under repo root",
            RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot);
        await AddCheckAsync(results,
            "RuntimePaths paths contain expected directory names",
            RuntimePaths_PathsContainExpectedDirectoryNames);
        await AddCheckAsync(results,
            "MMCSS registration uses Unicode AVRT entry point",
            MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint);
    }
}
