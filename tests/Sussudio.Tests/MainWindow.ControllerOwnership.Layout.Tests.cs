using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

static partial class Program
{
    internal static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ResponsiveShellLayoutController.cs").Replace("\r\n", "\n");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Bindings.cs")),
            "root startup binding sequence lives with the MainWindow composition root");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(controllerText, "internal sealed class ControlBarLabelVisibilityController");
        AssertContains(controllerText, "public required UIElement[] ControlBarLabels { get; init; }");
        AssertContains(controllerText, "internal static class ResponsiveShellLayoutPolicy");
        AssertContains(controllerText, "public const double ControlBarLabelThreshold = 900.0;");
        AssertContains(controllerText, "public const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(controllerText, "internal readonly record struct ResponsiveCaptureSettingsPlacement");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);");
        AssertContains(controllerText, "foreach (var label in _context.ControlBarLabels)");
        AssertContains(controllerText, "label.Visibility = visibility;");
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
        AssertDoesNotContain(controllerText, "_context.HdrToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(controllerText, "_context.FrameTimeOverlayToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(adapterText, "FlashbackToggleLabel = FlashbackToggleLabel,");
        AssertDoesNotContain(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertDoesNotContain(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ResponsiveShellLayout.cs")),
            "responsive shell layout adapter lives with shell chrome composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ControlBarLabelVisibilityController.cs")),
            "control-bar label visibility lives with responsive shell layout application");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ResponsiveShellLayoutPolicy.cs")),
            "responsive shell layout policy lives with responsive shell layout application");

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

namespace Sussudio.Tests
{
    public sealed class WindowSnapRegionLayoutPolicyTests
    {
        private const string PolicyTypeName = "Sussudio.Controllers.WindowSnapRegionLayoutPolicy";
        private const string ActionTypeName = "Sussudio.Models.AutomationWindowAction";

        [Theory]
        [InlineData("SnapLeft", 10, 20, 50, 55)]
        [InlineData("SnapRight", 60, 20, 51, 55)]
        [InlineData("SnapTopLeft", 10, 20, 50, 27)]
        [InlineData("SnapTopRight", 60, 20, 51, 27)]
        [InlineData("SnapBottomLeft", 10, 47, 50, 28)]
        [InlineData("SnapBottomRight", 60, 47, 51, 28)]
        [InlineData("Center", 44, 40, 33, 15)]
        public void ResolveTargetBounds_PreservesExistingSnapGeometry(string actionName, int x, int y, int width, int height)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.NotNull(result);
            AssertRect(result!, x, y, width, height);
        }

        [Theory]
        [InlineData("Restore")]
        [InlineData(null)]
        public void ResolveTargetBounds_ReturnsNullForNonSnapActions(string? actionName)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = actionName is null
                ? Enum.ToObject(actionType, 999)
                : Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.Null(result);
        }

        private static object CreateStruct(Type type, params int[] args)
            => Activator.CreateInstance(type, args.Cast<object>().ToArray())!;

        private static void AssertRect(object rect, int x, int y, int width, int height)
        {
            Assert.Equal(x, ReadIntProperty(rect, "X"));
            Assert.Equal(y, ReadIntProperty(rect, "Y"));
            Assert.Equal(width, ReadIntProperty(rect, "Width"));
            Assert.Equal(height, ReadIntProperty(rect, "Height"));
        }

        private static int ReadIntProperty(object instance, string propertyName)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
            if (property != null)
            {
                return (int)property.GetValue(instance)!;
            }

            return (int)type.GetField(propertyName, ReflectionFlags.Instance)!.GetValue(instance)!;
        }
    }
}
