using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class DiagnosticCycleExportTests
    {
        public static IEnumerable<object[]> ScenarioOutcomes()
        {
            var scenarios = new (string Name, string Artifact, bool PreviewStopped, string Requested, string ExportFailure)[]
            {
                ("flashback restart cycle", "flashback-restart-cycle-export.mp4", false,
                    "flashback restart cycle export requested", "flashback restart cycle: export failed - "),
                ("flashback encoder cycle", "flashback-encoder-cycle-export.mp4", false,
                    "flashback encoder cycle export requested", "flashback encoder cycle: export failed - "),
                ("flashback preview cycle", "flashback-preview-off-export.mp4", true,
                    "flashback preview cycle export while preview off requested", "flashback preview cycle: export while preview off failed - "),
                ("flashback playback preview cycle", "flashback-playback-preview-cycle.mp4", true,
                    "flashback playback preview cycle export while preview off requested", "flashback playback preview cycle: export while preview off failed - ")
            };
            foreach (var scenario in scenarios)
            foreach (var outcome in new[] { "success", "export-failed", "export-no-message", "verify-failed", "verify-no-message", "export-threw", "verify-threw", "export-canceled", "verify-canceled" })
                yield return new object[] { scenario.Name, scenario.Artifact, scenario.PreviewStopped, scenario.Requested, scenario.ExportFailure, outcome };
        }

        [Theory]
        [MemberData(nameof(ScenarioOutcomes))]
        public Task CycleExportPreservesRequestsAndOutcomes(string scenario, string artifact, bool previewStopped,
            string requestedAction, string exportFailurePrefix, string outcome)
            => global::Program.DiagnosticCycleExport_PreservesRequestsAndOutcomes(
                scenario, artifact, previewStopped, requestedAction, exportFailurePrefix, outcome);
    }
}

static partial class Program
{
    internal static async Task DiagnosticCycleExport_PreservesRequestsAndOutcomes(
        string scenario, string artifact, bool previewStopped, string requestedAction, string exportFailurePrefix, string outcome)
    {
        var assembly = LoadDiagnosticSessionRunnerAssembly();
        var method = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackExports", throwOnError: true)!
            .GetMethod("VerifyCycleExportAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        var exportPath = Path.Combine("synthetic-cycle-artifacts", artifact);
        var actions = new List<string> { "existing action" };
        var warnings = new List<string> { "existing warning" };
        var calls = new List<(string Command, Dictionary<string, object?> Payload, int? Timeout)>();
        var senderFailure = new InvalidOperationException("synthetic sender failure");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        async Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? payload, int? timeout)
        {
            var isExport = command == "FlashbackExport";
            Assert.Equal(isExport ? 0 : 1, calls.Count);
            Assert.Equal(isExport ? 1 : 2, actions.Count);
            Assert.Single(warnings);
            calls.Add((command, new Dictionary<string, object?>(payload!), timeout));
            await Task.Yield();
            var stage = isExport ? "export" : "verify";
            if (outcome == stage + "-threw") throw senderFailure;
            if (outcome == stage + "-canceled")
                return await Task.FromCanceled<JsonElement>(cancellation.Token);
            if (outcome == stage + "-failed")
                return JsonSerializer.SerializeToElement(new { Success = false, Message = "synthetic rejection" });
            if (outcome == stage + "-no-message")
                return JsonSerializer.SerializeToElement(new { Success = false });
            return JsonSerializer.SerializeToElement(new { Success = true });
        }

        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sender = SendAsync;
        var task = (Task)method.Invoke(null, new object[] { exportPath, scenario, actions, warnings, sender, previewStopped })!;
        var error = await Record.ExceptionAsync(() => task);
        var exportStopped = outcome.StartsWith("export-", StringComparison.Ordinal);
        Assert.Equal(exportStopped ? 1 : 2, calls.Count);
        Assert.Equal("FlashbackExport", calls[0].Command);
        Assert.Equal(60_000, calls[0].Timeout);
        Assert.Equal(2, calls[0].Payload.Count);
        Assert.Equal(1, calls[0].Payload["seconds"]);
        Assert.Equal(exportPath, calls[0].Payload["outputPath"]);
        if (!exportStopped)
        {
            Assert.Equal("VerifyFile", calls[1].Command);
            Assert.Equal(60_000, calls[1].Timeout);
            Assert.Equal(3, calls[1].Payload.Count);
            Assert.Equal(exportPath, calls[1].Payload["filePath"]);
            Assert.Equal(true, calls[1].Payload["strict"]);
            Assert.Equal("flashback-export", calls[1].Payload["verificationProfile"]);
        }

        var expectedActions = new List<string> { "existing action" };
        if (outcome != "export-threw" && outcome != "export-canceled") expectedActions.Add(requestedAction);
        if (outcome == "success") expectedActions.Add(scenario + " export verified");
        Assert.Equal(expectedActions, actions);
        var expectedWarnings = new List<string> { "existing warning" };
        if (outcome == "export-failed") expectedWarnings.Add(exportFailurePrefix + "synthetic rejection");
        if (outcome == "export-no-message") expectedWarnings.Add(exportFailurePrefix + "unknown error");
        if (outcome == "verify-failed") expectedWarnings.Add(scenario + " export verification: synthetic rejection");
        if (outcome == "verify-no-message") expectedWarnings.Add(scenario + " export verification: verification failed");
        Assert.Equal(expectedWarnings, warnings);

        if (outcome.EndsWith("-threw", StringComparison.Ordinal)) Assert.Same(senderFailure, error);
        else if (outcome.EndsWith("-canceled", StringComparison.Ordinal))
        {
            Assert.Equal(cancellation.Token, Assert.IsAssignableFrom<OperationCanceledException>(error).CancellationToken);
            Assert.True(task.IsCanceled);
        }
        else Assert.Null(error);
    }
}
