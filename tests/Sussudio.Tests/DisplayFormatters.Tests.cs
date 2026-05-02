using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates()
    {
        var formatterType = RequireType("Sussudio.DisplayFormatters");
        var formatSourceHdr = formatterType.GetMethod(
            "FormatSourceHdr",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool?), typeof(string) },
            modifiers: null);
        AssertNotNull(formatSourceHdr, "DisplayFormatters.FormatSourceHdr(bool?, string?)");

        AssertEqual(
            "On (BT.2020)",
            formatSourceHdr!.Invoke(null, new object?[] { true, "BT.2020" }),
            "FormatSourceHdr(true, colorimetry)");
        AssertEqual(
            "On",
            formatSourceHdr.Invoke(null, new object?[] { true, "   " }),
            "FormatSourceHdr(true, whitespace colorimetry)");
        AssertEqual(
            "On",
            formatSourceHdr.Invoke(null, new object?[] { true, null }),
            "FormatSourceHdr(true, null colorimetry)");
        AssertEqual(
            "Off",
            formatSourceHdr.Invoke(null, new object?[] { false, "BT.709" }),
            "FormatSourceHdr(false, colorimetry)");
        AssertEqual(
            "\u2014",
            formatSourceHdr.Invoke(null, new object?[] { null, "BT.2020" }),
            "FormatSourceHdr(null, colorimetry)");

        return Task.CompletedTask;
    }
}
