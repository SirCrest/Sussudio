using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class PreviewPacingOwnershipTests
{
    [Fact]
    public void PreviewPacingClassifier_SourceOwnershipIsSplit()
    {
        var classifierText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs")
            .Replace("\r\n", "\n");
        var d3dPolicyText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.D3D.cs")
            .Replace("\r\n", "\n");
        var sourceVisualLanePolicyText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.Lanes.SourceVisual.cs")
            .Replace("\r\n", "\n");
        var decodeJitterLanePolicyText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.Lanes.DecodeJitter.cs")
            .Replace("\r\n", "\n");
        var renderLanePolicyText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.Lanes.Render.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingClassificationModels.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");

        Assert.Contains("public sealed class PreviewPacingClassificationInput", modelText);
        Assert.Contains("public readonly record struct PreviewPacingClassification(", modelText);
        Assert.Contains("public static partial class PreviewPacingSlowStageClassifier", classifierText);
        Assert.DoesNotContain("public sealed class PreviewPacingClassificationInput", classifierText);
        Assert.DoesNotContain("public readonly record struct PreviewPacingClassification(", classifierText);
        Assert.Contains("var dominantStage = ResolveDominantD3DStage(input, targetFrameMs);", classifierText);
        Assert.DoesNotContain("private static string ResolveDominantD3DStage(", classifierText);
        Assert.Contains("TryClassifySourceCapture(input, sourceSampleReady, targetFps", classifierText);
        Assert.Contains("TryClassifyPreviewJitterScheduler(input, targetFrameMs", classifierText);
        Assert.Contains("TryClassifyRenderSubmit(input, out var renderSubmitClassification)", classifierText);
        Assert.DoesNotContain("private static bool IsSourceCaptureSuspect(", classifierText);
        Assert.Contains("private static bool TryClassifySourceCapture(", sourceVisualLanePolicyText);
        Assert.Contains("private static bool TryClassifyVisualDuplicateOrLowMotion(", sourceVisualLanePolicyText);
        Assert.Contains("private static bool IsSourceCaptureSuspect(", sourceVisualLanePolicyText);
        Assert.Contains("private static bool IsVisualDuplicateOrLowMotionSuspect(", sourceVisualLanePolicyText);
        Assert.DoesNotContain("TryClassifyMjpegDecode(", sourceVisualLanePolicyText);
        Assert.Contains("private static bool TryClassifyMjpegDecode(", decodeJitterLanePolicyText);
        Assert.Contains("private static bool TryClassifyPreviewJitterScheduler(", decodeJitterLanePolicyText);
        Assert.Contains("private static bool IsMjpegDecodeSuspect(", decodeJitterLanePolicyText);
        Assert.Contains("private static bool IsPreviewJitterSuspect(", decodeJitterLanePolicyText);
        Assert.DoesNotContain("TryClassifyRenderSubmit(", decodeJitterLanePolicyText);
        Assert.Contains("private static bool TryClassifyCompositorMiss(", renderLanePolicyText);
        Assert.Contains("private static bool TryClassifyRenderSubmit(", renderLanePolicyText);
        Assert.Contains("private static double CalculatePercent(", renderLanePolicyText);
        Assert.Contains("private static string ResolveDominantD3DStage(", d3dPolicyText);
        Assert.Contains("private static bool IsDominant(", d3dPolicyText);
        Assert.Contains("private static double Positive(double value)", d3dPolicyText);
        Assert.Contains("PreviewPacingSlowStageClassifier.D3D.cs", agentMapText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Lanes.SourceVisual.cs", agentMapText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Lanes.DecodeJitter.cs", agentMapText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Lanes.Render.cs", agentMapText);
    }

    [Fact]
    public void PreviewPacingClassifier_IsWiredIntoAutomationSnapshots()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();
        var diagnosticsSnapshotsText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.Access.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs"));
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsSnapshotProjectionCompositionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs");
        var diagnosticsSnapshotProjectionFlatteningText = string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Set.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"));
        var diagnosticsSnapshotProjectionSnapshotEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs");
        var diagnosticsSnapshotProjectionCaptureCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs");
        var diagnosticsPreviewPacingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.PreviewPacing.cs");
        var diagnosticsRealtimePreviewCountersText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs");
        var diagnosticsMjpegCountersText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.Mjpeg.cs");
        var diagnosticsFlashbackRecordingCountersText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.FlashbackRecording.cs");
        var diagnosticsCountersText = diagnosticsRealtimePreviewCountersText
            + "\n" + diagnosticsMjpegCountersText
            + "\n" + diagnosticsFlashbackRecordingCountersText;
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + diagnosticsSnapshotsText
            + "\n" + diagnosticsSnapshotProjectionText
            + "\n" + diagnosticsSnapshotProjectionCompositionText
            + "\n" + diagnosticsSnapshotProjectionFlatteningText
            + "\n" + diagnosticsSnapshotProjectionSnapshotEvaluationText
            + "\n" + diagnosticsSnapshotProjectionCaptureCadenceText
            + "\n" + diagnosticsPreviewPacingText
            + "\n" + diagnosticsCountersText
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.Preview.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.cs");

        Assert.Contains("public string PreviewPacingLikelySlowStage { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageConfidence { get; init; }", contractsText);
        Assert.Contains("public string PreviewPacingSlowStageEvidence { get; init; }", contractsText);
        Assert.Contains("var previewPacingClassification = ClassifyPreviewPacing(", diagnosticsSnapshotsText);
        Assert.DoesNotContain("new PreviewPacingClassificationInput", diagnosticsSnapshotsText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionFlatteningText);
        Assert.Contains("PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage", diagnosticsSnapshotProjectionSnapshotEvaluationText);
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsSnapshotProjectionSnapshotEvaluationText);
        Assert.Contains("private static PreviewPacingClassification ClassifyPreviewPacing(", diagnosticsPreviewPacingText);
        Assert.Contains("PreviewPacingSlowStageClassifier.Classify", diagnosticsPreviewPacingText);
        Assert.Contains("PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps", diagnosticsHubText);
        Assert.Contains("CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames", diagnosticsHubText);
        Assert.Contains("RecentD3DMissedRefreshes = recentD3DMissedRefreshes", diagnosticsHubText);
        Assert.Contains("RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount", diagnosticsHubText);
        Assert.Contains("RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts", diagnosticsHubText);
        Assert.Contains("UpdateD3DFrameLatencyWaitRecentCounters", diagnosticsHubText);
        Assert.Contains("private long UpdateD3DFrameLatencyWaitRecentCounters(", diagnosticsRealtimePreviewCountersText);
        Assert.DoesNotContain("private long UpdateD3DFrameLatencyWaitRecentCounters(", diagnosticsMjpegCountersText);
        Assert.DoesNotContain("private long UpdateD3DFrameLatencyWaitRecentCounters(", diagnosticsFlashbackRecordingCountersText);
        Assert.DoesNotContain("private long UpdateD3DFrameLatencyWaitRecentCounters(", ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs"));
        Assert.Contains("PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingLikelySlowStage = preview.PacingLikelySlowStage", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageConfidence = preview.PacingSlowStageConfidence", diagnosticsHubText);
        Assert.Contains("PreviewPacingSlowStageEvidence = preview.PacingSlowStageEvidence", diagnosticsHubText);
        Assert.Contains("PacingLikelySlowStage: snapshot.PreviewPacingLikelySlowStage", diagnosticsHubText);
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string ReadAutomationSnapshotFamilyText()
    {
        var files = new[]
        {
            "Sussudio/Models/Automation/AutomationSnapshot.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.UserSettings.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Hdr.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.AudioIngest.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Recording.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.CaptureFormat.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SourceTelemetry.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Preview.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.CaptureCadence.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.MjpegTiming.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.MjpegPreviewJitter.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.MjpegPacketHash.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.VisualCadence.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SystemHealth.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.FlashbackRecording.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.FlashbackPlayback.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.FlashbackExport.cs"
        };

        var parts = new List<string>();
        foreach (var file in files)
        {
            parts.Add(ReadRepoFile(file).Replace("\r\n", "\n"));
        }

        return string.Join("\n", parts);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}
