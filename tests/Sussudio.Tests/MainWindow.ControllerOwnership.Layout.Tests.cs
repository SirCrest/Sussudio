using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ResponsiveShellLayout.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ResponsiveShellLayoutController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/ResponsiveShellLayoutPolicy.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "=> _responsiveShellLayoutController.Attach();");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(policyText, "internal static class ResponsiveShellLayoutPolicy");
        AssertContains(policyText, "public const double ControlBarLabelThreshold = 900.0;");
        AssertContains(policyText, "public const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(policyText, "internal readonly record struct ResponsiveCaptureSettingsPlacement");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);");
        AssertContains(controllerText, "private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)");
        AssertContains(controllerText, "private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertDoesNotContain(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertDoesNotContain(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");

        return Task.CompletedTask;
    }

    private static Task ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements()
    {
        var policyType = RequireType("Sussudio.Controllers.ResponsiveShellLayoutPolicy");
        var shouldShowLabels = policyType.GetMethod(
            "ShouldShowControlBarLabels",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels not found.");
        var getLayoutKind = policyType.GetMethod(
            "GetCaptureSettingsLayoutKind",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind not found.");
        var getPlacement = policyType.GetMethod(
            "GetCaptureSettingsPlacement",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsPlacement not found.");

        AssertEqual(false, (bool)shouldShowLabels.Invoke(null, new object[] { 899.99 })!, "control bar labels below 900");
        AssertEqual(true, (bool)shouldShowLabels.Invoke(null, new object[] { 900.0 })!, "control bar labels at 900");

        var narrowKind = getLayoutKind.Invoke(null, new object[] { 699.99 })
            ?? throw new InvalidOperationException("Narrow responsive shell layout kind was null.");
        var wideKind = getLayoutKind.Invoke(null, new object[] { 700.0 })
            ?? throw new InvalidOperationException("Wide responsive shell layout kind was null.");
        AssertEqual("Narrow", narrowKind.ToString()!, "capture settings below 700");
        AssertEqual("Wide", wideKind.ToString()!, "capture settings at 700");

        var narrowPlacement = getPlacement.Invoke(null, new[] { narrowKind })
            ?? throw new InvalidOperationException("Narrow responsive shell placement was null.");
        AssertEqual(true, GetBoolProperty(narrowPlacement, "CollapseCaptureOptionColumns"), "narrow columns collapse");
        AssertGridSlot(narrowPlacement, "VideoFormat", 1, 1);
        AssertGridSlot(narrowPlacement, "Preset", 1, 2);
        AssertGridSlot(narrowPlacement, "Split", 1, 3);
        AssertGridSlot(narrowPlacement, "CustomBitrate", 1, 2);

        var widePlacement = getPlacement.Invoke(null, new[] { wideKind })
            ?? throw new InvalidOperationException("Wide responsive shell placement was null.");
        AssertEqual(false, GetBoolProperty(widePlacement, "CollapseCaptureOptionColumns"), "wide columns stay flexible");
        AssertGridSlot(widePlacement, "VideoFormat", 0, 0);
        AssertGridSlot(widePlacement, "Preset", 0, 5);
        AssertGridSlot(widePlacement, "Split", 0, 6);
        AssertGridSlot(widePlacement, "CustomBitrate", 0, 5);

        return Task.CompletedTask;
    }

    private static void AssertGridSlot(object placement, string propertyName, int expectedRow, int expectedColumn)
    {
        var slot = GetPropertyValue(placement, propertyName)
            ?? throw new InvalidOperationException($"Responsive grid slot '{propertyName}' was null.");
        AssertEqual(expectedRow, GetIntProperty(slot, "Row"), $"{propertyName} row");
        AssertEqual(expectedColumn, GetIntProperty(slot, "Column"), $"{propertyName} column");
    }
}
