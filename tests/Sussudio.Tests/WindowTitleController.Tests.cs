using System.Globalization;
using System.Reflection;

static partial class Program
{
    internal static Task WindowTitleController_FormatsBuildStampAndRecordingSuffix()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var controllerType = RequireType("Sussudio.Controllers.WindowTitleController");
            var formatBuildTitle = controllerType.GetMethod("FormatBuildTitle", BindingFlags.Static | BindingFlags.NonPublic)
                                   ?? throw new InvalidOperationException("WindowTitleController.FormatBuildTitle not found.");
            var formatTitle = controllerType.GetMethod("FormatTitle", BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new InvalidOperationException("WindowTitleController.FormatTitle not found.");

            var buildTime = new DateTime(2026, 5, 14, 22, 30, 45, DateTimeKind.Local);
            var buildTitle = formatBuildTitle.Invoke(null, new object?[] { buildTime });
            AssertEqual("Simple Sussudio (build 2026-05-14 22:30:45)", buildTitle, "invariant build title");
            AssertEqual("Simple Sussudio", formatBuildTitle.Invoke(null, new object?[] { DateTime.MinValue }), "missing build-time title");

            AssertEqual("Simple Sussudio", formatTitle.Invoke(null, new object?[] { "Simple Sussudio", false, "00:01:02" }), "idle title");
            AssertEqual(
                "Simple Sussudio - REC 00:01:02",
                formatTitle.Invoke(null, new object?[] { "Simple Sussudio", true, "00:01:02" }),
                "recording title");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.CompletedTask;
    }
}
