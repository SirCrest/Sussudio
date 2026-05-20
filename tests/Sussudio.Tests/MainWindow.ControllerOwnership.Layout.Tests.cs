using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ResponsiveShellLayout.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ResponsiveShellLayoutController.cs").Replace("\r\n", "\n");
        var labelControllerText = ReadRepoFile("Sussudio/Controllers/Shell/ControlBarLabelVisibilityController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/Shell/ResponsiveShellLayoutPolicy.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarLabelVisibilityController _controlBarLabelVisibilityController = null!;");
        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "var controlBarLabels = new UIElement[]");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "FlashbackToggleLabel,");
        AssertContains(adapterText, "_controlBarLabelVisibilityController = new ControlBarLabelVisibilityController(new ControlBarLabelVisibilityControllerContext");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "ControlBarLabels = controlBarLabels,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "_controlBarLabelVisibilityController.Attach();");
        AssertContains(adapterText, "_responsiveShellLayoutController.Attach();");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()\n    {\n        _controlBarLabelVisibilityController.Attach();\n        _responsiveShellLayoutController.Attach();\n    }");
        AssertContains(xamlText, "x:Name=\"FlashbackToggleLabel\"");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(labelControllerText, "internal sealed class ControlBarLabelVisibilityController");
        AssertContains(labelControllerText, "public required UIElement[] ControlBarLabels { get; init; }");
        AssertContains(policyText, "internal static class ResponsiveShellLayoutPolicy");
        AssertContains(policyText, "public const double ControlBarLabelThreshold = 900.0;");
        AssertContains(policyText, "public const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(policyText, "internal readonly record struct ResponsiveCaptureSettingsPlacement");
        AssertContains(labelControllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(labelControllerText, "public void Attach()");
        AssertContains(labelControllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(labelControllerText, "ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);");
        AssertContains(labelControllerText, "foreach (var label in _context.ControlBarLabels)");
        AssertContains(labelControllerText, "label.Visibility = visibility;");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);");
        AssertContains(controllerText, "private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)");
        AssertContains(controllerText, "private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)");
        AssertContains(agentMapText, "complete control-bar label set");
        AssertContains(cleanupPlanText, "complete control-bar label set");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertDoesNotContain(controllerText, "ControlBarBorder");
        AssertDoesNotContain(controllerText, "ControlBarLabels");
        AssertDoesNotContain(controllerText, "ApplyControlBarWidth");
        AssertDoesNotContain(controllerText, "_context.HdrToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(controllerText, "_context.FrameTimeOverlayToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(labelControllerText, "_context.HdrToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(labelControllerText, "_context.FrameTimeOverlayToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(adapterText, "FlashbackToggleLabel = FlashbackToggleLabel,");
        AssertDoesNotContain(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertDoesNotContain(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");

        return Task.CompletedTask;
    }

    internal static Task ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements()
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
