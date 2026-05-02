using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingStats_ComputesTotalsAndPreservesEstimateFlag()
    {
        var statsType = RequireType("Sussudio.Models.RecordingStats");
        AssertEqual(true, statsType.IsValueType, "RecordingStats value type");
        AssertEqual(true, statsType.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), inherit: false), "RecordingStats readonly metadata");

        foreach (var propertyName in new[] { "VideoBytes", "AudioBytes", "TotalBytes", "IsFlashbackEstimate" })
        {
            var property = statsType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"RecordingStats.{propertyName} was not found.");
            if (property.SetMethod != null)
            {
                throw new InvalidOperationException($"RecordingStats.{propertyName} should be get-only.");
            }
        }

        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool) })
            ?? throw new InvalidOperationException("RecordingStats(long, long, bool) constructor was not found.");
        var finalStats = ctor.Invoke(new object[] { 123L, 456L, false });
        AssertEqual(123L, GetLongProperty(finalStats, "VideoBytes"), "RecordingStats.VideoBytes");
        AssertEqual(456L, GetLongProperty(finalStats, "AudioBytes"), "RecordingStats.AudioBytes");
        AssertEqual(579L, GetLongProperty(finalStats, "TotalBytes"), "RecordingStats.TotalBytes");
        AssertEqual(false, GetBoolProperty(finalStats, "IsFlashbackEstimate"), "RecordingStats.IsFlashbackEstimate default");

        var flashbackStats = ctor.Invoke(new object[] { 10L, 5L, true });
        AssertEqual(15L, GetLongProperty(flashbackStats, "TotalBytes"), "RecordingStats flashback TotalBytes");
        AssertEqual(true, GetBoolProperty(flashbackStats, "IsFlashbackEstimate"), "RecordingStats flashback estimate flag");

        var negativeCorrection = ctor.Invoke(new object[] { 100L, -20L, false });
        AssertEqual(80L, GetLongProperty(negativeCorrection, "TotalBytes"), "RecordingStats signed byte correction");

        return Task.CompletedTask;
    }
}
