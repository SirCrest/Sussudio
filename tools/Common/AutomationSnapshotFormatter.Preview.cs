using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {Get(snapshot, "PreviewStartupState")} | First Visual: {Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {Get(snapshot, "PreviewGpuPlaybackState")} | Video: {Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {Get(snapshot, "PreviewGpuPositionEventCount")}");
            return;
        }

        if (IsPreviewD3DRendererMode(rendererMode))
        {
            AppendPreviewD3DSection(builder, snapshot);
            return;
        }

        builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
        builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {Get(snapshot, "PreviewCadenceSampleCount")} over {Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
    }

}
