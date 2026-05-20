using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task BitrateSampleWindow_PreservesBoundedAverageBehavior()
    {
        var windowType = RequireType("Sussudio.ViewModels.BitrateSampleWindow");
        var window = Activator.CreateInstance(
                         windowType,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                         binder: null,
                         args: new object[] { 10_000L },
                         culture: null)
                     ?? throw new InvalidOperationException("BitrateSampleWindow instance could not be created.");
        var sampleMethod = windowType.GetMethod("AddSampleAndCompute", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.AddSampleAndCompute was not found.");
        var clearMethod = windowType.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.Clear was not found.");

        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 0L, 100L }), "first sample bitrate");
        AssertNearlyEqual(
            8000.0,
            (double)sampleMethod.Invoke(window, new object[] { 1000L, 1100L })!,
            0.0001,
            "two sample bitrate");
        AssertNearlyEqual(
            4000.0,
            (double)sampleMethod.Invoke(window, new object[] { 11_000L, 6100L })!,
            0.0001,
            "trimmed sample bitrate");

        clearMethod.Invoke(window, null);
        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 12_000L, 6100L }), "cleared sample bitrate");

        return Task.CompletedTask;
    }
}
