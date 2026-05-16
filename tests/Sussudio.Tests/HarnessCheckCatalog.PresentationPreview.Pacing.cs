using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewPacingChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "D3D preview transition drain drops pending frames",
            D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration);
        await AddCheckAsync(results,
            "D3D preview frame capture cancellation clears pending request",
            D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest);
        await AddCheckAsync(results,
            "Shared D3D device references are duplicated under lifecycle lock",
            SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock);
    }
}
