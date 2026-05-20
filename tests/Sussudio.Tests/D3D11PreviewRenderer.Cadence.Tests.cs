using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+PresentCadenceMetrics");

        var expectedProps = new[]
        {
            "SampleCount", "ObservedFps", "ExpectedIntervalMs", "AverageIntervalMs",
            "P95IntervalMs", "P99IntervalMs", "MaxIntervalMs", "OnePercentLowFps", "JitterStdDevMs", "SlowFrameCount", "SlowFramePercent"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"PresentCadenceMetrics.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_presentCadenceLock", new object());
        SetPrivateField(renderer, "_presentIntervalWindowMs", new double[8]);

        var getMetrics = rendererType.GetMethod("GetPresentCadenceMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics not found.");

        // Use a deterministic fake clock: advance _lastPresentTick by a fixed step
        // (one 60 fps frame at the system frequency) before each TrackPresentCadence
        // call so the interval calculation is fully predictable without Thread.Sleep.
        var fakeStepTicks = System.Diagnostics.Stopwatch.Frequency / 60;

        // Call 1: establish baseline. _lastPresentTick starts at 0 (uninitialized
        // long), so the method sees previousTick <= 0 and returns 0.
        SetPrivateField(renderer, "_lastPresentTick", 0L);
        InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true });

        // Call 2: _lastPresentTick was just written by call 1 (real Stopwatch.GetTimestamp).
        // Prime it to a known value that is one step in the past so the interval is fakeStepTicks.
        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var firstInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, firstInterval > 0, "first measured cadence interval is recorded");

        var metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "sample count after first measured interval");

        // Call 3: suppressed present — advance the fake clock, result must be 0.0.
        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var suppressedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { false }));
        AssertEqual(0.0, suppressedInterval, "suppressed present does not report interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after suppressed present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "suppressed present does not add a sample");
        AssertEqual(1L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "suppressed present marks baseline pending");

        // Call 4: first measured present after suppression — resets baseline, returns 0.
        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var baselineInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(0.0, baselineInterval, "first measured present after suppression resets baseline");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after baseline present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "baseline reset does not add transition gap sample");
        AssertEqual(0L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "baseline pending flag clears after measured present");

        // Call 5: resumed measured present — should record a valid interval.
        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var resumedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, resumedInterval > 0, "second measured present after suppression records interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after resumed present.");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "measured cadence resumes after suppression baseline");

        return Task.CompletedTask;
    }
}
