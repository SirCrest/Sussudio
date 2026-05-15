using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelRecordingContextChecksAsync(List<CheckResult> results)
    {
        // --- GpuPipelineHandles ---
        await AddCheckAsync(results,
            "GpuPipelineHandles.None returns zeroed struct",
            GpuPipelineHandles_None_ReturnsZeroedStruct);

        // --- RecordingContextRequest ---
        await AddCheckAsync(results,
            "RecordingContextRequest defaults match RecordingContext defaults",
            RecordingContextRequest_DefaultsMatchRecordingContextDefaults);
    }
}
