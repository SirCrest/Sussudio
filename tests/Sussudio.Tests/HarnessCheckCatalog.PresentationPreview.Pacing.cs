using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewPacingChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Preview pacing classifier rejects weak samples",
            PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal);
        await AddCheckAsync(results,
            "Preview pacing classifier prefers source capture when source drops",
            PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail);
        await AddCheckAsync(results,
            "Preview pacing classifier flags compositor misses first",
            PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked);
        await AddCheckAsync(results,
            "Preview pacing classifier flags dominant render upload",
            PreviewPacingClassifier_ClassifiesDominantRenderUpload);
        await AddCheckAsync(results,
            "Preview pacing classifier flags frame latency wait timeout",
            PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout);
        await AddCheckAsync(results,
            "Preview pacing classifier ignores stale lifetime signals",
            PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas);
        await AddCheckAsync(results,
            "Preview pacing classifier flags recent jitter schedule-late",
            PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate);
        await AddCheckAsync(results,
            "Preview pacing classifier source ownership is split",
            PreviewPacingClassifier_SourceOwnershipIsSplit);
        await AddCheckAsync(results,
            "Preview pacing classifier is wired into automation snapshots",
            PreviewPacingClassifier_IsWiredIntoAutomationSnapshots);
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
