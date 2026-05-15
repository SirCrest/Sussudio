using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddMcpDiagnosticsPipelineChecksAsync(List<CheckResult> results)
    {
        await AddMcpToolSurfaceChecksAsync(results);
        await AddDiagnosticSessionChecksAsync(results);
        await AddMcpPerformanceAndProbeToolChecksAsync(results);
        await AddMjpegPipelineChecksAsync(results);
        await AddRecordingPipelineChecksAsync(results);
    }
}
