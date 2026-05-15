using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainWindowFlashbackToggle_RollsBackUiStateOnFailure()
    {
        var flashbackWindowText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineText = ReadRepoFile("Sussudio/MainWindow.FlashbackTimeline.cs")
            .Replace("\r\n", "\n");
        var flashbackSettingsText = ReadRepoFile("Sussudio/MainWindow.FlashbackSettingsBindings.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineControllerText = ReadRepoFile("Sussudio/Controllers/FlashbackTimelineController.cs")
            .Replace("\r\n", "\n");
        var flashbackSettingsControllerText = ReadRepoFile("Sussudio/Controllers/FlashbackSettingsBindingController.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs")
            .Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
        AssertContains(mainWindowText, "InitializeFlashbackTimelineController();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertContains(viewModelText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(viewModelText, "IsFlashbackTimelineVisible = false;");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(flashbackSettingsText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(flashbackSettingsText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(flashbackSettingsControllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackSettingsControllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsFlashbackEnabled):\n                HandleFlashbackEnabledChanged();");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):\n                HandleFlashbackTimelineVisibleChanged();");
        AssertContains(flashbackPropertyChangedText, "ApplyFlashbackTimelineLockout();");
        AssertContains(flashbackPropertyChangedText, "ApplyFlashbackTimelineVisibility(ViewModel.IsFlashbackTimelineVisible);");
        AssertContains(flashbackTimelineText, "private FlashbackTimelineController _flashbackTimelineController = null!;");
        AssertContains(flashbackTimelineText, "FlashbackToggle = FlashbackToggle,");
        AssertContains(flashbackTimelineText, "FlashbackTimelinePanel = FlashbackTimelinePanel,");
        AssertContains(flashbackTimelineText, "SnapPlayheadOnNextOpen = () => _snapFlashbackPlayheadOnNextUpdate = true,");
        AssertContains(flashbackTimelineText, "ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.OnToggleChecked();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ApplyLockout();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ResetAnimationForFullScreen();");
        AssertContains(flashbackTimelineText, "_isFlashbackScrubbing = false;");
        AssertContains(flashbackTimelineControllerText, "internal sealed class FlashbackTimelineController");
        AssertContains(flashbackTimelineControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(flashbackTimelineControllerText, "private bool _suppressToggle;");
        AssertContains(flashbackTimelineControllerText, "if (!_context.ViewModel.IsFlashbackEnabled)\n        {\n            ApplyLockout();\n            return;\n        }");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = true;");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = false;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackToggle.IsEnabled = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "SyncToggle(isVisible: false);");
        AssertContains(flashbackTimelineControllerText, "_context.ClearScrubInteraction();");
        AssertContains(flashbackTimelineControllerText, "CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "_timelineStoryboard?.Stop();");
        AssertContains(flashbackWindowText, "if (_suppressFlashbackEnabledToggle)");
        AssertContains(flashbackWindowText, "var requestedEnabled = FlashbackEnabledToggle.IsOn;");
        AssertContains(flashbackWindowText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");
        AssertContains(flashbackWindowText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(flashbackWindowText, "var previousEnabled = ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackWindowText, "ViewModel.IsFlashbackEnabled = requestedEnabled;");
        AssertContains(flashbackWindowText, "ViewModel.IsFlashbackEnabled = previousEnabled;");
        AssertContains(flashbackWindowText, "_suppressFlashbackEnabledToggle = true;");
        AssertContains(flashbackWindowText, "FlashbackEnabledToggle.IsOn = previousEnabled;");
        AssertContains(flashbackWindowText, "_suppressFlashbackEnabledToggle = false;");

        return Task.CompletedTask;
    }
}
