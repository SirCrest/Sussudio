using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationUi.cs").Replace("\r\n", "\n");
        var automationStatsUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationStatsUi.cs").Replace("\r\n", "\n");
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs").Replace("\r\n", "\n");
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModelSettingsPersistenceProjection.cs").Replace("\r\n", "\n");

        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(settingsProjectionText, "PreviewVolume = input.PreviewVolume,");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertDoesNotContain(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationStatsUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationStatsUiText, "public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationStatsUiText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationStatsUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        return Task.CompletedTask;
    }

    private static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPartialText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Settings.cs").Replace("\r\n", "\n");
        var settingsPersistenceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs").Replace("\r\n", "\n");
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModelSettingsPersistenceProjection.cs").Replace("\r\n", "\n");
        var captureOptionVisibilityText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureOptionVisibility.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/SettingsService.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? ShowAllCaptureOptions { get; set; }");
        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertContains(settingsPersistenceText, "private void LoadSettings()");
        AssertContains(settingsPersistenceText, "private void SaveSettings()");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildLoadPlan(");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildSaveSettings(");
        AssertContains(settingsPersistenceText, "private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsProjectionText, "internal static class MainViewModelSettingsPersistenceProjection");
        AssertContains(settingsProjectionText, "internal static MainViewModelSettingsLoadPlan BuildLoadPlan(");
        AssertContains(settingsProjectionText, "internal static UserSettings BuildSaveSettings(");
        AssertContains(settingsProjectionText, "ShowAllCaptureOptions: settings.ShowAllCaptureOptions,");
        AssertContains(settingsProjectionText, "IsStatsVisible: settings.IsStatsVisible,");
        AssertContains(settingsProjectionText, "ShowAllCaptureOptions = input.ShowAllCaptureOptions,");
        AssertContains(settingsProjectionText, "IsStatsVisible = input.IsStatsVisible,");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0)");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)");
        AssertContains(settingsProjectionText, "ResolveAvailableValue(");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.IsStatsVisible.HasValue)");
        AssertDoesNotContain(settingsPartialText, "private void LoadSettings()");
        AssertDoesNotContain(settingsPartialText, "private void SaveSettings()");
        AssertContains(settingsPartialText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertContains(captureOptionVisibilityText, "partial void OnShowAllCaptureOptionsChanged(bool value)");
        AssertContains(captureOptionVisibilityText, "RebuildResolutionOptions();\n        SaveSettings();");
        AssertDoesNotContain(settingsPartialText, "partial void OnShowAllCaptureOptionsChanged(bool value)");
        AssertDoesNotContain(settingsPartialText, "RebuildResolutionOptions();\n        SaveSettings();");

        return Task.CompletedTask;
    }
}
