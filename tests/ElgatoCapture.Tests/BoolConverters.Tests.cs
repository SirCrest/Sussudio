using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task BoolConverters_PreserveInversionAndVisibilityMappings()
    {
        // Verify all three converter types exist
        var inverseBoolType = RequireType("ElgatoCapture.Converters.InverseBoolConverter");
        var boolToVisType = RequireType("ElgatoCapture.Converters.BoolToVisibilityConverter");
        var inverseBoolToVisType = RequireType("ElgatoCapture.Converters.BoolToInverseVisibilityConverter");

        // All three must expose Convert and ConvertBack (IValueConverter contract)
        AssertNotNull(inverseBoolType.GetMethod("Convert"), "InverseBoolConverter.Convert");
        AssertNotNull(inverseBoolType.GetMethod("ConvertBack"), "InverseBoolConverter.ConvertBack");
        AssertNotNull(boolToVisType.GetMethod("Convert"), "BoolToVisibilityConverter.Convert");
        AssertNotNull(boolToVisType.GetMethod("ConvertBack"), "BoolToVisibilityConverter.ConvertBack");
        AssertNotNull(inverseBoolToVisType.GetMethod("Convert"), "InverseBoolToVisibilityConverter.Convert");
        AssertNotNull(inverseBoolToVisType.GetMethod("ConvertBack"), "InverseBoolToVisibilityConverter.ConvertBack");

        // Behavioral: InverseBoolConverter negates booleans
        var inverseInstance = Activator.CreateInstance(inverseBoolType)!;
        var convert = inverseBoolType.GetMethod("Convert")!;
        var convertBack = inverseBoolType.GetMethod("ConvertBack")!;

        var trueToFalse = convert.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" });
        AssertEqual(false, (bool)trueToFalse!, "InverseBoolConverter.Convert(true) → false");

        var falseToTrue = convert.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" });
        AssertEqual(true, (bool)falseToTrue!, "InverseBoolConverter.Convert(false) → true");

        var backTrue = convertBack.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" });
        AssertEqual(false, (bool)backTrue!, "InverseBoolConverter.ConvertBack(true) → false");

        var backFalse = convertBack.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" });
        AssertEqual(true, (bool)backFalse!, "InverseBoolConverter.ConvertBack(false) → true");

        // Non-bool passthrough
        var passthrough = convert.Invoke(inverseInstance, new object?[] { "not-a-bool", typeof(bool), null, "" });
        AssertEqual("not-a-bool", passthrough?.ToString(), "InverseBoolConverter passes through non-bool");

        return Task.CompletedTask;
    }
}
