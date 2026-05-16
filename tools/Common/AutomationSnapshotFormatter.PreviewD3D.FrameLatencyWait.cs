using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D frame-latency wait: enabled={Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
    }
}
