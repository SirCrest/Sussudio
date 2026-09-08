using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Sussudio.Tests;

// The same deterministic retained analysis was passed through the pre-refactor
// builder to capture the compatibility fixture. Distinct metric values make an
// accidental field swap visible across the complete serialized result.
internal static class DiagnosticCompositionFixture
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static JsonElement BuildResult(Assembly assembly)
    {
        var builderType = RequireType(assembly, "DiagnosticSessionResultBuilder");
        var snapshot = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["SelectedResolution"] = "3840x2160",
            ["SelectedFrameRate"] = 119.88,
            ["SelectedFriendlyFrameRate"] = "119.88 fps",
            ["SelectedExactFrameRateArg"] = "120000/1001",
            ["SelectedVideoFormat"] = "NV12",
            ["VideoRequestedSubtype"] = "requested-NV12",
            ["VideoNegotiatedSubtype"] = "negotiated-NV12",
            ["SourceWidth"] = 3840,
            ["SourceHeight"] = 2160,
            ["DetectedSourceFrameRate"] = 120.0,
            ["DetectedSourceFrameRateArg"] = "120/1",
            ["SourceIsHdr"] = true,
            ["SourceTelemetrySummaryText"] = "fixture source telemetry",
            ["ProcessCpuPercent"] = 6.25,
            ["WasapiPlaybackBufferedDurationMs"] = 100.0,
            ["WasapiPlaybackQueueDurationMs"] = 200.0,
            ["FlashbackPlaybackTargetFps"] = 120.0
        });
        var warnings = new List<string> { "fixture warning" };
        var plan = RequireType(assembly, "DiagnosticSessionScenarioPlan").GetMethod("From", Members)!
            .Invoke(null, new object[] { "observe" })!;
        var sampleType = RequireType(assembly, "DiagnosticSessionSample");
        var samples = Array.CreateInstance(sampleType, 2);
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = Activator.CreateInstance(sampleType)!;
            sampleType.GetProperty("OffsetMs")!.SetValue(sample, (long)(i + 1) * 1000);
            sampleType.GetProperty("Snapshot")!.SetValue(sample, JsonSerializer.SerializeToElement(
                new Dictionary<string, object> { ["ProcessCpuPercent"] = i == 0 ? 9.5 : 4.5 }));
            samples.SetValue(sample, i);
        }

        var runBootstrap = Construct(RequireType(assembly, "DiagnosticSessionRunBootstrap"), new Dictionary<string, object?>
        {
            ["Scenario"] = "observe",
            ["ScenarioPlan"] = plan,
            ["DurationSeconds"] = 10,
            ["SampleIntervalMs"] = 1000,
            ["SessionId"] = "composition-fixture",
            ["OutputDirectory"] = "fixture-output",
            ["StartedUtc"] = DateTimeOffset.Parse("2026-09-07T00:00:00+00:00"),
            ["RunnerProcessId"] = 1234
        });
        var request = Construct(RequireType(assembly, "DiagnosticSessionResultBuildRequest"), new Dictionary<string, object?>
        {
            ["Options"] = Activator.CreateInstance(RequireType(assembly, "DiagnosticSessionOptions")),
            ["RunBootstrap"] = runBootstrap,
            ["LivePath"] = "fixture-live.json",
            ["CommandFailureCount"] = 0,
            ["Samples"] = samples,
            ["InitialSnapshot"] = snapshot,
            ["HealthSnapshot"] = snapshot,
            ["Timeline"] = null,
            ["Verification"] = JsonSerializer.SerializeToElement(new { Succeeded = true, Message = "fixture verified" }),
            ["PresentMon"] = null,
            ["StartedPreview"] = false,
            ["EnabledFlashback"] = false,
            ["StartedFlashbackPlayback"] = false,
            ["StoppedRecordingForVerification"] = false,
            ["Actions"] = new[] { "fixture action" },
            ["Warnings"] = warnings
        });
        var analysis = Construct(builderType.GetNestedType("DiagnosticSessionResultAnalysis", Members)!, new Dictionary<string, object?>
        {
            ["LastSnapshot"] = snapshot,
            ["HealthSummary"] = Construct(builderType.GetNestedType("DiagnosticSessionHealthSummary", Members)!, new Dictionary<string, object?>
            {
                ["Snapshot"] = snapshot,
                ["HealthStatus"] = "Healthy",
                ["LikelyStage"] = "fixture-stage",
                ["Summary"] = "fixture summary",
                ["Evidence"] = "fixture evidence"
            }),
            ["PlaybackSessionMetrics"] = FillMetric(RequireType(assembly, "FlashbackPlaybackSessionMetrics"), 1, snapshot),
            ["PlaybackResultMetrics"] = FillMetric(RequireType(assembly, "FlashbackPlaybackResultMetrics"), 2, snapshot),
            ["RecordingMetrics"] = FillMetric(RequireType(assembly, "FlashbackRecordingSessionMetrics"), 3, snapshot),
            ["ExportMetrics"] = FillMetric(RequireType(assembly, "FlashbackExportSessionMetrics"), 4, snapshot),
            ["PreviewCadenceMetrics"] = FillMetric(RequireType(assembly, "PreviewCadenceSessionMetrics"), 5, snapshot),
            ["PreviewD3DMetrics"] = FillMetric(RequireType(assembly, "PreviewD3DMetrics"), 6, snapshot),
            ["VisualCadenceMetrics"] = FillMetric(RequireType(assembly, "VisualCadenceSessionMetrics"), 7, snapshot),
            ["PreviewScheduler"] = FillRecord(builderType.GetNestedType("DiagnosticSessionPreviewSchedulerAnalysis", Members)!, 8, snapshot),
            ["DiagnosticHealthSucceeded"] = true,
            ["FlashbackWarningsSucceeded"] = true
        });
        var runStateType = RequireType(assembly, "DiagnosticSessionRunState");
        var runState = Construct(runStateType, new Dictionary<string, object?>
        {
            ["isCancellationRequested"] = (Func<bool>)(() => false),
            ["warnings"] = warnings
        });
        runStateType.GetMethod("SetStage", Members)!.Invoke(runState, new object[] { "summary" });
        var artifactPaths = Construct(RequireType(assembly, "DiagnosticSessionResultArtifactPaths"), new Dictionary<string, object?>
        {
            ["SummaryPath"] = "fixture-summary.json",
            ["SamplesPath"] = "fixture-samples.json",
            ["FrameLedgerPath"] = "fixture-frame-ledger.json",
            ["TimelinePath"] = "fixture-timeline.json"
        });
        var result = builderType.GetMethod("CreateResult", Members)!.Invoke(null, new[]
        {
            request, runState, analysis, artifactPaths,
            (object)DateTimeOffset.Parse("2026-09-07T00:00:10+00:00"), "completed"
        })!;
        return JsonSerializer.SerializeToElement(result, result.GetType());
    }

    internal static JsonElement BuildPlaybackMetrics(Assembly assembly, bool observed)
    {
        var metricsType = RequireType(assembly, "FlashbackPlaybackResultMetrics");
        var values = new Dictionary<string, object>();
        var index = 0;
        foreach (var property in metricsType.GetProperties().Where(p => p.Name.EndsWith("AtEnd", StringComparison.Ordinal)).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var snapshotProperty = "FlashbackPlayback" + property.Name.Substring(0, property.Name.Length - "AtEnd".Length);
            values[snapshotProperty] = Sentinel(property.PropertyType, snapshotProperty, 11, ++index, default);
        }
        var snapshot = JsonSerializer.SerializeToElement(values);
        var sessionType = RequireType(assembly, "FlashbackPlaybackSessionMetrics");
        var session = FillMetric(sessionType, 12, snapshot);
        sessionType.GetProperty("Observed")!.SetValue(session, observed);
        sessionType.GetProperty("BaselineSnapshot")!.SetValue(session, JsonSerializer.SerializeToElement(
            new Dictionary<string, object> { ["FlashbackPlaybackSeekForwardDecodeCapHits"] = 10 }));
        var result = RequireType(assembly, "DiagnosticSessionFlashbackMetrics").GetMethod("BuildFlashbackPlaybackResultMetrics", Members)!
            .Invoke(null, new[] { session })!;
        return JsonSerializer.SerializeToElement(result, result.GetType());
    }
    private static object FillMetric(Type type, int seed, JsonElement snapshot)
    {
        var value = Activator.CreateInstance(type)!;
        var index = 0;
        foreach (var property in type.GetProperties().Where(p => p.SetMethod != null).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            property.SetValue(value, Sentinel(property.PropertyType, property.Name, seed, ++index, snapshot));
        }
        return value;
    }

    private static object FillRecord(Type type, int seed, JsonElement snapshot)
    {
        var constructor = type.GetConstructors(Members).OrderByDescending(c => c.GetParameters().Length).First();
        return constructor.Invoke(constructor.GetParameters().Select((p, i) => Sentinel(p.ParameterType, p.Name!, seed, i + 1, snapshot)).ToArray());
    }

    private static object Sentinel(Type type, string name, int seed, int index, JsonElement snapshot)
    {
        if (type == typeof(string)) return $"metric-{seed}-{name}";
        if (type == typeof(bool)) return index % 2 == 0;
        if (type == typeof(int)) return seed * 100 + index;
        if (type == typeof(long)) return (long)seed * 1000 + index;
        if (type == typeof(double)) return seed * 100 + index + 0.25;
        if (type == typeof(JsonElement)) return snapshot;
        throw new InvalidOperationException($"Unsupported fixture metric {name}: {type}.");
    }

    private static object Construct(Type type, IReadOnlyDictionary<string, object?> values)
    {
        var constructor = type.GetConstructors(Members).Single(c => c.GetParameters().Length == values.Count && c.GetParameters().All(p => values.ContainsKey(p.Name!)));
        return constructor.Invoke(constructor.GetParameters().Select(p => values[p.Name!]).ToArray());
    }

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType("Sussudio.Tools." + name, throwOnError: true)!;
}