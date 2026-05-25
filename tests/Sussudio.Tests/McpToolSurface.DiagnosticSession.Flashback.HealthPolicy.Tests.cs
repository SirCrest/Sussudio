using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "internal static class DiagnosticSessionFlashbackValidation");
        AssertContains(validationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(validationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(validationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(validationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(validationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(validationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(validationText, "\"flashback preview: present/display pressure \"");
        AssertContains(validationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("DiagnosticSessionSample type was not found.");
        var buildObservation = healthPolicyType.GetMethod(
                "BuildSessionDiagnosticHealthObservation",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildSessionDiagnosticHealthObservation was not found.");

        var samples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Warning", "flashback_playback", "startup 1% low")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var finalSnapshot = CreateDiagnosticSnapshot("Healthy", "none", "final");
        var transientWarningObservation = buildObservation.Invoke(
                null,
                new object?[] { samples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Transient warning observation was null.");
        AssertEqual("Healthy", GetPropertyValue(transientWarningObservation, "HealthStatus") as string, "flashback warmup health status");
        AssertEqual("none", GetPropertyValue(transientWarningObservation, "LikelyStage") as string, "flashback warmup likely stage");

        var criticalSamples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Critical", "flashback_playback", "startup crash")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var criticalObservation = buildObservation.Invoke(
                null,
                new object?[] { criticalSamples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Critical observation was null.");
        AssertEqual("Critical", GetPropertyValue(criticalObservation, "HealthStatus") as string, "flashback critical health status");
        AssertEqual("flashback_playback", GetPropertyValue(criticalObservation, "LikelyStage") as string, "flashback critical likely stage");

        return Task.CompletedTask;

        static object CreateDiagnosticSessionSampleList(Type sampleType, params (long OffsetMs, JsonElement Snapshot)[] values)
        {
            var listType = typeof(List<>).MakeGenericType(sampleType);
            var list = (System.Collections.IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("DiagnosticSessionSample list could not be created."));
            foreach (var value in values)
            {
                var sample = Activator.CreateInstance(sampleType)
                    ?? throw new InvalidOperationException("DiagnosticSessionSample instance could not be created.");
                sampleType.GetProperty("OffsetMs")!.SetValue(sample, value.OffsetMs);
                sampleType.GetProperty("TimestampUtc")!.SetValue(sample, DateTimeOffset.UtcNow);
                sampleType.GetProperty("Snapshot")!.SetValue(sample, value.Snapshot);
                list.Add(sample);
            }

            return list;
        }

        static JsonElement CreateDiagnosticSnapshot(string health, string stage, string evidence)
        {
            using var document = JsonDocument.Parse($$"""
                {
                  "DiagnosticHealthStatus": "{{health}}",
                  "DiagnosticLikelyStage": "{{stage}}",
                  "DiagnosticEvidence": "{{evidence}}"
                }
                """);
            return document.RootElement.Clone();
        }
    }
}
