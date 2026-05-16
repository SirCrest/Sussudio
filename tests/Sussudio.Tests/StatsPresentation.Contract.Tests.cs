using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task StatsVisualPresentation_TreatsExpectedDisplayRepeatAsGood()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", BindingFlags.Static | BindingFlags.Public)
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

        AssertEqual("120 Hz", GetStringProperty(presentation, "SummaryVisualFps"), "expected display-repeat visual summary");
        AssertEqual("crop 120 Hz", GetStringProperty(presentation, "VisualFps"), "expected display-repeat visual detail");
        AssertEqual("12.5% px / High", GetStringProperty(presentation, "VisualMotion"), "expected display-repeat visual motion");
        AssertEqual("Good", GetPropertyValue(presentation, "SummaryVisualFpsStatus")?.ToString(), "expected display-repeat summary status");
        AssertEqual("Good", GetPropertyValue(presentation, "VisualFpsStatus")?.ToString(), "expected display-repeat visual status");

        return Task.CompletedTask;
    }
}
