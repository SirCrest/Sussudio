using System.Reflection;

static partial class Program
{
    internal static Task PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameStatisticsPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate not found.");

        var frameStatistics = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SampleCount"), "null D3D frame-stat sample count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SuccessCount"), "null D3D frame-stat success count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "FailureCount"), "null D3D frame-stat failure count");
        AssertEqual(string.Empty, GetStringProperty(frameStatistics, "LastError"), "null D3D frame-stat last error");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentCount"), "null D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentRefreshCount"), "null D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "SyncRefreshCount"), "null D3D sync-refresh sentinel");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SyncQpcTime"), "null D3D sync QPC time");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentDelta"), "null D3D present delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentRefreshDelta"), "null D3D present-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastSyncRefreshDelta"), "null D3D sync-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "MissedRefreshCount"), "null D3D missed-refresh count");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameLatencyWaitPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate not found.");

        var frameLatencyWait = evaluate.Invoke(null, new object[] { null! })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy returned null.");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "Enabled"), "null D3D frame-latency wait enabled");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "HandleActive"), "null D3D frame-latency wait handle active");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "CallCount"), "null D3D frame-latency wait call count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "SignaledCount"), "null D3D frame-latency wait signaled count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "TimeoutCount"), "null D3D frame-latency wait timeout count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "UnexpectedResultCount"), "null D3D frame-latency wait unexpected-result count");
        AssertEqual(0u, GetPropertyValue(frameLatencyWait, "LastResult"), "null D3D frame-latency wait last result");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "LastWaitMs"), "null D3D frame-latency wait last wait");
        AssertEqual(0, GetIntProperty(frameLatencyWait, "SampleCount"), "null D3D frame-latency wait sample count");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "AverageMs"), "null D3D frame-latency wait average");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P95Ms"), "null D3D frame-latency wait p95");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P99Ms"), "null D3D frame-latency wait p99");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "MaxMs"), "null D3D frame-latency wait max");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameOwnershipPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate not found.");

        var frameOwnership = evaluate.Invoke(null, new object[] { null! })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedPreviewPresentId"), "null D3D submitted present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastSubmittedSourceSequenceNumber"), "null D3D submitted source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedSourcePtsTicks"), "null D3D submitted source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedQpc"), "null D3D submitted QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedUtcUnixMs"), "null D3D submitted UTC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedPreviewPresentId"), "null D3D rendered present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastRenderedSourceSequenceNumber"), "null D3D rendered source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedSourcePtsTicks"), "null D3D rendered source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedQpc"), "null D3D rendered QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedUtcUnixMs"), "null D3D rendered UTC");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedSchedulerToPresentMs"), "null D3D scheduler-to-present");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedPipelineLatencyMs"), "null D3D pipeline latency");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedPreviewPresentId"), "null D3D dropped present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastDroppedSourceSequenceNumber"), "null D3D dropped source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedSourcePtsTicks"), "null D3D dropped source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedQpc"), "null D3D dropped QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedUtcUnixMs"), "null D3D dropped UTC");
        AssertEqual(string.Empty, GetStringProperty(frameOwnership, "LastDropReason"), "null D3D drop reason");

        return Task.CompletedTask;
    }
}
