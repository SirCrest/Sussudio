using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthTolerancesType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthTolerances")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthTolerances type was not found.");
        var observationType = assembly.GetType("Sussudio.Tools.DiagnosticHealthObservation")
            ?? throw new InvalidOperationException("DiagnosticHealthObservation type was not found.");
        var sourceMetricsType = assembly.GetType("Sussudio.Tools.SourceCadenceSessionMetrics")
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics type was not found.");
        var sparseSourceWarning = healthTolerancesType.GetMethod(
                "IsSparseSourceCaptureCadenceWarningRun",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sparse source-cadence classifier was not found.");

        var observation = Activator.CreateInstance(
                observationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "Warning", "source_capture", "source gaps=1 drops=1", 85_727L, 2 },
                culture: null)
            ?? throw new InvalidOperationException("DiagnosticHealthObservation instance could not be created.");
        var metrics = Activator.CreateInstance(sourceMetricsType, nonPublic: true)
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics instance could not be created.");
        sourceMetricsType.GetProperty("MaxSevereGapCountObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxDropPercentObserved")!.SetValue(metrics, 0.042);

        AssertEqual(
            true,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "sparse source cadence warning without source counter deltas");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 1L, 0L, 300, true })!,
            "source reader drop delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 1L, 300, true })!,
            "video ingest error delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, false })!,
            "unhealthy visual cadence blocks sparse source cadence tolerance");

        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 3L);
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "repeated source cadence drops block sparse source cadence tolerance");

        return Task.CompletedTask;
    }
}
