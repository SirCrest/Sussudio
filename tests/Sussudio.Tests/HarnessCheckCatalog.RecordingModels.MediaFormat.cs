using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelMediaFormatChecksAsync(List<CheckResult> results)
    {
        // --- MediaFormat ---
        await AddCheckAsync(results,
            "MediaFormat equality with matching rational frame rates",
            MediaFormat_Equality_WithMatchingRationalFrameRates);
        await AddCheckAsync(results,
            "MediaFormat inequality when dimensions differ",
            MediaFormat_Inequality_WhenDimensionsDiffer);
        await AddCheckAsync(results,
            "MediaFormat GetHashCode consistency for equal objects",
            MediaFormat_GetHashCode_ConsistencyForEqualObjects);
    }
}
