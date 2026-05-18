using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public class StatsPresentationTests
{
    [Fact]
    public void DockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        object Build(string? codecName, bool recording = true)
        {
            var snapshot = CreateUninitializedObject(snapshotType);
            SetPropertyBackingField(snapshot, "Recording", recording);
            SetPropertyBackingField(snapshot, "EncoderCodecName", codecName);
            SetPropertyBackingField(snapshot, "EncoderWidth", 3840);
            SetPropertyBackingField(snapshot, "EncoderHeight", 2160);
            SetPropertyBackingField(snapshot, "EncoderFrameRate", 59.94);
            SetPropertyBackingField(snapshot, "EncoderTargetBitRate", 50_000_000u);
            SetPropertyBackingField(snapshot, "AvSyncEncoderDriftMs", (double?)2.25d);
            SetPropertyBackingField(snapshot, "AvSyncEncoderCorrectionSamples", (long?)7L);
            SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", string.Empty);

            return buildDockPresentation.Invoke(null, new[] { snapshot })
                ?? throw new InvalidOperationException("BuildDockPresentation returned null.");
        }

        var hevc = Build("hevc_nvenc");
        Assert.True(GetBoolProperty(hevc, "EncoderActive"));
        Assert.Equal("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"));
        Assert.Equal("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"));
        Assert.Equal("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"));
        Assert.Equal("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"));
        Assert.True(GetBoolProperty(hevc, "EncoderDriftVisible"));
        Assert.Equal("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"));

        var av1 = Build("av1_nvenc");
        Assert.Equal("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"));

        var passthrough = Build("software_custom");
        Assert.Equal("software_custom", GetStringProperty(passthrough, "EncoderCodec"));

        var inactive = Build(null);
        Assert.False(GetBoolProperty(inactive, "EncoderActive"));
        Assert.Equal(string.Empty, GetStringProperty(inactive, "EncoderCodec"));

        var idleDrift = Build("h264_nvenc", recording: false);
        Assert.False(GetBoolProperty(idleDrift, "EncoderDriftVisible"));
        Assert.Equal(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"));
    }

    [Fact]
    public void WindowPresentation_FormatsDetachedWindowText()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildWindowPresentation = builderType.GetMethod("BuildStatsWindowPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "Recording", false);
        SetPropertyBackingField(snapshot, "DiagnosticHealthStatus", "Healthy");
        SetPropertyBackingField(snapshot, "DiagnosticLikelyStage", "none");
        SetPropertyBackingField(snapshot, "DiagnosticEvidence", string.Empty);
        SetPropertyBackingField(snapshot, "DiagnosticSummary", "All monitored frame lanes are within current thresholds.");
        SetPropertyBackingField(snapshot, "SourceWidth", (int?)3840);
        SetPropertyBackingField(snapshot, "SourceHeight", (int?)2160);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)119.88d);
        SetPropertyBackingField(snapshot, "SourceIsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "SourceColorimetry", "BT.2020");
        SetPropertyBackingField(snapshot, "SourceVideoFormat", "YCbCr422");
        SetPropertyBackingField(snapshot, "TelemetryOrigin", "NativeXu");
        SetPropertyBackingField(snapshot, "TelemetryConfidence", "High");
        SetPropertyBackingField(snapshot, "SourceObservedFps", 119.8d);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 120d);
        SetPropertyBackingField(snapshot, "SourceAvgIntervalMs", 8.333d);
        SetPropertyBackingField(snapshot, "SourceP95IntervalMs", 8.75d);
        SetPropertyBackingField(snapshot, "SourceJitterMs", 0.125d);
        SetPropertyBackingField(snapshot, "SourceSevereGaps", 2L);
        SetPropertyBackingField(snapshot, "SourceEstDrops", 3L);
        SetPropertyBackingField(snapshot, "SourceEstDropPct", 0.25d);
        SetPropertyBackingField(snapshot, "PreviewObservedFps", 118.2d);
        SetPropertyBackingField(snapshot, "PreviewAvgIntervalMs", 8.44d);
        SetPropertyBackingField(snapshot, "PreviewP95IntervalMs", 9.1d);
        SetPropertyBackingField(snapshot, "PreviewSlowFrames", 4L);
        SetPropertyBackingField(snapshot, "PreviewSlowPct", 1.5d);
        SetPropertyBackingField(snapshot, "PipelineLatencyMs", 3.4d);
        SetPropertyBackingField(snapshot, "SourceFramesDelivered", 500L);
        SetPropertyBackingField(snapshot, "SourceFramesDropped", 5L);
        SetPropertyBackingField(snapshot, "RendererFramesRendered", 490L);
        SetPropertyBackingField(snapshot, "RendererFramesDropped", 6L);
        SetPropertyBackingField(snapshot, "PerformanceScore", 98.75d);

        var presentation = buildWindowPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation returned null.");

        Assert.Equal("Previewing", GetStringProperty(presentation, "SessionState"));
        Assert.Equal("Healthy", GetStringProperty(presentation, "DiagnosticStatus"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(presentation, "DiagnosticEvidence"));
        Assert.Equal("3840 x 2160", GetStringProperty(presentation, "SourceResolution"));
        Assert.Equal("119.88 fps", GetStringProperty(presentation, "SourceFrameRate"));
        Assert.Equal("On (BT.2020)", GetStringProperty(presentation, "SourceHdr"));
        Assert.Equal("YCbCr422", GetStringProperty(presentation, "SourceFormat"));
        Assert.Equal("NativeXu (High)", GetStringProperty(presentation, "TelemetryOrigin"));
        Assert.Equal("119.80", GetStringProperty(presentation, "SourceFps"));
        Assert.Equal("8.33ms avg", GetStringProperty(presentation, "SourceAvg"));
        Assert.Equal("3 drops (0.3%)", GetStringProperty(presentation, "SourceDrops"));
        Assert.Equal("4 frames (1.5%)", GetStringProperty(presentation, "PreviewSlow"));
        Assert.Equal("3.40ms avg", GetStringProperty(presentation, "PipelineLatency"));
        Assert.Equal("98.8 / 100", GetStringProperty(presentation, "PerformanceScore"));

        var telemetryDetails = GetPropertyValue(presentation, "TelemetryDetails")
            ?? throw new InvalidOperationException("StatsWindowPresentation.TelemetryDetails was null.");
        Assert.True(GetBoolProperty(telemetryDetails, "IsEmpty"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(telemetryDetails, "EmptyText"));
    }

    [Fact]
    public void VisualPresentation_TreatsExpectedDisplayRepeatAsGood()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 60d);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)60d);
        SetPropertyBackingField(snapshot, "VisualCadenceSamples", 120);
        SetPropertyBackingField(snapshot, "VisualCadenceOutputFps", 120d);
        SetPropertyBackingField(snapshot, "VisualCadenceChangeFps", 60d);
        SetPropertyBackingField(snapshot, "VisualCadenceRepeatPercent", 50d);
        SetPropertyBackingField(snapshot, "VisualCadenceLongestRepeatRun", 1L);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionScore", 12.5d);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", "High");

        var presentation = buildDockPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildDockPresentation returned null.");

        Assert.Equal("120 Hz", GetStringProperty(presentation, "SummaryVisualFps"));
        Assert.Equal("crop 120 Hz", GetStringProperty(presentation, "VisualFps"));
        Assert.Equal("12.5% px / High", GetStringProperty(presentation, "VisualMotion"));
        Assert.Equal("Good", GetPropertyValue(presentation, "SummaryVisualFpsStatus")?.ToString());
        Assert.Equal("Good", GetPropertyValue(presentation, "VisualFpsStatus")?.ToString());
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)(GetPropertyValue(instance, propertyName)
                  ?? throw new InvalidOperationException($"{propertyName} was not a bool."));
}
