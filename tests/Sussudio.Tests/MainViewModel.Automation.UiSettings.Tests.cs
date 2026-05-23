using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs").Replace("\r\n", "\n");
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModelSettingsPersistenceProjection.cs").Replace("\r\n", "\n");

        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(settingsProjectionText, "PreviewVolume = input.PreviewVolume,");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertContains(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationUiText, "public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationStatsUi.cs")),
            "MainViewModel stats UI automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationUi.cs")),
            "MainViewModel.AutomationUi.cs folded into MainViewModel.cs");
        return Task.CompletedTask;
    }

    internal static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPersistenceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs").Replace("\r\n", "\n");
        var settingsLoadApplicationText = settingsPersistenceText;
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModelSettingsPersistenceProjection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/SettingsService.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertContains(settingsPersistenceText, "private void LoadSettings()");
        AssertContains(settingsPersistenceText, "private void SaveSettings()");
        AssertContains(settingsPersistenceText, "SettingsService.Load()");
        AssertContains(settingsPersistenceText, "SettingsService.Save(settings)");
        AssertContains(settingsPersistenceText, "Directory.Exists");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = true;");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = false;");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildLoadPlan(");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildSaveSettings(");
        AssertContains(settingsPersistenceText, "private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);", "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);", "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);", "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);", "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);", "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertContains(settingsLoadApplicationText, "private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "_pendingSavedDeviceId = loadPlan.PendingDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;");
        foreach (var removedFile in new[]
        {
            "MainViewModel.SettingsLoadApplication.cs",
            "MainViewModel.SettingsLoadApplication.Recording.cs",
            "MainViewModel.SettingsLoadApplication.Audio.cs",
            "MainViewModel.SettingsLoadApplication.Ui.cs",
            "MainViewModel.SettingsLoadApplication.DeviceAudio.cs",
            "MainViewModel.SettingsLoadApplication.Flashback.cs",
            "MainViewModel.SettingsLoadApplication.PendingDevices.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedFile)),
                $"{removedFile} folded into MainViewModel.SettingsPersistence.cs");
        }
        AssertContains(settingsProjectionText, "internal static class MainViewModelSettingsPersistenceProjection");
        AssertContains(settingsProjectionText, "internal static MainViewModelSettingsLoadPlan BuildLoadPlan(");
        AssertContains(settingsProjectionText, "internal static UserSettings BuildSaveSettings(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadInput(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadPlan(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsSaveInput(");
        foreach (var removedProjectionFile in new[]
        {
            "MainViewModelSettingsPersistenceProjection.Load.cs",
            "MainViewModelSettingsPersistenceProjection.Save.cs",
            "MainViewModelSettingsPersistenceProjection.Models.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedProjectionFile)),
                $"{removedProjectionFile} folded into MainViewModelSettingsPersistenceProjection.cs");
        }
        AssertDoesNotContain(settingsProjectionText, "SettingsService");
        AssertDoesNotContain(settingsProjectionText, "Logger");
        AssertDoesNotContain(settingsProjectionText, "Directory.");
        AssertDoesNotContain(settingsProjectionText, "MainViewModel.");
        AssertContains(settingsProjectionText, "IsStatsVisible: settings.IsStatsVisible,");
        AssertContains(settingsProjectionText, "IsStatsVisible = input.IsStatsVisible,");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0)");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)");
        AssertContains(settingsProjectionText, "ResolveAvailableValue(");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.IsStatsVisible.HasValue)");
        AssertContains(settingsPersistenceText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Settings.cs")),
            "old settings pass-through partial removed");
        AssertDoesNotContain(settingsPersistenceText, "RebuildResolutionOptions();\n        SaveSettings();");

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics()
    {
        var settings = CreateSettings(
            ("SelectedDeviceId", "device-1"),
            ("OutputPath", "C:\\Rejected"),
            ("SelectedRecordingFormat", "AV1"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P7"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("CustomBitrateMbps", 42d),
            ("IsHdrEnabled", true),
            ("IsAudioEnabled", false),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", true),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("IsMicrophoneEnabled", true),
            ("SelectedMicrophoneDeviceId", "mic-1"),
            ("MicrophoneVolume", 150d),
            ("PreviewVolume", -0.25d),
            ("IsStatsVisible", false),
            ("SelectedDeviceAudioMode", "Analog"),
            ("AnalogAudioGainPercent", -5d),
            ("FlashbackGpuDecode", true),
            ("FlashbackBufferMinutes", 99));

        var plan = BuildSettingsLoadPlan(
            settings,
            availableRecordingFormats: new[] { "H264", "HEVC" },
            outputDirectoryExists: path => path == "C:\\Accepted");

        AssertEqual(null, GetPropertyValue(plan, "OutputPath"), "settings load invalid output path");
        AssertEqual(null, GetPropertyValue(plan, "SelectedRecordingFormat"), "settings load unavailable recording format");
        AssertEqual("AV1", GetPropertyValue(plan, "UnavailableRecordingFormat"), "settings load unavailable recording format marker");
        AssertEqual("High", GetPropertyValue(plan, "SelectedQuality"), "settings load selected quality");
        AssertEqual("P7", GetPropertyValue(plan, "SelectedPreset"), "settings load selected preset");
        AssertEqual("Auto", GetPropertyValue(plan, "SelectedSplitEncodeMode"), "settings load selected split encode mode");
        AssertEqual(42d, GetPropertyValue(plan, "CustomBitrateMbps"), "settings load custom bitrate");
        AssertEqual(true, GetPropertyValue(plan, "IsHdrEnabled"), "settings load hdr enabled");
        AssertEqual(false, GetPropertyValue(plan, "IsAudioEnabled"), "settings load audio enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsAudioPreviewEnabled"), "settings load audio preview enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsCustomAudioInputEnabled"), "settings load custom audio input enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsMicrophoneEnabled"), "settings load microphone enabled");
        AssertEqual(100d, GetPropertyValue(plan, "MicrophoneVolume"), "settings load microphone volume clamp");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneVolumeDeviceId"), "settings load microphone volume device");
        AssertEqual(0d, GetPropertyValue(plan, "PreviewVolume"), "settings load preview volume clamp");
        AssertEqual(false, GetPropertyValue(plan, "IsStatsVisible"), "settings load stats visible");
        AssertEqual("Analog", GetPropertyValue(plan, "SelectedDeviceAudioMode"), "settings load selected device audio mode");
        AssertEqual(0d, GetPropertyValue(plan, "AnalogAudioGainPercent"), "settings load analog gain clamp");
        AssertEqual(true, GetPropertyValue(plan, "FlashbackGpuDecode"), "settings load flashback gpu decode");
        AssertEqual(30, GetPropertyValue(plan, "FlashbackBufferMinutes"), "settings load flashback buffer clamp");
        AssertEqual("device-1", GetPropertyValue(plan, "PendingDeviceId"), "settings load pending device");
        AssertEqual("audio-1", GetPropertyValue(plan, "PendingAudioDeviceId"), "settings load pending audio device");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneDeviceId"), "settings load pending microphone device");
        AssertEqual("Analog", GetPropertyValue(plan, "PendingDeviceAudioMode"), "settings load pending audio mode");
        AssertEqual(-5d, GetPropertyValue(plan, "PendingAnalogAudioGainPercent"), "settings load pending analog gain");

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_SaveSettingsMapsPersistedValues()
    {
        var settings = BuildSettingsSaveSettings(
            selectedDeviceId: "device-2",
            outputPath: "C:\\Capture",
            selectedRecordingFormat: "HEVC",
            selectedQuality: "Balanced",
            selectedPreset: "P5",
            selectedSplitEncodeMode: "Disabled",
            customBitrateMbps: 55d,
            isHdrEnabled: true,
            isAudioEnabled: true,
            isAudioPreviewEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-2",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-2",
            microphoneVolume: 75d,
            previewVolume: 0.625d,
            isStatsVisible: true,
            selectedDeviceAudioMode: "Embedded",
            analogAudioGainPercent: 33d,
            flashbackGpuDecode: false,
            flashbackBufferMinutes: 12);

        AssertEqual("device-2", GetPropertyValue(settings, "SelectedDeviceId"), "settings save selected device");
        AssertEqual("C:\\Capture", GetPropertyValue(settings, "OutputPath"), "settings save output path");
        AssertEqual("HEVC", GetPropertyValue(settings, "SelectedRecordingFormat"), "settings save recording format");
        AssertEqual("Balanced", GetPropertyValue(settings, "SelectedQuality"), "settings save quality");
        AssertEqual("P5", GetPropertyValue(settings, "SelectedPreset"), "settings save preset");
        AssertEqual("Disabled", GetPropertyValue(settings, "SelectedSplitEncodeMode"), "settings save split encode mode");
        AssertEqual(55d, GetPropertyValue(settings, "CustomBitrateMbps"), "settings save custom bitrate");
        AssertEqual(true, GetPropertyValue(settings, "IsHdrEnabled"), "settings save hdr enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsAudioEnabled"), "settings save audio enabled");
        AssertEqual(false, GetPropertyValue(settings, "IsAudioPreviewEnabled"), "settings save audio preview enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsCustomAudioInputEnabled"), "settings save custom audio input enabled");
        AssertEqual("audio-2", GetPropertyValue(settings, "SelectedAudioInputDeviceId"), "settings save selected audio input");
        AssertEqual(true, GetPropertyValue(settings, "IsMicrophoneEnabled"), "settings save microphone enabled");
        AssertEqual("mic-2", GetPropertyValue(settings, "SelectedMicrophoneDeviceId"), "settings save selected microphone");
        AssertEqual(75d, GetPropertyValue(settings, "MicrophoneVolume"), "settings save microphone volume");
        AssertEqual(0.625d, GetPropertyValue(settings, "PreviewVolume"), "settings save preview volume");
        AssertEqual(true, GetPropertyValue(settings, "IsStatsVisible"), "settings save stats visible");
        AssertEqual("Embedded", GetPropertyValue(settings, "SelectedDeviceAudioMode"), "settings save selected device audio mode");
        AssertEqual(33d, GetPropertyValue(settings, "AnalogAudioGainPercent"), "settings save analog gain");
        AssertEqual(false, GetPropertyValue(settings, "FlashbackGpuDecode"), "settings save flashback gpu decode");
        AssertEqual(12, GetPropertyValue(settings, "FlashbackBufferMinutes"), "settings save flashback buffer minutes");

        return Task.CompletedTask;
    }

    private static object CreateSettings(params (string Property, object? Value)[] values)
    {
        var settings = CreateInstance("Sussudio.Services.Runtime.UserSettings");
        foreach (var (property, value) in values)
        {
            SetPropertyOrBackingField(settings, property, value);
        }

        return settings;
    }

    private static object BuildSettingsLoadPlan(
        object settings,
        string[] availableRecordingFormats,
        Func<string, bool> outputDirectoryExists)
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsLoadInput");
        var input = InvokeSingleConstructor(inputType,
            availableRecordingFormats,
            new[] { "High", "Balanced" },
            new[] { "P7", "P5" },
            new[] { "Auto", "Disabled" },
            new[] { "Embedded", "Analog" },
            outputDirectoryExists);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildLoadPlan = projectionType.GetMethod(
            "BuildLoadPlan",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildLoadPlan was not found.");

        return buildLoadPlan.Invoke(null, new[] { settings, input })
               ?? throw new InvalidOperationException("BuildLoadPlan returned null.");
    }

    private static object BuildSettingsSaveSettings(
        string? selectedDeviceId,
        string outputPath,
        string selectedRecordingFormat,
        string selectedQuality,
        string selectedPreset,
        string selectedSplitEncodeMode,
        double customBitrateMbps,
        bool isHdrEnabled,
        bool isAudioEnabled,
        bool isAudioPreviewEnabled,
        bool isCustomAudioInputEnabled,
        string? selectedAudioInputDeviceId,
        bool isMicrophoneEnabled,
        string? selectedMicrophoneDeviceId,
        double microphoneVolume,
        double previewVolume,
        bool isStatsVisible,
        string selectedDeviceAudioMode,
        double analogAudioGainPercent,
        bool flashbackGpuDecode,
        int flashbackBufferMinutes)
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsSaveInput");
        var input = InvokeSingleConstructor(inputType,
            selectedDeviceId,
            outputPath,
            selectedRecordingFormat,
            selectedQuality,
            selectedPreset,
            selectedSplitEncodeMode,
            customBitrateMbps,
            isHdrEnabled,
            isAudioEnabled,
            isAudioPreviewEnabled,
            isCustomAudioInputEnabled,
            selectedAudioInputDeviceId,
            isMicrophoneEnabled,
            selectedMicrophoneDeviceId,
            microphoneVolume,
            previewVolume,
            isStatsVisible,
            selectedDeviceAudioMode,
            analogAudioGainPercent,
            flashbackGpuDecode,
            flashbackBufferMinutes);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildSaveSettings = projectionType.GetMethod(
            "BuildSaveSettings",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSaveSettings was not found.");

        return buildSaveSettings.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("BuildSaveSettings returned null.");
    }

    private static object InvokeSingleConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);

        return constructor.Invoke(arguments);
    }
}
