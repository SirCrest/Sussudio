using System.Threading.Tasks;

static partial class Program
{
    private static Task SettingsShelfLifecycle_LivesInController()
    {
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var fullScreenText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var settingsShelfText = ReadRepoFile("Sussudio/MainWindow.SettingsShelf.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/SettingsShelfController.cs").Replace("\r\n", "\n");

        AssertContains(settingsShelfText, "private SettingsShelfController _settingsShelfController = null!;");
        AssertContains(settingsShelfText, "private void InitializeSettingsShelfController()");
        AssertContains(settingsShelfText, "=> _settingsShelfController.Toggle();");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ApplyVisibility(visible);");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ResetAnimationState();");
        AssertContains(mainWindowText, "InitializeSettingsShelfController();");
        AssertContains(fullScreenText, "ResetSettingsShelfAnimation = ResetSettingsShelfAnimationForFullScreen,");
        AssertContains(controllerText, "internal sealed class SettingsShelfController");
        AssertContains(controllerText, "private bool _isAnimating;");
        AssertContains(controllerText, "public bool IsAnimating => _isAnimating;");
        AssertContains(controllerText, "public void Toggle()");
        AssertContains(controllerText, "public void ApplyVisibility(bool visible)");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.UpdateLayout();");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;");
        AssertDoesNotContain(mainWindowText, "private bool _isSettingsShelfAnimating;");
        AssertDoesNotContain(eventHandlersText, "private void SettingsToggleButton_Click(");

        return Task.CompletedTask;
    }

    private static Task MainWindowTitlePresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var titleText = ReadRepoFile("Sussudio/MainWindow.WindowTitle.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/WindowTitleController.cs").Replace("\r\n", "\n");

        AssertContains(titleText, "private WindowTitleController _windowTitleController = null!;");
        AssertContains(titleText, "private void InitializeWindowTitleController()");
        AssertContains(titleText, "private void ApplyWindowTitle()");
        AssertContains(titleText, "=> _windowTitleController = new WindowTitleController();");
        AssertContains(titleText, "=> Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);");
        AssertContains(controllerText, "internal sealed class WindowTitleController");
        AssertContains(controllerText, "private const string DefaultTitle = \"Simple Sussudio\";");
        AssertContains(controllerText, "public string BuildTitle(bool isRecording, string recordingTime)");
        AssertContains(controllerText, "internal static string BuildWindowTitleBase()");
        AssertContains(controllerText, "Environment.ProcessPath");
        AssertContains(controllerText, "File.GetLastWriteTime(exePath)");
        AssertContains(controllerText, "internal static string FormatBuildTitle(DateTime buildTime)");
        AssertContains(controllerText, "CultureInfo.InvariantCulture");
        AssertContains(controllerText, "internal static string FormatTitle(string baseTitle, bool isRecording, string recordingTime)");
        AssertContains(controllerText, "=> isRecording ? $\"{baseTitle} - REC {recordingTime}\" : baseTitle;");
        AssertContains(mainWindowText, "InitializeWindowTitleController();");
        AssertContains(mainWindowText, "ApplyWindowTitle();");
        AssertContains(propertyChangedText, "ApplyWindowTitle();");
        AssertDoesNotContain(mainWindowText, "private static string BuildWindowTitleBase()");
        AssertDoesNotContain(mainWindowText, "private void ApplyWindowTitle()");
        AssertDoesNotContain(mainWindowText, "CultureInfo.InvariantCulture");
        AssertDoesNotContain(titleText, "Environment.ProcessPath");
        AssertDoesNotContain(titleText, "File.GetLastWriteTime(");
        AssertDoesNotContain(titleText, "CultureInfo.InvariantCulture");

        return Task.CompletedTask;
    }
}
