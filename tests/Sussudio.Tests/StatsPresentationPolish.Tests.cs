using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class StatsPresentationPolishTests
{
    public StatsPresentationPolishTests()
        => global::Program.EnsureTargetAssemblyLoadedForXUnit();

    [Fact]
    public void DockMotion_RapidReversalIgnoresAnEarlierHideCompletion()
    {
        var state = Create("Sussudio.Controllers.StatsDockMotionState");
        Request(state, visible: true, animate: true);
        var hideRevision = Request(state, visible: false, animate: true);
        var showRevision = Request(state, visible: true, animate: true);

        Assert.False(Complete(state, hideRevision));
        Assert.True(Read<bool>(state, "Visible"));
        Assert.True(Read<bool>(state, "Animating"));
        Assert.True(Complete(state, showRevision));
        Assert.True(Read<bool>(state, "Visible"));
        Assert.False(Read<bool>(state, "Animating"));
    }

    [Fact]
    public void DockMotion_ImmediateHideInvalidatesPendingAnimation()
    {
        var state = Create("Sussudio.Controllers.StatsDockMotionState");
        var oldRevision = Request(state, visible: true, animate: true);
        var finalRevision = Request(state, visible: false, animate: false);

        Assert.False(Complete(state, oldRevision));
        Assert.False(Read<bool>(state, "Visible"));
        Assert.False(Read<bool>(state, "Animating"));
        Assert.True(Complete(state, finalRevision));
    }

    [Fact]
    public void DockMotion_CancellationInvalidatesQueuedCompletion()
    {
        var state = Create("Sussudio.Controllers.StatsDockMotionState");
        var revision = Request(state, visible: true, animate: true);
        state.GetType().GetMethod("Cancel")!.Invoke(state, null);

        Assert.False(Complete(state, revision));
        Assert.False(Read<bool>(state, "Animating"));
    }

    [Fact]
    public void PreviewSummary_NamesTheP99EquivalentWithoutCallingItOnePercentLow()
    {
        var snapshot = CreateSnapshot();
        Set(snapshot, "PreviewCadenceSamples", 10);
        Set(snapshot, "PreviewObservedFps", 59.98d);
        Set(snapshot, "PreviewAvgIntervalMs", 16.67d);
        Set(snapshot, "PreviewP99IntervalMs", 20d);
        Set(snapshot, "PreviewOnePercentLowFps", 50d);
        Set(snapshot, "SourceExpectedFps", 60d);

        var presentation = Build("BuildDockPresentation", snapshot);
        var text = Read<string>(presentation, "SummaryRendererFps");
        Assert.Contains("59.98 fps", text);
        Assert.Contains("P99 equivalent 50.00 fps", text);
        Assert.DoesNotContain("1% low", text);
    }

    [Fact]
    public void RecordingSummary_DistinguishesIdleFromActiveEncoding()
    {
        var snapshot = CreateSnapshot();
        Set(snapshot, "EncoderCodecName", "hevc_nvenc");
        Set(snapshot, "EncoderTargetBitRate", 25_000_000u);
        Assert.Equal("Not recording", Read<string>(Build("BuildDockPresentation", snapshot), "SummaryRecording"));

        Set(snapshot, "Recording", true);
        Assert.Equal("HEVC (NVENC) · 25 Mbps", Read<string>(Build("BuildDockPresentation", snapshot), "SummaryRecording"));
    }

    [Fact]
    public void MissingLatency_IsShownAsUnavailableRatherThanZero()
    {
        var snapshot = CreateSnapshot();
        var dock = Build("BuildDockPresentation", snapshot);
        Assert.Equal("—", Read<string>(dock, "SummaryLatency"));
        Assert.Equal("—", Read<string>(dock, "PipelineLatency"));
        Assert.Equal("—", Read<string>(Build("BuildStatsWindowPresentation", snapshot), "PipelineLatency"));
        Set(snapshot, "PreviewCadenceSamples", 60);
        Set(snapshot, "PreviewP99IntervalMs", 20d);
        Assert.Contains("estimated capture → preview —", Read<string>(Build("BuildFrameTimePresentation", snapshot), "LatencyText"));
    }

    [Fact]
    public void GraphLabels_KeepCaptureAndVisualChangeRatesSeparateFromPreviewPresents()
    {
        var snapshot = CreateSnapshot();
        Set(snapshot, "SourceCadenceSamples", 120);
        Set(snapshot, "SourceObservedFps", 119.88d);
        Set(snapshot, "SourceExpectedFps", 119.88d);
        Set(snapshot, "PreviewCadenceSamples", 60);
        Set(snapshot, "PreviewObservedFps", 59.94d);
        Set(snapshot, "VisualCadenceSamples", 120);
        Set(snapshot, "VisualCadenceChangeFps", 29.97d);

        var presentation = Build("BuildFrameTimePresentation", snapshot);
        Assert.Equal("119.88 fps", Read<string>(presentation, "SourceText"));
        Assert.Equal("59.94 fps", Read<string>(presentation, "PreviewText"));
        Assert.Equal("29.97 fps", Read<string>(presentation, "VisualText"));
    }

    [Fact]
    public void GraphFooter_NamesP99AndEstimatedLatencyInTheRenderedText()
    {
        var snapshot = CreateSnapshot();
        Set(snapshot, "PreviewCadenceSamples", 60);
        Set(snapshot, "PreviewP99IntervalMs", 20d);
        Set(snapshot, "PipelineLatencyMs", 8d);

        var footer = Read<string>(Build("BuildFrameTimePresentation", snapshot), "LatencyText");
        Assert.Contains("P99 present interval", footer);
        Assert.Contains("estimated capture → preview", footer);
        Assert.Contains("not game frame rate", footer);
    }

    private static Type Require(string name)
        => SussudioAssembly.Load().GetType(name, throwOnError: true)!;

    private static object Create(string name)
        => Activator.CreateInstance(Require(name), nonPublic: true)!;

    private static object CreateSnapshot()
        => RuntimeHelpers.GetUninitializedObject(Require("Sussudio.StatsSnapshot"));

    private static object Build(string method, object snapshot)
        => Require("Sussudio.ViewModels.StatsPresentationBuilder").GetMethod(method)!.Invoke(null, new[] { snapshot })!;

    private static long Request(object state, bool visible, bool animate)
        => (long)state.GetType().GetMethod("Request")!.Invoke(state, new object[] { visible, animate })!;

    private static bool Complete(object state, long revision)
        => (bool)state.GetType().GetMethod("TryComplete")!.Invoke(state, new object[] { revision })!;

    private static T Read<T>(object value, string name)
        => (T)value.GetType().GetProperty(name)!.GetValue(value)!;

    private static void Set(object value, string name, object fieldValue)
        => value.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(value, fieldValue);
}
